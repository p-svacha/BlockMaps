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
        /// <summary>
        /// If the distance to a target is lower than this value, it is considered as reached.
        /// </summary>
        protected const float REACH_EPSILON = 0.02f;

        protected World World => From.World;
        public BlockmapNode From { get; private set; }
        public BlockmapNode To { get; private set; }
        public Direction Direction { get; private set; }
        public Direction OppositeDirection { get; private set; }

        /// <summary>
        /// The maximum height a moving entity is allowed to have to use this transition.
        /// </summary>
        protected int MaxHeight { get; private set; }

        public Transition(BlockmapNode from, BlockmapNode to, Direction dir, int maxHeight)
        {
            From = from;
            To = to;
            Direction = dir;
            OppositeDirection = HelperFunctions.GetOppositeDirection(Direction);
            MaxHeight = maxHeight;
        }

        /// <summary>
        /// Returns if the given entity can use this transition.
        /// </summary>
        public virtual bool CanPass(MovingEntity entity)
        {
            if (entity.Height > MaxHeight) return false;

            return true;
        }

        /// <summary>
        /// Returns the amount of energy is required for the given entity to use this transition.
        /// <br/>Walking to one adjacent tile has a cost of about 1 - 4 depending on surface. (1.1 for tarmac, 2 for grass, 3.3 for sand).
        /// </summary>
        public abstract float GetMovementCost(MovingEntity entity);

        /// <summary>
        /// Gets executed when the given entity starts using this transition.
        /// </summary>
        public abstract void OnTransitionStart(MovingEntity entity);
        /// <summary>
        /// Updates the position of an entity using this transition in 1 tick.
        /// <br/> Returns if the entity has finished this transition as an out param.
        /// <br/> Returns the node that the entity is currently on (its origin node) as an out param.
        /// </summary>
        public abstract void EntityMovementTick(MovingEntity entity, out bool finishedTransition, out BlockmapNode currentNode);

        /// <summary>
        /// Returns a list of points that approximately show the path within this transition.
        /// <br/> Used for navmesh preview.
        /// </summary>
        public abstract List<Vector3> GetPreviewPath();
    }
}
