using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WS004_Window : WallShape
    {
        private const float WIDTH = 0.1f;
        private const float WINDOW_MARGIN = 0.1f;
        private const float WINDOW_WIDTH = 0.05f;

        public override WallShapeId Id => WallShapeId.Window;
        public override string Name => "Window";
        public override bool BlocksVision => false;
        public override bool IsClimbable => true;
        public override float Width => WIDTH;

        public override void GenerateMesh(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored)
        {
            // Define submeshes
            int outsideSubmesh = meshBuilder.GetSubmesh(material);
            int glassSubmesh = meshBuilder.GetSubmesh("Glass");

            // Define some positional values
            float xWindowStart = WINDOW_MARGIN;
            float xWindowEnd = 1f - WINDOW_MARGIN;
            float windowLength = 1f - 2 * WINDOW_MARGIN;

            float yWindowStart = WINDOW_MARGIN;
            float yWindowEnd = World.TILE_HEIGHT - WINDOW_MARGIN;
            float windowHeight = World.TILE_HEIGHT - 2 * WINDOW_MARGIN;

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
            if (connectAbove) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowStart, yWindowEnd), new Vector2(windowLength, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowStart, yWindowEnd), new Vector2(windowLength, WINDOW_MARGIN));

            // Top-left
            if (connectTL) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, yWindowEnd), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, yWindowEnd), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));

            // Top-right
            if (connectTR) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, yWindowEnd), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, yWindowEnd), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));

            // Bot-center
            if (connectBelow) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowStart, 0f), new Vector2(windowLength, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowStart, 0f), new Vector2(windowLength, WINDOW_MARGIN));

            // Bot-left
            if (connectBL) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, 0f), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, 0f), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));

            // Bot-right
            if (connectBR) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, 0f), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, 0f), new Vector2(WINDOW_MARGIN, WINDOW_MARGIN));

            // center-left
            if (connectLeft) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(0f, WINDOW_MARGIN), new Vector2(WINDOW_MARGIN, windowHeight));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(0f, WINDOW_MARGIN), new Vector2(WINDOW_MARGIN, windowHeight));

            // center-right
            if (connectRight) DrawGlassPane(meshBuilder, localCellPosition, side, glassSubmesh, new Vector2(xWindowEnd, WINDOW_MARGIN), new Vector2(WINDOW_MARGIN, windowHeight));
            else DrawSolidWallCube(meshBuilder, localCellPosition, side, outsideSubmesh, new Vector2(xWindowEnd, WINDOW_MARGIN), new Vector2(WINDOW_MARGIN, windowHeight));

            
        }

        private void DrawSolidWallCube(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, int wallSubmesh, Vector2 start, Vector2 dim2d)
        {
            Vector3 pos = new Vector3(start.x, start.y, 0f);
            Vector3 dim = new Vector3(dim2d.x, dim2d.y, WIDTH);
            meshBuilder.BuildCube(localCellPosition, side, wallSubmesh, pos, dim);
        }

        private void DrawGlassPane(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, int glassSubmesh, Vector2 start, Vector2 dim)
        {
            float zFrontPane = WINDOW_WIDTH / 2f;
            float zBackPane = WIDTH - WINDOW_WIDTH / 2f;

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

        private bool ShouldConnectWindowInDirection(World world, Vector3Int globalCellCoordinates, Direction side, Direction dir)
        {
            if (HelperFunctions.IsSide(dir))
            {
                Wall adjWall = world.GetWall(HelperFunctions.GetAdjacentWallCellCoordinates(globalCellCoordinates, side, dir), side);
                return (adjWall != null && adjWall.Shape.Id == WallShapeId.Window);
            }
            else
            {
                Wall adjWall = world.GetWall(HelperFunctions.GetAdjacentWallCellCoordinates(globalCellCoordinates, side, dir), side);
                bool connectPrev = ShouldConnectWindowInDirection(world, globalCellCoordinates, side, HelperFunctions.GetPreviousDirection8(dir));
                bool connectNext = ShouldConnectWindowInDirection(world, globalCellCoordinates, side, HelperFunctions.GetNextDirection8(dir));
                return (adjWall != null && adjWall.Shape.Id == WallShapeId.Window && connectPrev && connectNext);
            }
        }
    }
}
