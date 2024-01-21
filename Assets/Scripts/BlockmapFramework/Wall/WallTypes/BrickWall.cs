using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class BrickWall : WallType
    {
        private const float WALL_WIDTH = 0.1f;

        public override WallTypeId Id => WallTypeId.BrickWall;
        public override string Name => "Brick Wall";
        public override int MaxHeight => World.MAX_HEIGHT;
        public override bool FollowSlopes => false;
        public override bool BlocksVision => true;
        public override Sprite PreviewSprite => ResourceManager.Singleton.BrickWallSprite;

        // IClimbable
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Intermediate;
        public override float ClimbCostUp => 2.5f;
        public override float ClimbCostDown => 1.5f;
        public override float ClimbSpeedUp => 0.3f;
        public override float ClimbSpeedDown => 0.4f;
        public override float Width => WALL_WIDTH;


        public override void GenerateSideMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(isPreview));

            float startX = 0;
            float dimX = 1f;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * height;
            float startZ = 0f;
            float dimZ = WALL_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            BuildCube(node, side, meshBuilder, submesh, pos, dim);
        }
        public override void GenerateCornerMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(isPreview));

            float startX = 0;
            float dimX = WALL_WIDTH;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * height;
            float startZ = 0f;
            float dimZ = WALL_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            BuildCube(node, side, meshBuilder, submesh, pos, dim);
        }

        private Material GetMaterial(bool isPreview)
        {
            if (isPreview) return ResourceManager.Singleton.BuildPreviewMaterial;
            else return ResourceManager.Singleton.BrickWallMaterial;
        }
    }
}
