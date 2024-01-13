using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class MovingEntity : Entity
    {
        // Current movement
        public float MovementSpeed { get; protected set; }
        public bool IsMoving { get; private set; }

        // Pathfinding
        public BlockmapNode Target { get; private set; }
        public List<BlockmapNode> TargetPath { get; private set; }
        public Transition CurrentTransition { get; private set; }

        // Movement Attributes
        public bool CanSwim { get; protected set; }

        protected override void OnInitialized()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("MovingEntities can't be bigger than 1x1 for now.");

            IsMoving = false;
        }

        public override void UpdateEntity()
        {
            if(IsMoving)
            {
                CurrentTransition.UpdateEntityMovement(this, out bool finishedTransition, out BlockmapNode currentOriginNode);

                if (OriginNode != currentOriginNode) SetOriginNode(currentOriginNode);
                if(finishedTransition) ReachNextNode();
            }
        }

        /// <summary>
        /// Finds and walks towards the target node
        /// </summary>
        public void GoTo(BlockmapNode target)
        {
            // Get node where to start from. If we are moving take the next node in our current path. Else just where we are standing now.
            BlockmapNode startNode = IsMoving ? TargetPath[0] : OriginNode;

            // Find path to target
            List<BlockmapNode> path =  Pathfinder.GetPath(this, startNode, target);

            // Check if we found a valid path
            if (path == null || path.Count == 0)
            {
                Stop();
                return;
            }

            if(!IsMoving) // If we are standing still, set the first transition and discard the first node since its the one we stand on and therefore already reached it.
            {
                CurrentTransition = path[0].Transitions[path[1]];
                path.RemoveAt(0);
            }

            // Set new path
            TargetPath = path;
            Target = path.Last();
            IsMoving = true;
            OnNewPath();
        }


        /// <summary>
        /// Gets triggered when the entity starts moving to a new target.
        /// </summary>
        protected virtual void OnNewPath() { }

        /// <summary>
        /// Recalculates the path to the current target.
        /// </summary>
        private void UpdateTargetPath()
        {
            GoTo(Target);
        }

        /// <summary>
        /// Gets triggered when a node of the target path is reached. Updates the NextNode and MoveDirection
        /// </summary>
        private void ReachNextNode()
        {
            BlockmapNode reachedNode = TargetPath[0];
            TargetPath.RemoveAt(0);

            // Target not yet reached
            if (TargetPath.Count > 0)
            {
                BlockmapNode newNextNode = TargetPath[0];
                reachedNode.Transitions.TryGetValue(newNextNode, out Transition newTransition);

                // TargetPath is still valid => take that path
                if (newTransition != null && newTransition.CanPass(this))
                {
                    IsMoving = true;
                    newNextNode.AddEntity(this);
                    CurrentTransition = newTransition;
                }
                // TargetPath is no longer valid, find new path
                else
                {
                    // Debug.Log("Target path no longer valid, finding new path");
                    UpdateTargetPath();
                }

            }
            // Target reached
            else
            {
                Stop();
                OnTargetReached();
            }
        }

        /// <summary>
        /// Stops all movement instantly.
        /// </summary>
        private void Stop()
        {
            IsMoving = false;
            CurrentTransition = null;
            TargetPath = null;
            Target = null;
            OnStopMoving();
        }

        protected virtual void OnStopMoving() { }
        protected virtual void OnTargetReached() { }

    }
}
