using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class PRC002_UrbanBlock : Parcel
    {
        public override ParcelType Type => ParcelType.UrbanBlock;

        public PRC002_UrbanBlock(World world, Vector2Int position, Vector2Int dimensions) : base(world, position, dimensions) { }

        public override void Generate()
        {
            FillGround(SurfaceId.Street);
        }
    }
}
