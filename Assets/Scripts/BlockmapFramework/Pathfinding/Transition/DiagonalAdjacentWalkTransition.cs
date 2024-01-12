using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition for when walking from one node to another that is adjacent in a straight direction.
    /// </summary>
    public class DiagonalAdjacentWalkTransition : Transition
    {
        public DiagonalAdjacentWalkTransition(BlockmapNode from, BlockmapNode to, Direction dir) : base(from, to)
        {
            Direction = dir;
        }

        public override float GetMovementCost(Entity entity)
        {
            float value = (0.5f * (1f / From.GetSpeedModifier())) + (0.5f * (1f / To.GetSpeedModifier()));
            value *= 1.4142f; // Because diagonal
            return value;
        }
    }
}
