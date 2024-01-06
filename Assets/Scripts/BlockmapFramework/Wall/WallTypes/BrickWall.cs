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
        public override void GenerateMesh(MeshBuilder meshBuilder, Wall wall)
        {
            int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.BrickWallMaterial);
            meshBuilder.BuildCube(submesh, GetWallStartPos(wall.Node, wall.Side, WALL_WIDTH), GetWallDimensions(wall.Node, wall.Side, wall.Height, WALL_WIDTH));
        }
    }
}
