using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WallType : IClimbable
    {
        public abstract WallTypeId Id { get; }
        public abstract string Name { get; }
        public abstract int MaxHeight { get; }
        public abstract bool FollowSlopes { get; }
        public abstract bool BlocksVision { get; }
        public abstract Sprite PreviewSprite { get; }

        // IClimbable
        public abstract ClimbingCategory SkillRequirement { get; }
        public abstract float CostUp { get; }
        public abstract float CostDown { get; }
        public abstract float SpeedUp { get; }
        public abstract float SpeedDown { get; }
        public abstract float TransformOffset { get; }
        public int MaxClimbHeight(ClimbingCategory skill)
        {
            return skill switch
            {
                ClimbingCategory.None => 0,
                ClimbingCategory.Basic => MovingEntity.MAX_BASIC_CLIMB_HEIGHT,
                ClimbingCategory.Intermediate => MovingEntity.MAX_INTERMEDIATE_CLIMB_HEIGHT,
                ClimbingCategory.Advanced => MovingEntity.MAX_ADVANCED_CLIMB_HEIGHT,
                ClimbingCategory.Unclimbable => 0,
                _ => throw new System.Exception("category " + skill.ToString() + " not handled.")
            };
        }

        #region Draw

        public abstract void GenerateSideMesh(MeshBuilder meshBuilder, Wall wall);
        public abstract void GenerateCornerMesh(MeshBuilder meshBuilder, Wall wall);

        protected Vector3 GetWallStartPos(BlockmapNode node, Direction side, float wallWidth)
        {
            int startHeightCoordinate = node.GetMinHeight(side);
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
            List<float> heightOffsets = new List<float>() { 0f, 0f, 0f, 0f };

            // Calculate vertex height offsets if built on a slope
            bool adjustHeightsToSlope = (HelperFunctions.IsSide(wall.Side) && wall.Type.FollowSlopes);
            if (adjustHeightsToSlope)
            {
                float slope = World.TILE_HEIGHT * (wall.Node.Height[HelperFunctions.GetNextAnticlockwiseDirection8(wall.Side)] - wall.Node.Height[HelperFunctions.GetNextClockwiseDirection8(wall.Side)]);
                float startY = 0;
                if (slope < 0)
                {
                    startY = -slope;
                    slope = 0;
                }

                float startYOffset = Mathf.Lerp(startY, slope, pos.x);
                float endYOffset = Mathf.Lerp(startY, slope, pos.x + dimensions.x);
                heightOffsets[0] = startYOffset;
                heightOffsets[1] = endYOffset;
                heightOffsets[2] = endYOffset;
                heightOffsets[3] = startYOffset;
            }

            // Calculate footprint vertex positions
            List<Vector3> footprint = new List<Vector3>() {
                new Vector3(pos.x, pos.y, pos.z),
                new Vector3(pos.x + dimensions.x, pos.y, pos.z),
                new Vector3(pos.x + dimensions.x, pos.y, pos.z + dimensions.z),
                new Vector3(pos.x, pos.y, pos.z + dimensions.z),
            };

            // Translate footprint positions based on direction
            footprint = footprint.Select(x => MeshBuilder.TranslatePosition(x, wall.Side)).ToList();

            // Apply height offsets from slope
            for (int i = 0; i < 4; i++) footprint[i] += new Vector3(0f, heightOffsets[i], 0f);

            // Apply offset based on node position on chunk to footprint
            int startHeightCoordinate = wall.Node.GetMinHeight(wall.Side);
            float worldHeight = wall.Node.World.GetWorldHeight(startHeightCoordinate);
            Vector3 nodeOffsetPos = new Vector3(wall.Node.LocalCoordinates.x, worldHeight, wall.Node.LocalCoordinates.y);

            for (int i = 0; i < 4; i++) footprint[i] += nodeOffsetPos;

            // Build cube
            meshBuilder.BuildCube(submesh, footprint[0], footprint[1], footprint[2], footprint[3], dimensions.y);
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
