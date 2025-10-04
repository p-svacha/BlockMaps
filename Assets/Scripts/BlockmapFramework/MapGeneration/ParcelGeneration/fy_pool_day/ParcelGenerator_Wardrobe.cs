using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelGenerator_Wardrobe : ParcelGenerator
    {
        private bool IsBlue;

        protected override void Generate()
        {
            IsBlue = Random.value < 0.5f;
            SurfaceDef surface = IsBlue ? RetroSurfaceDefOf.fy_pool_day_TileBlue : RetroSurfaceDefOf.fy_pool_day_TileRed;

            CreateGround(ParcelWorldGenerator_Pool.BASE_ALTITUDE, surface);

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
                        WallMaterialDef wallMat = IsBlue ? RetroWallMaterialDefOf.fy_pool_day_TileBlue : RetroWallMaterialDefOf.fy_pool_day_TileRed;
                        World.BuildWalls(cellCoords, side, WallShapeDefOf.Solid, wallMat, mirrored: false, height: ParcelWorldGenerator_Pool.WALL_HEIGHT, updateWorld: false);
                    }
                }
            }
        }
    }
}
