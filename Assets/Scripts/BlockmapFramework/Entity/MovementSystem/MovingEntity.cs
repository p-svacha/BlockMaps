using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class MovingEntity : Entity
    {
        // Component cache
        public Comp_Movement MovementComp { get; private set; }

        #region Init

        protected override void OnCompInitialized(EntityComp comp)
        {
            if (comp is Comp_Movement mComp) MovementComp = mComp;
        }

        protected override void OnInitialized()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("Moving entities must be 1x1 big.");
            if (MovementComp == null) throw new System.Exception($"Moving entities must have a Comp_Movement. {LabelCap} does not.");
        }

        #endregion

        #region Render

        public override void Render(float alpha)
        {
            if (IsVisibleBy(World.ActiveVisionActor))
            {
                // Interpolate position
                Vector3 interpolatedPosition = Vector3.Lerp(
                    WorldPositionPrev,
                    WorldPosition,
                    alpha
                );

                // Interpolate rotation
                Quaternion interpolatedRotation = Quaternion.Slerp(
                    WorldRotationPrev,
                    WorldRotation,
                    alpha
                );

                // Apply to the MeshObject for rendering
                MeshObject.transform.position = interpolatedPosition;
                MeshObject.transform.rotation = interpolatedRotation;
            }
        }

        #endregion

        #region Getters

        // Movement attributes
        public virtual float MovementSpeed => MovementComp.MovementSpeed; // in nodes/second when movement cost is 1
        public virtual bool CanSwim => MovementComp.CanSwim;
        public virtual ClimbingCategory ClimbingSkill => MovementComp.ClimbingSkill;
        public virtual int MaxHopUpDistance => MovementComp.MaxHopUpDistance;
        public virtual int MaxHopDownDistance => MovementComp.MaxHopDownDistance;

        // Aptitudes (affect the cost of using of transitions in the navmesh)
        public virtual float ClimbingAptitude => MovementComp.ClimbingAptitude;
        public virtual float GetSurfaceAptitude(SurfaceDef def) => MovementComp.GetSurfaceAptitude(def);
        public virtual float HoppingAptitude => 1f;

        /// <summary>
        /// Returns if the target node is reachable with a path that costs less than the given limit.
        /// </summary>
        public bool IsInRange(BlockmapNode targetNode, float maxCost, out float totalCost) => MovementComp.IsInRange(targetNode, maxCost, out totalCost);

        /// <summary>
        /// Returns the movement speed this entity has right now taking into account the surface its on.
        /// </summary>
        public float GetCurrentWalkingSpeed(Direction from, Direction to)
        {
            float value = MovementSpeed * GameLoop.TickDeltaTime;
            value *= (1f / OriginNode.GetMovementCost(this, from, to));

            return value;
        }

        #endregion
    }
}
