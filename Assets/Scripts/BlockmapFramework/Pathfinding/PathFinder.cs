using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class Pathfinder
    {
        public static World World;

        public static void Init(World world)
        {
            World = world;
        }

        #region A*

        // A* algorithm implementation. https://pavcreations.com/tilemap-based-a-star-algorithm-implementation-in-unity-game/
        /// <summary>
        /// Returns the shortest path from a source node to a target node for the given entity.
        /// <br/>Returned path includes both source and target.
        /// </summary>
        /// <param name="considerUnexploredNodes">If true, only nodes that are explored by the entity's actor are considered for a path.</param>
        public static NavigationPath GetPath(MovingEntity entity, BlockmapNode from, BlockmapNode to, bool considerUnexploredNodes = false, List<BlockmapNode> forbiddenNodes = null)
        {
            if (from == null || to == null) return null;
            if (from == to || !to.IsPassable(entity)) return null;

            PriorityQueue<BlockmapNode> openSet = new PriorityQueue<BlockmapNode>(); // Nodes that are queued for searching
            HashSet<BlockmapNode> closedSet = new HashSet<BlockmapNode>(); // Nodes that have already been searched

            Dictionary<BlockmapNode, float> gCosts = new Dictionary<BlockmapNode, float>(); // G-Costs are the accumulated real costs from the source node to any other node
            Dictionary<BlockmapNode, float> fCosts = new Dictionary<BlockmapNode, float>(); // F-Costs are the combined cost (assumed(h) + real(g)) from the source node to any other node
            Dictionary<BlockmapNode, Transition> transitionToNodes = new Dictionary<BlockmapNode, Transition>(); // Stores for each node which transition comes before it to get there the shortest way
            Dictionary<Transition, Transition> transitionToTransitions = new Dictionary<Transition, Transition>(); // Stores for each transition which transition comes before it to get there the shortest way

            // Initialize start node
            gCosts[from] = 0;
            fCosts[from] = GetHCost(from, to);
            openSet.Enqueue(from, fCosts[from]);  // priority = F cost

            while (openSet.Count > 0)
            {
                // Grab the node with the smallest F cost
                BlockmapNode currentNode = openSet.Dequeue();

                // If it's already in closedSet, it might be a stale entry
                if (closedSet.Contains(currentNode)) continue;

                // If we've reached the goal
                if (currentNode == to)
                {
                    return GetFinalPath(to, transitionToNodes, transitionToTransitions);
                }

                closedSet.Add(currentNode);

                // Explore neighbours
                foreach (Transition transition in currentNode.Transitions)
                {
                    BlockmapNode neighbour = transition.To;

                    // Skip any invalid or closed neighbours
                    if (closedSet.Contains(neighbour)) continue;
                    if (!transition.CanPass(entity)) continue;
                    if (considerUnexploredNodes && !neighbour.IsExploredBy(entity.Actor)) continue;
                    if (forbiddenNodes != null && forbiddenNodes.Contains(neighbour)) continue;

                    float tentativeGCost = gCosts[currentNode] + GetCCost(transition, entity);

                    // If this neighbor has never been visited or we found a cheaper path
                    if (!gCosts.ContainsKey(neighbour) || tentativeGCost < gCosts[neighbour])
                    {
                        gCosts[neighbour] = tentativeGCost;
                        float newF = tentativeGCost + GetHCost(neighbour, to);
                        fCosts[neighbour] = newF;

                        transitionToNodes[neighbour] = transition;
                        if (currentNode != from)
                        {
                            transitionToTransitions[transition] = transitionToNodes[transition.From];
                        }

                        // Enqueue or update priority
                        openSet.Enqueue(neighbour, newF);
                    }
                }
            }

            // Out of tiles -> no path
            Debug.Log($"[Pathfinder] Couldn't find path {from} -> {to} for {entity?.LabelCap} after checking all transitions.");
            return null;
        }

        /// <summary>
        /// Returns the cost of going from any one node to any other for a specified entity when taking the cheapest possible path.
        /// </summary>
        public static float GetPathCost(MovingEntity entity, BlockmapNode from, BlockmapNode to, bool considerUnexploredNodes = false, List<BlockmapNode> forbiddenNodes = null)
        {
            NavigationPath path = GetPath(entity, from, to, considerUnexploredNodes, forbiddenNodes);
            if (path == null) return float.MaxValue;
            else return path.GetCost(entity);
        }

        /// <summary>
        /// Assumed cost of that path. This function is not allowed to overestimate the cost. The real cost must be >= this cost.
        /// </summary>
        private static float GetHCost(BlockmapNode from, BlockmapNode to)
        {
            return Vector2Int.Distance(from.WorldCoordinates, to.WorldCoordinates);
        }

        /// <summary>
        /// Real cost of going from one node to another.
        /// </summary>
        private static float GetCCost(Transition t, MovingEntity e)
        {
            return t.GetMovementCost(e);
        }

        /// <summary>
        /// Returns the final path to the given target node with all the intermediary steps that have been cached.
        /// </summary>
        private static NavigationPath GetFinalPath(BlockmapNode to, Dictionary<BlockmapNode, Transition> transitionToNodes, Dictionary<Transition, Transition> transitionToTransitions)
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>(); // reversed list of traversed nodes
            List<Transition> transitions = new List<Transition>(); // reversed list of traversed transitions

            nodes.Add(to);
            Transition currentTransition = transitionToNodes[to];
            transitions.Add(currentTransition);

            while (transitionToTransitions.ContainsKey(currentTransition))
            {
                nodes.Add(currentTransition.From);
                transitions.Add(transitionToTransitions[currentTransition]);
                currentTransition = transitionToTransitions[currentTransition];
            }
            nodes.Add(currentTransition.From);

            nodes.Reverse();
            transitions.Reverse();

            return new NavigationPath(nodes, transitions);
        }

        #endregion

        #region Other useful functions

        /// <summary>
        /// Checks and returns if the given start node has at least the given amount of nodes
        /// available to move around (for the given entity if != null).
        /// <br/>Useful to check if entities are stuck in enclosed or blocked spaces.
        /// </summary>
        public static bool HasRoamingArea(BlockmapNode startNode, int numNodes, MovingEntity e = null, List<BlockmapNode> forbiddenNodes = null)
        {
            // 1) Get the node where the entity currently stands.
            if (startNode == null) return false;

            // 2) BFS (breadth-first search) initialization
            Queue<BlockmapNode> queue = new Queue<BlockmapNode>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            // 3) BFS loop
            while (queue.Count > 0)
            {
                BlockmapNode current = queue.Dequeue();

                // Early exit if we've visited enough nodes
                if (visited.Count >= numNodes)
                    return true;

                // Explore neighbors
                foreach (Transition transition in current.Transitions)
                {
                    // The transition must be passable by this entity
                    if (e != null && !transition.CanPass(e)) continue;

                    BlockmapNode neighbour = transition.To;

                    // The neighbor must be passable, and not yet visited
                    if (forbiddenNodes != null && forbiddenNodes.Contains(neighbour)) continue;
                    if (visited.Contains(neighbour)) continue;
                    if (e != null && !neighbour.IsPassable(e)) continue;

                    // Valid
                    visited.Add(neighbour);
                    queue.Enqueue(neighbour);
                }
            }

            // If BFS ends without reaching numNodes, it's not big enough
            return (visited.Count >= numNodes);
        }

        #endregion

        #region Preview

        public static void ShowPathPreview(LineRenderer line, NavigationPath path, float width, Color color)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;

            List<Vector3> completePathLine = new List<Vector3>();
            foreach(Transition t in path.Transitions)
            {
                completePathLine.AddRange(t.GetPreviewPath());
            }

            line.positionCount = completePathLine.Count;
            for (int i = 0; i < completePathLine.Count; i++)
            {
                line.SetPosition(i, completePathLine[i] + new Vector3(0f, NavmeshVisualizer.TRANSITION_Y_OFFSET, 0f));
            }
        }

        #endregion
    }
}
