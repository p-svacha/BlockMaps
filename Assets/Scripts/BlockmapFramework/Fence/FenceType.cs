using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class FenceType
    {
        public abstract FenceTypeId Id { get; }
        public abstract string Name { get; }
        public virtual int MaxHeight => World.MAX_ALTITUDE;
        public abstract bool CanBuildOnCorners { get; }
        public abstract bool BlocksVision { get; }
        public Sprite PreviewSprite => Resources.Load<Sprite>("Editor/Thumbnails/Fences/" + Id.ToString());

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
            if (isPreview) return MaterialManager.BuildPreviewMaterial;
            else return mat;
        }

        #endregion
    }
}
