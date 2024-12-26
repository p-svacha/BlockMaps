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
        public const int MaxEntityHeight = 5;

        public CompProperties_Movement Props => (CompProperties_Movement)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (Entity.Dimensions.x != 1 || Entity.Dimensions.z != 1) throw new System.Exception("Moving entities can't be bigger than 1x1.");
            if (Entity.Def.RenderProperties.RenderType != EntityRenderType.StandaloneModel && Entity.Def.RenderProperties.RenderType != EntityRenderType.StandaloneGenerated) 
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
        public int TransitionPathIndex { get; set; }
        public float TransitionSpeed { get; set; }

        // Pathfinding
        public NavigationPath TargetPath { get; private set; }
        public BlockmapNode Target => TargetPath.Target;
        public Transition CurrentTransition { get; private set; }

        // Overrides
        private bool isOverrideMovementSpeedActive;
        private float overrideMovementSpeed;

        private bool isOverrideCanSwimActive;
        private bool overrideCanSwim;

        private bool isOverrideClimbSkillActive;
        private ClimbingCategory overrideClimbSkill;

        private bool isOverrideMaxHopUpDistanceActive;
        private int overrideMaxHopUpDistance;

        private bool isOverrideMaxHopDownDistanceActive;
        private int overrideMaxHopDownDistance;

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
                CurrentTransition.UpdateEntityMovement(Entity, out bool finishedTransition, out BlockmapNode currentOriginNode);

                if (Entity.OriginNode != currentOriginNode)
                {
                    Entity.SetOriginNode(currentOriginNode);

                    // Recalculate vision of all nearby entities (including this)
                    if (Entity.BlocksVision()) World.UpdateVisionOfNearbyEntitiesDelayed(Entity.OriginNode.MeshCenterWorldPosition, callback: Entity.UpdateVisibility);
                    else // If this entity doesn't block vision, only update the vision of itself and of entities from other actors
                    {
                        World.UpdateVisionOfNearbyEntitiesDelayed(Entity.OriginNode.MeshCenterWorldPosition, callback: Entity.UpdateVisibility, excludeActor: Entity.Actor);
                        Entity.UpdateVision();
                    }
                }
                if (finishedTransition) ReachNextNode();


                // Update transform if visible
                if (Entity.IsVisibleBy(World.ActiveVisionActor))
                {
                    Entity.MeshObject.transform.position = Entity.WorldPosition;
                    Entity.MeshObject.transform.rotation = Entity.WorldRotation;
                }
            }
        }

        public override void Validate()
        {
            if (Entity.Height > MaxEntityHeight) throw new System.Exception($"Height cannot be greater than {MaxEntityHeight} for moving entities.");
        }

        #region Actions

        /// <summary>
        /// Finds a path and walks towards the target node
        /// </summary>
        public void MoveTo(BlockmapNode target)
        {
            // Get node where to start from. If we are moving take the next node in our current path. Else just where we are standing now.
            BlockmapNode startNode = IsMoving ? CurrentTransition.To : Entity.OriginNode;

            // Find path to target
            NavigationPath path = Pathfinder.GetPath(Entity, startNode, target);

            // Check if we found a valid path
            if (path == null)
            {
                Stop();
                return;
            }

            // If we are still moving, add that transition we're currently on as the first transition of the path
            if (IsMoving) path.Transitions.Insert(0, CurrentTransition);

            // Set new path
            MoveAlong(path);
        }

        /// <summary>
        /// Starts moving according to the given path.
        /// </summary>
        public void MoveAlong(NavigationPath path)
        {
            if (!IsMoving) // If we are standing still, set the first transition and discard the first node since its the one we stand on and therefore already reached it.
            {
                SetCurrentTransition(path.Transitions[0]);
                path.RemoveFirstNode();
            }

            TargetPath = path;
            IsMoving = true;
            OnNewPath?.Invoke();
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
        private void FindNewPathToTarget()
        {
            MoveTo(Target);
        }

        /// <summary>
        /// Gets triggered when a node of the target path is reached. Updates the NextNode and MoveDirection
        /// </summary>
        private void ReachNextNode()
        {
            BlockmapNode reachedNode = CurrentTransition.To;
            TargetPath.RemoveFirstTransition();

            // Update the last known position of this entity for all actors that can currently see it
            foreach (Entity e in Entity.SeenBy) Entity.UpdateLastKnownPositionFor(e.Actor);

            // Target reached
            if(reachedNode == Target)
            {
                Stop();
                OnTargetReached?.Invoke();
            }
            // Target not yet reached
            else
            {
                Transition newTransition = TargetPath.Transitions[0];
                BlockmapNode newNextNode = newTransition.To;

                // TargetPath is still valid => take that path
                if (newTransition != null && newTransition.CanPass(Entity))
                {
                    IsMoving = true;
                    newNextNode.AddEntity(Entity);
                    TargetPath.RemoveFirstNode();
                    SetCurrentTransition(newTransition);
                }
                // TargetPath is no longer valid, find new path
                else
                {
                    // Debug.Log("Target path no longer valid, finding new path");
                    FindNewPathToTarget();
                }

            }

        }

        private void SetCurrentTransition(Transition t)
        {
            CurrentTransition = t;
            CurrentTransition.OnTransitionStart(Entity);
        }

        /// <summary>
        /// Stops all movement instantly.
        /// </summary>
        private void Stop()
        {
            IsMoving = false;
            CurrentTransition = null;
            TargetPath = null;
            OnStopMoving?.Invoke();
        }

        #endregion

        #region Override Actions

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

        public void EnableOverrideMaxHopUpDistance(int value)
        {
            isOverrideMaxHopUpDistanceActive = true;
            overrideMaxHopUpDistance = value;
        }
        public void DisableOverrideMaxHopDistance()
        {
            isOverrideMaxHopUpDistanceActive = false;
        }

        public void EnableOverrideMaxHopDownDistance(int value)
        {
            isOverrideMaxHopDownDistanceActive = true;
            overrideMaxHopDownDistance = value;
        }
        public void DisableOverrideMaxDropDistance()
        {
            isOverrideMaxHopDownDistanceActive = false;
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
        /// The maximum height the entity can hop upwards to an adjacent node.
        /// </summary>
        public int MaxHopUpDistance => isOverrideMaxHopUpDistanceActive ? overrideMaxHopUpDistance : Props.MaxHopUpDistance;

        /// <summary>
        /// The maximum height the entity can hop downwards to an adjacent node.
        /// </summary>
        public int MaxHopDownDistance => isOverrideMaxHopDownDistanceActive ? overrideMaxHopDownDistance : Props.MaxHopDownDistance;

        /// <summary>
        /// Returns if the target node is reachable with a path that costs less than the given limit.
        /// </summary>
        public bool IsInRange(BlockmapNode targetNode, float maxCost)
        {
            // First check if the target node is even close. If not: skip detailed check
            if (Vector2.Distance(Entity.OriginNode.WorldCoordinates, targetNode.WorldCoordinates) > maxCost) return false;

            // Setup
            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();

            // Start with origin node
            BlockmapNode sourceNode = Entity.OriginNode;
            priorityQueue.Add(sourceNode, 0f);
            nodeCosts.Add(sourceNode, 0f);

            while (priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach (Transition t in currentNode.Transitions)
                {
                    BlockmapNode toNode = t.To;
                    float transitionCost = t.GetMovementCost(Entity);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > maxCost) continue; // not in range
                    if (!t.CanPass(Entity)) continue; // transition not passable for this character

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