using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    public class AIPlayer : Player
    {
        public bool TurnFinished { get; private set; }

        // AI Behaviour
        private const float INVISIBLE_CHARACTER_SPEED = 25;
        private const float CHANCE_THAT_ATTACKERS_TURN_INTO_DEFENDERS_AFTER_JAIL = 0.5f;
        public const float CHANCE_THAT_RANDOM_DEFENDER_JOB_IS_EXPLORE = 0.25f;
        public const float CHANCE_THAT_DEFENDER_SWITCHES_TO_ATTACKER_EACH_ACTION = 0.01f;

        public const float MAX_STAMINA_FOR_REST_CHANCE = 0.4f; // If stamina is below is value (in %), there is a chance that characters with non-urgent jobs chose to rest
        public const float NON_URGENT_REST_CHANCE_PER_ACTION = 0.06f;

        public const float FLEE_DISTANCE = 16; // Path cost at which characters start fleeing from opponents

        private const float DEFEND_PERIMETER_RADIUS = 60; // Transition cost

        private List<CtfCharacter> ShuffledCharacters;
        private int CurrentCharacterIndex; // Resets each turn - each character performs all actions before the next one starts
        private CharacterAction CurrentAction; // Actions are performed one after the other

        private Dictionary<AICharacterRole, int> RoleProbabilities = new Dictionary<AICharacterRole, int>()
        {
            { AICharacterRole.Attacker, 6 },
            { AICharacterRole.Defender, 2 },
        };
        public Dictionary<CtfCharacter, AICharacterRole> Roles = new Dictionary<CtfCharacter, AICharacterRole>();
        private Dictionary<CtfCharacter, AICharacterJob> Jobs = new Dictionary<CtfCharacter, AICharacterJob>();
        private Dictionary<CtfCharacter, bool> HasStartedTurn = new Dictionary<CtfCharacter, bool>();

        /// <summary>
        /// Used to track if the last positions of opponents that were marked to be searched for in case they disappeared in our territory.
        /// </summary>
        private Dictionary<CtfCharacter, BlockmapNode> LastPositionsToBeChecked = new Dictionary<CtfCharacter, BlockmapNode>();
        /// <summary>
        /// Stores for each opponent character, if and what position we should check where to character has been last seen.
        /// <br/>If we don't need to check the last position of the character (because we don't know it, or we see them, or we already checked), it is null.
        /// </summary>
        public Dictionary<CtfCharacter, BlockmapNode> OpponentPositionsToCheckForDefense = new Dictionary<CtfCharacter, BlockmapNode>();


        public List<BlockmapNode> DefendPerimeterNodes;

        // Camera follow
        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other


        public AIPlayer(ClientInfo info) : base(info) { }

        #region Game Loop

        public override void OnMatchReady(CtfMatch game)
        {
            base.OnMatchReady(game);

            // Assign a weighted-random role to all characters
            for (int i = 0; i < Characters.Count; i++)
            {
                AICharacterRole randomRole = HelperFunctions.GetWeightedRandomElement(RoleProbabilities);
                Roles.Add(Characters[i], randomRole);
                Jobs.Add(Characters[i], new AIJob_InitialJob(Characters[i]));
                HasStartedTurn.Add(Characters[i], false);
            }

            foreach (CtfCharacter opp in OpponentCharacters)
            {
                LastPositionsToBeChecked.Add(opp, null);
                OpponentPositionsToCheckForDefense.Add(opp, null);
            }

            // Calculate some important things once that will be used for the whole game
            DefendPerimeterNodes = Flag.OriginNode.GetNodesInRange(DEFEND_PERIMETER_RADIUS).Where(n => !FlagZone.ContainsNode(n) && !Opponent.Territory.ContainsNode(n)).ToList();
        }

        public void StartTurn()
        {
            // Reset
            TurnFinished = false;
            CurrentCharacterIndex = -1;
            CurrentAction = null;
            ActionsToFollow.Clear();
            CurrentFollowedAction = null;
            ShuffledCharacters = Characters.GetShuffledList();

            foreach (CtfCharacter c in Characters) HasStartedTurn[c] = false;

            // See if we should check any last known position of an opponent
            foreach(CtfCharacter opponentCharacter in OpponentCharacters)
            {
                // Last known node of an opponent is in our territory, but we don't see them anymore => flag them that we should search them
                if(opponentCharacter.GetLastKnownNode(Actor) != LastPositionsToBeChecked[opponentCharacter] &&
                    opponentCharacter.GetLastKnownNode(Actor) != null &&
                    !opponentCharacter.IsVisibleBy(Actor) &&
                    Territory.Nodes.Contains(opponentCharacter.GetLastKnownNode(Actor)))
                {
                    Debug.Log($"[AI] Marking {opponentCharacter.LabelCap}'s last position ({opponentCharacter.GetLastKnownNode(Actor)}) to be checked for search.");
                    LastPositionsToBeChecked[opponentCharacter] = opponentCharacter.GetLastKnownNode(Actor);
                    OpponentPositionsToCheckForDefense[opponentCharacter] = opponentCharacter.GetLastKnownNode(Actor);
                }

                // Last known position of an opponent character is flagged that we should check but now they're visible again => stop searching
                if(OpponentPositionsToCheckForDefense[opponentCharacter] != null && opponentCharacter.IsVisibleBy(Actor))
                {
                    UnmarkOpponentCharactersLastPositionToBeChecked(opponentCharacter);
                }
            }
        }

        /// <summary>
        /// Gets called every frame during the AI's turn.
        /// </summary>
        public void TickTurn()
        {
            UpdateCharacterActions();
            UpdateCameraFollow();
        }

        private void UpdateCharacterActions()
        {
            // Get a new action if the current one is null or done.
            // Characters are iterated through one by one
            if (CurrentAction == null || CurrentAction.IsDone)
            {
                CtfCharacter currentCharacter = CurrentCharacterIndex > -1 ? ShuffledCharacters[CurrentCharacterIndex] : null;
                CharacterAction nextAction = CurrentCharacterIndex > -1 ? GetNextCharacterAction(currentCharacter) : null;

                if (nextAction == null) // Character is done for this turn => go to next character
                {
                    CurrentCharacterIndex++;

                    if (CurrentCharacterIndex == ShuffledCharacters.Count) // If it was last character turn is done
                    {
                        TurnFinished = true;
                    }
                    else
                    {
                        currentCharacter = ShuffledCharacters[CurrentCharacterIndex];
                        nextAction = GetNextCharacterAction(currentCharacter);
                    }
                }

                currentCharacter.RefreshLabelText();

                // Start performing next action immediately
                if (nextAction != null)
                {
                    if (!nextAction.CanPerformNow()) throw new System.Exception($"GetNextCharacterAction returned an action that can't be performed. Character = {currentCharacter.LabelCap}, Job = {Jobs[currentCharacter].DevmodeDisplayText}");
                    CurrentAction = nextAction;
                    nextAction.Perform();
                    currentCharacter.MovementComp.EnableOverrideMovementSpeed(INVISIBLE_CHARACTER_SPEED); // Speed up enemy characters so player doesn't have to wait for long
                }
            }
        }
        private void UpdateCameraFollow()
        {
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
                if (!CurrentFollowedAction.Character.IsVisible)
                {
                    CurrentFollowedAction.UnpauseAction();
                    CurrentFollowedAction = null;
                }

                // Else Pan to character
                else World.CameraPanToFocusEntity(CurrentFollowedAction.Character, duration: 1f, followAfterPan: true, unbreakableFollow: true);
            }
        }

        public override void OnCharacterGotSentToJail(CtfCharacter c)
        {
            Jobs[c] = new AIJob_InitialJob(c);
        }

        public override void OnCharacterGotReleasedFromJail(CtfCharacter c)
        {
            // Chance that attackers that get released from jail turn into defenders
            if(Roles[c] == AICharacterRole.Attacker)
            {
                if(Random.value < CHANCE_THAT_ATTACKERS_TURN_INTO_DEFENDERS_AFTER_JAIL)
                {
                    Log(c, $"Changing role to Defender after being released from jail");
                    Roles[c] = AICharacterRole.Defender;
                }
            }
        }

        #endregion



        #region Logic



        public void UnmarkOpponentCharactersLastPositionToBeChecked(CtfCharacter c)
        {
            Debug.Log($"[AI] Marking {c.LabelCap}'s last position to be no longer checked for search.");
            OpponentPositionsToCheckForDefense[c] = null;
        }

        /// <summary>
        /// Returns the action the given character will do next this turn.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        private CharacterAction GetNextCharacterAction(CtfCharacter c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            AICharacterJob currentJob = Jobs[c];

            if (!HasStartedTurn[c]) currentJob.OnCharacterStartsTurn();
            currentJob.OnNextActionRequested();

            // Ask the current job what job should be used for next action (can stay the same or switch)
            int attempts = 0;
            int maxAttempts = 10;
            AICharacterJob nextJob = currentJob.GetJobForNextAction();
            while (nextJob != currentJob && attempts < maxAttempts)
            {
                attempts++;

                Jobs[c] = nextJob;
                currentJob = nextJob;

                if (!HasStartedTurn[c]) currentJob.OnCharacterStartsTurn();
                currentJob.OnNextActionRequested();

                nextJob = currentJob.GetJobForNextAction();
            }

            // Log if we couldn't get a valid job after many attempts (if current job still wants to switch)
            if (nextJob != currentJob && attempts >= maxAttempts)
            {
                Log(c, $"After {attempts} attempts we still didn't get a job that shouldn't immediately be replaced by another job. Therefore we go to jail");
                return c.PossibleSpecialActions.First(a => a is Action_GoToJail);
            }

            // Get action based on job
            HasStartedTurn[c] = true;
            return currentJob.GetNextAction();
        }


        public List<CtfCharacter> OpponentCharacters => Opponent.Characters;
        public List<CtfCharacter> VisibleOpponentCharactersNotInJail => Opponent.Characters.Where(c => c.IsVisibleByOpponent && c.JailTime <= 1).ToList();


        public string GetDevModeLabel(CtfCharacter c)
        {
            return $"{c.LabelCap}: {Roles[c]} | {Jobs[c].DevmodeDisplayText}";
        }
        public void Log(CtfCharacter c, string msg, bool isWarning = false)
        {
            if (Match.DevMode)
            {
                string logMessage = $"[AI - {c.LabelCap}] {msg}";
                if (isWarning) Debug.LogWarning(logMessage);
                else Debug.Log(logMessage);
            }
        }

        /// <summary>
        /// A role is a macro-level, long-term (mostly for a full game) assignment that dictates what jobs a character can and will do.
        /// </summary>
        public enum AICharacterRole
        {
            Defender,
            Attacker
        }

        #endregion
    }
}
