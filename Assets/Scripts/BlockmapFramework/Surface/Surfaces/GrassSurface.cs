using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class GrassSurface : Surface
    {
        public GrassSurface() : base() { }

        public override SurfaceId Id => SurfaceId.Grass;
        public override string Name => "Grass";
        public override SurfaceProperties Properties => SurfaceManager.Instance.GetSurfaceProperties(SurfacePropertyId.Grass);
        public override Color Color => ResourceManager.Singleton.GrassColor;
        public override Texture2D Texture => ResourceManager.Singleton.GrassTexture;
    }
}
