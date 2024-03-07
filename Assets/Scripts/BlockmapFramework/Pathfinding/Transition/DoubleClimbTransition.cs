using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition for when going to an adjacent node and having to climb up AND down between it.
    /// </summary>
    public class DoubleClimbTransition : Transition
    {
        public List<IClimbable> ClimbUp { get; private set; }
        public List<IClimbable> ClimbDown { get; private set; }

        public int StartHeight { get; private set; }
        public int EndHeight { get; private set; }
        public int HeightUp { get; private set; }
        public int HeightDown { get; private set; }
        public int HeadSpaceRequirementUp { get; private set; }
        public int HeadSpaceRequirementDown { get; private set; }
        public ClimbingCategory ClimbSkillRequirement { get; private set; }

        public DoubleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, List<IClimbable> climbUp, List<IClimbable> climbDown) : base(from, to)
        {
            Direction = dir;
            ClimbUp = climbUp;
            ClimbDown = climbDown;

            StartHeight = from.GetMinHeight(dir);
            EndHeight = to.GetMaxHeight(HelperFunctions.GetOppositeDirection(dir));

            HeightUp = climbUp.Count;
            HeightDown = climbDown.Count;

            // Calculate head space needed to use this
            HeadSpaceRequirementUp = From.GetFreeHeadSpace(dir) - HeightUp;
            HeadSpaceRequirementDown = To.GetFreeHeadSpace(HelperFunctions.GetOppositeDirection(dir)) - HeightDown;

            ClimbSkillRequirement = (ClimbingCategory)(climbUp.Concat(climbDown).Max(x => (int)x.SkillRequirement));
        }

        public override float GetMovementCost(MovingEntity entity)
        {
            float value = (0.5f * (1f / From.GetSurfaceProperties().SpeedModifier)) + (0.5f * (1f / To.GetSurfaceProperties().SpeedModifier)); // Cost of moving between start and end tile

            // Add cost of climbing
            foreach (IClimbable climb in ClimbUp) value += climb.CostUp;
            foreach (IClimbable climb in ClimbDown) value += climb.CostDown;

            return value;
        }

        public override bool CanPass(MovingEntity entity)
        {
            // Headspace
            if (entity.Height > HeadSpaceRequirementUp) return false;
            if (entity.Height > HeadSpaceRequirementDown) return false;

            // Climb skill
            if ((int)entity.ClimbingSkill < (int)ClimbSkillRequirement) return false;

            // Height
            foreach (IClimbable climb in ClimbUp)
                if (HeightUp > climb.MaxClimbHeight(entity.ClimbingSkill)) return false;
            foreach (IClimbable climb in ClimbDown)
                if (HeightDown > climb.MaxClimbHeight(entity.ClimbingSkill)) return false;

            // Base requirements (adapted to ignore ladder)
            if (!From.IsPassable(Direction, entity, checkClimbables: false)) return false;
            if (!To.IsPassable(HelperFunctions.GetOppositeDirection(Direction), entity, checkClimbables: false)) return false;

            return true;
        }

        public override void OnTransitionStart(MovingEntity entity)
        {
            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
            entity.ClimbPhase = ClimbPhase.PreClimb;
        }

        public override void UpdateEntityMovement(MovingEntity entity, out bool finishedTransition, out BlockmapNode currentNode)
        {
            switch (entity.ClimbPhase)
            {
                case ClimbPhase.PreClimb:
                    {
                        // Get current entity position
                        Vector2 entityPosition2d = new Vector2(entity.WorldPosition.x, entity.WorldPosition.z);

                        // Get 2d position of climb start
                        Vector3 startClimbPoint = GetClimbUpStartPoint(entity, 0);
                        Vector2 startClimbPoint2d = new Vector2(startClimbPoint.x, startClimbPoint.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, startClimbPoint2d, entity.MovementSpeed * Time.deltaTime * From.GetSurfaceProperties().SpeedModifier);

                        // Calculate altitude
                        float y = World.GetWorldHeightAt(newPosition2d, From);
                        if (From.Type == NodeType.Water) y -= entity.WorldHeight / 2f;

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector2.Distance(newPosition2d, startClimbPoint2d) <= REACH_EPSILON)
                        {
                            entity.ClimbPhase = ClimbPhase.ClimbUp;
                            entity.ClimbIndex = 0;
                            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }
                case ClimbPhase.ClimbUp:
                    {
                        // Get where exactly we are within the climb
                        int index = entity.ClimbIndex;
                        IClimbable climb = ClimbUp[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbUpEndPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.SpeedUp);

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            entity.ClimbIndex++;

                            if (entity.ClimbIndex == ClimbUp.Count) // Reached the top
                            {
                                entity.ClimbIndex = ClimbDown.Count - 1;
                                entity.ClimbPhase = ClimbPhase.ClimbTransfer;
                                entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
                            }
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }
                case ClimbPhase.ClimbTransfer:
                    {
                        // Get where exactly we are within the climb
                        int index = entity.ClimbIndex;
                        IClimbable climb = ClimbDown[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbDownStartPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.SpeedUp);

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            entity.ClimbPhase = ClimbPhase.ClimbDown;
                            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetOppositeDirection(Direction))); // Look at wall
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.ClimbDown:
                    {
                        // Get where exactly we are within the climb
                        int index = entity.ClimbIndex;
                        IClimbable climb = ClimbDown[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbDownEndPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.SpeedDown);

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            entity.ClimbIndex--;

                            if (entity.ClimbIndex == -1) // Reached the bottom
                            {
                                entity.ClimbPhase = ClimbPhase.PostClimb;
                                entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(Direction)); // Look straight ahead
                            }
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = To;
                        return;
                    }

                case ClimbPhase.PostClimb:
                    {
                        // Get current entity position
                        Vector2 entityPosition2d = new Vector2(entity.WorldPosition.x, entity.WorldPosition.z);

                        // Get 2d position of next node
                        Vector3 endPosition = To.GetCenterWorldPosition();
                        Vector2 endPosition2d = new Vector2(endPosition.x, endPosition.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, endPosition2d, entity.MovementSpeed * Time.deltaTime * To.GetSurfaceProperties().SpeedModifier);

                        // Calculate altitude
                        float y = World.GetWorldHeightAt(newPosition2d, To);
                        if (To.Type == NodeType.Water) y -= entity.WorldHeight / 2f;

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.SetWorldPosition(newPosition);

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

        private Vector3 GetClimbUpStartPoint(MovingEntity entity, int index)
        {
            IClimbable climb = ClimbUp[index];
            float y = (StartHeight + index) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: true);
        }
        private Vector3 GetClimbUpEndPoint(MovingEntity entity, int index)
        {
            IClimbable climb = ClimbUp[index];
            float y = (StartHeight + (index + 1)) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: true);
        }
        private Vector3 GetClimbDownStartPoint(MovingEntity entity, int index)
        {
            IClimbable climb = ClimbDown[index];
            float y = (EndHeight + (index + 1)) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: false);
        }
        private Vector3 GetClimbDownEndPoint(MovingEntity entity, int index)
        {
            IClimbable climb = ClimbDown[index];
            float y = (EndHeight + index) * World.TILE_HEIGHT;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: false);
        }
        private Vector3 GetOffset(MovingEntity entity, IClimbable climb, bool isAscend)
        {
            Direction climbDir = isAscend ? Direction : HelperFunctions.GetOppositeDirection(Direction);
            float offset = (climbDir == climb.ClimbSide) ? climb.TransformOffset : 0f;

            float entityLength = (entity != null) ? entity.WorldSize.x / 2f : 0f;
            if (isAscend) return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f - entityLength - offset);
            else return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f + entityLength + offset);
        }

        public override List<Vector3> GetPreviewPath()
        {
            return new List<Vector3>() { From.GetCenterWorldPosition(), GetClimbDownStartPoint(null, ClimbDown.Count - 1), To.GetCenterWorldPosition() };
        }
    }
}
