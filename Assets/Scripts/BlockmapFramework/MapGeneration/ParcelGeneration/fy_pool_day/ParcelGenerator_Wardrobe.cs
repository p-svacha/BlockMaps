using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelGenerator_Wardrobe : ParcelGenerator
    {
        protected override void Generate()
        {
            SurfaceDef surface = Random.value < 0.5f ? RetroSurfaceDefOf.fy_pool_day_TileBlue : RetroSurfaceDefOf.fy_pool_day_TileRed;

            CreateGround(ParcelWorldGenerator_Pool.BASE_ALTITUDE, surface);
        }
    }
}
