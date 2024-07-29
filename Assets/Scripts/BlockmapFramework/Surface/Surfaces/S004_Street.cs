using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S004_Street : Surface
    {
        public S004_Street(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Street;
        public override string Name => "Street";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Default;
        public override bool DoBlend => false;
        public override bool UseLongEdges => false;
        public override Color PreviewColor => ResourceManager.Singleton.TarmacColor;
        public override Texture2D BlendingTexture => ResourceManager.Singleton.ConcreteTexture;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.BuildBorderedNodeSurface(world, node, meshBuilder, ResourceManager.Singleton.Mat_Asphalt, ResourceManager.Singleton.Mat_Cobblestone, 0.05f, 0.05f, 0.2f);
        }

        #endregion
    }
}

