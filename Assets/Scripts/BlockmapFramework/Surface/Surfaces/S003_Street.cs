using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S003_Street : Surface
    {
        public S003_Street(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Path;
        public override string Name => "Path";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Tarmac;
        public override bool DoBlend => false;
        public override Color Color => ResourceManager.Singleton.TarmacColor;
        public override Texture2D Texture => ResourceManager.Singleton.ConcreteTexture;

        #region Draw

        public override void DrawNodeSurface(BlockmapNode node, MeshBuilder meshBuilder)
        {
            DefaultPathMeshBuilder.BuildPath(node, meshBuilder);
        }

        #endregion
    }
}
