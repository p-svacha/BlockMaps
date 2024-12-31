using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition type used for hopping up and/or down to a straight-adjacent node.
    /// </summary>
    public class HopTransition : Transition
    {
        // Limits
        public const int MaxHopUpDistance = 6;
        public const int MaxHopDownDistance = 10;

        // Cost
        private const float BaseCost = 1f;
        private const float CostPerHopUpAltitude = 2f;
        private const float CostPerHopDownAltitude = 0.2f;

        // Hop path
        private List<Vector3> HopArc;
        private const float TransitionSpeed = 5f;

        /// <summary>
        /// The required distance an entity must be able to hop up to use this transition.
        /// </summary>
        public int HopUpDistance { get; private set; }

        /// <summary>
        /// The required distance an entity must be able to hop down to use this transition.
        /// </summary>
        public int HopDownDistance { get; private set; }

        public HopTransition(BlockmapNode from, BlockmapNode to, Direction dir, int maxHeight, int hopUpDistance, int hopDownDistance) : base(from, to, dir, maxHeight)
        {
            HopUpDistance = hopUpDistance;
            HopDownDistance = hopDownDistance;

            float arcHeight = (Mathf.Max(hopUpDistance, hopDownDistance) * World.NodeHeight);
            HopArc = HelperFunctions.CreateArc(from.MeshCenterWorldPosition, to.MeshCenterWorldPosition, arcHeight, segments: 12);
        }

        public override bool CanPass(Entity entity)
        {
            if (entity.MaxHopUpDistance < HopUpDistance) return false;
            if (entity.MaxHopDownDistance < HopDownDistance) return false;

            return base.CanPass(entity);
        }

        public override float GetMovementCost(Entity entity)
        {
            float value = BaseCost;
            float costFromDistance = (HopUpDistance * CostPerHopUpAltitude) + (HopDownDistance * CostPerHopDownAltitude);
            if (entity != null) costFromDistance *= (1f / entity.HoppingAptitude);
            value += costFromDistance;
            return value;
        }

        public override List<Vector3> GetPreviewPath()
        {
            return HopArc;
        }

        public override void OnTransitionStart(Entity entity)
        {
            Comp_Movement moveComp = entity.GetComponent<Comp_Movement>();

            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction));
            moveComp.TransitionPathIndex = 0;
            moveComp.TransitionSpeed = TransitionSpeed;
        }

        public override void UpdateEntityMovement(Entity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            finishedTransition = false;
            Comp_Movement moveComp = entity.GetComponent<Comp_Movement>();

            // Calculate the current segment end point
            Vector3 segmentEnd = HopArc[moveComp.TransitionPathIndex + 1];

            // Move the entity based on the current speed
            Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, segmentEnd, moveComp.TransitionSpeed * Time.deltaTime);

            if (Vector3.Distance(segmentEnd, newPosition) < 0.01f) // reached segment end point
            {
                moveComp.TransitionPathIndex++;
                if (moveComp.TransitionPathIndex == HopArc.Count - 1)
                {
                    finishedTransition = true;
                    newPosition = To.MeshCenterWorldPosition;
                }
            }
            currentNode = moveComp.TransitionPathIndex > HopArc.Count / 2 ? To : From;

            entity.SetWorldPosition(newPosition);
        }
    }
}
