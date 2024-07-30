using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S008_DirtPath : Surface
    {
        public S008_DirtPath(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.DirtPath;
        public override string Name => "Dirt Path";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.DirtPath;
        public override bool DoBlend => true;
        public override bool UseLongEdges => false;
        public override Color PreviewColor => ResourceManager.Singleton.Mat_DirtPath.color;
        public override Texture2D BlendingTexture => (Texture2D)ResourceManager.Singleton.Mat_DirtPath.mainTexture;
        public override float BlendingTextureScale => ResourceManager.Singleton.Mat_DirtPath.GetFloat("_TextureScale");

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.SurfaceMaterial);
            meshBuilder.DrawShapePlane(node, submesh, height: 0f, 0f, 1f, 0f, 1f);
        }

        #endregion
    }
}
