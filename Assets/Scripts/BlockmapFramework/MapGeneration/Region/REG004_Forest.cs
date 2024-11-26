using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class REG004_Forest : Region
    {
        public override ParcelType Type => ParcelType.Industry;

        public REG004_Forest(World world, Vector2Int position, Vector2Int dimensions) : base(world, position, dimensions) { }

        public override void Generate()
        {
            FillGround(SurfaceDefOf.Sand);
        }
    }
}
