using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class MovingEntity : Entity
    {
        // Current movement
        private float MovementSpeed = 2f;
        public bool IsMoving;
        public Direction CurrentDirection;
        public BlockmapNode NextNode;

        // Pathfinding
        public BlockmapNode Target;
        public List<BlockmapNode> TargetPath;

        public GameObject TargetFlag;
        private float TargetFlagScale = 0.1f;

        public override void Init(World world, BlockmapNode position, bool[,,] shape)
        {
            if (shape.GetLength(0) != 1 || shape.GetLength(1) != 1) throw new System.Exception("Characters can't be bigger than 1x1");
            base.Init(world, position, shape);
            IsMoving = false;

            NextNode = position;
            TargetFlag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            TargetFlag.transform.SetParent(transform.parent);
            GoTo(World.GetRandomOwnedTerrainNode());
        }

        // Update is called once per frame
        void Update()
        {
            if (TargetPath == null) GoTo(World.GetRandomOwnedTerrainNode());

            // Update transform position and occupied tiles (for collisions) when moving
            Vector2 currentPosition2d = new Vector2(transform.position.x, transform.position.z);
            Vector2Int currentWorldCoordinates = World.GetWorldCoordinates(currentPosition2d);
            Vector3 nextNodePosition = NextNode.GetCenterWorldPosition();
            Vector2 nextNodePosition2d = new Vector2(nextNodePosition.x, nextNodePosition.z);
            if (IsMoving)
            {
                Vector2 newPosition2d = Vector2.MoveTowards(currentPosition2d, nextNodePosition2d, MovementSpeed * Time.deltaTime * OriginNode.GetSpeedModifier());
                Vector2Int newWorldCoordinates = World.GetWorldCoordinates(newPosition2d);
                if (currentWorldCoordinates != newWorldCoordinates) OriginNode = TargetPath[0]; // Change origin node when passing over a node border

                Vector3 newPosition = new Vector3(newPosition2d.x, World.GetTerrainHeightAt(newPosition2d), newPosition2d.y);
                if ((OriginNode.Type == NodeType.Surface && ((SurfaceNode)OriginNode).HasPath) || OriginNode.Type == NodeType.AirPath || OriginNode.Type == NodeType.AirPathSlope) newPosition = new Vector3(newPosition.x, World.GetPathHeightAt(newPosition2d, OriginNode.BaseHeight), newPosition.z);
                transform.position = newPosition;
                UpdateOccupiedTerrainTiles();

                // TODO: fix rotation
                transform.rotation = Get2dRotationByDirection(CurrentDirection);
            }

            // Character is near the destination get the next movement command
            if (Vector2.Distance(currentPosition2d, nextNodePosition2d) <= 0.03f)
            {
                OnNextNodeReached();
            }
        }

        /// <summary>
        /// Finds and walks towards the target node
        /// </summary>
        private void GoTo(BlockmapNode target)
        {
            Target = target;

            TargetFlag.transform.position = Target.GetCenterWorldPosition();
            TargetFlag.transform.localScale = new Vector3(TargetFlagScale, 1f, TargetFlagScale);
            TargetFlag.GetComponent<MeshRenderer>().material.color = Color.red;

            TargetPath = Pathfinder.GetPath(NextNode, Target);
        }

        private void UpdateTargetPath()
        {
            TargetPath = Pathfinder.GetPath(NextNode, Target);
        }

        /// <summary>
        /// Gets triggered when a node of the target path is reached. Updates the NextNode and MoveDirection
        /// </summary>
        /// <returns></returns>
        protected void OnNextNodeReached()
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
                    if (Pathfinder.CanTransition(NextNode, TargetPath[0]))
                    {
                        IsMoving = true;
                        CurrentDirection = Pathfinder.GetDirection(lastNode, TargetPath[0]);
                        NextNode = TargetPath[0];
                        NextNode.AddEntity(this);
                    }
                    // TargetPath is no longer valid, find new path
                    else
                    {
                        Debug.Log("Target path no longer valid, fidning new path");
                        IsMoving = false;
                        UpdateTargetPath();
                    }

                }
                // Target reached
                else
                {
                    IsMoving = false;
                    GoTo(World.GetRandomOwnedTerrainNode());
                }
            }
        }
    }
}
