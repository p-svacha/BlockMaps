using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// The speed at which the entity moves around in the world.
        /// </summary>
        public float MovementSpeed { get; init; } = 1f;

        /// <summary>
        /// Flag if the entity can pass water nodes.
        /// </summary>
        public bool CanSwim { get; init; } = true;

        /// <summary>
        /// Maximum climbability of climbables that the entity can climb.
        /// </summary>
        public ClimbingCategory ClimbingSkill { get; init; } = ClimbingCategory.None;

        /// <summary>
        /// Modifies the cost of using climbing transitions.
        /// </summary>
        public float ClimbingAptitude { get; init; } = 1f;

        /// <summary>
        /// The maximum height the entity can hop upwards to an adjacent node.
        /// </summary>
        public int MaxHopUpDistance { get; init; } = 0;

        /// <summary>
        /// The maximum height the entity can drop downwards to an adjacent node.
        /// </summary>
        public int MaxHopDownDistance { get; init; } = 0;

        /// <summary>
        /// Modifier of the cost the entity has to pay to move on the given SurfaceDef.
        /// </summary>
        public Dictionary<SurfaceDef, float> SurfaceAptitudes { get; init; } = new Dictionary<SurfaceDef, float>();

        public override bool Validate(EntityDef parent)
        {
            if (parent.Impassable) parent.ThrowValidationError("Entities capable of movement cannot be impassable.");

            return base.Validate(parent);
        }

        public override CompProperties Clone()
        {
            return new CompProperties_Movement()
            {
                CompClass = this.CompClass,
                MovementSpeed = this.MovementSpeed,
                CanSwim = this.CanSwim,
                ClimbingSkill = this.ClimbingSkill,
                MaxHopUpDistance = this.MaxHopUpDistance,
                MaxHopDownDistance = this.MaxHopDownDistance,
                SurfaceAptitudes = this.SurfaceAptitudes.ToDictionary(x => x.Key, x => x.Value)
            };
        }
    }
}
