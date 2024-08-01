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

        private Dictionary<Character, AICharacterJob> Jobs = new Dictionary<Character, AICharacterJob>();
        private Dictionary<Character, CharacterAction> Actions = new Dictionary<Character, CharacterAction>();
        private List<BlockmapNode> TargetNodes = new List<BlockmapNode>();

        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other

        public AIPlayer(Actor actor, Zone jailZone, Zone flagZone) : base(actor, jailZone, flagZone) { }


        public void StartTurn()
        {
            TurnFinished = false;

            // Assign a job for each character
            Jobs.Clear();
            for(int i = 0; i < Characters.Count; i++)
            {
                if (i < 2) Jobs.Add(Characters[i], AICharacterJob.DefendFlag); // 2 Defenders
                else Jobs.Add(Characters[i], AICharacterJob.AttackEnemyFlag); // 6 Attackers
            }

            // Get action for each character depending on job
            Actions.Clear();
            TargetNodes.Clear();
            ActionsToFollow.Clear();
            CurrentFollowedAction = null;
            foreach (Character c in Characters)
            {
                Actions.Add(c, GetCharacterAction(c));
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

            // Check if we should follow a new action
            foreach (CharacterAction action in Actions.Values)
            {
                if (action.IsDone) continue;
                if (action == CurrentFollowedAction) continue;

                // Character is newly visible
                if (action.Character.IsVisibleToLocalPlayer && !ActionsToFollow.Contains(action))
                {
                    action.PauseAction();
                    ActionsToFollow.Enqueue(action);
                }
            }

            // Update the currently followed action
            if (CurrentFollowedAction != null)
            {
                // Unpause action if camera arrived at character
                if (CurrentFollowedAction.IsPaused && World.Camera.FollowedEntity == CurrentFollowedAction.Character.Entity)
                {
                    CurrentFollowedAction.UnpauseAction();
                }

                // Stop following if character moves out of vision or if action is done
                if (CurrentFollowedAction.IsDone || !CurrentFollowedAction.Character.IsVisibleToLocalPlayer)
                {
                    World.Camera.Unfollow();
                    CurrentFollowedAction = null;
                }
            }

            // Get next action to follow
            else if (ActionsToFollow.Count > 0)
            {
                CurrentFollowedAction = ActionsToFollow.Dequeue();
                World.CameraPanToFocusEntity(CurrentFollowedAction.Character.Entity, duration: 1f, followAfterPan: true);
            }
            
        }

        #region Private

        /// <summary>
        /// Returns the action the given character will do this turn depending on their job and game state.
        /// </summary>
        private CharacterAction GetCharacterAction(Character c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            // Move to random reachable node, with heigher weights for nodes that are further west
            Dictionary<Action_Movement, float> movementProbabilities = new Dictionary<Action_Movement, float>();
            int maxX = c.PossibleMoves.Max(x => x.Value.Target.WorldCoordinates.x);
            foreach (var possibleMove in c.PossibleMoves)
            {
                if (TargetNodes.Contains(possibleMove.Value.Target)) continue; // Can't go on a node that another character is going to already
                movementProbabilities.Add(possibleMove.Value, maxX - possibleMove.Value.Target.WorldCoordinates.x + 1);
            }

            Action_Movement randomMove = HelperFunctions.GetWeightedRandomElement(movementProbabilities);
            TargetNodes.Add(randomMove.Target);

            return randomMove;
        }

        private enum AICharacterJob
        {
            DefendFlag,             // Stay near own flag
            AttackEnemyFlag,        // Go straight towards enemy flag
            AttackEnemyCharacter,   // Go straight towards an enemy character in own half
            Sneak,                  // Go towards enemy flag where noone sees you
            Retreat,                // Go back to own half
            Hide                    // Go away from all enemy characters
        }

        #endregion
    }
}
