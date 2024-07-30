using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S001_Grass : Surface
    {
        public S001_Grass(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Grass;
        public override string Name => "Grass";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Grass;
        public override bool DoBlend => true;
        public override bool UseLongEdges => false;
        public override Color PreviewColor => ResourceManager.Singleton.GrassColor;
        public override Texture2D BlendingTexture => ResourceManager.Singleton.GrassTexture;
        public override float BlendingTextureScale => 0.2f;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.DrawStandardSurface(node, meshBuilder);
        }

        #endregion
    }
}
