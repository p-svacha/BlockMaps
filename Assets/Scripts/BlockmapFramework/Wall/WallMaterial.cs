using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Each possible wall material has exactly one instance of this class, which is handled by the singleton WallManager.
    /// <br/>WallMaterial defines what texture a wall has its climbability.
    /// </summary>
    public abstract class WallMaterial
    {
        public abstract WallMaterialId Id { get; }
        public abstract string Name { get; }
        public abstract Material Material { get; }

        // Climbing attributes
        public abstract ClimbingCategory ClimbSkillRequirement { get; }
        public abstract float ClimbCostUp { get; }
        public abstract float ClimbCostDown { get; }
        public abstract float ClimbSpeedUp { get; }
        public abstract float ClimbSpeedDown { get; }
    }
}
