using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WallType
    {
        public abstract WallTypeId Id { get; }
        public abstract string Name { get; }
        public abstract bool BlocksVision { get; }
        public abstract Sprite PreviewSprite { get; }

        #region Draw

        public abstract void GenerateMesh(MeshBuilder meshBuilder, Wall wall);

        protected Vector3 GetWallStartPos(BlockmapNode node, Direction side, float wallWidth)
        {
            int startHeightCoordinate = Wall.GetWallStartY(node, side);
            float worldHeight = node.World.GetWorldHeight(startHeightCoordinate);

            return side switch
            {
                Direction.N => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y + (1f - wallWidth)),
                Direction.E => new Vector3(node.LocalCoordinates.x + (1f - wallWidth), worldHeight, node.LocalCoordinates.y),
                Direction.S => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                Direction.W => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }


        protected Vector3 GetWallDimensions(BlockmapNode node, Direction side, int height, float wallWidth)
        {
            float worldHeight = height * World.TILE_HEIGHT;

            return side switch
            {
                Direction.N => new Vector3(1f, worldHeight, wallWidth),
                Direction.E => new Vector3(wallWidth, worldHeight, 1f),
                Direction.S => new Vector3(1f, worldHeight, wallWidth),
                Direction.W => new Vector3(wallWidth, worldHeight, 1f),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }

        /// <summary>
        /// When building a cube for wall, define the values for building it to the south on 0/0 and then pass those values into this function to translate the values to the correct node and direction.
        /// </summary>
        protected void BuildCube(Wall wall, MeshBuilder meshBuilder, int submesh, Vector3 pos, Vector3 dimensions)
        {
            Vector3 translatedPos = pos;
            Vector3 translatedDimensions = dimensions;

            // Translate position and dimension based on wall side
            switch(wall.Side)
            {
                case Direction.S:
                    translatedPos = pos;
                    translatedDimensions = dimensions;
                    break;

                case Direction.W:
                    translatedPos = new Vector3(pos.z, pos.y, pos.x);
                    translatedDimensions = new Vector3(dimensions.z, dimensions.y, dimensions.x);
                    break;

                case Direction.E:
                    translatedPos = new Vector3(1 - pos.z, pos.y, 1 - pos.x);
                    translatedDimensions = new Vector3(-dimensions.z, dimensions.y, -dimensions.x);
                    break;

                case Direction.N:
                    translatedPos = new Vector3(1 - pos.x, pos.y, 1 - pos.z);
                    translatedDimensions = new Vector3(-dimensions.x, dimensions.y, -dimensions.z);
                    break;

            }

            // Apply offset based on node position on chunk
            int startHeightCoordinate = Wall.GetWallStartY(wall.Node, wall.Side);
            float worldHeight = wall.Node.World.GetWorldHeight(startHeightCoordinate);

            Vector3 nodeOffsetPos = new Vector3(wall.Node.LocalCoordinates.x, worldHeight, wall.Node.LocalCoordinates.y);
            translatedPos += nodeOffsetPos;

            // Build cube
            meshBuilder.BuildCube(submesh, translatedPos, translatedDimensions);
        }
        protected void BuildCube(Wall wall, MeshBuilder meshBuilder, int submesh, float startX, float endX, float startY, float endY, float startZ, float endZ)
        {
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dimensions = new Vector3(endX - startX, endY - startY, endZ - startZ);
            BuildCube(wall, meshBuilder, submesh, pos, dimensions);
        }

        #endregion
    }
}
