using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BlockmapFramework
{
    /// <summary>
    /// An object representing a specific path from one BlockmapNode to another.
    /// <br/>Is not static and be changed.
    /// </summary>
    public class NavigationPath
    {
        /// <summary>
        /// All nodes that are visited along the path, including the source and target node.
        /// </summary>
        public List<BlockmapNode> Nodes { get; private set; }

        /// <summary>
        /// All transitions that are traversed along the path.
        /// </summary>
        public List<Transition> Transitions { get; private set; }

        /// <summary>
        /// The final destination node of this path.
        /// </summary>
        public BlockmapNode Target => Nodes.Last();

        /// <summary>
        /// Creates a path with the given source node as the starting point and no transitions or target yet.
        /// </summary>
        public NavigationPath(BlockmapNode source)
        {
            Nodes = new List<BlockmapNode>() { source };
            Transitions = new List<Transition>();
        }

        /// <summary>
        /// Creates a copy of an existing NavigationPath.
        /// </summary>
        public NavigationPath(NavigationPath source)
        {
            Nodes = new List<BlockmapNode>();
            Nodes.AddRange(source.Nodes);

            Transitions = new List<Transition>();
            Transitions.AddRange(source.Transitions);
        }

        /// <summary>
        /// Creates a complete path with all the given nodes and transitions.
        /// </summary>
        public NavigationPath(List<BlockmapNode> nodes, List<Transition> transitions)
        {
            Nodes = nodes;
            Transitions = transitions;
        }

        #region Change Path

        /// <summary>
        /// Adds the given transition and transition target to this path.
        /// </summary>
        public void AddTransition(Transition t)
        {
            if (t.From != Target) throw new System.Exception("The start point of the given transition doesn't fit the current target of this path.");

            Transitions.Add(t);
            Nodes.Add(t.To);
        }

        /// <summary>
        /// Removes the first node, representing that the starting point of the path is now the first element of the transitions list.
        /// </summary>
        public void RemoveFirstNode()
        {
            if (Nodes.Count != Transitions.Count + 1) throw new System.Exception("Can't remove first node if the current starting point of this path is already a transition.");

            Nodes.RemoveAt(0);
        }

        /// <summary>
        /// Remove the first transition, representing that the starting point of the path is now the first element of the nodes list.
        /// </summary>
        public void RemoveFirstTransition()
        {
            if (Transitions.Count != Nodes.Count) throw new System.Exception("Can't remove first transition if the current starting point of this path is already a node.");

            Transitions.RemoveAt(0);
        }

        /// <summary>
        /// Changes the path so that the new starting point is the given node.
        /// <br/>Only works if the node is part of the path.
        /// </summary>
        public void CutEverythingBefore(BlockmapNode node)
        {
            if (!Nodes.Contains(node)) throw new System.Exception($"Can't cut path because {node} is not part of it.");
            while(Nodes[0] != node)
            {
                RemoveFirstNode();
                RemoveFirstTransition();
            }
        }

        #endregion


        #region Getters

        /// <summary>
        /// Returns the cost for a specified entity to complete this path.
        /// <br/>This function assumes that the entity is allowed and capable of taking this path, it won't check that.
        /// </summary>
        public float GetCost(Entity entity)
        {
            return Transitions.Sum(t => t.GetMovementCost(entity));
        }

        /// <summary>
        /// Returns if this is a path from one node to another one in a single transition.
        /// </summary>
        public bool IsSingleTransitionPath()
        {
            return Nodes.Count == 2 && Transitions.Count == 1;
        }

        /// <summary>
        /// Checks for all transitions and nodes in this path if they still exist.
        /// </summary>
        public bool IsValid()
        {
            if (Nodes.Any(n => Pathfinder.World.GetNode(n.Id) == null)) return false;
            foreach(Transition t in Transitions)
            {
                if (!t.From.TransitionsByTarget.ContainsKey(t.To)) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks and returns if this path can be fully used by the given entity.
        /// </summary>
        public bool CanPass(Entity e)
        {
            if (!IsValid()) return false;
            if (Nodes.Any(n => !n.IsPassable(e))) return false;
            if (Transitions.Any(t => !t.CanPass(e))) return false;
            return true;
        }

        #endregion
    }
}
