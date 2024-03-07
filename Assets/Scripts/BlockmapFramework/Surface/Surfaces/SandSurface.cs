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
        public override SurfaceProperties Properties => SurfaceManager.Instance.GetSurfaceProperties(SurfacePropertyId.Sand);
        public override Color Color => ResourceManager.Singleton.SandColor;
        public override Texture2D Texture => ResourceManager.Singleton.SandTexture;
    }
}
