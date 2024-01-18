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
        public ClimbingCategory SkillRequirement => ClimbingCategory.Advanced;
        public int MaxClimbHeight(ClimbingCategory skill) => 5;
        public float CostUp => 2.5f;
        public float CostDown => 1.5f;
        public float SpeedUp => 0.3f;
        public float SpeedDown => 0.4f;
        public float TransformOffset => 0f;
    }
}
