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
        public override SurfaceProperties Properties => SurfaceManager.Instance.GetSurfaceProperties(SurfacePropertyId.Tarmac);
        public override Color Color => ResourceManager.Singleton.TarmacColor;
        public override Texture2D Texture => ResourceManager.Singleton.TarmacTexture;
    }
}
