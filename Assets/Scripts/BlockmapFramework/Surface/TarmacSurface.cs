using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class TarmacSurface : Surface
    {
        public TarmacSurface() : base() { }

        public override SurfaceId Id => SurfaceId.Tarmac;
        public override string Name => "Tarmac";
        public override float SpeedModifier => 0.9f;
        public override Color Color => ResourceManager.Singleton.TarmacColor;
        public override Texture2D Texture => ResourceManager.Singleton.TarmacTexture;
    }
}
