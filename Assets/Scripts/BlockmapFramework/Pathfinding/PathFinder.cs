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
        public static NavigationPath GetPath(Entity entity, BlockmapNode from, BlockmapNode to, bool considerUnexploredNodes = false, List<BlockmapNode> forbiddenNodes = null)
        {
            if (from == to || !to.IsPassable(entity)) return null;
            
            List<BlockmapNode> openList = new List<BlockmapNode>() { from }; // tiles that are queued for searching
            List<BlockmapNode> closedList = new List<BlockmapNode>(); // tiles that have already been searched

            Dictionary<BlockmapNode, float> gCosts = new Dictionary<BlockmapNode, float>(); // G-Costs are the accumulated real costs from the source node to any other node
            Dictionary<BlockmapNode, float> fCosts = new Dictionary<BlockmapNode, float>(); // F-Costs are the combined cost (assumed(h) + real(g)) from the source node to any other node
            Dictionary<BlockmapNode, Transition> transitionToNodes = new Dictionary<BlockmapNode, Transition>(); // Stores for each node which transition comes before it to get there the shortest way
            Dictionary<Transition, Transition> transitionToTransitions = new Dictionary<Transition, Transition>(); // Stores for each transition which transition comes before it to get there the shortest way

            gCosts.Add(from, 0);
            fCosts.Add(from, gCosts[from] + GetHCost(from, to));

            while (openList.Count > 0)
            {
                BlockmapNode currentNode = GetLowestFCostNode(openList, fCosts);
                if (currentNode == to) // Reached goal
                {
                    return GetFinalPath(to, transitionToNodes, transitionToTransitions);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (Transition transition in currentNode.Transitions)
                {
                    if (closedList.Contains(transition.To)) continue;
                    if (!transition.CanPass(entity)) continue;
                    if (considerUnexploredNodes && !transition.To.IsExploredBy(entity.Actor)) continue;
                    if (forbiddenNodes != null && forbiddenNodes.Contains(transition.To)) continue;

                    float tentativeGCost = gCosts[currentNode] + GetCCost(transition, entity);
                    if (!gCosts.ContainsKey(transition.To) || tentativeGCost < gCosts[transition.To])
                    {
                        transitionToNodes[transition.To] = transition;
                        if(currentNode != from) transitionToTransitions[transition] = transitionToNodes[transition.From];

                        gCosts[transition.To] = tentativeGCost;
                        fCosts[transition.To] = tentativeGCost + GetHCost(transition.To, to);

                        if (!openList.Contains(transition.To)) openList.Add(transition.To);
                    }
                }
            }

            // Out of tiles -> no path
            Debug.Log($"[Pathfinder] Couldn't find path {from} -> {to} for {entity?.Label} after checking all transitions.");
            return null;
        }

        /// <summary>
        /// Returns the cost of going from any one node to any other for a specified entity when taking the cheapest possible path.
        /// </summary>
        public static float GetPathCost(Entity entity, BlockmapNode from, BlockmapNode to, bool considerUnexploredNodes = false, List<BlockmapNode> forbiddenNodes = null)
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
        private static float GetCCost(Transition t, Entity e)
        {
            return t.GetMovementCost(e);
        }

        private static BlockmapNode GetLowestFCostNode(List<BlockmapNode> list, Dictionary<BlockmapNode, float> fCosts)
        {
            float lowestCost = float.MaxValue;
            BlockmapNode lowestCostTile = list[0];
            foreach (BlockmapNode tile in list)
            {
                if (fCosts[tile] < lowestCost)
                {
                    lowestCostTile = tile;
                    lowestCost = fCosts[tile];
                }
            }
            return lowestCostTile;
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
