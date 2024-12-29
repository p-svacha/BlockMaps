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
        private const float INVISIBLE_CHARACTER_SPEED = 50;

        private const float MAX_CHASE_DISTANCE = 40; // Transition cost
        private const float DEFEND_PERIMETER_RADIUS = 40; // Transition cost

        private int CurrentCharacterIndex; // Resets each turn - each character performs all actions before the next one starts
        private CharacterAction CurrentAction; // Actions are performed one after the other

        private Dictionary<AICharacterRole, int> RoleTable = new Dictionary<AICharacterRole, int>()
        {
            { AICharacterRole.Attacker, 6 },
            { AICharacterRole.Defender, 2 },
        };
        private Dictionary<CtfCharacter, AICharacterRole> Roles = new Dictionary<CtfCharacter, AICharacterRole>();
        private Dictionary<CtfCharacter, AICharacterJob> Jobs = new Dictionary<CtfCharacter, AICharacterJob>();

        public List<BlockmapNode> DefendPerimeterNodes;

        // Camera follow
        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other


        public AIPlayer(ClientInfo info) : base(info) { }

        public override void OnMatchReady(CtfMatch game)
        {
            base.OnMatchReady(game);

            // Assign a weighted-random role to all characters
            for (int i = 0; i < Characters.Count; i++)
            {
                AICharacterRole randomRole = HelperFunctions.GetWeightedRandomElement(RoleTable);
                Roles.Add(Characters[i], randomRole);
                Jobs.Add(Characters[i], new AIJob_Idle(Characters[i]));
            }

            // Calculate some important things once that will be used for the whole game
            DefendPerimeterNodes = Flag.OriginNode.GetNodesInRange(DEFEND_PERIMETER_RADIUS).Where(x => !FlagZone.ContainsNode(x)).ToList();
        }

        public void StartTurn()
        {
            // Reset
            TurnFinished = false;
            CurrentCharacterIndex = -1;
            CurrentAction = null;
            ActionsToFollow.Clear();
            CurrentFollowedAction = null;
        }

        /// <summary>
        /// Gets called every frame during the AI's turn.
        /// </summary>
        public void UpdateTurn()
        {
            // Get a new action if the current one is null or done.
            if(CurrentAction == null || CurrentAction.IsDone)
            {
                CtfCharacter currentCharacter = CurrentCharacterIndex > -1 ? Characters[CurrentCharacterIndex] : null;
                CharacterAction nextAction = CurrentCharacterIndex > -1 ? GetNextCharacterAction(currentCharacter) : null;

                if(nextAction == null) // Character is done for this turn => go to next character
                {
                    CurrentCharacterIndex++;

                    if (CurrentCharacterIndex == Characters.Count) // If it was last character turn is done
                    {
                        TurnFinished = true;
                    }
                    else
                    {
                        currentCharacter = Characters[CurrentCharacterIndex];
                        nextAction = GetNextCharacterAction(currentCharacter);
                    }
                }

                // Start performing next action immediately
                if (nextAction != null) 
                {
                    CurrentAction = nextAction;
                    nextAction.Perform();
                    currentCharacter.MovementComp.EnableOverrideMovementSpeed(INVISIBLE_CHARACTER_SPEED); // Speed up enemy characters so player doesn't have to wait for long
                }
            }

            // Check if we should queue-follow an action
            foreach (CtfCharacter character in Characters.Where(c => c.IsInAction))
            {
                CharacterAction action = character.CurrentAction;
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
                if (World.Camera.FollowedEntity == CurrentFollowedAction.Character)
                {
                    // Unpause action if camera arrived at character
                    if (CurrentFollowedAction.IsPaused)
                    {
                        CurrentFollowedAction.Character.MovementComp.DisableOverrideMovementSpeed();
                        CurrentFollowedAction.UnpauseAction();
                    }

                    // Stop following if character moves out of vision or if action is done
                    else if (CurrentFollowedAction.IsDone || !CurrentFollowedAction.Character.IsVisible)
                    {
                        World.Camera.Unfollow();
                        CurrentFollowedAction.Character.MovementComp.EnableOverrideMovementSpeed(INVISIBLE_CHARACTER_SPEED); // Speed up enemy characters so player doesn't have to wait for long
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
                else World.CameraPanToFocusEntity(CurrentFollowedAction.Character, duration: 1f, followAfterPan: true, unbreakableFollow: true);
            }
        }

        #region Private

        protected override void SetDevModeLabels()
        {
            foreach (CtfCharacter c in Characters)
            {
                string label = $"{ c.LabelCap } ({ c.Id}): {Roles[c]} | {Jobs[c].DevmodeDisplayText}";
                c.UI_Label.SetLabelText(label);
            }
        }

        /// <summary>
        /// Returns the action the given character will do next this turn.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        private CharacterAction GetNextCharacterAction(CtfCharacter c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            AICharacterJob currentJob = Jobs[c];

            // Ask the current job if (any or a forced) new job should be assigned to the character
            int attempts = 0;
            int maxAttempts = 10;
            AICharacterJob forcedNewJob;
            while (currentJob.ShouldStopJob(out forcedNewJob) && attempts < maxAttempts)
            {
                attempts++;
                
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
            if (currentJob.ShouldStopJob(out _) && attempts < maxAttempts) throw new System.Exception($"After {attempts} we still didn't get a job that shouldn't immediately be stopped. Current job = {currentJob.DevmodeDisplayText}");


            // Update dev mode labels
            if (Match.DevMode) SetDevModeLabels();

            // Get action based on job
            return currentJob.GetNextAction();
        }

        /// <summary>
        /// Returns a new job that the given character should do given their role and current game state.
        /// </summary>
        private AICharacterJob GetNewCharacterJob(CtfCharacter c)
        {
            // If we can directly tag an opponent, do that no matter the role
            if (CanTagCharacterDirectly(c, out CtfCharacter target0)) return new AIJob_TagOpponent(c, target0);

            switch (Roles[c])
            {
                case AICharacterRole.Attacker:

                    // If we should flee, do so
                    if (ShouldFlee(c)) return new AIJob_Flee(c);

                    // If we know where enemy flag is => move directly
                    if (Opponent.Flag.IsExploredBy(Actor)) return new AIJob_CaptureOpponentFlag(c);

                    // Else chose a random unexplored node in enemy territory to go to
                    else return new AIJob_SearchForOpponentFlag(c);

                case AICharacterRole.Defender:

                    // If there is a visible opponent nearby
                    if (ShouldChaseCharacterToTag(c, out CtfCharacter target1)) return new AIJob_TagOpponent(c, target1);

                    // Else just patrol own flag
                    return new AIJob_PatrolDefendFlag(c);
            }

            throw new System.Exception("Gamestate not handled");
        }

        /// <summary>
        /// Returns if the given character can tag an opponent with their possible moves.
        /// </summary>
        public bool CanTagCharacterDirectly(CtfCharacter source, out CtfCharacter target)
        {
            target = null;

            foreach (CtfCharacter opponentCharacter in Opponent.Characters)
            {
                if (!opponentCharacter.IsInOpponentTerritory) continue;
                if (!opponentCharacter.IsVisibleByOpponent) continue;

                if (source.PossibleMoves.ContainsKey(opponentCharacter.Node))
                {
                    target = opponentCharacter;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if a character should flee from an opponent.
        /// </summary>
        public bool ShouldFlee(CtfCharacter c)
        {
            if (!c.IsInOpponentTerritory) return false;

            foreach (CtfCharacter opponentCharacter in Opponent.Characters)
            {
                if (!opponentCharacter.IsVisibleByOpponent) continue;

                if (c.IsVisibleBy(opponentCharacter)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns if the given character should chase an opponent by going towards them.
        /// </summary>
        public bool ShouldChaseCharacterToTag(CtfCharacter source, out CtfCharacter target)
        {
            target = null;

            foreach (CtfCharacter opponentCharacter in Opponent.Characters)
            {
                if (opponentCharacter.IsInOpponentTerritory && opponentCharacter.IsVisibleByOpponent && source.MovementComp.IsInRange(opponentCharacter.Node, MAX_CHASE_DISTANCE))
                {
                    target = opponentCharacter;
                    return true;
                }
            }

            return false;
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
