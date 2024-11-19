using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class CompProperties_Movement : CompProperties
    {
        public CompProperties_Movement()
        {
            CompClass = typeof(Comp_Movement);
        }

        /// <summary>
        /// The speed at which this entity moves around in the world.
        /// </summary>
        public float MovementSpeed { get; init; } = 1f;

        /// <summary>
        /// Flag if this entity can pass water nodes.
        /// </summary>
        public bool CanSwim { get; init; } = true;

        /// <summary>
        /// Maximum climbability of climbables that this entity can climb.
        /// </summary>
        public ClimbingCategory ClimbingSkill { get; init; } = ClimbingCategory.None;

        public override bool Validate(EntityDef parent)
        {
            if (parent.Impassable) parent.ThrowValidationError("Entities capable of movement cannot be impassable.");

            return base.Validate(parent);
        }
    }
}
