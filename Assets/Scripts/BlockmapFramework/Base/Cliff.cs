using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton instance for getting cliff climbing attributes.
    /// </summary>
    public class Cliff : IClimbable
    {
        private static Cliff _Instance;
        public static Cliff Instance
        {
            get
            {
                if (_Instance == null) _Instance = new Cliff();
                return _Instance;
            }
        }

        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Advanced;
        public float ClimbCostUp => 2.5f;
        public float ClimbCostDown => 1.5f;
        public float ClimbTransformOffset => 0f;
        public Direction ClimbSide => Direction.None;
        public bool IsClimbable => true;
    }
}
