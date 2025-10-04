using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelGenerator_PoolArea : ParcelGenerator
    {
        protected override void Generate()
        {
            CreateGround(ParcelWorldGenerator_Pool.BASE_ALTITUDE, RetroSurfaceDefOf.fy_pool_day_TileWhite);

            BuildOutsideWalls();

        }

        private void BuildOutsideWalls()
        {
            foreach (Direction side in HelperFunctions.GetSides())
            {
                foreach (Vector2Int coord in Parcel.GetBorderCoordinates(side))
                {
                    if (!IsGateway(coord))
                    {
                        Vector3Int cellCoords = new Vector3Int(coord.x, ParcelWorldGenerator_Pool.BASE_ALTITUDE, coord.y);
                        World.BuildWalls(cellCoords, side, WallShapeDefOf.Solid, RetroWallMaterialDefOf.fy_pool_day_TileWhite, mirrored: false, height: ParcelWorldGenerator_Pool.WALL_HEIGHT, updateWorld:false);
                    }
                }
            }
        }
    }
}
