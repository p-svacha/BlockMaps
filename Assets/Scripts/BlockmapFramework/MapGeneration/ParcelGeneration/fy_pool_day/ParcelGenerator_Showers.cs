using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelGenerator_Showers : ParcelGenerator
    {
        protected override void Generate()
        {
            bool isBlue = Random.value < 0.5f;
            SurfaceDef surface = isBlue ? RetroSurfaceDefOf.fy_pool_day_TileMiniBlue : RetroSurfaceDefOf.fy_pool_day_TileMiniRed;

            // Layout
            TileType[,] layout = new TileType[Parcel.Dimensions.x, Parcel.Dimensions.y];
            for (int x = 0; x < Parcel.Dimensions.x; x++)
            {
                for (int y = 0; y < Parcel.Dimensions.y; y++)
                {
                    Vector2Int worldCoords = Parcel.GetWorldCoordinates(x, y);
                    TileType type = TileType.Floor;

                    bool isWall = false;

                    if (!IsGateway(worldCoords) && IsBorder(worldCoords))
                    {
                        isWall = true;
                    }

                    if (isWall) type = TileType.Wall;

                    layout[x, y] = type;
                }
            }

            // Place floor
            for (int x = 0; x < Parcel.Dimensions.x; x++)
            {
                for (int y = 0; y < Parcel.Dimensions.y; y++)
                {
                    Vector2Int worldCoords = Parcel.GetWorldCoordinates(x, y);
                    TileType type = layout[x, y];

                    if (type == TileType.Floor) World.BuildAirNode(worldCoords, ParcelWorldGenerator_Pool.BASE_ALTITUDE, surface, updateWorld: false);
                }
            }

            // Place walls
            for (int x = 0; x < Parcel.Dimensions.x; x++)
            {
                for (int y = 0; y < Parcel.Dimensions.y; y++)
                {
                    Vector2Int worldCoords = Parcel.GetWorldCoordinates(x, y);

                    // Check type of each adjacent tile. If void, place an outside wall (on void tile).
                    TileType ownType = layout[x, y];
                    if (ownType == TileType.Wall) continue;

                    foreach (Direction dir in HelperFunctions.GetSides())
                    {
                        Vector2Int adjCoords = HelperFunctions.GetCoordinatesInDirection(new Vector2Int(x, y), dir);

                        if (IsGateway(worldCoords) && GetGateways(worldCoords).Any(x => x.Side == dir)) continue; // No walls on outgoing gateways

                        TileType neighborType = HelperFunctions.InBounds(adjCoords, layout) ? layout[adjCoords.x, adjCoords.y] : TileType.Wall;

                        // Neighbour is wall and we are not --> build wall
                        if (neighborType == TileType.Wall)
                        {
                            Vector2Int worldAdjCoords = Parcel.GetWorldCoordinates(adjCoords);

                            Vector3Int cellCoords = new Vector3Int(worldAdjCoords.x, ParcelWorldGenerator_Pool.BASE_ALTITUDE, worldAdjCoords.y);

                            // Get wall material by tile type
                            WallMaterialDef wallMat = isBlue ? RetroWallMaterialDefOf.fy_pool_day_TileMiniBlue : RetroWallMaterialDefOf.fy_pool_day_TileMiniRed;

                            World.BuildWalls(cellCoords, HelperFunctions.GetOppositeDirection(dir), WallShapeDefOf.Solid, wallMat, mirrored: false, height: ParcelWorldGenerator_Pool.WALL_HEIGHT, updateWorld: false);
                        }
                    }
                }
            }

        }

        private enum TileType
        {
            Floor,
            Wall
        }
    }
}
