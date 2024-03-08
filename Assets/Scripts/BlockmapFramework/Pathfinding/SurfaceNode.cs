using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WorldEditor.BlockEditor;
using static BlockmapFramework.BlockmapNode;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents one tile on the surface of the terrain.
    /// </summary>
    public class SurfaceNode : BlockmapNode
    {
        public Surface Surface { get; private set; }

        public override NodeType Type => NodeType.Surface;
        public override bool IsSolid => true;

        /// <summary>
        /// The water node covering this node.
        /// </summary>
        public WaterNode WaterNode { get; private set; }

        public SurfaceNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, Surface surface) : base(world, chunk, id, localCoordinates, height)
        {
            Surface = surface;
        }

        protected override bool ShouldConnectToNodeDirectly(BlockmapNode adjNode, Direction dir)
        {
            // Always connect to water if this has water as well.
            if (WaterNode != null && adjNode.Type == NodeType.Water) return true;

            return base.ShouldConnectToNodeDirectly(adjNode, dir);
        }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            DrawSurface(meshBuilder);
            DrawSides(meshBuilder);
        }

        private void DrawSurface(MeshBuilder meshBuilder)
        {
            Surface.DrawNodeSurface(this, meshBuilder);
        }

        private void DrawSides(MeshBuilder meshBuilder)
        {
            int cliffSubmesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.CliffMaterial);
            DrawEastSide(meshBuilder, cliffSubmesh);
            DrawWestSide(meshBuilder, cliffSubmesh);
            DrawSouthSide(meshBuilder, cliffSubmesh);
            DrawNorthSide(meshBuilder, cliffSubmesh);
        }
        private void DrawEastSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode eastNode = World.GetAdjacentSurfaceNode(this, Direction.E);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[Direction.NE] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode == null ? World.MAP_EDGE_HEIGHT : eastNode.Height[Direction.NW] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode == null ? World.MAP_EDGE_HEIGHT : eastNode.Height[Direction.SW] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xEnd, Height[Direction.SE] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xEnd, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yCenter), new Vector2(0.5f, 0.5f));

            if(eastNode == null) meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1); // Map edge

            else if (Height[Direction.NE] < eastNode.Height[Direction.NW] && Height[Direction.SE] < eastNode.Height[Direction.SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Height[Direction.NE] < eastNode.Height[Direction.NW]) // Only NE corner is lower
            {
                if (Height[Direction.SE] == eastNode.Height[Direction.SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Height[Direction.SE] < eastNode.Height[Direction.SW]) // Only SE corner is lower
            {
                if (Height[Direction.NE] == eastNode.Height[Direction.NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawSouthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode southNode = World.GetAdjacentSurfaceNode(this, Direction.S);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[Direction.SE] * World.TILE_HEIGHT, yStart), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, southNode == null ? World.MAP_EDGE_HEIGHT : southNode.Height[Direction.NE]* World.TILE_HEIGHT, yStart), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, southNode == null ? World.MAP_EDGE_HEIGHT : southNode.Height[Direction.NW] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[Direction.SW] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yStart), new Vector2(0.5f, 0.5f));

            if (southNode == null) meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1); // Map edge

            else if (Height[Direction.SE] < southNode.Height[Direction.NE] && Height[Direction.SW] < southNode.Height[Direction.NW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Height[Direction.SE] < southNode.Height[Direction.NE]) // Only SE corner is lower
            {
                if (Height[Direction.SW] == southNode.Height[Direction.NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Height[Direction.SW] < southNode.Height[Direction.NW]) // Only SW corner is lower
            {
                if (Height[Direction.SE] == southNode.Height[Direction.NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawWestSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode westNode = World.GetAdjacentSurfaceNode(this, Direction.W);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xStart, Height[Direction.NW] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xStart, westNode == null ? World.MAP_EDGE_HEIGHT : westNode.Height[Direction.NE] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, westNode == null ? World.MAP_EDGE_HEIGHT : westNode.Height[Direction.SE] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[Direction.SW] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xStart, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yCenter), new Vector2(0.5f, 0.5f));

            if (westNode == null) meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4); // Map edge

            else if (Height[Direction.NW] < westNode.Height[Direction.NE] && Height[Direction.SW] < westNode.Height[Direction.SE]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Height[Direction.NW] < westNode.Height[Direction.NE]) // Only NE corner is lower
            {
                if (Height[Direction.SW] == westNode.Height[Direction.SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v3, v2);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Height[Direction.SW] < westNode.Height[Direction.SE]) // Only SE corner is lower
            {
                if (Height[Direction.NW] == westNode.Height[Direction.NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }
        private void DrawNorthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode northNode = World.GetAdjacentSurfaceNode(this, Direction.N);

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[Direction.NE] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, northNode == null ? World.MAP_EDGE_HEIGHT : northNode.Height[Direction.SE] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, northNode == null ? World.MAP_EDGE_HEIGHT : northNode.Height[Direction.SW] * World.TILE_HEIGHT, yEnd), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[Direction.NW] * World.TILE_HEIGHT, yEnd), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yEnd), new Vector2(0.5f, 0.5f));

            if (northNode == null) meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4); // Map edge

            else if (Height[Direction.NE] < northNode.Height[Direction.SE] && Height[Direction.NW] < northNode.Height[Direction.SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Height[Direction.NE] < northNode.Height[Direction.SE]) // Only SE corner is lower
            {
                if (Height[Direction.NW] == northNode.Height[Direction.SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Height[Direction.NW] < northNode.Height[Direction.SW]) // Only SW corner is lower
            {
                if (Height[Direction.NE] == northNode.Height[Direction.SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }

        #endregion

        #region Actions

        public void SetSurface(SurfaceId id)
        {
            Surface = SurfaceManager.Instance.GetSurface(id);
        }

        public bool CanChangeHeight(Direction mode, bool isIncrease)
        {
            if (Entities.Count > 0) return false;
            if (WaterNode != null) return false;

            // Walls
            foreach (Direction wallDir in Walls.Keys)
                if (HelperFunctions.DoAffectedCornersOverlap(mode, wallDir))
                    return false;

            // Ladders
            foreach (Direction ladderDir in TargetLadders.Keys)
                if (HelperFunctions.DoAffectedCornersOverlap(mode, ladderDir))
                    return false;

            // Calculate new heights
            Dictionary<Direction, int> newHeights = GetNewHeights(mode, isIncrease);
            if (!IsValid(newHeights)) return false;
            int newBaseHeight = newHeights.Values.Min();
            int newMaxHeight = newHeights.Values.Max();

            // Check if all heights are within allowed values
            if (newHeights.Values.Any(x => x < 0)) return false;
            if (newHeights.Values.Any(x => x > World.MAX_HEIGHT)) return false;

            // Check if a node above would block increase
            if (isIncrease)
            {
                List<BlockmapNode> nodesBelow = World.GetNodes(WorldCoordinates, 0, newBaseHeight);
                if (nodesBelow.Any(x => x != this && !World.IsAbove(x.Height, newHeights))) return false;
            }

            // Check if decreasing height would leave node under water of adjacent water body
            if (!isIncrease)
            {
                foreach (Direction corner1 in HelperFunctions.GetAffectedCorners(mode))
                {
                    foreach (Direction side in HelperFunctions.GetAffectedSides(corner1))
                    {
                        WaterNode adjWater = World.GetAdjacentWaterNode(WorldCoordinates, side);
                        if (adjWater == null) continue;
                        bool ownSideUnderwater = false;
                        bool otherSideUnderwater = false;
                        foreach (Direction corner2 in HelperFunctions.GetAffectedCorners(side))
                        {
                            if (newHeights[corner2] < adjWater.WaterBody.ShoreHeight) ownSideUnderwater = true;
                            if (adjWater.SurfaceNode.Height[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) otherSideUnderwater = true;

                            if (ownSideUnderwater && adjWater.SurfaceNode.Height[HelperFunctions.GetMirroredCorner(corner2, side)] < adjWater.WaterBody.ShoreHeight) return false;
                            if (newHeights[corner2] < adjWater.WaterBody.ShoreHeight && otherSideUnderwater) return false;
                        }
                    }
                }
            }

            return true;
        }
        public void ChangeHeight(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> preChange = new Dictionary<Direction, int>();
            foreach (Direction dir in Height.Keys) preChange[dir] = Height[dir];

            Height = GetNewHeights(mode, isIncrease);

            // Don't apply change if resulting shape is not valid
            if (!IsValid(Height))
            {
                foreach (Direction dir in HelperFunctions.GetCorners()) Height[dir] = preChange[dir];
            }
            else UseAlternativeVariant = isIncrease;

            RecalculateShape();
        }
        private Dictionary<Direction, int> GetNewHeights(Direction mode, bool isIncrease)
        {
            Dictionary<Direction, int> newHeights = new Dictionary<Direction, int>();
            foreach (Direction dir in Height.Keys) newHeights[dir] = Height[dir];

            // Calculate min and max height of affected corners
            int minHeight = World.MAX_HEIGHT;
            int maxHeight = 0;
            foreach(Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (Height[dir] < minHeight) minHeight = Height[dir];
                if (Height[dir] > maxHeight) maxHeight = Height[dir];
            }

            // Calculate new heights
            foreach (Direction dir in HelperFunctions.GetAffectedCorners(mode))
            {
                if (newHeights[dir] == minHeight && isIncrease) newHeights[dir] += 1;
                if (newHeights[dir] == maxHeight && !isIncrease) newHeights[dir] -= 1;
            }

            return newHeights;
        }
        public void SetHeight(Dictionary<Direction, int> newHeights)
        {
            if (IsValid(newHeights))
            {
                foreach (Direction dir in HelperFunctions.GetCorners())
                    Height[dir] = newHeights[dir];

                RecalculateShape();
            }
        }

        public void SetWaterNode(WaterNode waterNode)
        {
            WaterNode = waterNode;
        }

        #endregion

        #region Getters

        private bool IsValid(Dictionary<Direction,int> height)
        {
            if (height.Values.Any(x => x < 0)) return false;
            if (height.Values.Any(x => x > World.MAX_HEIGHT)) return false;

            return !(Mathf.Abs(height[Direction.SE] - height[Direction.SW]) > 1 ||
            Mathf.Abs(height[Direction.SW] - height[Direction.NW]) > 1 ||
            Mathf.Abs(height[Direction.NW] - height[Direction.NE]) > 1 ||
            Mathf.Abs(height[Direction.NE] - height[Direction.SE]) > 1);
        }

        public override Surface GetSurface() => Surface;
        public override SurfaceProperties GetSurfaceProperties() => Surface.Properties;
        
        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeightAt(WorldCoordinates + new Vector2(0.5f, 0.5f), this), WorldCoordinates.y + 0.5f);
        }

        public override bool IsPassable(Entity entity = null)
        {
            if (IsCenterUnderWater) return false;

            return base.IsPassable(entity);
        }

        public bool IsCenterUnderWater => (WaterNode != null && GetCenterWorldPosition().y < WaterNode.WaterBody.WaterSurfaceWorldHeight);

        public override int GetSubType() => (int)Surface.Id;

        #endregion

    }
}