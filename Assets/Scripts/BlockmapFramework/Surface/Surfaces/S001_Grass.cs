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
        public override Color Color => ResourceManager.Singleton.GrassColor;
        public override Texture2D Texture => ResourceManager.Singleton.GrassTexture;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.DrawStandardSurface(node, meshBuilder);
        }

        #endregion
    }
}
