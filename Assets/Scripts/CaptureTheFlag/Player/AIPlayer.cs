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
        private Dictionary<AICharacterJob, int> JobTable = new Dictionary<AICharacterJob, int>()
        {
            { AICharacterJob.AttackEnemyFlag, 6 },
            { AICharacterJob.DefendFlag, 2 },
        };
        private Dictionary<Character, AICharacterJob> Jobs = new Dictionary<Character, AICharacterJob>();

        // Camera follow
        private CharacterAction CurrentFollowedAction; // which action is currently being followed with the camera
        private Queue<CharacterAction> ActionsToFollow = new Queue<CharacterAction>(); // queue containing all character actions that are visible to local player and awaiting to be followed by camera, one after the other


        public AIPlayer(Actor actor, Zone jailZone, Zone flagZone) : base(actor, jailZone, flagZone) { }

        public override void OnStartGame(CTFGame game)
        {
            base.OnStartGame(game);

            // Assign a weighted-random job to all characters
            for (int i = 0; i < Characters.Count; i++)
            {
                AICharacterJob randomJob = HelperFunctions.GetWeightedRandomElement(JobTable);
                Jobs.Add(Characters[i], randomJob);

                // Debug
                Characters[i].Name = randomJob.ToString();
                Characters[i].UI_Label.Init(Characters[i]);
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

            // Get next action for character and assign it
            CharacterAction newAction = GetNextCharacterAction(character);
            if (newAction != null) Actions[character] = newAction;
        }

        #region Private

        /// <summary>
        /// Returns the action the given character will do next this turn depending on their job and game state.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        private CharacterAction GetNextCharacterAction(Character c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            AICharacterJob job = Jobs[c];

            if (job == AICharacterJob.AttackEnemyFlag)
            {
                // If we have the enemy flag in vision => move directly towards it
                if (Opponent.Flag.IsVisibleBy(Actor))
                    return GetMovementDirectlyTo(c, Opponent.Flag.OriginNode);

                // If we know where enemy flag is => move weighted-randomly towards it
                if (Opponent.Flag.IsExploredBy(Actor))
                    return GetWeightedMovementTowards(c, Opponent.Flag.OriginNode.WorldCoordinates);

                // If we don't know where enemy flag is => move weighted-randomly westwards
                else
                    return GetWeightedMovementTowards(c, new Vector2Int(0, c.WorldCoordinates.y));
            }

            if(job == AICharacterJob.DefendFlag)
            {
                return null;
            }

            throw new System.Exception("AICharacterJob " + job.ToString() + " not handled.");
        }

        /// <summary>
        /// Returns the possible movement that is most directly towards the given node.
        /// <br/>Moves onto the node if within range.
        /// </summary>
        private Action_Movement GetMovementDirectlyTo(Character c, BlockmapNode targetNode)
        {
            // Check if we can reach the node
            if (c.PossibleMoves.TryGetValue(targetNode, out Action_Movement directMove)) return directMove;

            // Move as close as possible by finding the first node we can reach while backtracking from flag
            List<BlockmapNode> path = Pathfinder.GetPath(c.Entity, c.Entity.OriginNode, targetNode, ignoreUnexploredNodes: true);
            for(int i = 0; i < path.Count; i++)
            {
                BlockmapNode backtrackNode = path[path.Count - i - 1];
                if (c.PossibleMoves.TryGetValue(backtrackNode, out Action_Movement closestMove)) return closestMove;
            }

            // Error
            throw new System.Exception("Couldn't find a direct path towards target node.");
        }

        /// <summary>
        /// Returns a random possible move that is heavily weighted towards a specific world coordinate.
        /// </summary>
        private Action_Movement GetWeightedMovementTowards(Character c, Vector2Int coordinates)
        {
            Dictionary<Action_Movement, float> movementDistances = new Dictionary<Action_Movement, float>();
            Dictionary<Action_Movement, float> movementProbabilities = new Dictionary<Action_Movement, float>();

            foreach (Action_Movement possibleMove in c.PossibleMoves.Values)
            {
                if (!CanPerformMovement(possibleMove)) continue; // Can't go on a node that another character is going to already
                movementDistances.Add(possibleMove, Vector2.Distance(possibleMove.Target.WorldCoordinates, coordinates));
            }

            if (movementDistances.Count == 0) return null;

            float maxDistance = movementDistances.Values.Max();
            float minDistance = movementDistances.Values.Min();
            float distanceRange = maxDistance - minDistance;

            foreach (var possibleMove in movementDistances)
            {
                float distance = possibleMove.Value;
                float weight = Mathf.Pow(distanceRange - (distance - minDistance), 2.5f);
                weight += 1f; // to avoid zero values
                movementProbabilities.Add(possibleMove.Key, weight);
            }

            Action_Movement randomMove = HelperFunctions.GetWeightedRandomElement(movementProbabilities);
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
