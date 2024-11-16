using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WallMeshGenerator
    {
        #region Full chunk mesh

        /// <summary>
        /// Generates the meshes for all altitude levels of a chunk
        /// </summary>
        public static Dictionary<int, WallMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, WallMesh> meshes = new Dictionary<int, WallMesh>();

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                // Get walls for altitude level
                List<Wall> wallsToDraw = chunk.GetWalls(altitude);
                if (wallsToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("WallMesh_" + altitude);
                WallMesh mesh = meshObject.AddComponent<WallMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach(Wall wall in wallsToDraw)
                {
                    wall.Shape.RenderFunction(chunk.World, meshBuilder, wall.GlobalCellCoordinates, wall.LocalCellCoordinates, wall.Side, wall.Material.Material, wall.IsMirrored);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }

        #endregion


        private const float WallWidth = 0.1f;

        #region Single wall pieces

        /// <summary>
        /// Adds the mesh of a single wall piece to a MeshBuilder.
        /// </summary>
        public static void DrawWall(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, WallShapeDef shape, WallMaterialDef material, bool isMirrored, bool isPreview = false)
        {
            shape.RenderFunction(world, meshBuilder, globalCellPosition, localCellPosition, side, isPreview ? MaterialManager.BuildPreviewMaterial : material.Material, isMirrored);
        }

        public static void DrawSolidWall(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, Material material)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = 0;
            float dimX = 1f;
            float startY = 0f;
            float dimY = World.NodeHeight;
            float startZ = 0f;
            float dimZ = WallWidth;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(localCellPosition, side, submesh, pos, dim);
        }

        public static void DrawCornerWall(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, Material material)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = 0;
            float dimX = WallWidth;
            float startY = 0f;
            float dimY = World.NodeHeight;
            float startZ = 0f;
            float dimZ = WallWidth;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(localCellPosition, side, submesh, pos, dim);
        }

        public static void DrawSlopeWall(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = isMirrored ? 1f : 0f;
            float endX = isMirrored ? 0f : 1f;

            // Front triangle
            Vector3 ft1 = new Vector3(startX, 0f, 0f);
            Vector3 ft2 = new Vector3(endX, World.NodeHeight, 0f);
            Vector3 ft3 = new Vector3(endX, 0f, 0f);
            meshBuilder.BuildTriangle(localCellPosition, side, submesh, ft1, ft2, ft3, !isMirrored);

            // Back triangle
            Vector3 bt1 = new Vector3(startX, 0f, WallWidth);
            Vector3 bt2 = new Vector3(endX, World.NodeHeight, WallWidth);
            Vector3 bt3 = new Vector3(endX, 0f, WallWidth);
            meshBuilder.BuildTriangle(localCellPosition, side, submesh, bt1, bt2, bt3, isMirrored);

            // Side plane
            Vector3 sp1 = new Vector3(endX, 0f, 0f);
            Vector3 sp2 = new Vector3(endX, World.NodeHeight, 0f);
            Vector3 sp3 = new Vector3(endX, World.NodeHeight, WallWidth);
            Vector3 sp4 = new Vector3(endX, 0f, WallWidth);
            meshBuilder.BuildPlane(localCellPosition, side, submesh, sp1, sp2, sp3, sp4, !isMirrored);

            // Top sloped plane
            Vector3 tsp1 = new Vector3(startX, 0f, 0f);
            Vector3 tsp2 = new Vector3(endX, World.NodeHeight, 0f);
            Vector3 tsp3 = new Vector3(endX, World.NodeHeight, WallWidth);
            Vector3 tsp4 = new Vector3(startX, 0f, WallWidth);
            meshBuilder.BuildPlane(localCellPosition, side, submesh, tsp1, tsp2, tsp3, tsp4, isMirrored);
        }


        private const float WindowMargin = 0.1f;
        private const float WindowWidth = 0.05f;
        public static void DrawWindowWall(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored)
        {
            // Define submeshes
            int outsideSubmesh = meshBuilder.GetSubmesh(material);
            int glassSubmesh = meshBuilder.GetSubmesh("Glass");

            // Define some positional values
            float xWindowStart = WindowMargin;
            float xWindowEnd = 1f - WindowMargin;
            float windowLength = 1f - 2 * WindowMargin;

            float yWindowStart = WindowMargin;
            float yWindowEnd = World.NodeHeight - WindowMargin;
            float windowHeight = World.NodeHeight - 2 * WindowMargin;

            // check for adjacent windows that we can merge into
            bool connectAbove = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.N);
            bool connectBelow = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.S);
            bool connectLeft = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.W);
            bool connectRight = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.E);

            bool connectTL = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.NW);
            bool connectTR = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.NE);
            bool connectBL = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.SW);
            bool connectBR = ShouldConnectWindowInDirection(world, globalCellPosition, side, Direction.SE);


            // Center (always window glass)
            DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowStart, yWindowStart), new Vector2(windowLength, windowHeight));

            // Top-center
            if (connectAbove) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowStart, yWindowEnd), new Vector2(windowLength, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowStart, yWindowEnd), new Vector2(windowLength, WindowMargin));

            // Top-left
            if (connectTL) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, yWindowEnd), new Vector2(WindowMargin, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, yWindowEnd), new Vector2(WindowMargin, WindowMargin));

            // Top-right
            if (connectTR) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, yWindowEnd), new Vector2(WindowMargin, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, yWindowEnd), new Vector2(WindowMargin, WindowMargin));

            // Bot-center
            if (connectBelow) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowStart, 0f), new Vector2(windowLength, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowStart, 0f), new Vector2(windowLength, WindowMargin));

            // Bot-left
            if (connectBL) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, 0f), new Vector2(WindowMargin, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, 0f), new Vector2(WindowMargin, WindowMargin));

            // Bot-right
            if (connectBR) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, 0f), new Vector2(WindowMargin, WindowMargin));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, 0f), new Vector2(WindowMargin, WindowMargin));

            // center-left
            if (connectLeft) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, WindowMargin), new Vector2(WindowMargin, windowHeight));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, WindowMargin), new Vector2(WindowMargin, windowHeight));

            // center-right
            if (connectRight) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, WindowMargin), new Vector2(WindowMargin, windowHeight));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, WindowMargin), new Vector2(WindowMargin, windowHeight));
        }

        private static void DrawSolidWallCube(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, int wallSubmesh, Vector2 start, Vector2 dim2d)
        {
            Vector3 pos = new Vector3(start.x, start.y, 0f);
            Vector3 dim = new Vector3(dim2d.x, dim2d.y, WallWidth);
            meshBuilder.BuildCube(localCellPosition, side, wallSubmesh, pos, dim);
        }
        private static void DrawGlassPane(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, int glassSubmesh, Vector2 start, Vector2 dim)
        {
            float zFrontPane = WindowWidth / 2f;
            float zBackPane = WallWidth - WindowWidth / 2f;

            // Window pane front
            meshBuilder.BuildPlane(localCellPosition, side, glassSubmesh,
                new Vector3(start.x, start.y, zFrontPane),
                new Vector3(start.x + dim.x, start.y, zFrontPane),
                new Vector3(start.x + dim.x, start.y + dim.y, zFrontPane),
                new Vector3(start.x, start.y + dim.y, zFrontPane)
                );

            // Window pane back
            meshBuilder.BuildPlane(localCellPosition, side, glassSubmesh,
                new Vector3(start.x, start.y, zBackPane),
                new Vector3(start.x + dim.x, start.y, zBackPane),
                new Vector3(start.x + dim.x, start.y + dim.y, zBackPane),
                new Vector3(start.x, start.y + dim.y, zBackPane),
                mirror: true
                );
        }

        private static bool ShouldConnectWindowInDirection(World world, Vector3Int globalCellCoordinates, Direction side, Direction dir)
        {
            if (HelperFunctions.IsSide(dir))
            {
                Wall adjWall = world.GetWall(HelperFunctions.GetAdjacentWallCellCoordinates(globalCellCoordinates, side, dir), side);
                return (adjWall != null && adjWall.Shape == WallShapeDefOf.Window);
            }
            else
            {
                Wall adjWall = world.GetWall(HelperFunctions.GetAdjacentWallCellCoordinates(globalCellCoordinates, side, dir), side);
                bool connectPrev = ShouldConnectWindowInDirection(world, globalCellCoordinates, side, HelperFunctions.GetPreviousDirection8(dir));
                bool connectNext = ShouldConnectWindowInDirection(world, globalCellCoordinates, side, HelperFunctions.GetNextDirection8(dir));
                return (adjWall != null && adjWall.Shape == WallShapeDefOf.Window && connectPrev && connectNext);
            }
        }

        #endregion
    }
}
