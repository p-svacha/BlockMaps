using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S003_Concrete : Surface
    {
        public S003_Concrete(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Concrete;
        public override string Name => "Concrete";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Default;
        public override bool DoBlend => false;
        public override bool UseLongEdges => false;
        public override Color PreviewColor => ResourceManager.Singleton.TarmacColor;
        public override Texture2D BlendingTexture => ResourceManager.Singleton.ConcreteTexture;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.BuildBorderedNodeSurface(world, node, meshBuilder, ResourceManager.Singleton.Mat_ConcreteDark, ResourceManager.Singleton.Mat_Concrete2, 0.1f, 0.1f, 0.1f);
        }

        #endregion
    }
}
