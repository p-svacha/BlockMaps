using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class PRC001_Park : Parcel
    {
        public override ParcelType Type => ParcelType.Park;

        public PRC001_Park(World world, Vector2Int position, Vector2Int dimensions) : base(world, position, dimensions) { }

        public override void Generate()
        {
            FillGround(SurfaceDefOf.Grass);
        }
    }
}
