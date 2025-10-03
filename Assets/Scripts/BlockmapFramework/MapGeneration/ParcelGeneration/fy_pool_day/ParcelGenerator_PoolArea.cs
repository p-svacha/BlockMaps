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
        }
    }
}
