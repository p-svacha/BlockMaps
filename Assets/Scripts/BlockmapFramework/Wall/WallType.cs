using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WallType
    {
        public abstract WallTypeId Id { get; }
        public abstract string Name { get; }
        public abstract int MaxHeight { get; }
        public abstract bool FollowSlopes { get; }
        public abstract bool BlocksVision { get; }
        public abstract Sprite PreviewSprite { get; }

        #region Draw

        public abstract void GenerateSideMesh(MeshBuilder meshBuilder, Wall wall);
        public abstract void GenerateCornerMesh(MeshBuilder meshBuilder, Wall wall);

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
        /// When building a cube for wall, define the values for building it from the south-west corner on 0/0 and then pass those values into this function to translate the values to the correct node and direction.
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

                case Direction.SW:
                    translatedPos = pos;
                    translatedDimensions = dimensions;
                    break;

                case Direction.SE:
                    translatedPos = new Vector3(1 - dimensions.x, pos.y, pos.z);
                    translatedDimensions = dimensions;
                    break;

                case Direction.NE:
                    translatedPos = new Vector3(1 - pos.x, pos.y, 1 - pos.z);
                    translatedDimensions = new Vector3(-dimensions.x, dimensions.y, -dimensions.z);
                    break;

                case Direction.NW:
                    translatedPos = new Vector3(pos.x, pos.y, 1 - dimensions.z);
                    translatedDimensions = dimensions;
                    break;

            }

            // Apply offset based on node position on chunk
            int startHeightCoordinate = Wall.GetWallStartY(wall.Node, wall.Side);
            float worldHeight = wall.Node.World.GetWorldHeight(startHeightCoordinate);

            Vector3 nodeOffsetPos = new Vector3(wall.Node.LocalCoordinates.x, worldHeight, wall.Node.LocalCoordinates.y);
            translatedPos += nodeOffsetPos;

            // Build cube
            if(HelperFunctions.IsSide(wall.Side) &&  wall.Type.FollowSlopes) // Adjust for slope
            {
                float slope = World.TILE_HEIGHT * (wall.Node.Height[HelperFunctions.GetNextAnticlockwiseDirection8(wall.Side)] - wall.Node.Height[HelperFunctions.GetNextClockwiseDirection8(wall.Side)]);
                float startY = 0;
                if (slope < 0)
                {
                    startY = -slope;
                    slope = 0;
                }
                Debug.Log(slope);

                Vector3 vb1 = new Vector3(translatedPos.x, translatedPos.y, translatedPos.z);
                Vector3 vb2 = new Vector3(translatedPos.x + translatedDimensions.x, translatedPos.y, translatedPos.z);
                Vector3 vb3 = new Vector3(translatedPos.x + translatedDimensions.x, translatedPos.y, translatedPos.z + translatedDimensions.z);
                Vector3 vb4 = new Vector3(translatedPos.x, translatedPos.y, translatedPos.z + translatedDimensions.z);

                if (wall.Side == Direction.S || wall.Side == Direction.N)
                {
                    vb1 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.x), 0f);
                    vb2 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.x + dimensions.x), 0f);
                    vb3 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.x + dimensions.x), 0f);
                    vb4 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.x), 0f);
                }
                if (wall.Side == Direction.E || wall.Side == Direction.W)
                {
                    vb1 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.z), 0f);
                    vb2 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.z + dimensions.z), 0f);
                    vb3 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.z + dimensions.z), 0f);
                    vb4 += new Vector3(0f, Mathf.Lerp(startY, slope, pos.z), 0f);
                }

                meshBuilder.BuildCube(submesh, vb1, vb2, vb3, vb4, dimensions.y);
            }
            else meshBuilder.BuildCube(submesh, translatedPos, translatedDimensions);
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
