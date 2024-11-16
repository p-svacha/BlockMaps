using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WorldEditor.BlockEditor;
using static BlockmapFramework.BlockmapNode;

namespace BlockmapFramework
{
    /// <summary>
    /// Ground nodes make up the terrain and are the bottom most layer of nodes.
    /// <br/>There is always exactly one GroundNode per coordinate.
    /// </summary>
    public class GroundNode : DynamicNode
    {
        public override NodeType Type => NodeType.Ground;
        public override bool SupportsEntities => true;

        /// <summary>
        /// Returns true if this ground node is void, meaning outside the playable world or an impassable abyss.
        /// </summary>
        public bool IsVoid => SurfaceDef == SurfaceDefOf.Void;

        /// <summary>
        /// The water node covering this node.
        /// </summary>
        public WaterNode WaterNode { get; private set; }

        public GroundNode() { }
        public GroundNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceDef surfaceDef) : base(world, chunk, id, localCoordinates, height, surfaceDef) { }

        protected override bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            // Always connect to water if this has water as well.
            if (WaterNode != null && adjNode.Type == NodeType.Water) return true;

            return base.ShouldConnectToNodeDirectly(adjNode, dir);
        }

        #region Draw

        public void DrawSides(MeshBuilder meshBuilder)
        {
            int cliffSubmesh = meshBuilder.GetSubmesh(MaterialManager.LoadMaterial("Cliff"));
            DrawEastSide(meshBuilder, cliffSubmesh);
            DrawWestSide(meshBuilder, cliffSubmesh);
            DrawSouthSide(meshBuilder, cliffSubmesh);
            DrawNorthSide(meshBuilder, cliffSubmesh);
        }
        private void DrawEastSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            GroundNode eastNode = World.GetAdjacentGroundNode(this, Direction.E);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Altitude[Direction.NE] * World.NodeHeight, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode == null ? World.MAP_EDGE_HEIGHT : eastNode.Altitude[Direction.NW] * World.NodeHeight, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode == null ? World.MAP_EDGE_HEIGHT : eastNode.Altitude[Direction.SW] * World.NodeHeight, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xEnd, Altitude[Direction.SE] * World.NodeHeight, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xEnd, (BaseAltitude * World.NodeHeight) + (World.NodeHeight * 0.5f), yCenter), new Vector2(0.5f, 0.5f));

            if(eastNode == null) meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1); // Map edge

            else if (Altitude[Direction.NE] < eastNode.Altitude[Direction.NW] && Altitude[Direction.SE] < eastNode.Altitude[Direction.SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Altitude[Direction.NE] < eastNode.Altitude[Direction.NW]) // Only NE corner is lower
            {
                if (Altitude[Direction.SE] == eastNode.Altitude[Direction.SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Altitude[Direction.SE] < eastNode.Altitude[Direction.SW]) // Only SE corner is lower
            {
                if (Altitude[Direction.NE] == eastNode.Altitude[Direction.NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawSouthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            GroundNode southNode = World.GetAdjacentGroundNode(this, Direction.S);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Altitude[Direction.SE] * World.NodeHeight, yStart), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, southNode == null ? World.MAP_EDGE_HEIGHT : southNode.Altitude[Direction.NE]* World.NodeHeight, yStart), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, southNode == null ? World.MAP_EDGE_HEIGHT : southNode.Altitude[Direction.NW] * World.NodeHeight, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Altitude[Direction.SW] * World.NodeHeight, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseAltitude * World.NodeHeight) + (World.NodeHeight * 0.5f), yStart), new Vector2(0.5f, 0.5f));

            if (southNode == null) meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1); // Map edge

            else if (Altitude[Direction.SE] < southNode.Altitude[Direction.NE] && Altitude[Direction.SW] < southNode.Altitude[Direction.NW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Altitude[Direction.SE] < southNode.Altitude[Direction.NE]) // Only SE corner is lower
            {
                if (Altitude[Direction.SW] == southNode.Altitude[Direction.NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Altitude[Direction.SW] < southNode.Altitude[Direction.NW]) // Only SW corner is lower
            {
                if (Altitude[Direction.SE] == southNode.Altitude[Direction.NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawWestSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            GroundNode westNode = World.GetAdjacentGroundNode(this, Direction.W);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xStart, Altitude[Direction.NW] * World.NodeHeight, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xStart, westNode == null ? World.MAP_EDGE_HEIGHT : westNode.Altitude[Direction.NE] * World.NodeHeight, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, westNode == null ? World.MAP_EDGE_HEIGHT : westNode.Altitude[Direction.SE] * World.NodeHeight, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Altitude[Direction.SW] * World.NodeHeight, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xStart, (BaseAltitude * World.NodeHeight) + (World.NodeHeight * 0.5f), yCenter), new Vector2(0.5f, 0.5f));

            if (westNode == null) meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4); // Map edge

            else if (Altitude[Direction.NW] < westNode.Altitude[Direction.NE] && Altitude[Direction.SW] < westNode.Altitude[Direction.SE]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Altitude[Direction.NW] < westNode.Altitude[Direction.NE]) // Only NE corner is lower
            {
                if (Altitude[Direction.SW] == westNode.Altitude[Direction.SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v3, v2);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Altitude[Direction.SW] < westNode.Altitude[Direction.SE]) // Only SE corner is lower
            {
                if (Altitude[Direction.NW] == westNode.Altitude[Direction.NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }
        private void DrawNorthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            GroundNode northNode = World.GetAdjacentGroundNode(this, Direction.N);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Altitude[Direction.NE] * World.NodeHeight, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, northNode == null ? World.MAP_EDGE_HEIGHT : northNode.Altitude[Direction.SE] * World.NodeHeight, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, northNode == null ? World.MAP_EDGE_HEIGHT : northNode.Altitude[Direction.SW] * World.NodeHeight, yEnd), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Altitude[Direction.NW] * World.NodeHeight, yEnd), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseAltitude * World.NodeHeight) + (World.NodeHeight * 0.5f), yEnd), new Vector2(0.5f, 0.5f));

            if (northNode == null) meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4); // Map edge

            else if (Altitude[Direction.NE] < northNode.Altitude[Direction.SE] && Altitude[Direction.NW] < northNode.Altitude[Direction.SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Altitude[Direction.NE] < northNode.Altitude[Direction.SE]) // Only SE corner is lower
            {
                if (Altitude[Direction.NW] == northNode.Altitude[Direction.SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Altitude[Direction.NW] < northNode.Altitude[Direction.SW]) // Only SW corner is lower
            {
                if (Altitude[Direction.NE] == northNode.Altitude[Direction.SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }

        #endregion

        #region Actions

        public void SetWaterNode(WaterNode waterNode)
        {
            WaterNode = waterNode;
        }


        public void SetAsVoid()
        {
            SetSurface(SurfaceDefOf.Void);
            SetHeight(World.MAP_EDGE_ALTITUDE);
        }

        public void UnsetAsVoid(int altitude)
        {
            SetSurface(SurfaceDefOf.Grass);
            SetHeight(altitude);
        }

        #endregion

        #region Getters

        public override bool CanChangeShape(Direction mode, bool isIncrease)
        {
            if (WaterNode != null) return false;

            return base.CanChangeShape(mode, isIncrease);
        }

        protected override bool IsGenerallyPassable()
        {
            if (IsCenterUnderWater) return false;
            if (IsVoid) return false;
            return base.IsGenerallyPassable();
        }

        public bool IsCenterUnderWater => (WaterNode != null && CenterWorldPosition.y < WaterNode.WaterBody.WaterSurfaceWorldHeight);

        #endregion

    }
}