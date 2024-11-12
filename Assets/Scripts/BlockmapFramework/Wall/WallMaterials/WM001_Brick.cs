using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WM001_Brick : WallMaterial
    {
        public override WallMaterialId Id => WallMaterialId.Brick;
        public override string Name => "Brick";
        public override Material Material => MaterialManager.LoadMaterial("Brick");

        // Climbing attributes
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Intermediate;
        public override float ClimbCostUp => 2.5f;
        public override float ClimbCostDown => 1.5f;
        public override float ClimbSpeedUp => 0.3f;
        public override float ClimbSpeedDown => 0.4f;
    }
}
