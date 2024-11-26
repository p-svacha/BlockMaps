using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class REG002_UrbanBlock : Region
    {
        public override ParcelType Type => ParcelType.UrbanBlock;

        public REG002_UrbanBlock(World world, Vector2Int position, Vector2Int dimensions) : base(world, position, dimensions) { }

        public override void Generate()
        {
            FillGround(SurfaceDefOf.Street);
        }
    }
}
