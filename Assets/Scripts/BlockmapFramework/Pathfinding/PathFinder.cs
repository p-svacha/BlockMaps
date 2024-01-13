using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BlockmapFramework.BlockmapNode;

namespace BlockmapFramework
{
    public static class Pathfinder
    {
        private static World World;

        public static void Init(World world)
        {
            World = world;
        }

        #region A*

        // A* algorithm implementation. https://pavcreations.com/tilemap-based-a-star-algorithm-implementation-in-unity-game/
        public static List<BlockmapNode> GetPath(MovingEntity entity, BlockmapNode from, BlockmapNode to)
        {
            if (from == to || !to.IsPassable(entity)) return null;

            List<BlockmapNode> openList = new List<BlockmapNode>() { from }; // tiles that are queued for searching
            List<BlockmapNode> closedList = new List<BlockmapNode>(); // tiles that have already been searched

            Dictionary<BlockmapNode, float> gCosts = new Dictionary<BlockmapNode, float>(); // G-Costs are the estimated distance from the source node to any other node
            Dictionary<BlockmapNode, float> fCosts = new Dictionary<BlockmapNode, float>(); // F-Costs are the combined cost
            Dictionary<BlockmapNode, BlockmapNode> previousTiles = new Dictionary<BlockmapNode, BlockmapNode>();

            gCosts.Add(from, 0);
            fCosts.Add(from, gCosts[from] + GetHCost(from, to));

            while (openList.Count > 0)
            {
                BlockmapNode currentNode = GetLowestFCostNode(openList, fCosts);
                if (currentNode == to) // Reached goal
                {
                    return GetFinalPath(to, previousTiles);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (Transition transition in currentNode.Transitions.Values)
                {
                    if (closedList.Contains(transition.To)) continue;
                    if (!transition.CanPass(entity)) continue;

                    float tentativeGCost = gCosts[currentNode] + GetCCost(transition, entity);
                    if (!gCosts.ContainsKey(transition.To) || tentativeGCost < gCosts[transition.To])
                    {
                        previousTiles[transition.To] = currentNode;
                        gCosts[transition.To] = tentativeGCost;
                        fCosts[transition.To] = tentativeGCost + GetHCost(transition.To, to);

                        if (!openList.Contains(transition.To)) openList.Add(transition.To);
                    }
                }
            }

            // Out of tiles -> no path
            return null;
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

        private static List<BlockmapNode> GetFinalPath(BlockmapNode to, Dictionary<BlockmapNode, BlockmapNode> previousTiles)
        {
            List<BlockmapNode> path = new List<BlockmapNode>();
            path.Add(to);
            BlockmapNode currentTile = to;
            while (previousTiles.ContainsKey(currentTile))
            {
                path.Add(previousTiles[currentTile]);
                currentTile = previousTiles[currentTile];
            }
            path.Reverse();
            return path;
        }

        #endregion

        #region Preview

        public static void ShowPathPreview(LineRenderer line, List<BlockmapNode> path, float width, Color color)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                line.SetPosition(i, path[i].GetCenterWorldPosition() + new Vector3(0f, World.TILE_HEIGHT * 0.5f, 0f));
            }
        }

        #endregion
    }
}
