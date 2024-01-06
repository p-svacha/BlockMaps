using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class BrickWall : WallType
    {
        public override WallTypeId Id => WallTypeId.BrickWall;
        public override string Name => "Brick Wall";
        public override int MaxHeight => World.MAX_HEIGHT;
        public override bool BlocksVision => true;
        public override Sprite PreviewSprite => ResourceManager.Singleton.BrickWallSprite;


        private const float WALL_WIDTH = 0.1f;
        public override void GenerateSideMesh(MeshBuilder meshBuilder, Wall wall)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.BrickWallMaterial);

            float startX = 0;
            float dimX = 1f;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * wall.Height;
            float startZ = 0f;
            float dimZ = WALL_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            BuildCube(wall, meshBuilder, submesh, pos, dim);
        }
        public override void GenerateCornerMesh(MeshBuilder meshBuilder, Wall wall)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.BrickWallMaterial);

            float startX = 0;
            float dimX = WALL_WIDTH;
            float startY = 0f;
            float dimY = World.TILE_HEIGHT * wall.Height;
            float startZ = 0f;
            float dimZ = WALL_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            BuildCube(wall, meshBuilder, submesh, pos, dim);
        }
    }
}
