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
        public ClimbingCategory ClimbSkillRequirement { get; private set; }

        public DoubleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, List<IClimbable> climbUp, List<IClimbable> climbDown, int maxHeight) : base(from, to, dir, maxHeight)
        {
            ClimbUp = climbUp;
            ClimbDown = climbDown;

            StartHeight = from.GetMinAltitude(dir);
            EndHeight = to.GetMaxAltitude(HelperFunctions.GetOppositeDirection(dir));

            HeightUp = climbUp.Count;
            HeightDown = climbDown.Count;

            ClimbSkillRequirement = (ClimbingCategory)(climbUp.Concat(climbDown).Max(x => (int)x.ClimbSkillRequirement));
        }

        public override float GetMovementCost(Entity entity)
        {
            float value = (0.5f * From.GetMovementCost(entity)) + (0.5f * To.GetMovementCost(entity)); // Cost of moving between start and end tile

            // Add cost of climbing
            foreach (IClimbable climb in ClimbUp) value += climb.GetClimbCostUp(entity);
            foreach (IClimbable climb in ClimbDown) value += climb.GetClimbCostDown(entity);

            return value;
        }

        public override bool CanPass(Entity entity)
        {
            // Climb skill
            if ((int)entity.ClimbingSkill < (int)ClimbSkillRequirement) return false;

            return base.CanPass(entity);
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
                        Vector3 startClimbPoint = GetClimbUpStartPoint(entity, 0);
                        Vector2 startClimbPoint2d = new Vector2(startClimbPoint.x, startClimbPoint.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, startClimbPoint2d, entity.GetCurrentWalkingSpeed() * Time.deltaTime);

                        // Calculate altitude
                        float y = World.GetWorldAltitudeAt(newPosition2d, From);
                        if (From.Type == NodeType.Water) y -= entity.WorldHeight / 2f;

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector2.Distance(newPosition2d, startClimbPoint2d) <= REACH_EPSILON)
                        {
                            moveComp.ClimbPhase = ClimbPhase.ClimbUp;
                            moveComp.ClimbIndex = 0;
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
                        int index = moveComp.ClimbIndex;
                        IClimbable climb = ClimbUp[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbUpEndPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.GetClimbSpeedUp(entity));

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            moveComp.ClimbIndex++;

                            if (moveComp.ClimbIndex == ClimbUp.Count) // Reached the top
                            {
                                moveComp.ClimbIndex = ClimbDown.Count - 1;
                                moveComp.ClimbPhase = ClimbPhase.ClimbTransfer;
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
                        int index = moveComp.ClimbIndex;
                        IClimbable climb = ClimbDown[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbDownStartPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.GetClimbSpeedUp(entity));

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            moveComp.ClimbPhase = ClimbPhase.ClimbDown;
                            entity.SetWorldRotation(HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetOppositeDirection(Direction))); // Look at fence
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.ClimbDown:
                    {
                        // Get where exactly we are within the climb
                        int index = moveComp.ClimbIndex;
                        IClimbable climb = ClimbDown[index];

                        // Move towards climb end
                        Vector3 nextPoint = GetClimbDownEndPoint(entity, index);
                        Vector3 newPosition = Vector3.MoveTowards(entity.WorldPosition, nextPoint, Time.deltaTime * climb.GetClimbSpeedDown(entity));

                        // Set new position
                        entity.SetWorldPosition(newPosition);

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, nextPoint) <= REACH_EPSILON)
                        {
                            moveComp.ClimbIndex--;

                            if (moveComp.ClimbIndex == -1) // Reached the bottom
                            {
                                moveComp.ClimbPhase = ClimbPhase.PostClimb;
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
                        Vector3 endPosition = To.MeshCenterWorldPosition;
                        Vector2 endPosition2d = new Vector2(endPosition.x, endPosition.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, endPosition2d, entity.GetCurrentWalkingSpeed() * Time.deltaTime);

                        // Calculate altitude
                        float y = World.GetWorldAltitudeAt(newPosition2d, To);
                        if (To.Type == NodeType.Water) y -= entity.WorldHeight / 2f;

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

        private Vector3 GetClimbUpStartPoint(Entity entity, int index)
        {
            IClimbable climb = ClimbUp[index];
            float y = (StartHeight + index) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: true);
        }
        private Vector3 GetClimbUpEndPoint(Entity entity, int index)
        {
            IClimbable climb = ClimbUp[index];
            float y = (StartHeight + (index + 1)) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: true);
        }
        private Vector3 GetClimbDownStartPoint(Entity entity, int index)
        {
            IClimbable climb = ClimbDown[index];
            float y = (EndHeight + (index + 1)) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: false);
        }
        private Vector3 GetClimbDownEndPoint(Entity entity, int index)
        {
            IClimbable climb = ClimbDown[index];
            float y = (EndHeight + index) * World.NodeHeight;

            return new Vector3(From.WorldCoordinates.x + 0.5f, y, From.WorldCoordinates.y + 0.5f) + GetOffset(entity, climb, isAscend: false);
        }
        private Vector3 GetOffset(Entity entity, IClimbable climb, bool isAscend)
        {
            Direction climbDir = isAscend ? Direction : HelperFunctions.GetOppositeDirection(Direction);
            float offset = (climbDir == climb.ClimbSide) ? climb.ClimbTransformOffset : 0f;

            float entityLength = (entity != null) ? entity.WorldSize.x / 2f : 0f;
            if (isAscend) return new Vector3(HelperFunctions.GetDirectionVector(Direction).x, 0f, HelperFunctions.GetDirectionVector(Direction).y) * (0.5f - entityLength - offset);
            else return new Vector3(HelperFunctions.GetDirectionVector(Direction).x, 0f, HelperFunctions.GetDirectionVector(Direction).y) * (0.5f + entityLength + offset);
        }

        public override List<Vector3> GetPreviewPath()
        {
            return new List<Vector3>() { From.MeshCenterWorldPosition, GetClimbDownStartPoint(null, ClimbDown.Count - 1), To.MeshCenterWorldPosition };
        }
    }
}
