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
        public static List<BlockmapNode> GetPath(Entity entity, BlockmapNode from, BlockmapNode to)
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

                foreach (KeyValuePair<Direction, BlockmapNode> connectedNode in currentNode.ConnectedNodes)
                {
                    if (closedList.Contains(connectedNode.Value)) continue;
                    if (!CanTransition(entity, currentNode, connectedNode.Value)) continue;

                    float tentativeGCost = gCosts[currentNode] + GetCCost(currentNode, connectedNode.Value, connectedNode.Key);
                    if (!gCosts.ContainsKey(connectedNode.Value) || tentativeGCost < gCosts[connectedNode.Value])
                    {
                        previousTiles[connectedNode.Value] = currentNode;
                        gCosts[connectedNode.Value] = tentativeGCost;
                        fCosts[connectedNode.Value] = tentativeGCost + GetHCost(connectedNode.Value, to);

                        if (!openList.Contains(connectedNode.Value)) openList.Add(connectedNode.Value);
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
        private static float GetCCost(BlockmapNode from, BlockmapNode to, Direction dir)
        {
            float value = (0.5f * (1f / from.GetSpeedModifier())) + (0.5f * (1f / to.GetSpeedModifier()));
            if (dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) value *= 1.4142f;
            return value;
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

        /// <summary>
        /// Checks and returns if the given entity can move from one node to another.
        /// </summary>
        public static bool CanTransition(Entity entity, BlockmapNode from, BlockmapNode to)
        {
            return from.ConnectedNodes.ContainsValue(to) && from.IsPassable(entity) && to.IsPassable(entity);
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



        #region Transitions



        /// <summary>
        /// If existing, returns the PathNode (non-surface-node) at the given position
        /// </summary>
        public static BlockmapNode TryGetPathNode(Vector2Int worldCoordinates, int height)
        {
            List<BlockmapNode> nodes = World.GetAirNodes(worldCoordinates);
            if (nodes == null) return null;
            foreach (BlockmapNode node in nodes) if (node.BaseHeight == height) return node;
            return null;
        }

        /// <summary>
        /// Returns true if the two shapes don't touch each other.
        /// </summary>
        public static bool CanNodesBeAboveEachOther(string botShape, string topShape)
        {
            for (int i = 0; i < 4; i++) if (int.Parse(botShape[i].ToString()) > int.Parse(topShape[i].ToString())) return false;
            return true;
        }

        /// <summary>
        /// Checks and returns if two adjacent nodes match seamlessly in the given direction.
        /// </summary>
        public static bool DoAdjacentHeightsMatch(BlockmapNode fromNode, BlockmapNode toNode, Direction dir)
        {
            if (toNode.WorldCoordinates != World.GetWorldCoordinatesInDirection(fromNode.WorldCoordinates, dir))
                throw new System.Exception("toNode is not adjacent to fromNode in the given direction. fromNode = " + fromNode.WorldCoordinates.ToString() + ", toNode = " + toNode.WorldCoordinates.ToString() + ", direction = " + dir.ToString());

            switch (dir)
            {
                case Direction.N:
                    return (fromNode.Height[NE] == toNode.Height[SE]) && (fromNode.Height[NW] == toNode.Height[SW]);

                case Direction.S:
                    return (fromNode.Height[SE] == toNode.Height[NE]) && (fromNode.Height[SW] == toNode.Height[NW]);

                case Direction.E:
                    return (fromNode.Height[SE] == toNode.Height[SW]) && (fromNode.Height[NE] == toNode.Height[NW]);

                case Direction.W:
                    return (fromNode.Height[SW] == toNode.Height[SE]) && (fromNode.Height[NW] == toNode.Height[NE]);

                case Direction.NW:
                    return fromNode.Height[NW] == toNode.Height[SE];

                case Direction.NE:
                    return fromNode.Height[NE] == toNode.Height[SW];

                case Direction.SW:
                    return fromNode.Height[SW] == toNode.Height[NE];

                case Direction.SE:
                    return fromNode.Height[SE] == toNode.Height[NW];

                default:
                    return false;
            }
        }

        #endregion
    }
}
