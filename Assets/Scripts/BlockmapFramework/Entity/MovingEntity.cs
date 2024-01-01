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
        public Direction CurrentDirection { get; private set; }
        public BlockmapNode NextNode { get; private set; }

        // Pathfinding
        public BlockmapNode Target { get; private set; }
        public List<BlockmapNode> TargetPath { get; private set; }

        // Movement Attributes
        public bool CanSwim { get; protected set; }

        protected override void OnInitialized()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("MovingEntities can't be bigger than 1x1 for now.");

            IsMoving = false;
            NextNode = OriginNode;
        }

        public override void UpdateEntity()
        {
            // Update transform position and occupied tiles (for collisions) when moving
            Vector2 oldPosition2d = new Vector2(transform.position.x, transform.position.z);
            Vector2Int oldWorldCoordinates = World.GetWorldCoordinates(oldPosition2d);

            Vector3 nextNodePosition = NextNode.GetCenterWorldPosition();
            Vector2 nextNodePosition2d = new Vector2(nextNodePosition.x, nextNodePosition.z);
            if (IsMoving)
            {
                // Calculate new world position and coordinates
                Vector2 newPosition2d = Vector2.MoveTowards(oldPosition2d, nextNodePosition2d, MovementSpeed * Time.deltaTime * OriginNode.GetSpeedModifier());
                Vector2Int newWorldCoordinates = World.GetWorldCoordinates(newPosition2d);

                // Change origin node when passing over a node border
                if (oldWorldCoordinates != newWorldCoordinates) SetOriginNode(TargetPath[0]); 

                // Set new position/rotation
                Vector3 newPosition = new Vector3(newPosition2d.x, World.GetWorldHeightAt(newPosition2d, OriginNode), newPosition2d.y);
                transform.position = newPosition;
                transform.rotation = Get2dRotationByDirection(CurrentDirection);
            }

            // Character is near the destination get the next movement command
            if (Vector2.Distance(oldPosition2d, nextNodePosition2d) <= 0.03f)
            {
                ReachNextNode();
            }
        }

        /// <summary>
        /// Finds and walks towards the target node
        /// </summary>
        public void GoTo(BlockmapNode target)
        {
            Target = target;
            TargetPath = Pathfinder.GetPath(this, NextNode, Target);
            OnNewTarget();
        }

        public void SetTargetPath(List<BlockmapNode> targetPath)
        {
            if (targetPath[0] != OriginNode) throw new System.Exception("TargetPath needs to start at current entity location.");
            Target = targetPath.Last();
            NextNode = targetPath[0];
            TargetPath = targetPath;
            OnNewTarget();
        }


        /// <summary>
        /// Gets triggered when the entity starts moving to a new target.
        /// </summary>
        protected virtual void OnNewTarget() { }

        private void UpdateTargetPath()
        {
            TargetPath = Pathfinder.GetPath(this, NextNode, Target);
        }

        /// <summary>
        /// Gets triggered when a node of the target path is reached. Updates the NextNode and MoveDirection
        /// </summary>
        private void ReachNextNode()
        {
            // No target path => no movement expected
            if (TargetPath == null || TargetPath.Count == 0) IsMoving = false;

            // Follow a target path
            else
            {
                BlockmapNode lastNode = TargetPath[0];
                TargetPath.RemoveAt(0);

                // Target not yet reached
                if (TargetPath.Count > 0)
                {
                    // TargetPath is still valid => take that path
                    if (Pathfinder.CanTransition(this, NextNode, TargetPath[0]))
                    {
                        IsMoving = true;
                        CurrentDirection = HelperFunctions.GetDirection(lastNode.WorldCoordinates, TargetPath[0].WorldCoordinates);
                        NextNode = TargetPath[0];
                        NextNode.AddEntity(this);
                    }
                    // TargetPath is no longer valid, find new path
                    else
                    {
                        Debug.Log("Target path no longer valid, finding new path");
                        IsMoving = false;
                        UpdateTargetPath();
                        if(TargetPath == null) OnTargetReached();
                    }

                }
                // Target reached
                else
                {
                    IsMoving = false;
                    OnTargetReached();
                }
            }
        }

        protected virtual void OnTargetReached() { }

    }
}
