using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class WorldGenerator_PoolDay : WorldGenerator
    {
        public override string Label => "fy_pool_day";
        public override string Description => "A generator to create maps based on the classic CS 1.6 map fy_pool_day.";
        public override bool StartAsVoid => true;

        private const int WORLD_EDGE_MARGIN = 5; // cells from map edge
        private const int BASE_ALTITUDE = 10;

        private int WallHeight;
        private TileType[,] Layout;

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>()
            {
                GenerateLayout,
                PlaceFloorNodes,
                PlaceWalls,
            };
        }

        /// <summary>
        /// Creates a 2D grid of the whole map, assigning each tile an id defining what will be on it.
        /// </summary>
        private void GenerateLayout()
        {
            WallHeight = Random.Range(5, 7 + 1);

            int showerAreaWidth = Random.Range(4, 7 + 1);
            int miniPoolAreaWidth = Random.Range(6, 10 + 1);
            int centralAreaWidth = Random.Range(15, 25 + 1);

            int wardrobeLength = Random.Range(10, 16 + 1);
            int centralAreaLength = Random.Range(12, 20 + 1);

            int fullLengthZ = 2 * wardrobeLength + centralAreaLength;
            int halfwayZ = fullLengthZ / 2;

            Layout = new TileType[WorldSize, WorldSize];
            for(int x = 0; x < WorldSize; x++)
            {
                for(int z = 0; z < WorldSize; z++)
                {
                    TileType type = TileType.Void;

                    if (x < showerAreaWidth)
                    {
                        if (z < halfwayZ) type = TileType.ShowerAreaBlue;
                        else if (z < fullLengthZ) type = TileType.ShowerAreaRed;
                    }

                    else if (x < showerAreaWidth + centralAreaWidth)
                    {
                        if (z < wardrobeLength) type = TileType.WardrobeBlue;
                        else if (z < wardrobeLength + centralAreaLength) type = TileType.CentralPoolArea;
                        else if (z < wardrobeLength + centralAreaLength + wardrobeLength) type = TileType.WardrobeRed;
                    }

                    else if (x < showerAreaWidth + centralAreaWidth + miniPoolAreaWidth)
                    {
                        if (z < wardrobeLength) type = TileType.WardrobeBlue;
                        else if (z < wardrobeLength + centralAreaLength) type = TileType.MiniPoolArea;
                        else if (z < wardrobeLength + centralAreaLength + wardrobeLength) type = TileType.WardrobeRed;
                    }

                    Layout[x, z] = type;
                }
            }
        }

        /// <summary>
        /// Places all nodes making up the floor of the map.
        /// </summary>
        private void PlaceFloorNodes()
        {
            for (int x = 0; x < WorldSize; x++)
            {
                for (int z = 0; z < WorldSize; z++)
                {
                    int worldX = x + WORLD_EDGE_MARGIN;
                    int worldZ = z + WORLD_EDGE_MARGIN;

                    TileType type = Layout[x, z];
                    GroundNode groundNode = World.GetGroundNode(worldX, worldZ);

                    if (type == TileType.Void) continue;
                    else if (type == TileType.ShowerAreaBlue) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileMiniBlue, BASE_ALTITUDE);
                    else if (type == TileType.ShowerAreaRed) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileMiniRed, BASE_ALTITUDE);
                    else if (type == TileType.WardrobeBlue) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileBlue, BASE_ALTITUDE);
                    else if (type == TileType.WardrobeRed) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileRed, BASE_ALTITUDE);
                    else if (type == TileType.CentralPoolArea) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileWhite, BASE_ALTITUDE);
                    else if (type == TileType.MiniPoolArea) groundNode.UnsetAsVoid(RetroSurfaceDefOf.fy_pool_day_TileWhite, BASE_ALTITUDE);
                }
            }
        }

        /// <summary>
        /// Places walls.
        /// </summary>
        private void PlaceWalls()
        {
            for (int x = 0; x < WorldSize; x++)
            {
                for (int z = 0; z < WorldSize; z++)
                {
                    int realX = x + WORLD_EDGE_MARGIN;
                    int realZ = z + WORLD_EDGE_MARGIN;

                    // Check type of each adjacent tile. If void, place an outside wall (on void tile).
                    TileType ownType = Layout[x, z];
                    if (ownType == TileType.Void) continue;

                    foreach (Direction dir in HelperFunctions.GetSides())
                    {
                        Vector2Int adjCoords = HelperFunctions.GetCoordinatesInDirection(new Vector2Int(x, z), dir);
                        TileType neighborType = HelperFunctions.InBounds(adjCoords, Layout) ? Layout[adjCoords.x, adjCoords.y] : TileType.Void;

                        // Neighbour is void and we are not --> wall
                        if (neighborType == TileType.Void)
                        {
                            Vector3Int cellCoords = new Vector3Int(adjCoords.x + WORLD_EDGE_MARGIN, BASE_ALTITUDE, adjCoords.y + WORLD_EDGE_MARGIN);

                            // Get wall material by tile type
                            WallMaterialDef wallMat = WallMaterialDefOf.WoodPlanks;
                            if (ownType == TileType.ShowerAreaBlue) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileMiniBlue;
                            if (ownType == TileType.ShowerAreaRed) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileMiniRed;
                            if (ownType == TileType.CentralPoolArea) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileWhite;
                            if (ownType == TileType.MiniPoolArea) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileWhite;
                            if (ownType == TileType.WardrobeRed) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileRed;
                            if (ownType == TileType.WardrobeBlue) wallMat = RetroWallMaterialDefOf.fy_pool_day_TileBlue;

                            World.BuildWalls(cellCoords, HelperFunctions.GetOppositeDirection(dir), WallShapeDefOf.Solid, wallMat, mirrored: false, height: WallHeight, updateWorld: false);
                        }
                    }
                }
            }
        }


        private enum TileType
        {
            Void,
            Pool,
            CentralPoolArea,
            ShowerAreaBlue,
            ShowerAreaRed,
            MiniPoolArea,
            Wall,
            WardrobeBlue,
            WardrobeRed,
        }
    }
}
