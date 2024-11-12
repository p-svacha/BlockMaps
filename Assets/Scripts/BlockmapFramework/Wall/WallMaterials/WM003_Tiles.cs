using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WM003_Tiles : WallMaterial
    {
        public override WallMaterialId Id => WallMaterialId.Tiles;
        public override string Name => "Tiles";
        public override Material Material => MaterialManager.LoadMaterial("TilesBlue");

        // Climbing attributes
        public override ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Unclimbable;
        public override float ClimbCostUp => 0f;
        public override float ClimbCostDown => 0f;
        public override float ClimbSpeedUp => 0f;
        public override float ClimbSpeedDown => 0f;
    }
}
