using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class MovingEntity : Entity
    {
        // Const movement rules
        public const int MAX_BASIC_CLIMB_HEIGHT = 2; // in tiles = 0.5m
        public const int MAX_INTERMEDIATE_CLIMB_HEIGHT = 6; // in tiles = 0.5m
        public const int MAX_ADVANCED_CLIMB_HEIGHT = 10; // in tiles = 0.5m

        // Current movement
        public bool IsMoving { get; private set; }
        public bool IsMovementPaused { get; private set; }
        public ClimbPhase ClimbPhase { get; set; }
        public int ClimbIndex { get; set; }

        // Pathfinding
        public BlockmapNode Target { get; private set; }
        public List<BlockmapNode> TargetPath { get; private set; }
        public Transition CurrentTransition { get; private set; }

        // Movement Attributes
        public float MovementSpeed;
        public bool CanSwim;
        public ClimbingCategory ClimbingSkill;

        // Events
        public event System.Action OnTargetReached;

        // Components
        private Projector SelectionIndicator;

        protected override void OnInitialized()
        {
            if (Dimensions.x != 1 || Dimensions.z != 1) throw new System.Exception("MovingEntities can't be bigger than 1x1.");

            // Selection indicator
            SelectionIndicator = Instantiate(ResourceManager.Singleton.SelectionIndicator);
            SelectionIndicator.transform.SetParent(transform);
            SelectionIndicator.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            SelectionIndicator.orthographicSize = Mathf.Max(Dimensions.x, Dimensions.z) * 0.5f;
            SetSelected(false);

            IsMoving = false;
        }

        public override void UpdateEntity()
        {
            base.UpdateEntity();

            // Movement
            if(IsMoving && !IsMovementPaused)
            {
                CurrentTransition.UpdateEntityMovement(this, out bool finishedTransition, out BlockmapNode currentOriginNode);

                if (OriginNode != currentOriginNode)
                {
                    SetOriginNode(currentOriginNode);

                    // Recalculate vision of all nearby entities (including this)
                    if (BlocksVision) World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.CenterWorldPosition, callback: UpdateVisibility);
                    else // If this entity doesn't block vision, only update the vision of itself and of entities from other actors
                    {
                        World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.CenterWorldPosition, callback: UpdateVisibility, excludeActor: Owner);
                        UpdateVision();
                    }
                }
                if(finishedTransition) ReachNextNode();


                // Update transform if visible
                if(IsVisibleBy(World.ActiveVisionActor))
                {
                    transform.position = WorldPosition;
                    transform.rotation = WorldRotation;
                }
            }
        }

        /// <summary>
        /// Shows/hides the selection indicator of this entity.
        /// </summary>
        public void SetSelected(bool value)
        {
            SelectionIndicator.gameObject.SetActive(value);
        }

        /// <summary>
        /// Finds a path and walks towards the target node
        /// </summary>
        public void MoveTo(BlockmapNode target)
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

            // Set new path
            Move(path);
        }

        /// <summary>
        /// Starts moving according to the given path.
        /// </summary>
        public void Move(List<BlockmapNode> path)
        {
            if (!IsMoving) // If we are standing still, set the first transition and discard the first node since its the one we stand on and therefore already reached it.
            {
                SetCurrentTransition(path[0].Transitions[path[1]]);
                path.RemoveAt(0);
            }

            TargetPath = path;
            Target = path.Last();
            IsMoving = true;
            OnNewPath();
        }

        /// <summary>
        /// Pauses the current movement. Doesn't trigger any hooks or logic. Movement will continue normally with UnpauseMovement().
        /// </summary>
        public void PauseMovement()
        {
            IsMovementPaused = true;
        }
        /// <summary>
        /// Unpause the entity so it will move normally again.
        /// </summary>
        public void UnpauseMovement()
        {
            IsMovementPaused = false;
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
            MoveTo(Target);
        }

        /// <summary>
        /// Gets triggered when a node of the target path is reached. Updates the NextNode and MoveDirection
        /// </summary>
        private void ReachNextNode()
        {
            BlockmapNode reachedNode = TargetPath[0];
            TargetPath.RemoveAt(0);

            // Update the last known position of this entity for all actors that can currently see it
            foreach (Entity e in SeenBy) UpdateLastKnownPositionFor(e.Owner);

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
                    SetCurrentTransition(newTransition);
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
                OnTargetReached?.Invoke();
            }
        }

        private void SetCurrentTransition(Transition t)
        {
            CurrentTransition = t;
            CurrentTransition.OnTransitionStart(this);
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

        #region Getters

        /// <summary>
        /// Returns if the target node is reachable with a path that costs less than the given limit.
        /// </summary>
        public bool IsInRange(BlockmapNode targetNode, float maxCost)
        {
            // First check if the target node is even close. If not: skip detailed check
            if (Vector2.Distance(OriginNode.WorldCoordinates, targetNode.WorldCoordinates) > maxCost) return false;

            // Setup
            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();

            // Start with origin node
            BlockmapNode sourceNode = OriginNode;
            priorityQueue.Add(sourceNode, 0f);
            nodeCosts.Add(sourceNode, 0f);

            while (priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach (KeyValuePair<BlockmapNode, Transition> t in currentNode.Transitions)
                {
                    BlockmapNode toNode = t.Key;
                    float transitionCost = t.Value.GetMovementCost(this);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > maxCost) continue; // not in range
                    if (!t.Value.CanPass(this)) continue; // transition not passable for this character

                    // Check if we reached target
                    if (toNode == targetNode) return true;

                    // Node has not yet been visited or cost is lower than previously lowest cost => Update
                    if (!nodeCosts.ContainsKey(toNode) || totalCost < nodeCosts[toNode])
                    {
                        // Update cost to this node
                        nodeCosts[toNode] = totalCost;

                        // Add target node to queue to continue search
                        if (!priorityQueue.ContainsKey(toNode) || priorityQueue[toNode] > totalCost)
                            priorityQueue[toNode] = totalCost;
                    }
                }
            }

            // No more nodes to check -> target not in range
            return false;
        }

        #endregion

    }
}
