using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class S005_RoofingTiles : Surface
    {
        public S005_RoofingTiles(SurfaceManager sm) : base(sm) { }

        public override SurfaceId Id => SurfaceId.RoofingTiles;
        public override string Name => "Roofing Tiles";
        public override SurfacePropertyId PropertiesId => SurfacePropertyId.Default;
        public override bool DoBlend => false;
        public override bool UseLongEdges => true;
        public override Color Color => ResourceManager.Singleton.Mat_Brick.color;

        #region Draw

        public override void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.Mat_RoofingTiles);
            meshBuilder.DrawShapePlane(node, submesh, height: 0f, 0f, 1f, 0f, 1f);
        }

        #endregion
    }
}
