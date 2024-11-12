using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class FT03_Cliff : FenceType
    {
        public override FenceTypeId Id => FenceTypeId.Cliff;
        public override string Name => "Cliff Wall";
        public override int MaxHeight => World.MAX_ALTITUDE;
        public override bool CanBuildOnCorners => false;
        public override bool BlocksVision => true;

        // IClimbable
        public override ClimbingCategory ClimbSkillRequirement => Cliff.Instance.ClimbSkillRequirement;
        public override float ClimbCostUp => Cliff.Instance.ClimbCostUp;
        public override float ClimbCostDown => Cliff.Instance.ClimbCostDown;
        public override float ClimbSpeedUp => Cliff.Instance.ClimbSpeedUp;
        public override float ClimbSpeedDown => Cliff.Instance.ClimbSpeedDown;
        public override float Width => 0f;


        public override void GenerateSideMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(MaterialManager.LoadMaterial("Cliff"), isPreview));

            Vector3 p1 = new Vector3(0f, 0f, 0f);
            Vector3 p2 = new Vector3(0f, World.TILE_HEIGHT * height, 0f);
            Vector3 p3 = new Vector3(1f, World.TILE_HEIGHT * height, 0f);
            Vector3 p4 = new Vector3(1f, 0f, 0f);

            meshBuilder.BuildPlane(node, side, submesh, p1, p2, p3, p4, adjustToNodeSlope: true);
            meshBuilder.BuildPlane(node, side, submesh, p1, p2, p3, p4, adjustToNodeSlope: true, mirror: true);
        }
        public override void GenerateCornerMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview) { } // can't build on corners
    }
}
