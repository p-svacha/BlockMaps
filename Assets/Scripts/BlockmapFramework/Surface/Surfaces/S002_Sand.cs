using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S002_Sand : Surface
    {
        public S002_Sand(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Sand;
        public override string Name => "Sand";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Sand;
        public override bool DoBlend => true;
        public override Color Color => ResourceManager.Singleton.SandColor;
        public override Texture2D Texture => ResourceManager.Singleton.SandTexture;


        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            NodeMeshGenerator.DrawStandardSurface(node, meshBuilder);
        }

        #endregion
    }
}
