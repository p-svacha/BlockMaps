using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition represents the path from one BlockmapNode to another.
    /// <br/> Transitions are 'temporary' as in they are recreated when the navmesh is recalculated.
    /// </summary>
    public abstract class Transition
    {
        public BlockmapNode From { get; private set; }
        public BlockmapNode To { get; private set; }
        public Direction Direction { get; protected set; }

        public Transition(BlockmapNode from, BlockmapNode to)
        {
            From = from;
            To = to;
            // Direction = HelperFunctions.GetGeneralDirection(from.WorldCoordinates, to.WorldCoordinates);
        }

        /// <summary>
        /// Returns if the given entity can use this transition.
        /// </summary>
        public bool CanPass(Entity entity)
        {
            return From.IsPassable(Direction, entity) && To.IsPassable(HelperFunctions.GetOppositeDirection(Direction), entity);
        }

        /// <summary>
        /// Returns the amount of energy is required for the given entity to use this transition.
        /// </summary>
        public abstract float GetMovementCost(Entity entity);
        /*{
            float value = (0.5f * (1f / From.GetSpeedModifier())) + (0.5f * (1f / To.GetSpeedModifier()));
            if (dir == Direction.NE || dir == Direction.NW || dir == Direction.SE || dir == Direction.SW) value *= 1.4142f;
            return value;
        }*/

        /// <summary>
        /// Returns the position an entity using this transition has at the relative time t (0-1), whereas 0 is when starting the transition and 1 when ending it.
        /// </summary>
        public Vector3 GetPosition(float t)
        {
            return Vector3.zero;
        }
    }
}
