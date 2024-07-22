using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WS002_Corner : WallShape
    {
        private const float WIDTH = 0.1f;

        public override WallShapeId Id => WallShapeId.Corner;
        public override string Name => "Corner";
        public override bool BlocksVision => true;
        public override bool IsClimbable => false;
        public override float Width => WIDTH;
        public override List<Direction> ValidSides => HelperFunctions.GetCorners();

        public override void GenerateMesh(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = 0;
            float dimX = WIDTH;
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
