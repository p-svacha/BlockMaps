using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WS001_Solid : WallShape
    {
        private const float WIDTH = 0.1f;

        public override WallShapeId Id => WallShapeId.Solid;
        public override string Name => "Solid";
        public override bool BlocksVision => true;
        public override float Width => WIDTH;

        public override void GenerateMesh(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, Material material)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = 0;
            float dimX = 1f;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT;
            float startZ = 0f;
            float dimZ = WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(localCellPosition, side, submesh, pos, dim);
        }
    }
}
