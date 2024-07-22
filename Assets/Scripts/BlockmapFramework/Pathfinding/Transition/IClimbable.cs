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
        public ClimbingCategory ClimbSkillRequirement { get; }

        /// <summary>
        /// The movement cost of climbing one tile up.
        /// </summary>
        public float ClimbCostUp { get; }

        /// <summary>
        /// The movement cost of climbing one tile down.
        /// </summary>
        public float ClimbCostDown { get; }

        /// <summary>
        /// The movement speed when climbing up.
        /// </summary>
        public float ClimbSpeedUp { get; }

        /// <summary>
        /// The movement speed when climbing down.
        /// </summary>
        public float ClimbSpeedDown { get; }
        
        /// <summary>
        /// The world distance from the node edge that a MovingEntity has when climbing this. Basically its width.
        /// </summary>
        public float ClimbTransformOffset { get; }

        /// <summary>
        /// On which node side this climbable is located. The TransformOffset is only applied when climbing on this side.
        /// </summary>
        public Direction ClimbSide { get; }

        /// <summary>
        /// Returns if this piece is generally climbable.
        /// </summary>
        public bool IsClimbable { get; }
    }
}
