using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIPlayer : Player
    {
        public bool TurnFinished { get; private set; }

        // AI Behaviour
        private Dictionary<AICharacterRole, int> RoleTable = new Dictionary<AICharacterRole, int>()
        {
            { AICharacterRole.Attacker, 6 },
            { AICharacterRole.Defender, 2 },
        };
        private Dictionary<Character, AICharacterRole> Roles = new Dictionary<Character, AICharacterRole>();
        private Dictionary<Character, AICharacterJob> Jobs = new Dictionary<Character, AICharacterJob>();

        // Camera follow
        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other


        public AIPlayer(Actor actor, Zone jailZone, Zone flagZone) : base(actor, jailZone, flagZone) { }

        public override void OnStartGame(CTFGame game)
        {
            base.OnStartGame(game);

            // Assign a weighted-random role to all characters
            for (int i = 0; i < Characters.Count; i++)
            {
                AICharacterRole randomRole = HelperFunctions.GetWeightedRandomElement(RoleTable);
                Roles.Add(Characters[i], randomRole);
                Jobs.Add(Characters[i], new AIJob_Idle(Characters[i]));

                // Debug
                UpdateDebugLabel(Characters[i]);
            }
        }

        public void StartTurn()
        {
            TurnFinished = false;

            // Get initial action for each character
            Actions.Clear();
            ActionsToFollow.Clear();
            CurrentFollowedAction = null;
            foreach (Character c in Characters)
            {
                CharacterAction initialAction = GetNextCharacterAction(c);
                if (initialAction != null) Actions[c] = initialAction;
            }
        }

        /// <summary>
        /// Gets called every frame during the AI's turn.
        /// </summary>
        public void UpdateTurn()
        {
            // Start the first unstarted action (they are not started at the same time to avoid lag spikes)
            CharacterAction firstNonStarted = Actions.Values.FirstOrDefault(x => x.IsPending);
            if (firstNonStarted != null) firstNonStarted.Perform();

            // Check if AI turn is finished
            if (Characters.All(x => !x.IsInAction)) TurnFinished = true;

            // Check if we should queue-follow a new action
            foreach (CharacterAction action in Actions.Values)
            {
                if (action.State != CharacterActionState.Performing) continue;
                if (action == CurrentFollowedAction) continue;

                // Character is newly visible
                if (action.Character.IsVisible && !ActionsToFollow.Contains(action))
                {
                    action.PauseAction();
                    ActionsToFollow.Enqueue(action);
                }
            }

            // Update the currently followed action
            if (CurrentFollowedAction != null)
            {
                // Wait for camera
                if (World.Camera.FollowedEntity == CurrentFollowedAction.Character.Entity)
                {
                    // Unpause action if camera arrived at character
                    if (CurrentFollowedAction.IsPaused)
                    {
                        CurrentFollowedAction.UnpauseAction();
                    }

                    // Stop following if character moves out of vision or if action is done
                    else if (CurrentFollowedAction.IsDone || !CurrentFollowedAction.Character.IsVisible)
                    {
                        World.Camera.Unfollow();
                        CurrentFollowedAction = null;
                    }
                }
            }
            else if (ActionsToFollow.Count > 0) // Get next action to follow
            {
                CurrentFollowedAction = ActionsToFollow.Dequeue();

                // If character went out of vision while waiting in queue, just unpause and go next
                if(!CurrentFollowedAction.Character.IsVisible)
                {
                    CurrentFollowedAction.UnpauseAction();
                    CurrentFollowedAction = null;
                }

                // Else Pan to character
                else World.CameraPanToFocusEntity(CurrentFollowedAction.Character.Entity, duration: 1f, followAfterPan: true, unbreakableFollow: true);
            }
        }

        public override void OnActionDone(CharacterAction action)
        {
            Character character = action.Character;

            // If there are no more possible moves, take no further action
            if (character.PossibleMoves.Count == 0) return;

            // Get next action for character and immediately start it
            CharacterAction newAction = GetNextCharacterAction(character);
            if (newAction != null)
            {
                Actions[character] = newAction;
                newAction.Perform();
            }
        }

        #region Private

        /// <summary>
        /// Sets the name and visible label of a character according to its role and job to easily debug what they are doing.
        /// </summary>
        private void UpdateDebugLabel(Character c)
        {
            c.Name = Roles[c].ToString() + " | " + Jobs[c].DisplayName;
            c.UI_Label.Init(c);
        }

        /// <summary>
        /// Returns the action the given character will do next this turn.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        private CharacterAction GetNextCharacterAction(Character c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            AICharacterJob currentJob = Jobs[c];

            // Ask the current job if (any or a forced) new job should be assigned to the character
            if(currentJob.ShouldStopJob(out AICharacterJob forcedNewJob))
            {
                // Assign the job that is getting forced by the current job as the new job
                if(forcedNewJob != null)
                {
                    Jobs[c] = forcedNewJob;
                    currentJob = forcedNewJob;
                }
                // Find a new job based on general rules
                else
                {
                    AICharacterJob newJob = GetNewCharacterJob(c);
                    Jobs[c] = newJob;
                    currentJob = newJob;
                }
            }

            // Update debug label
            UpdateDebugLabel(c);

            // Get action based on job
            return currentJob.GetNextAction();
        }

        /// <summary>
        /// Returns a new job that the given character should do given their role and current game state.
        /// </summary>
        private AICharacterJob GetNewCharacterJob(Character c)
        {
            switch(Roles[c])
            {
                case AICharacterRole.Attacker:

                    // If we know where enemy flag is => move directly
                    if (Opponent.Flag.IsExploredBy(Actor))
                        return new AIJob_CaptureOpponentFlag(c);

                    // Else chose a random unexplored node in enemy territory to go to
                    else return new AIJob_SearchForOpponentFlag(c);

                case AICharacterRole.Defender:

                    // Stay idle for now
                    return new AIJob_Idle(c);
            }

            throw new System.Exception("Gamestate not handled");
        }

        /// <summary>
        /// A role is a macro-level, long-term (mostly for a full game) assignment that dictates what jobs a character can and will do.
        /// </summary>
        private enum AICharacterRole
        {
            Defender,
            Attacker
        }

        #endregion
    }
}
