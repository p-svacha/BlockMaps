using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class ParcelGenerator_Showers : ParcelGenerator
    {
        protected override void Generate()
        {
            SurfaceDef surface = Random.value < 0.5f ? RetroSurfaceDefOf.fy_pool_day_TileMiniBlue : RetroSurfaceDefOf.fy_pool_day_TileMiniRed;

            CreateGround(ParcelWorldGenerator_Pool.BASE_ALTITUDE, surface);
        }
    }
}
