using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WallType
    {
        public abstract WallTypeId Id { get; }
        public abstract string Name { get; }
        public abstract int MaxHeight { get; }
        public abstract bool FollowSlopes { get; }
        public abstract bool CanBuildOnCorners { get; }
        public abstract bool BlocksVision { get; }
        public abstract Sprite PreviewSprite { get; }

        // Climbing attributes
        public abstract ClimbingCategory ClimbSkillRequirement { get; }
        public abstract float ClimbCostUp { get; }
        public abstract float ClimbCostDown { get; }
        public abstract float ClimbSpeedUp { get; }
        public abstract float ClimbSpeedDown { get; }
        public abstract float Width { get; }

        #region Draw

        public abstract void GenerateSideMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview);
        public abstract void GenerateCornerMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview);

        protected Material GetMaterial(Material mat, bool isPreview)
        {
            if (isPreview) return ResourceManager.Singleton.BuildPreviewMaterial;
            else return mat;
        }

        #endregion
    }
}
