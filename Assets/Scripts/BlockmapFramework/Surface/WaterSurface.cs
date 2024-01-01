using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WaterSurface : Surface
    {
        public WaterSurface() : base() { }

        public override SurfaceId Id => SurfaceId.Water;
        public override string Name => "Water";
        public override float SpeedModifier => 0.5f;
        public override Color Color => ResourceManager.Singleton.WaterColor;
        public override Texture2D Texture => ResourceManager.Singleton.WaterTexture;
    }
}
