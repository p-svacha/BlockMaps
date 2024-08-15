using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// A short-term, specific task an AI character will pursue or try to achieve.
    /// <br/>The current job decides what actions a character performs.
    /// </summary>
    public abstract class AICharacterJob
    {
        public Character Character { get; private set; }
        public AIPlayer Player => (AIPlayer)Character.Owner;
        public CTFGame Game => Character.Game;

        public abstract AICharacterJobId Id { get; }
        public abstract string DevmodeDisplayText { get; }

        public AICharacterJob(Character c)
        {
            Character = c;
        }

        /// <summary>
        /// Returns if a new job should be assigned to the character instead of continuing to pursue this one.
        /// <br/>If yes, a specific job can be forced. If nothing is forced, a new job will be looked for generally.
        /// </summary>
        public abstract bool ShouldStopJob(out AICharacterJob forcedNewJob);

        /// <summary>
        /// Returns the next action the character with this job will do next this turn.
        /// <br/>Can return null if no further action should be taken by the character.
        /// </summary>
        public abstract CharacterAction GetNextAction();

        #region Helper

        /// <summary>
        /// Returns the possible adjacent movement (only moving 1 node) directly towards the given node.
        /// <br/>This method is cheating a little since it will look for the perfect path even through unexplored territory.
        /// </summary>
        protected Action_Movement GetSingleNodeMovementTo(BlockmapNode targetNode)
        {
            // Get path to target
            List<BlockmapNode> path = GetPath(targetNode);

            // Check if the path contains an adjacent move
            foreach(Action_Movement movement in Character.PossibleMoves.Values.Where(x => x.Path.Count == 2))
            {
                if (Player.Opponent.Characters.Any(x => x.Node == movement.Path[1])) continue; // Don't go there if an opponent is on that node
                if (path[1] == movement.Path[1]) return movement;
            }

            // No path found
            if (Game.DevMode) Debug.LogWarning("Couldn't find a direct path towards target node. (" + Character.Entity.OriginNode.ToStringShort() + " --> " + targetNode.ToStringShort() + ")");
            return null;
        }

        /// <summary>
        /// Returns the possible movement that is as far as possible directly towards the given node.
        /// <br/>Moves onto the node if within range.
        /// <br/>This method is cheating a little since it will look for the perfect path even through unexplored territory.
        /// </summary>
        protected Action_Movement GetMovementTo(BlockmapNode targetNode)
        {
            // Check if we can reach the node
            if (Character.PossibleMoves.TryGetValue(targetNode, out Action_Movement directMove)) return directMove;

            // Move as close as possible by finding the first node we can reach while backtracking from flag
            List<BlockmapNode> path = GetPath(targetNode);
            if (path == null) return null; // no path to target
            for (int i = 0; i < path.Count; i++)
            {
                BlockmapNode backtrackNode = path[path.Count - i - 1];
                if (Character.PossibleMoves.TryGetValue(backtrackNode, out Action_Movement closestMove) && Character.Owner.CanPerformMovement(closestMove)) return closestMove;
            }

            // No possible move is part of path
            if(Game.DevMode) Debug.LogWarning("Couldn't find a direct path towards target node. (" + Character.Entity.OriginNode.ToStringShort() + " --> " + targetNode.ToStringShort() + ")");
            return null;
        }

        /// <summary>
        /// Returns the fastest possible path to the given node without going the own flag zone.
        /// </summary>
        private List<BlockmapNode> GetPath(BlockmapNode targetNode)
        {
            return Pathfinder.GetPath(Character.Entity, Character.Entity.OriginNode, targetNode, considerUnexploredNodes: false, Player.FlagZone.Nodes);
        }

        #endregion
    }
}
