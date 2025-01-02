using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag.AI
{
    /// <summary>
    /// A short-term, specific task an AI character will pursue or try to achieve.
    /// <br/>The current job decides what actions a character performs.
    /// </summary>
    public abstract class AICharacterJob
    {
        public CtfCharacter Character { get; private set; }
        public AIPlayer Player => (AIPlayer)Character.Owner;
        public Player Opponent => Player.Opponent;
        public CtfMatch Match => Character.Match;

        public abstract AICharacterJobId Id { get; }
        public abstract string DevmodeDisplayText { get; }

        public AICharacterJob(CtfCharacter c)
        {
            Character = c;
        }

        /// <summary>
        /// Gets called before the character with this job is requested to provide their actions for this turn.
        /// </summary>
        public virtual void OnCharacterStartsTurn() { }

        /// <summary>
        /// Gets called once when the AI Player wants to get the next action from this job BEFORE ShouldStopJob() and GetNextAction() are called.
        /// </summary>
        public virtual void OnNextActionRequested() { }

        /// <summary>
        /// Returns if/what new job should be assigned to the character instead of continuing to pursue this one.
        /// <br/>Returns self if this should be continued.
        /// </summary>
        public abstract AICharacterJob GetJobForNextAction();

        /// <summary>
        /// Returns the next action the character with this job will do next this turn.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        public abstract CharacterAction GetNextAction();

        #region Helper

        protected List<CtfCharacter> VisibleOpponentCharactersNotInJail => Player.VisibleOpponentCharactersNotInJail;

        /// <summary>
        /// Returns the possible adjacent movement (only moving 1 node) directly towards the given node, optionally by using the provided targetPath.
        /// <br/>This method is cheating a little since it will look for the perfect path even through unexplored territory.
        /// </summary>
        protected Action_Movement GetSingleNodeMovementTo(BlockmapNode targetNode, NavigationPath targetPath = null)
        {
            // Get path to target
            if (targetPath == null || !targetPath.Nodes.Contains(Character.OriginNode)) targetPath = GetPath(targetNode);
            else targetPath.CutEverythingBefore(Character.OriginNode); // Adapt the given path so the start point is where the character is currently at

            if (targetPath == null) // No path found
            {
                if (Match.DevMode) Debug.Log("Couldn't find a direct path towards target node. (" + Character.OriginNode + " --> " + targetNode + ")");
                return null;
            }

            // Look for a possible move that corresponds to a single step in the target path.
            // If the move exists but is blocked, try moves going over multiple nodes (up to 5) along the path.
            List<Action_Movement> candidateMoves = Character.PossibleMoves.Values.Where(m => m.CanPerformNow() && (Player.Territory.ContainsNode(m.Target) || !Opponent.Characters.Any(c => c.Node == m.Target))).ToList();
            for (int i = 1; i < 5; i++)
            {
                if (targetPath.Nodes.Count < (i + 1)) break;

                BlockmapNode singleMoveTarget = targetPath.Nodes[i];
                foreach (Action_Movement movement in candidateMoves)
                {
                    if (singleMoveTarget == movement.Target) return movement;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a new non-urgent job for this character that fits their role.
        /// </summary>
        /// <returns></returns>
        protected AICharacterJob GetNewNonUrgentJob()
        {
            switch (Player.Roles[Character])
            {
                case AIPlayer.AICharacterRole.Defender:
                    return GetNewDefenderJob();

                case AIPlayer.AICharacterRole.Attacker:
                    return GetNewAttackerJob();

                default:
                    throw new System.Exception($"Role {Player.Roles[Character]} not handled.");
            }
        }

        /// <summary>
        /// Returns a job that a defender should no if there is no urgent thing to do.
        /// </summary>
        private AICharacterJob GetNewDefenderJob()
        {
            if (Random.value < AIPlayer.CHANCE_THAT_RANDOM_DEFENDER_JOB_IS_EXPLORE) return new AIJob_ExploreOwnTerritory(Character);
            else return new AIJob_PatrolDefendFlag(Character);
        }

        /// <summary>
        /// Returns a job that an attacker should no if there is no urgent thing to do.
        /// </summary>
        private AICharacterJob GetNewAttackerJob()
        {
            // If we know where enemy flag is => capture it
            if (IsEnemyFlagExplored)
            {
                return new AIJob_CaptureOpponentFlag(Character);
            }

            // If we don't know where enemy flag is => search it
            else
            {
                return new AIJob_SearchOpponentFlag(Character);
            }
        }

        /// <summary>
        /// Returns the fastest possible path to the given node without going through the own flag zone.
        /// </summary>
        protected NavigationPath GetPath(BlockmapNode targetNode)
        {
            return Pathfinder.GetPath(Character, Character.OriginNode, targetNode, considerUnexploredNodes: false, forbiddenNodes: Player.FlagZone.Nodes);
        }
        protected float GetPathCost(BlockmapNode targetNode) => GetPath(targetNode).GetCost(Character);

        protected bool IsOnOrNear(BlockmapNode node)
        {
            return (Character.OriginNode == node || Character.OriginNode.TransitionsByTarget.ContainsKey(node));
        }

        protected bool IsAnyOpponentNearby(float maxCost)
        {
            foreach(CtfCharacter opp in VisibleOpponentCharactersNotInJail)
            {
                bool isInRange = (Character.IsInRange(opp.Node, maxCost, out float cost));
                // Log($"IsAnyOpponentNearby: Is {opp.LabelCap} in Range? {isInRange} with cost = {cost}");
                if (isInRange) return true;
            }
            return false;
        }

        protected bool IsEnemyFlagExplored => Opponent.Flag.IsExploredBy(Player.Actor);

        /// <summary>
        /// Returns if the given character can tag an opponent with their possible moves.
        /// </summary>
        protected bool CanTagCharacterDirectly(out CtfCharacter target)
        {
            target = null;

            foreach (CtfCharacter opponentCharacter in Opponent.Characters)
            {
                if (!opponentCharacter.IsInOpponentTerritory) continue;
                if (!opponentCharacter.IsVisibleByOpponent) continue;

                if (Character.PossibleMoves.TryGetValue(opponentCharacter.Node, out Action_Movement move))
                {
                    if (!move.CanPerformNow()) continue;

                    target = opponentCharacter;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If there is a visible opponent character or a position that is marked to be checked for search within the given search, returns true and the corresponding AIJob_ChaseToTagOpponent or AIJob_SearchOpponentInOwnTerritory job.
        /// <br/>Else returns false.
        /// </summary>
        protected bool ShouldChaseOrSearchOpponent(float maxDistanceCost, out CtfCharacter target, out AICharacterJobId jobId, out float costToTarget)
        {
            target = null;
            jobId = AICharacterJobId.Error;

            costToTarget = float.MaxValue;

            // Look for visibile opponents nearby
            foreach (CtfCharacter opp in VisibleOpponentCharactersNotInJail)
            {
                if (!opp.IsInOpponentTerritory) continue;
                if (Character.IsInRange(opp.Node, maxDistanceCost, out float cost))
                {
                    Log($"Detected possibility to switch from {Id} to ChaseToTagOpponent because {opp.LabelCap} is nearby (distance = {cost}).");

                    if(cost < costToTarget)
                    {
                        costToTarget = cost;
                        target = opp;
                        jobId = AICharacterJobId.ChaseAndTagOpponent;
                    }
                }
            }

            // Look for positions nearby that are flagged to check
            foreach (CtfCharacter opp in Opponent.Characters)
            {
                BlockmapNode positionToSearch = Player.OpponentPositionsToCheckForDefense[opp];
                if (positionToSearch == null) continue;

                if (Character.IsInRange(positionToSearch, maxDistanceCost, out float cost))
                {
                    Log($"Detected possibility to switch from {Id} to SearchOpponentInOwnTerritory because {opp.LabelCap} was seen nearby (distance = {cost}).");

                    if (cost < costToTarget)
                    {
                        target = opp;
                        jobId = AICharacterJobId.SearchOpponentInOwnTerritory;
                    }
                }
            }

            // Return job with lowest cost to target
            return target != null;
        }

        /// <summary>
        /// Returns if a character should flee from an opponent.
        /// </summary>
        public bool ShouldFlee()
        {
            if (!Character.IsInOpponentTerritory) return false;
            if (GetOpponentsToFleeFrom().Count == 0) return false;

            return true;
        }
        protected List<CtfCharacter> GetOpponentsToFleeFrom()
        {
            List<CtfCharacter> relevantOpponents = new List<CtfCharacter>();
            foreach (CtfCharacter opponentCharacter in VisibleOpponentCharactersNotInJail)
            {
                if (opponentCharacter.IsInRange(Character.Node, AIPlayer.FLEE_DISTANCE, out float cost))
                {
                    Log($"Should flee from {opponentCharacter.LabelCap} because distance is {cost}.");
                    relevantOpponents.Add(opponentCharacter);
                }
            }
            return relevantOpponents;
        }

        protected void Log(string msg, bool isWarning = false)
        {
            if (Match.DevMode) Player.Log(Character, msg, isWarning);
        }

        #endregion
    }
}
