using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathNode : BlockmapNode
    {
        public override int Layer => World.Layer_Path;

        #region Connections

        public override void UpdateConnectedNodesStraight()
        {
            ConnectedNodes.Clear();

            // Connection surface nodes
            if (Pathfinder.CanTransitionFromAirPathToSurface(this, Direction.N)) ConnectedNodes.Add(Direction.N, World.GetAdjacentSurfaceNode(this, Direction.N));
            if (Pathfinder.CanTransitionFromAirPathToSurface(this, Direction.E)) ConnectedNodes.Add(Direction.E, World.GetAdjacentSurfaceNode(this, Direction.E));
            if (Pathfinder.CanTransitionFromAirPathToSurface(this, Direction.S)) ConnectedNodes.Add(Direction.S, World.GetAdjacentSurfaceNode(this, Direction.S));
            if (Pathfinder.CanTransitionFromAirPathToSurface(this, Direction.W)) ConnectedNodes.Add(Direction.W, World.GetAdjacentSurfaceNode(this, Direction.W));

            // Connection to other PathNodes on same height (can overwrite connection to surface node)
            TryConnectToAdjacentPathNodes(Direction.N);
            TryConnectToAdjacentPathNodes(Direction.E);
            TryConnectToAdjacentPathNodes(Direction.S);
            TryConnectToAdjacentPathNodes(Direction.W);
        }

        private void TryConnectToAdjacentPathNodes(Direction dir)
        {
            BlockmapNode adjacentNodeSameLevel = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight, dir);
            if (adjacentNodeSameLevel != null)
            {
                if (adjacentNodeSameLevel.Type == NodeType.AirPath) ConnectedNodes[dir] = adjacentNodeSameLevel;
                else if (adjacentNodeSameLevel.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentNodeSameLevel).SlopeDirection == dir) ConnectedNodes[dir] = adjacentNodeSameLevel;
            }

            BlockmapNode adjacentNodeBelow = Pathfinder.TryGetAdjacentPathNode(WorldCoordinates, BaseHeight - 1, dir);
            if (adjacentNodeBelow != null)
            {
                if (adjacentNodeBelow.Type == NodeType.AirPathSlope && ((AirPathSlopeNode)adjacentNodeBelow).SlopeDirection == Pathfinder.GetOppositeDirection(dir)) ConnectedNodes[dir] = adjacentNodeBelow;
            }
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
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight), WorldCoordinates.y + 0.5f);
        }
    }
}
