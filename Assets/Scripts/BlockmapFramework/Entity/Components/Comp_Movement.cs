using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// EntityComponent that contains all rules of how an entity can move around in the world.
    /// </summary>
    public class Comp_Movement : EntityComp
    {
        public CompProperties_Movement Props => (CompProperties_Movement)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (Parent.Dimensions.x != 1 || Parent.Dimensions.z != 1) throw new System.Exception("Moving entities can't be bigger than 1x1.");
            if (Parent.Def.RenderProperties.RenderType != EntityRenderType.StandaloneModel && Parent.Def.RenderProperties.RenderType != EntityRenderType.StandaloneGenerated) 
                throw new System.Exception("Moving entities must have standalone rendering.");

            IsMoving = false;
        }

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

        // Overrides
        private bool isOverrideMovementSpeedActive;
        private float overrideMovementSpeed;

        private bool isOverrideCanSwimActive;
        private bool overrideCanSwim;

        private bool isOverrideClimbSkillActive;
        private ClimbingCategory overrideClimbSkill;

        // Events
        public event System.Action OnTargetReached;
        /// <summary>
        /// Gets fired when the entity starts moving along a new path.
        /// </summary>
        public event System.Action OnNewPath;
        /// <summary>
        /// Gets fired when the entity stops moving and a path is no longer set.
        /// </summary>
        public event System.Action OnStopMoving;

        public override void Tick()
        {
            // Movement
            if (IsMoving && !IsMovementPaused)
            {
                CurrentTransition.UpdateEntityMovement(Parent, out bool finishedTransition, out BlockmapNode currentOriginNode);

                if (Parent.OriginNode != currentOriginNode)
                {
                    Parent.SetOriginNode(currentOriginNode);

                    // Recalculate vision of all nearby entities (including this)
                    if (Parent.BlocksVision) World.UpdateVisionOfNearbyEntitiesDelayed(Parent.OriginNode.CenterWorldPosition, callback: Parent.UpdateVisibility);
                    else // If this entity doesn't block vision, only update the vision of itself and of entities from other actors
                    {
                        World.UpdateVisionOfNearbyEntitiesDelayed(Parent.OriginNode.CenterWorldPosition, callback: Parent.UpdateVisibility, excludeActor: Parent.Actor);
                        Parent.UpdateVision();
                    }
                }
                if (finishedTransition) ReachNextNode();


                // Update transform if visible
                if (Parent.IsVisibleBy(World.ActiveVisionActor))
                {
                    Parent.MeshObject.transform.position = Parent.WorldPosition;
                    Parent.MeshObject.transform.rotation = Parent.WorldRotation;
                }
            }
        }

        #region Actions

        /// <summary>
        /// Finds a path and walks towards the target node
        /// </summary>
        public void MoveTo(BlockmapNode target)
        {
            // Get node where to start from. If we are moving take the next node in our current path. Else just where we are standing now.
            BlockmapNode startNode = IsMoving ? TargetPath[0] : Parent.OriginNode;

            // Find path to target
            List<BlockmapNode> path = Pathfinder.GetPath(Parent, startNode, target);

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
            OnNewPath.Invoke();
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
            foreach (Entity e in Parent.SeenBy) Parent.UpdateLastKnownPositionFor(e.Actor);

            // Target not yet reached
            if (TargetPath.Count > 0)
            {
                BlockmapNode newNextNode = TargetPath[0];
                reachedNode.Transitions.TryGetValue(newNextNode, out Transition newTransition);

                // TargetPath is still valid => take that path
                if (newTransition != null && newTransition.CanPass(Parent))
                {
                    IsMoving = true;
                    newNextNode.AddEntity(Parent);
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
            CurrentTransition.OnTransitionStart(Parent);
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
            OnStopMoving.Invoke();
        }

        public void EnableOverrideMovementSpeed(float value)
        {
            isOverrideMovementSpeedActive = true;
            overrideMovementSpeed = value;
        }
        public void DisableOverrideMovementSpeed()
        {
            isOverrideMovementSpeedActive = false;
        }
        public void EnableOverrideCanSwim(bool value)
        {
            isOverrideCanSwimActive = true;
            overrideCanSwim = value;
        }
        public void DisableOverrideCanSwim()
        {
            isOverrideCanSwimActive = false;
        }
        public void EnableOverrideClimbSkill(ClimbingCategory value)
        {
            isOverrideClimbSkillActive = true;
            overrideClimbSkill = value;
        }
        public void DisableOverrideClimbSkill()
        {
            isOverrideClimbSkillActive = false;
        }

        #endregion

        #region Getters

        /// <summary>
        /// The speed at which this entity moves around in the world.
        /// </summary>
        public float MovementSpeed => isOverrideMovementSpeedActive ? overrideMovementSpeed : Props.MovementSpeed;

        /// <summary>
        /// Flag if this entity can pass water nodes.
        /// </summary>
        public bool CanSwim => isOverrideCanSwimActive ? overrideCanSwim : Props.CanSwim;

        /// <summary>
        /// Maximum climbability of climbables that this entity can climb.
        /// </summary>
        public ClimbingCategory ClimbingSkill => isOverrideClimbSkillActive ? overrideClimbSkill : Props.ClimbingSkill;

        /// <summary>
        /// Returns if the target node is reachable with a path that costs less than the given limit.
        /// </summary>
        public bool IsInRange(BlockmapNode targetNode, float maxCost)
        {
            // First check if the target node is even close. If not: skip detailed check
            if (Vector2.Distance(Parent.OriginNode.WorldCoordinates, targetNode.WorldCoordinates) > maxCost) return false;

            // Setup
            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();

            // Start with origin node
            BlockmapNode sourceNode = Parent.OriginNode;
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
                    float transitionCost = t.Value.GetMovementCost(Parent);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > maxCost) continue; // not in range
                    if (!t.Value.CanPass(Parent)) continue; // transition not passable for this character

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