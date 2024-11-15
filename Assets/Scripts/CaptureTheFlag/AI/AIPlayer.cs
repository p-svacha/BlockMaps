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
        private Dictionary<Character, AICharacterRole> Roles = new Dictionary<Character, AICharacterRole>();
        private Dictionary<Character, AICharacterJob> Jobs = new Dictionary<Character, AICharacterJob>();

        public List<BlockmapNode> DefendPerimeterNodes;

        // Camera follow
        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other


        public AIPlayer(Actor actor, Zone territory, Zone jailZone, Zone flagZone) : base(actor, territory, jailZone, flagZone) { }

        public override void OnStartGame(CTFGame game)
        {
            base.OnStartGame(game);

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
            Actions.Clear();
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
                Character currentCharacter = CurrentCharacterIndex > -1 ? Characters[CurrentCharacterIndex] : null;
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
                    Actions[currentCharacter] = nextAction;
                    CurrentAction = nextAction;
                    nextAction.Perform();
                    currentCharacter.EnableOverrideMovementSpeed(INVISIBLE_CHARACTER_SPEED); // Speed up enemy characters so player doesn't have to wait for long
                }


            }

            // Check if we should queue-follow an action
            foreach (CharacterAction action in Actions.Values.Where(x => x.State == CharacterActionState.Performing))
            {
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
                        CurrentFollowedAction.Character.DisableOverrideMovementSpeed();
                        CurrentFollowedAction.UnpauseAction();
                    }

                    // Stop following if character moves out of vision or if action is done
                    else if (CurrentFollowedAction.IsDone || !CurrentFollowedAction.Character.IsVisible)
                    {
                        World.Camera.Unfollow();
                        CurrentFollowedAction.Character.EnableOverrideMovementSpeed(INVISIBLE_CHARACTER_SPEED); // Speed up enemy characters so player doesn't have to wait for long
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

        /// <summary>
        /// Gets called when dev mode gets activated or deactivated.
        /// </summary>
        public void OnSetDevMode(bool active)
        {
            if(active)
            {
                SetDevModeLabels();
            }
            else
            {
                foreach (Character c in Characters) c.UI_Label.Init(c);
            }
        }


        #region Private

        /// <summary>
        /// Sets the visible label of all characters according to their role and job to easily debug what they are doing.
        /// </summary>
        private void SetDevModeLabels()
        {
            foreach (Character c in Characters)
            {
                string label = Roles[c].ToString() + " | " + Jobs[c].DevmodeDisplayText;
                c.UI_Label.SetLabelText(label);
            }
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

            // Update dev mode labels
            if (Game.DevMode) SetDevModeLabels();

            // Get action based on job
            return currentJob.GetNextAction();
        }

        /// <summary>
        /// Returns a new job that the given character should do given their role and current game state.
        /// </summary>
        private AICharacterJob GetNewCharacterJob(Character c)
        {
            // If we can directly tag an opponent, do that no matter the role
            if (CanTagCharacterDirectly(c, out Character target0)) return new AIJob_TagOpponent(c, target0);

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
                    if (ShouldChaseCharacterToTag(c, out Character target1)) return new AIJob_TagOpponent(c, target1);

                    // Else just patrol own flag
                    return new AIJob_PatrolDefendFlag(c);
            }

            throw new System.Exception("Gamestate not handled");
        }

        /// <summary>
        /// Returns if the given character can tag an opponent with their possible moves.
        /// </summary>
        public bool CanTagCharacterDirectly(Character source, out Character target)
        {
            target = null;

            foreach (Character opponentCharacter in Opponent.Characters)
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
        public bool ShouldFlee(Character c)
        {
            if (!c.IsInOpponentTerritory) return false;

            foreach (Character opponentCharacter in Opponent.Characters)
            {
                if (!opponentCharacter.IsVisibleByOpponent) continue;

                if (c.Entity.IsVisibleBy(opponentCharacter.Entity)) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns if the given character should chase an opponent by going towards them.
        /// </summary>
        public bool ShouldChaseCharacterToTag(Character source, out Character target)
        {
            target = null;

            foreach (Character opponentCharacter in Opponent.Characters)
            {
                if (opponentCharacter.IsInOpponentTerritory && opponentCharacter.IsVisibleByOpponent && source.Entity.IsInRange(opponentCharacter.Node, MAX_CHASE_DISTANCE))
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
