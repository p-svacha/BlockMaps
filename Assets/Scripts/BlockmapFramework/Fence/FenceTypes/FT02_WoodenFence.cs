using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class FT02_WoodenFence : FenceType
    {
        public override FenceTypeId Id => FenceTypeId.WoodenFence;
        public override string Name => "Wooden Fence";
        public override int MaxHeight => 1;
        public override bool FollowSlopes => true;
        public override bool CanBuildOnCorners => true;
        public override bool BlocksVision => false;


        // IClimbable
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Basic;
        public override float ClimbCostUp => 1.8f;
        public override float ClimbCostDown => 1.1f;
        public override float ClimbSpeedUp => 0.8f;
        public override float ClimbSpeedDown => 0.9f;
        public override float Width => POLE_WIDTH;

        #region Draw

        private const float POLE_WIDTH = 0.1f;
        private const int NUM_POLES = 2;
        private const float POLE_HEIGHT = 0.4f;

        private const float CROSS_BRACE_START_Y = 0.2f;
        private const float CROSS_BRACE_HEIGHT = 0.1f;
        private const float CROSS_BRACE_WIDTH = 0.05f;

        public override void GenerateSideMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(ResourceManager.Singleton.Mat_Wood, isPreview));

            // Poles
            float poleStep = 1f / NUM_POLES;
            for(int i = 0; i < NUM_POLES; i++)
            {
                float startX = (poleStep / 2f) + (i * poleStep - (POLE_WIDTH / 2f));
                float dimX = POLE_WIDTH;
                float startY = 0f;
                float dimY = POLE_HEIGHT;
                float startZ = 0f;
                float dimZ = POLE_WIDTH;
                Vector3 polePos = new Vector3(startX, startY, startZ);
                Vector3 poleDims = new Vector3(dimX, dimY, dimZ);
                meshBuilder.BuildCube(node, side, submesh, polePos, poleDims, FollowSlopes);
            }

            // Cross brace
            float cb_x = 0f;
            float cb_dimX = 1f;
            float cb_y = CROSS_BRACE_START_Y;
            float cb_dimY = CROSS_BRACE_HEIGHT;
            float cb_z = (POLE_WIDTH - CROSS_BRACE_WIDTH) / 2f;
            float cb_dimZ = CROSS_BRACE_WIDTH;
            Vector3 cbPos = new Vector3(cb_x, cb_y, cb_z);
            Vector3 cbDims = new Vector3(cb_dimX, cb_dimY, cb_dimZ);
            meshBuilder.BuildCube(node, side, submesh, cbPos, cbDims, FollowSlopes);
        }
        public override void GenerateCornerMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(ResourceManager.Singleton.Mat_Wood, isPreview));

            float startX = 0;
            float dimX = POLE_WIDTH;
            float startY = 0f;
            float dimY = POLE_HEIGHT;
            float startZ = 0f;
            float dimZ = POLE_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(node, side, submesh, pos, dim, FollowSlopes);
        }

        #endregion
    }
}
