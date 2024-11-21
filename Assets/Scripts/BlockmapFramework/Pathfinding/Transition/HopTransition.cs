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
        public HopTransition(BlockmapNode from, BlockmapNode to, Direction dir, int maxHeight) : base(from, to, dir, maxHeight) { }

        public override float GetMovementCost(Entity entity)
        {
            throw new System.NotImplementedException();
        }

        public override List<Vector3> GetPreviewPath()
        {
            throw new System.NotImplementedException();
        }

        public override void OnTransitionStart(Entity entity)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateEntityMovement(Entity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            throw new System.NotImplementedException();
        }
    }
}
