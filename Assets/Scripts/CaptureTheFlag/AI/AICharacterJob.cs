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
        public CtfCharacter Character { get; private set; }
        public AIPlayer Player => (AIPlayer)Character.Owner;
        public Player Opponent => Player.Opponent;
        public CtfMatch Game => Character.Match;

        public abstract AICharacterJobId Id { get; }
        public abstract string DevmodeDisplayText { get; }

        public AICharacterJob(CtfCharacter c)
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
        /// Returns the possible adjacent movement (only moving 1 node) directly towards the given node, optionally by using the provided targetPath.
        /// <br/>This method is cheating a little since it will look for the perfect path even through unexplored territory.
        /// </summary>
        protected Action_Movement GetSingleNodeMovementTo(BlockmapNode targetNode, NavigationPath targetPath = null)
        {
            // Get path to target
            if (targetPath == null) targetPath = GetPath(targetNode);
            else targetPath.CutEverythingBefore(Character.OriginNode); // Adapt the given path so the start point is where the character is currently at

            if (targetPath == null) // No path found
            {
                if (Game.DevMode) Debug.Log("Couldn't find a direct path towards target node. (" + Character.OriginNode + " --> " + targetNode + ")");
                return null;
            }

            // Check if the path contains an adjacent move
            foreach(Action_Movement movement in Character.PossibleMoves.Values.Where(x => x.Path.IsSingleTransitionPath()))
            {
                if (Player.Opponent.Characters.Any(x => x.Node == movement.Target)) continue; // Don't go there if an opponent is on that node
                if (targetPath.Nodes[1] == movement.Target) return movement;
            }

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
            NavigationPath path = GetPath(targetNode);
            if (path == null) return null; // no path to target
            for (int i = 0; i < path.Nodes.Count; i++)
            {
                BlockmapNode backtrackNode = path.Nodes[path.Nodes.Count - i - 1];
                if (Character.PossibleMoves.TryGetValue(backtrackNode, out Action_Movement closestMove) && closestMove.CanPerformNow()) return closestMove;
            }

            // No possible move is part of path
            if(Game.DevMode) Debug.LogWarning("Couldn't find a direct path towards target node. (" + Character.OriginNode + " --> " + targetNode + ")");
            return null;
        }

        /// <summary>
        /// Returns the fastest possible path to the given node without going through the own flag zone.
        /// </summary>
        protected NavigationPath GetPath(BlockmapNode targetNode)
        {
            return Pathfinder.GetPath(Character, Character.OriginNode, targetNode, considerUnexploredNodes: false, forbiddenNodes: Player.FlagZone.Nodes);
        }

        #endregion
    }
}
