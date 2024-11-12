using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WM002_Plaster : WallMaterial
    {
        public override WallMaterialId Id => WallMaterialId.Plaster;
        public override string Name => "Plaster";
        public override Material Material => MaterialManager.LoadMaterial("Plaster");

        // Climbing attributes
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Unclimbable;
        public override float ClimbCostUp => 0f;
        public override float ClimbCostDown => 0f;
        public override float ClimbSpeedUp => 0f;
        public override float ClimbSpeedDown => 0f;
    }
}
