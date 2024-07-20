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
        public float CostUp => 2.5f;
        public float CostDown => 1.5f;
        public float SpeedUp => 0.3f;
        public float SpeedDown => 0.4f;
        public float TransformOffset => 0f;
        public Direction ClimbSide => Direction.None;
        public int MaxClimbHeight(ClimbingCategory skill)
        {
            return skill switch
            {
                ClimbingCategory.None => 0,
                ClimbingCategory.Basic => MovingEntity.MAX_BASIC_CLIMB_HEIGHT,
                ClimbingCategory.Intermediate => MovingEntity.MAX_INTERMEDIATE_CLIMB_HEIGHT,
                ClimbingCategory.Advanced => MovingEntity.MAX_ADVANCED_CLIMB_HEIGHT,
                ClimbingCategory.Unclimbable => 0,
                _ => throw new System.Exception("category " + skill.ToString() + " not handled.")
            };
        }
    }
}
