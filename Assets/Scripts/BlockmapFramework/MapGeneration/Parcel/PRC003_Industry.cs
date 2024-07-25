using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class PRC003_Industry : Parcel
    {
        public override ParcelType Type => ParcelType.Industry;

        public PRC003_Industry(World world, Vector2Int position, Vector2Int dimensions) : base(world, position, dimensions) { }

        public override void Generate()
        {
            FillGround(SurfaceId.Tiles);
        }
    }
}
