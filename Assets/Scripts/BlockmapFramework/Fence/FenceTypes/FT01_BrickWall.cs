using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class FT01_BrickWall : FenceType
    {
        private const float WIDTH = 0.1f;

        public override FenceTypeId Id => FenceTypeId.BrickWall;
        public override string Name => "Brick Wall";
        public override int MaxHeight => World.MAX_ALTITUDE;
        public override bool CanBuildOnCorners => true;
        public override bool BlocksVision => true;

        // IClimbable
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Intermediate;
        public override float ClimbCostUp => 2.5f;
        public override float ClimbCostDown => 1.5f;
        public override float ClimbSpeedUp => 0.3f;
        public override float ClimbSpeedDown => 0.4f;
        public override float Width => WIDTH;


        public override void GenerateSideMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(MaterialManager.LoadMaterial("Brick"), isPreview));

            float startX = 0;
            float dimX = 1f;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * height;
            float startZ = 0f;
            float dimZ = WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(node, side, submesh, pos, dim, adjustToNodeSlope: true);
        }
        public override void GenerateCornerMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(MaterialManager.LoadMaterial("Brick"), isPreview));

            float startX = 0;
            float dimX = WIDTH;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * height;
            float startZ = 0f;
            float dimZ = WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(node, side, submesh, pos, dim, adjustToNodeSlope: true);
        }
    }
}
