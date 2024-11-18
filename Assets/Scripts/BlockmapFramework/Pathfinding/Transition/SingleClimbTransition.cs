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
        public BlockmapNode LowerNode { get; private set; }
        public BlockmapNode HigherNode { get; private set; }
        public int StartHeight { get; private set; }
        public int EndHeight { get; private set; }
        public int Height { get; private set; }
        public Direction ClimbDirection { get; private set; } // Side on node that the MovingEntity is at when climbing (it's always on the lower node)
        public ClimbingCategory ClimbSkillRequirement { get; private set; }


        /// <summary>
        /// Each element of this list represents one tile of climbing on that climbing surface.
        /// <br/> Always ordered so the lowest comes first.
        /// </summary>
        public List<IClimbable> Climb { get; private set; }

        public SingleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, List<IClimbable> climb, int maxHeight) : base(from, to, maxHeight)
        {
            Direction = dir;
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            Climb = climb;

            StartHeight = from.GetMinAltitude(dir);
            EndHeight = to.GetMaxAltitude(oppositeDir);
            Height = Mathf.Abs(EndHeight - StartHeight);

            IsAscend = (EndHeight > StartHeight);
            if (StartHeight == EndHeight) throw new System.Exception("Can't create cliff climb transition on equal heights from " + From.ToString() + " to " + To.ToString() + ".");

            LowerNode = IsAscend ? From : To;
            HigherNode = IsAscend ? To : From;

            ClimbDirection = IsAscend ? dir : oppositeDir;

            ClimbSkillRequirement = (ClimbingCategory)(climb.Max(x => (int)x.ClimbSkillRequirement));
        }

        public override float GetMovementCost(Entity entity)
        {
            float value = (0.5f * (1f / From.SurfaceDef.MovementSpeedModifier)) + (0.5f * (1f / To.SurfaceDef.MovementSpeedModifier)); // Cost of moving between start and end tile

            // Add cost of climbing
            foreach (IClimbable climb in Climb) value += IsAscend ? climb.ClimbCostUp : climb.ClimbCostDown;

            return value;
        }

        public override bool CanPass(Entity entity)
        {
            // Entity height
            if (entity.Height > MaxHeight) return false;

            // Climb skill
            if ((int)entity.GetComponent<Comp_Movement>().ClimbingSkill < (int)ClimbSkillRequirement) return false;

            // Base requirements (lower node needs to ignore climbables)
            if (!HigherNode.IsPassable(HelperFunctions.GetOppositeDirection(ClimbDirection), entity)) return false;

            return true;
        }

        public override void OnTransitionStart(Entity entity)
        {
            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
            entity.GetComponent<Comp_Movement>().ClimbPhase = ClimbPhase.PreClimb;
        }
        public override void UpdateEntityMovement(Entity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            Comp_Movement moveComp = entity.GetComponent<Comp_Movement>();

            switch (moveComp.ClimbPhase)
            {
                case ClimbPhase.PreClimb:
                    {
                        // Get current entity position
                        Vector2 entityPosition2d = new Vector2(entity.WorldPosition.x, entity.WorldPosition.z);

                        // Get 2d position of climb start
                        Vector3 startClimbPoint = GetStartClimbPoint(entity, Climb[0], 0);
                        Vector2 startClimbPoint2d = new Vector2(startClimbPoint.x, startClimbPoint.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, startClimbPoint2d, moveComp.MovementSpeed * Time.deltaTime * From.SurfaceDef.MovementSpeedModifier);

                        // Calculate y coordinate
                        float y;
                        if (World.IsOnNode(newPosition2d, From))
                        {
                            y = World.GetWorldHeightAt(newPosition2d, From);
                            if (From.Type == NodeType.Water) y -= entity.WorldHeight / 2f;
                            if (!IsAscend && y < World.GetWorldHeight(StartHeight)) y = World.GetWorldHeight(StartHeight);
                        }
                        else y = World.NodeHeight * From.GetMinAltitude(Direction);

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector2.Distance(newPosition2d, startClimbPoint2d) <= REACH_EPSILON)
                        {
                            moveComp.ClimbPhase = ClimbPhase.InClimb;
                            moveComp.ClimbIndex = 0;
                            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(IsAscend ? Direction : HelperFunctions.GetOppositeDirection(Direction))); // Look at fence
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.InClimb:
                    {
                        // Get where exactly we are within the climb
                        int index = moveComp.ClimbIndex;
                        IClimbable climb = Climb[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetEndClimbPoint(entity, climb, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * (IsAscend ? climb.ClimbSpeedUp : climb.ClimbSpeedDown));

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            moveComp.ClimbIndex++;

                            if (moveComp.ClimbIndex == Climb.Count) // Reached the top
                            {
                                moveComp.ClimbPhase = ClimbPhase.PostClimb;
                                entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
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
                        Vector2 entityPosition2d = new Vector2(entity.WorldPosition.x, entity.WorldPosition.z);

                        // Get 2d position of next node
                        Vector3 endPosition = To.CenterWorldPosition;
                        Vector2 endPosition2d = new Vector2(endPosition.x, endPosition.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, endPosition2d, moveComp.MovementSpeed * Time.deltaTime * To.SurfaceDef.MovementSpeedModifier);

                        // Calculate altitude
                        float y;
                        if (World.IsOnNode(newPosition2d, To))
                        {
                            y = World.GetWorldHeightAt(newPosition2d, To);
                            if (To.Type == NodeType.Water) y -= entity.WorldHeight / 2f;
                        }
                        else y = World.NodeHeight * To.GetMinAltitude(HelperFunctions.GetOppositeDirection(Direction));

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.SetWorldPosition(newPosition);

                        // Out params
                        finishedTransition = false;
                        if (Vector2.Distance(newPosition2d, endPosition2d) <= REACH_EPSILON)
                        {
                            finishedTransition = true;
                            moveComp.ClimbPhase = ClimbPhase.None;
                        }
                        currentNode = To;
                        return;
                    }
            }
            throw new System.Exception("Should never be reached, climbphase = " + moveComp.ClimbPhase.ToString());
        }

        private Vector3 GetStartClimbPoint(Entity entity, IClimbable climb, int index)
        {
            float y;
            if (IsAscend) y = (StartHeight + index) * World.NodeHeight;
            else y = (StartHeight - index) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb);
        }
        private Vector3 GetEndClimbPoint(Entity entity, IClimbable climb, int index)
        {
            float y;
            if (IsAscend) y = (StartHeight + (index + 1)) * World.NodeHeight;
            else y = (StartHeight - (index + 1)) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb);
        }
        private Vector3 GetOffset(Entity entity, IClimbable climb)
        {
            float offset = (ClimbDirection == climb.ClimbSide) ? climb.ClimbTransformOffset : 0f;

            float entityLength = (entity != null) ? entity.WorldSize.x / 2f : 0f;
            if (IsAscend) return new Vector3(HelperFunctions.GetDirectionVector(Direction).x, 0f, HelperFunctions.GetDirectionVector(Direction).y) * (0.5f - entityLength - offset);
            else return new Vector3(HelperFunctions.GetDirectionVector(Direction).x, 0f, HelperFunctions.GetDirectionVector(Direction).y) * (0.5f + entityLength + offset);
        }

        public override List<Vector3> GetPreviewPath()
        {
            if(IsAscend)
                return new List<Vector3>() { From.CenterWorldPosition, GetEndClimbPoint(null, Climb.Last(), Climb.Count - 1), To.CenterWorldPosition };
            else
                return new List<Vector3>() { From.CenterWorldPosition, GetStartClimbPoint(null, Climb[0], 0), To.CenterWorldPosition };
        }
    }
}
