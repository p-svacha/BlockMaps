using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SandSurface : Surface
    {
        public SandSurface() : base() { }

        public override SurfaceId Id => SurfaceId.Sand;
        public override string Name => "Sand";
        public override float SpeedModifier => 0.2f;
        public override Color Color => ResourceManager.Singleton.SandColor;
        public override Texture2D Texture => ResourceManager.Singleton.SandTexture;
        public override bool IsPaintable => true;
    }
}
