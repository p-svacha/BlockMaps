using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathSlopeNode : BlockmapNode
    {
        public Direction SlopeDirection;
        public override int Layer => World.Layer_Path;

        public override void Init(World world, Chunk chunk, NodeData data)
        {
            base.Init(world, chunk, data);

            SlopeDirection = GetDirectionFromShape(Shape);
        }

        public static string GetShapeFromDirection(Direction dir)
        {
            if (dir == Direction.N) return "0011";
            else if (dir == Direction.E) return "0110";
            else if (dir == Direction.S) return "1100";
            else if (dir == Direction.W) return "1001";
            else throw new System.Exception("Invalid directions for slope");
        }

        public static int[] GetHeightsFromDirection(int height, Direction dir)
        {
            if (dir == Direction.N) return new int[] { height, height, height + 1, height + 1 };
            else if (dir == Direction.E) return new int[] { height, height + 1, height + 1, height };
            else if (dir == Direction.S) return new int[] { height + 1, height + 1, height, height };
            else if (dir == Direction.W) return new int[] { height + 1, height, height, height + 1 };
            else throw new System.Exception("Invalid direction for slope");
        }

        private Direction GetDirectionFromShape(string shape)
        {
            if (shape == "0011") return Direction.N;
            if (shape == "0110") return Direction.E;
            if (shape == "1100") return Direction.S;
            if (shape == "1001") return Direction.W;
            throw new System.Exception("Unhandled heights");
        }

        #region Connections

        public override void UpdateConnectedNodesStraight()
        {
            ConnectedNodes.Clear();

            // Connection surface nodes
            if (Pathfinder.CanTransitionFromAirSlopeToSurface(this, Direction.N)) ConnectedNodes.Add(Direction.N, World.GetAdjacentSurfaceNode(this, Direction.N));
            if (Pathfinder.CanTransitionFromAirSlopeToSurface(this, Direction.E)) ConnectedNodes.Add(Direction.E, World.GetAdjacentSurfaceNode(this, Direction.E));
            if (Pathfinder.CanTransitionFromAirSlopeToSurface(this, Direction.S)) ConnectedNodes.Add(Direction.S, World.GetAdjacentSurfaceNode(this, Direction.S));
            if (Pathfinder.CanTransitionFromAirSlopeToSurface(this, Direction.W)) ConnectedNodes.Add(Direction.W, World.GetAdjacentSurfaceNode(this, Direction.W));

            // Try to connect to PathNode in downwards direction (can override surface connection)
            BlockmapNode adjacentNodeBelow = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight - 1, Pathfinder.GetOppositeDirection(SlopeDirection));
            if (adjacentNodeBelow != null && adjacentNodeBelow.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentNodeBelow).SlopeDirection == SlopeDirection) ConnectedNodes[Pathfinder.GetOppositeDirection(SlopeDirection)] = adjacentNodeBelow;

            // Try to connect to PathNode on same level in downwards direction (can override surface connection)
            BlockmapNode adjacentFromNodeSameLevel = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight, Pathfinder.GetOppositeDirection(SlopeDirection));
            if (adjacentFromNodeSameLevel != null && adjacentFromNodeSameLevel.Type == NodeType.AirPath) ConnectedNodes[Pathfinder.GetOppositeDirection(SlopeDirection)] = adjacentFromNodeSameLevel;
            if (adjacentFromNodeSameLevel != null && adjacentFromNodeSameLevel.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentFromNodeSameLevel).SlopeDirection == Pathfinder.GetOppositeDirection(SlopeDirection)) ConnectedNodes[Pathfinder.GetOppositeDirection(SlopeDirection)] = adjacentFromNodeSameLevel;

            // Try to connect to PathNode on same level in downwards direction (can override surface connection)
            BlockmapNode adjacentToNodeSameLevel = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight, SlopeDirection);
            if (adjacentToNodeSameLevel != null && adjacentToNodeSameLevel.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentToNodeSameLevel).SlopeDirection == Pathfinder.GetOppositeDirection(SlopeDirection)) ConnectedNodes[SlopeDirection] = adjacentToNodeSameLevel;

            // Try to connect to PathNode in upwards direction (can override surface connection)
            BlockmapNode adjacentNodeAbove = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight + 1, SlopeDirection);
            if (adjacentNodeAbove != null && adjacentNodeAbove.Type == NodeType.AirPath) ConnectedNodes[SlopeDirection] = adjacentNodeAbove;
            if (adjacentNodeAbove != null && adjacentNodeAbove.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentNodeAbove).SlopeDirection == SlopeDirection) ConnectedNodes[SlopeDirection] = adjacentNodeAbove;
        }

        #endregion

        #region Draw

        public override void Draw()
        {
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);
            PathMeshBuilder.BuildPath(World, meshBuilder, this);
            meshBuilder.ApplyMesh();
        }

        #endregion

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight + 0.5f), WorldCoordinates.y + 0.5f);
        }
    }
}
