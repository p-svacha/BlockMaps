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
        public override NodeType Type => NodeType.Surface;
        public override bool IsPath => HasPath;

        /// <summary>
        /// The water node covering this node.
        /// </summary>
        public WaterNode WaterNode { get; private set; }

        // Path
        public bool HasPath;
        public Surface PathSurface;

        public SurfaceNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, int[] height, SurfaceId surface) : base(world, chunk, id, localCoordinates, height, surface) { }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            DrawSurface(meshBuilder);
            DrawSides(meshBuilder);
            if(HasPath) DrawPath(meshBuilder);
        }

        private void DrawSurface(MeshBuilder meshBuilder)
        {
            int surfaceSubmesh = 0;

            // Surface vertices
            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1;
            MeshVertex v1a = meshBuilder.AddVertex(new Vector3(xStart, Height[0] * World.TILE_HEIGHT, yStart), new Vector2((float)LocalCoordinates.x / Chunk.Size, (float)LocalCoordinates.y / Chunk.Size), new Vector2(0f, 0f));
            MeshVertex v1b = meshBuilder.AddVertex(new Vector3(xStart, Height[0] * World.TILE_HEIGHT, yStart), new Vector2((float)LocalCoordinates.x / Chunk.Size, (float)LocalCoordinates.y / Chunk.Size), new Vector2(0f, 0f));
            MeshVertex v2a = meshBuilder.AddVertex(new Vector3(xEnd, Height[1] * World.TILE_HEIGHT, yStart), new Vector2((float)(LocalCoordinates.x + 1) / Chunk.Size, (float)LocalCoordinates.y / Chunk.Size), new Vector2(1f, 0f));
            MeshVertex v2b = meshBuilder.AddVertex(new Vector3(xEnd, Height[1] * World.TILE_HEIGHT, yStart), new Vector2((float)(LocalCoordinates.x + 1) / Chunk.Size, (float)LocalCoordinates.y / Chunk.Size), new Vector2(1f, 0f));
            MeshVertex v3a = meshBuilder.AddVertex(new Vector3(xEnd, Height[2] * World.TILE_HEIGHT, yEnd), new Vector2((float)(LocalCoordinates.x + 1) / Chunk.Size, (float)(LocalCoordinates.y + 1) / Chunk.Size), new Vector2(1f, 1f));
            MeshVertex v3b = meshBuilder.AddVertex(new Vector3(xEnd, Height[2] * World.TILE_HEIGHT, yEnd), new Vector2((float)(LocalCoordinates.x + 1) / Chunk.Size, (float)(LocalCoordinates.y + 1) / Chunk.Size), new Vector2(1f, 1f));
            MeshVertex v4a = meshBuilder.AddVertex(new Vector3(xStart, Height[3] * World.TILE_HEIGHT, yEnd), new Vector2((float)LocalCoordinates.x / Chunk.Size, (float)(LocalCoordinates.y + 1) / Chunk.Size), new Vector2(0f, 1f));
            MeshVertex v4b = meshBuilder.AddVertex(new Vector3(xStart, Height[3] * World.TILE_HEIGHT, yEnd), new Vector2((float)LocalCoordinates.x / Chunk.Size, (float)(LocalCoordinates.y + 1) / Chunk.Size), new Vector2(0f, 1f));

            switch (Shape)
            {
                case "0000":
                case "1100":
                case "0110":
                case "0011":
                case "1001":
                case "0001":
                case "1011":
                case "0100":
                case "1110":
                case "0121":
                case "1012":
                case "2101":
                case "1210":
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4a, v3b);
                    break;

                case "1000":
                case "0010":
                case "0111":
                case "1101":
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3a);
                    break;

                case "1010":
                    if (UseAlternativeVariant)
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3a);
                    }
                    else
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4a, v3b);
                    }
                    break;

                case "0101":
                    if (UseAlternativeVariant)
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4a, v3b);
                    }
                    else
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3a);
                    }
                    break;
            }
        }

        private void DrawSides(MeshBuilder meshBuilder)
        {
            int cliffSubmesh = 1;
            DrawEastSide(meshBuilder, cliffSubmesh);
            DrawWestSide(meshBuilder, cliffSubmesh);
            DrawSouthSide(meshBuilder, cliffSubmesh);
            DrawNorthSide(meshBuilder, cliffSubmesh);
        }
        private void DrawEastSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode eastNode = World.GetAdjacentSurfaceNode(this, Direction.E);
            if (eastNode == null) return;

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[NE] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode.Height[NW] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xEnd, eastNode.Height[SW] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xEnd, Height[SE] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xEnd, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yCenter), new Vector2(0.5f, 0.5f));


            if (Height[NE] < eastNode.Height[NW] && Height[SE] < eastNode.Height[SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Height[NE] < eastNode.Height[NW]) // Only NE corner is lower
            {
                if (Height[SE] == eastNode.Height[SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Height[SE] < eastNode.Height[SW]) // Only SE corner is lower
            {
                if (Height[NE] == eastNode.Height[NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawSouthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode southNode = World.GetAdjacentSurfaceNode(this, Direction.S);
            if (southNode == null) return;

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[SE] * World.TILE_HEIGHT, yStart), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, southNode.Height[NE]* World.TILE_HEIGHT, yStart), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, southNode.Height[NW] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[SW] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yStart), new Vector2(0.5f, 0.5f));


            if (Height[SE] < southNode.Height[NE] && Height[SW] < southNode.Height[NW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v4, v3, v2, v1);

            else if (Height[SE] < southNode.Height[NE]) // Only SE corner is lower
            {
                if (Height[SW] == southNode.Height[NW])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v2, cc);
            }

            else if (Height[SW] < southNode.Height[NW]) // Only SW corner is lower
            {
                if (Height[SE] == southNode.Height[NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v3, v4, cc);
            }
        }
        private void DrawWestSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode westNode = World.GetAdjacentSurfaceNode(this, Direction.W);
            if (westNode == null) return;

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xStart, Height[NW] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xStart, westNode.Height[NE] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, westNode.Height[SE] * World.TILE_HEIGHT, yStart), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[SW] * World.TILE_HEIGHT, yStart), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xStart, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yCenter), new Vector2(0.5f, 0.5f));


            if (Height[NW] < westNode.Height[NE] && Height[SW] < westNode.Height[SE]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Height[NW] < westNode.Height[NE]) // Only NE corner is lower
            {
                if (Height[SW] == westNode.Height[SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v1, v3, v2);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Height[SW] < westNode.Height[SE]) // Only SE corner is lower
            {
                if (Height[NW] == westNode.Height[NE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }
        private void DrawNorthSide(MeshBuilder meshBuilder, int cliffSubmesh)
        {
            SurfaceNode northNode = World.GetAdjacentSurfaceNode(this, Direction.N);
            if (northNode == null) return;

            float xStart = LocalCoordinates.x;
            float xEnd = LocalCoordinates.x + 1f;
            float xCenter = LocalCoordinates.x + 0.5f;
            float yStart = LocalCoordinates.y;
            float yEnd = LocalCoordinates.y + 1f;
            float yCenter = LocalCoordinates.y + 0.5f;
            MeshVertex v1 = meshBuilder.AddVertex(new Vector3(xEnd, Height[NE] * World.TILE_HEIGHT, yEnd), new Vector2(0, 0));
            MeshVertex v2 = meshBuilder.AddVertex(new Vector3(xEnd, northNode.Height[SE] * World.TILE_HEIGHT, yEnd), new Vector2(1, 0));
            MeshVertex v3 = meshBuilder.AddVertex(new Vector3(xStart, northNode.Height[SW] * World.TILE_HEIGHT, yEnd), new Vector2(0, 1));
            MeshVertex v4 = meshBuilder.AddVertex(new Vector3(xStart, Height[NW] * World.TILE_HEIGHT, yEnd), new Vector2(1, 1));
            MeshVertex cc = meshBuilder.AddVertex(new Vector3(xCenter, (BaseHeight * World.TILE_HEIGHT) + (World.TILE_HEIGHT * 0.5f), yEnd), new Vector2(0.5f, 0.5f));


            if (Height[NE] < northNode.Height[SE] && Height[NW] < northNode.Height[SW]) // Both corners are lower than next tile
                meshBuilder.AddPlane(cliffSubmesh, v1, v2, v3, v4);

            else if (Height[NE] < northNode.Height[SE]) // Only SE corner is lower
            {
                if (Height[NW] == northNode.Height[SW])
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, v3);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v2, v1, cc);
            }

            else if (Height[NW] < northNode.Height[SW]) // Only SW corner is lower
            {
                if (Height[NE] == northNode.Height[SE])
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, v1);

                else
                    meshBuilder.AddTriangle(cliffSubmesh, v4, v3, cc);
            }
        }

        private void DrawPath(MeshBuilder meshBuilder)
        {
            PathMeshBuilder.BuildPath(this, meshBuilder, pathSubmesh: 2, pathCurbSubmesh: 3);
        }

        #endregion

        #region Actions

        public bool CanChangeHeight(Direction mode, bool increase)
        {
            if (HasPath) return false;
            if (Entities.Count > 0) return false;
            if (WaterNode != null) return false;

            int[] newHeights = new int[4];
            for (int i = 0; i < 4; i++) newHeights[i] = Height[i];
            foreach (int h in GetAffectedCornerIds(mode)) newHeights[h] += increase ? 1 : -1;
            BlockmapNode pathNodeOn = Pathfinder.TryGetPathNode(WorldCoordinates, newHeights.Min());
            BlockmapNode pathNodeAbove = Pathfinder.TryGetPathNode(WorldCoordinates, newHeights.Min() + 1);
            string newShape = GetShape(newHeights);
            if (pathNodeOn != null) return false;
            if (pathNodeAbove != null && !Pathfinder.CanNodesBeAboveEachOther(newShape, pathNodeAbove.Shape)) return false;

            return true;
        }
        public void ChangeHeight(Direction mode, bool isIncrease)
        {
            int[] preChange = new int[Height.Length];
            for (int i = 0; i < preChange.Length; i++) preChange[i] = Height[i];

            switch (mode)
            {
                case Direction.None:
                    ChangeFullHeight(isIncrease);
                    break;

                case Direction.N:
                    ChangeSideHeight(isIncrease, NW, NE);
                    break;

                case Direction.E:
                    ChangeSideHeight(isIncrease, SE, NE);
                    break;

                case Direction.S:
                    ChangeSideHeight(isIncrease, SW, SE);
                    break;

                case Direction.W:
                    ChangeSideHeight(isIncrease, SW, NW);
                    break;

                case Direction.NE:
                    Height[NE] += isIncrease ? 1 : -1;
                    break;

                case Direction.NW:
                    Height[NW] += isIncrease ? 1 : -1;
                    break;

                case Direction.SW:
                    Height[SW] += isIncrease ? 1 : -1;
                    break;

                case Direction.SE:
                    Height[SE] += isIncrease ? 1 : -1;
                    break;
            }

            // Don't apply change if resulting shape is not valid
            if (!IsValid(Height))
            {
                for (int i = 0; i < preChange.Length; i++) Height[i] = preChange[i];
            }
            else UseAlternativeVariant = isIncrease;

            RecalculateShape();
        }
        private void ChangeFullHeight(bool increase)
        {
            if (Height.All(x => x == Height[0]))
            {
                for (int i = 0; i < Height.Length; i++) Height[i] += increase ? 1 : -1;
            }
            else
            {
                for (int i = 0; i < Height.Length; i++) Height[i] = increase ? Height.Max(x => x) : Height.Min(x => x);
            }
        }
        private void ChangeSideHeight(bool increase, int i1, int i2)
        {
            if (Height[i1] != Height[i2])
            {
                Height[i1] = increase ? Mathf.Max(Height[i1], Height[i2]) : Mathf.Min(Height[i1], Height[i2]);
                Height[i2] = increase ? Mathf.Max(Height[i1], Height[i2]) : Mathf.Min(Height[i1], Height[i2]);
            }
            else
            {
                Height[i1] += increase ? 1 : -1;
                Height[i2] += increase ? 1 : -1;
            }
        }

        
        public void BuildPath(Surface surface)
        {
            HasPath = true;
            PathSurface = surface;
        }

        public void SetWaterNode(WaterNode waterNode)
        {
            WaterNode = waterNode;
        }

        #endregion

        #region Getters

        private bool IsValid(int[] height)
        {
            return !(Mathf.Abs(height[SE] - height[SW]) > 1 ||
            Mathf.Abs(height[SW] - height[NW]) > 1 ||
            Mathf.Abs(height[NW] - height[NE]) > 1 ||
            Mathf.Abs(height[NE] - height[SE]) > 1);
        }

        public override float GetSpeedModifier()
        {
            if (HasPath) return PathSurface.SpeedModifier;
            else return Surface.SpeedModifier;
        }
        
        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeightAt(WorldCoordinates + new Vector2(0.5f, 0.5f), this), WorldCoordinates.y + 0.5f);
        }

        public override bool IsPassable(Entity entity = null)
        {
            if (IsCenterUnderWater) return false;

            return base.IsPassable(entity);
        }
        public override bool IsPassable(Direction dir, Entity entity = null)
        {
            // Check if the side has a corner underwater
            if (WaterNode != null)
            {
                if (GetAffectedCornerIds(dir).Any(x => Height[x] < WaterNode.WaterBody.ShoreHeight)) return false;
            }

            return base.IsPassable(dir, entity);
        }
        public bool IsCenterUnderWater => (WaterNode != null && GetCenterWorldPosition().y < WaterNode.WaterBody.WaterSurfaceWorldHeight);

        #endregion

    }
}