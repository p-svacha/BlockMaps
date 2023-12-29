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

        // A* algorithm implementation. https://pavcreations.com/tilemap-based-a-star-algorithm-implementation-in-unity-game/
        public static List<BlockmapNode> GetPath(BlockmapNode from, BlockmapNode to)
        {
            if (from == to) return null;

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

        public static Direction GetDirection(BlockmapNode from, BlockmapNode to)
        {
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(1, 0)) return Direction.E;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(-1, 0)) return Direction.W;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(0, 1)) return Direction.N;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(0, -1)) return Direction.S;

            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(1, 1)) return Direction.NE;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(-1, 1)) return Direction.NW;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(1, -1)) return Direction.SE;
            if (to.WorldCoordinates == from.WorldCoordinates + new Vector2Int(-1, -1)) return Direction.SW;
            throw new System.Exception("Position is not adjacent to character position.");
        }

        public static Direction GetOppositeDirection(Direction dir)
        {
            switch (dir)
            {
                case Direction.N: return Direction.S;
                case Direction.E: return Direction.W;
                case Direction.S: return Direction.N;
                case Direction.W: return Direction.E;
                case Direction.NE: return Direction.SW;
                case Direction.NW: return Direction.SE;
                case Direction.SW: return Direction.NE;
                case Direction.SE: return Direction.NW;
                default: return Direction.None;
            }
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

        #region Transitions

        public static bool CanTransition(BlockmapNode from, BlockmapNode to)
        {
            return from.ConnectedNodes.ContainsValue(to);
        }

        /// <summary>
        /// Checks and returns if the transition between two adjacent TerrainNodes is possible.
        /// </summary>
        public static bool CanTransitionFromSurfaceToSurface(SurfaceNode source, Direction dir)
        {
            if (!source.IsPassable()) return false;

            Vector2Int targetWorldCoordinates = World.GetWorldCoordinatesInDirection(source.WorldCoordinates, dir);
            SurfaceNode target = World.GetSurfaceNode(targetWorldCoordinates);
            if (target == null) return false;
            if (!target.IsPassable()) return false;

            BlockmapNode pathNodeOnSource = TryGetPathNode(source.WorldCoordinates, source.BaseHeight);
            BlockmapNode pathNodeOnTarget = TryGetPathNode(targetWorldCoordinates, target.BaseHeight);
            BlockmapNode pathNodeAboveSource = TryGetPathNode(source.WorldCoordinates, source.BaseHeight + 1);
            BlockmapNode pathNodeAboveTarget = TryGetPathNode(targetWorldCoordinates, target.BaseHeight + 1);

            if (pathNodeOnSource != null) return false;
            if (pathNodeOnTarget != null) return false;
            if (pathNodeAboveSource != null && !CanNodesBeAboveEachOther(source.Shape, pathNodeAboveSource.Shape)) return false;
            if (pathNodeAboveTarget != null && !CanNodesBeAboveEachOther(target.Shape, pathNodeAboveTarget.Shape)) return false;

            switch (dir)
            {
                case Direction.N:
                case Direction.E:
                case Direction.S:
                case Direction.W:
                    return DoAdjacentHeightsMatch(source, target, dir);

                case Direction.NW:
                    return CanTransitionFromSurfaceToSurface(source, Direction.N) && CanTransitionFromSurfaceToSurface(source, Direction.W) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.W), Direction.N) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.N), Direction.W) && DoAdjacentHeightsMatch(source, target, dir);

                case Direction.NE:
                    return CanTransitionFromSurfaceToSurface(source, Direction.N) && CanTransitionFromSurfaceToSurface(source, Direction.E) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.E), Direction.N) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.N), Direction.E) && DoAdjacentHeightsMatch(source, target, dir);

                case Direction.SW:
                    return CanTransitionFromSurfaceToSurface(source, Direction.S) && CanTransitionFromSurfaceToSurface(source, Direction.W) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.W), Direction.S) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.S), Direction.W) && DoAdjacentHeightsMatch(source, target, dir);

                case Direction.SE:
                    return CanTransitionFromSurfaceToSurface(source, Direction.S) && CanTransitionFromSurfaceToSurface(source, Direction.E) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.E), Direction.S) && CanTransitionFromSurfaceToSurface(World.GetAdjacentSurfaceNode(source, Direction.S), Direction.E) && DoAdjacentHeightsMatch(source, target, dir);

                default:
                    return false;
            }
        }

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
        /// Returns the adjacent airpath node from an airpath node if there is one. Else returns null
        /// </summary>
        public static BlockmapNode TryGetAdjacentPathNode(Vector2Int worldCoordinates, int height, Direction dir)
        {
            List<BlockmapNode> nodes = World.GetAdjacentPathNodes(worldCoordinates, dir);
            if (nodes == null) return null;
            foreach (BlockmapNode node in nodes) if (node.BaseHeight == height) return node;
            return null;
        }


        public static bool CanTransitionFromAirPathToSurface(AirPathNode source, Direction dir)
        {
            if (!source.IsPassable()) return false;

            SurfaceNode target = World.GetAdjacentSurfaceNode(source, dir);
            if (target == null) return false;
            if (!target.IsPassable()) return false;

            switch (dir)
            {
                case Direction.N:
                    return (source.BaseHeight == target.Height[SE]) && (source.BaseHeight == target.Height[SW]);

                case Direction.S:
                    return (source.BaseHeight == target.Height[NE]) && (source.BaseHeight == target.Height[NW]);

                case Direction.E:
                    return (source.BaseHeight == target.Height[SW]) && (source.BaseHeight == target.Height[NW]);

                case Direction.W:
                    return (source.BaseHeight == target.Height[SE]) && (source.BaseHeight == target.Height[NE]);

                default:
                    return false;
            }
        }

        public static bool CanTransitionFromAirSlopeToSurface(AirPathSlopeNode source, Direction dir)
        {
            if (!source.IsPassable()) return false;
            if (dir != source.SlopeDirection && dir != GetOppositeDirection(source.SlopeDirection)) return false; // Can only connect two sides

            SurfaceNode target = World.GetAdjacentSurfaceNode(source, dir);
            if (target == null) return false;
            if (!target.IsPassable()) return false;

            int targetHeight = source.SlopeDirection == dir ? source.BaseHeight + 1 : source.BaseHeight;

            switch (dir)
            {
                case Direction.N:
                    return (targetHeight == target.Height[SE]) && (targetHeight == target.Height[SW]);

                case Direction.S:
                    return (targetHeight == target.Height[NE]) && (targetHeight == target.Height[NW]);

                case Direction.E:
                    return (targetHeight == target.Height[SW]) && (targetHeight == target.Height[NW]);

                case Direction.W:
                    return (targetHeight == target.Height[SE]) && (targetHeight == target.Height[NE]);

                default:
                    return false;
            }
        }

        public static bool CanNodesBeAboveEachOther(string botShape, string topShape)
        {
            for (int i = 0; i < 4; i++) if (int.Parse(botShape[i].ToString()) > int.Parse(topShape[i].ToString())) return false;
            return true;
        }

        public static bool DoAdjacentHeightsMatch(SurfaceNode fromNode, SurfaceNode toNode, Direction dir)
        {
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
