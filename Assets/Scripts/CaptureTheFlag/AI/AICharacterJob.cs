using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
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
        public Player Player => Character.Owner;
        public CTFGame Game => Character.Game;

        public abstract AICharacterJobId Id { get; }
        public abstract string DisplayName { get; }

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
        /// Returns the possible movement that is most directly towards the given node.
        /// <br/>Moves onto the node if within range.
        /// <br/>This method is cheating a little since it will look for the perfect path even through unexplored territory.
        /// </summary>
        protected Action_Movement GetMovementDirectlyTo(BlockmapNode targetNode)
        {
            // Check if we can reach the node
            if (Character.PossibleMoves.TryGetValue(targetNode, out Action_Movement directMove)) return directMove;

            // Move as close as possible by finding the first node we can reach while backtracking from flag
            List<Zone> fordbiddenZones = new List<Zone>() { Character.Owner.FlagZone };
            List<BlockmapNode> path = Pathfinder.GetPath(Character.Entity, Character.Entity.OriginNode, targetNode, considerUnexploredNodes: false, fordbiddenZones);
            for (int i = 0; i < path.Count; i++)
            {
                BlockmapNode backtrackNode = path[path.Count - i - 1];
                if (Character.PossibleMoves.TryGetValue(backtrackNode, out Action_Movement closestMove) && Character.Owner.CanPerformMovement(closestMove)) return closestMove;
            }

            // Error
            if(Game.DevMode) Debug.LogWarning("Couldn't find a direct path towards target node. (" + Character.Entity.OriginNode.ToStringShort() + " --> " + targetNode.ToStringShort() + ")");
            return null;
        }

        #endregion
    }
}
