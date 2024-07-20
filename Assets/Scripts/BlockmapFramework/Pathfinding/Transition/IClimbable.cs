using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// All vertical things that can be climbed by entites need to implement this.
    /// <br/> This includes ladders, cliffs and fences.
    /// </summary>
    public interface IClimbable
    {
        /// <summary>
        /// What kind of skill a MovingEntity needs to climb this.
        /// </summary>
        public ClimbingCategory SkillRequirement { get; }

        /// <summary>
        /// Returns the maximum height this climbable can be so that a MovingEntity with the given climbing skill can still climb it.
        /// </summary>
        public int MaxClimbHeight(ClimbingCategory skill);

        /// <summary>
        /// The movement cost of climbing one tile up.
        /// </summary>
        public float CostUp { get; }

        /// <summary>
        /// The movement cost of climbing one tile down.
        /// </summary>
        public float CostDown { get; }

        /// <summary>
        /// The movement speed when climbing up.
        /// </summary>
        public float SpeedUp { get; }

        /// <summary>
        /// The movement speed when climbing down.
        /// </summary>
        public float SpeedDown { get; }
        
        /// <summary>
        /// The world distance from the node edge that a MovingEntity has when climbing this. Basically its width.
        /// </summary>
        public float TransformOffset { get; }

        /// <summary>
        /// On which node side this climbable is located. The TransformOffset is only applied when climbing on this side.
        /// </summary>
        public Direction ClimbSide { get; }
    }
}
