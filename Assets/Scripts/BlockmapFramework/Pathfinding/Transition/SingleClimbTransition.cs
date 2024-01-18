using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition for when going to an adjacent node and having to climb up OR down between it.
    /// </summary>
    public class SingleClimbTransition : Transition
    {
        public bool IsAscend { get; private set; } // true when climbing up, false when climbing down
        public int StartHeight { get; private set; }
        public int EndHeight { get; private set; }
        public int Height { get; private set; }
        public int HeadSpaceRequirement { get; private set; }
        public ClimbingCategory ClimbSkillRequirement { get; private set; }

        /// <summary>
        /// Each element of this list represents one tile of climbing on that climbing surface.
        /// </summary>
        public List<IClimbable> Climb { get; private set; }

        public SingleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, List<IClimbable> climb) : base(from, to)
        {
            Direction = dir;
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            Climb = climb;

            StartHeight = from.GetMinHeight(dir);
            EndHeight = to.GetMaxHeight(oppositeDir);
            Height = Mathf.Abs(EndHeight - StartHeight);

            IsAscend = (EndHeight > StartHeight);

            if (StartHeight == EndHeight) throw new System.Exception("Can't create cliff climb transition on equal heights from " + From.ToString() + " to " + To.ToString() + ".");

            // Calculate head space needed to use this
            BlockmapNode lowerNode = IsAscend ? From : To;
            Direction lowerDir = IsAscend ? dir : oppositeDir;
            HeadSpaceRequirement = lowerNode.GetFreeHeadSpace(lowerDir) - Height;

            ClimbSkillRequirement = (ClimbingCategory)(climb.Max(x => (int)x.SkillRequirement));
        }

        public override float GetMovementCost(MovingEntity entity)
        {
            float value = (0.5f * (1f / From.GetSpeedModifier())) + (0.5f * (1f / To.GetSpeedModifier())); // Cost of moving between start and end tile

            // Add cost of climbing
            foreach (IClimbable climb in Climb) value += IsAscend ? climb.CostUp : climb.CostDown;

            return value;
        }

        public override bool CanPass(MovingEntity entity)
        {
            // Headspace
            if (entity.Height > HeadSpaceRequirement) return false;

            // Climb skill
            if ((int)entity.ClimbingSkill < (int)ClimbSkillRequirement) return false;

            // Height
            foreach (IClimbable climb in Climb)
                if (Height > climb.MaxClimbHeight(entity.ClimbingSkill)) return false;

            // Base requirements (adapted to ignore ladder)
            if (!From.IsPassable(Direction, entity, checkLadder: false)) return false;
            if (!To.IsPassable(HelperFunctions.GetOppositeDirection(Direction), entity, checkLadder: false)) return false;

            return true;
        }

        public override void OnTransitionStart(MovingEntity entity)
        {
            entity.transform.rotation = HelperFunctions.Get2dRotationByDirection(Direction); // Look straight ahead
            entity.ClimbPhase = ClimbPhase.PreClimb;
        }
        public override void UpdateEntityMovement(MovingEntity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            switch (entity.ClimbPhase)
            {
                case ClimbPhase.PreClimb:
                    {
                        // Get current entity position
                        Vector2 entityPosition2d = new Vector2(entity.transform.position.x, entity.transform.position.z);

                        // Get 2d position of climb start
                        Vector3 startClimbPoint = GetStartClimbPoint(entity, Climb[0], 0);
                        Vector2 startClimbPoint2d = new Vector2(startClimbPoint.x, startClimbPoint.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, startClimbPoint2d, entity.MovementSpeed * Time.deltaTime * From.GetSpeedModifier());

                        // Calculate altitude
                        float y = World.GetWorldHeightAt(newPosition2d, From);
                        if (From.Type == NodeType.Water) y -= entity.WorldHeight / 2f;
                        if (!IsAscend && y < World.GetWorldHeight(StartHeight)) y = World.GetWorldHeight(StartHeight);

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.transform.position = newPosition;

                        // Check if we reach next phase
                        if (Vector2.Distance(newPosition2d, startClimbPoint2d) <= REACH_EPSILON)
                        {
                            entity.ClimbPhase = ClimbPhase.InClimb;
                            entity.ClimbIndex = 0;
                            entity.transform.rotation = HelperFunctions.Get2dRotationByDirection(IsAscend ? Direction : HelperFunctions.GetOppositeDirection(Direction)); // Look at cliff wall
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.InClimb:
                    {
                        // Get where exactly we are within the climb
                        int index = entity.ClimbIndex;
                        IClimbable climb = Climb[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetEndClimbPoint(entity, climb, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.transform.position, nextPoint, Time.deltaTime * (IsAscend ? climb.SpeedUp : climb.SpeedDown));

                        // Set new position
                        entity.transform.position = newPosition;

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            entity.ClimbIndex++;

                            if (entity.ClimbIndex == Climb.Count) // Reached the top
                            {
                                entity.ClimbPhase = ClimbPhase.PostClimb;
                                entity.transform.rotation = HelperFunctions.Get2dRotationByDirection(Direction); // Look straight ahead
                            }
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.PostClimb:
                    {
                        // Get current entity position
                        Vector2 entityPosition2d = new Vector2(entity.transform.position.x, entity.transform.position.z);

                        // Get 2d position of next node
                        Vector3 endPosition = To.GetCenterWorldPosition();
                        Vector2 endPosition2d = new Vector2(endPosition.x, endPosition.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, endPosition2d, entity.MovementSpeed * Time.deltaTime * To.GetSpeedModifier());

                        // Calculate altitude
                        float y = World.GetWorldHeightAt(newPosition2d, To);
                        if (To.Type == NodeType.Water) y -= entity.WorldHeight / 2f;

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.transform.position = newPosition;

                        // Out params
                        finishedTransition = false;
                        if (Vector2.Distance(newPosition2d, endPosition2d) <= REACH_EPSILON)
                        {
                            finishedTransition = true;
                            entity.ClimbPhase = ClimbPhase.None;
                        }
                        currentNode = To;
                        return;
                    }
            }
            throw new System.Exception("Should never be reached, climbphase = " + entity.ClimbPhase.ToString());
        }

        private Vector3 GetStartClimbPoint(MovingEntity entity, IClimbable climb, int index)
        {
            float y;
            if (IsAscend) y = (StartHeight + index) * World.TILE_HEIGHT;
            else y = (StartHeight - index) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb);
        }
        private Vector3 GetEndClimbPoint(MovingEntity entity, IClimbable climb, int index)
        {
            float y;
            if (IsAscend) y = (StartHeight + (index + 1)) * World.TILE_HEIGHT;
            else y = (StartHeight - (index + 1)) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb);
        }
        private Vector3 GetOffset(MovingEntity entity, IClimbable climb)
        {
            if(IsAscend) return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f - entity.WorldSize.x / 2f - climb.TransformOffset);
            else return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f + entity.WorldSize.x / 2f + climb.TransformOffset);
        }
    }
}
