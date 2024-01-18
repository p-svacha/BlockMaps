using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition for when going to an adjacent node and having to climb up AND down between it.
    /// </summary>
    public class DoubleClimbTransition : Transition
    {
        public DoubleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, float cost, float speed, float transformOffset) : base(from, to) { }

        public override float GetMovementCost(MovingEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public override void OnTransitionStart(MovingEntity entity)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateEntityMovement(MovingEntity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            throw new System.NotImplementedException();
        }
    }
}
