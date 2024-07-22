using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BlockmapFramework
{
    public class S007_Tiles : Surface
    {
        public S007_Tiles(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.Tiles;
        public override string Name => "Tiles";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Default;
        public override bool DoBlend => false;
        public override bool UseLongEdges => false;
        public override Color Color => ResourceManager.Singleton.Mat_TilesWhite.color;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.Mat_TilesWhite);
            meshBuilder.DrawShapePlane(node, submesh, height: 0f, 0f, 1f, 0f, 1f);
        }

        #endregion
    }
}
