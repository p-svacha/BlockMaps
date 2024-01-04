using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WallMeshBuilder
    {
        private const float WALL_WIDTH = 0.1f;

        public static void DrawWall(MeshBuilder meshBuilder, Wall wall)
        {
            switch (wall.Type.Id)
            {
                case "brickWall":
                    int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.BrickWallMaterial);
                    meshBuilder.BuildCube(submesh, GetWallStartPos(wall.Node, wall.Side), GetWallDimensions(wall.Node, wall.Side, wall.Height));
                    break;
            }
        }

        private static Vector3 GetWallStartPos(BlockmapNode node, Direction side)
        {
            List<Direction> relevantCorners = HelperFunctions.GetAffectedCorners(side);
            int startHeightCoordinate = node.Height.Where(x => relevantCorners.Contains(x.Key)).Min(x => x.Value);
            float worldHeight = node.World.GetWorldHeight(startHeightCoordinate);

            return side switch
            {
                Direction.N => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y + (1f - WALL_WIDTH)),
                Direction.E => new Vector3(node.LocalCoordinates.x + (1f - WALL_WIDTH), worldHeight, node.LocalCoordinates.y),
                Direction.S => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                Direction.W => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }

        private static Vector3 GetWallDimensions(BlockmapNode node, Direction side, int height)
        {
            float worldHeight = height * World.TILE_HEIGHT;

            return side switch
            {
                Direction.N => new Vector3(1f, worldHeight, WALL_WIDTH),
                Direction.E => new Vector3(WALL_WIDTH, worldHeight, 1f),
                Direction.S => new Vector3(1f, worldHeight, WALL_WIDTH),
                Direction.W => new Vector3(WALL_WIDTH, worldHeight, 1f),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }
    }
}
