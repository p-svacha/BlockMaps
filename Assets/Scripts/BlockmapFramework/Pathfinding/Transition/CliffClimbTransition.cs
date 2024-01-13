using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class CliffClimbTransition : Transition
    {
        private const float COST_PER_TILE_UP = 2.5f;
        private const float COST_PER_TILE_DOWN = 1.5f;

        private const float SPEED_ON_CLIMB_UP = 0.3f;
        private const float SPEED_ON_CLIMB_DOWN = 0.4f;

        public bool IsAscend { get; private set; } // true when climbing up, false when climbing down
        public int StartHeight { get; private set; }
        public int EndHeight { get; private set; }

        public CliffClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir) : base(from, to)
        {
            Direction = dir;
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);

            StartHeight = from.Height.Where(x => HelperFunctions.GetAffectedCorners(dir).Contains(x.Key)).Min(x => x.Value);
            EndHeight = to.Height.Where(x => HelperFunctions.GetAffectedCorners(oppositeDir).Contains(x.Key)).Max(x => x.Value);

            IsAscend = (EndHeight > StartHeight);

            if (StartHeight == EndHeight) throw new System.Exception("Can't create cliff climb transition on equal heights from " + From.ToString() + " to " + To.ToString() + ".");
        }

        public override float GetMovementCost(MovingEntity entity)
        {
            float value = (0.5f * (1f / From.GetSpeedModifier())) + (0.5f * (1f / To.GetSpeedModifier())); // Cost of moving between start and end tile

            // Add cost of climbing
            int deltaY = Mathf.Abs(EndHeight - StartHeight);
            value += deltaY * (IsAscend ? COST_PER_TILE_UP : COST_PER_TILE_DOWN);

            return value;
        }

        public override void OnTransitionStart(MovingEntity entity)
        {
            entity.transform.rotation = Entity.Get2dRotationByDirection(Direction); // Look straight ahead
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
                        Vector3 startClimbPoint = GetStartClimbPoint(entity);
                        Vector2 startClimbPoint2d = new Vector2(startClimbPoint.x, startClimbPoint.z);

                        // Calculate new 2d world position and coordinates by moving towards next node in 2d
                        Vector2 newPosition2d = Vector2.MoveTowards(entityPosition2d, startClimbPoint2d, entity.MovementSpeed * Time.deltaTime * From.GetSpeedModifier());

                        // Calculate altitude
                        float y = World.GetWorldHeightAt(newPosition2d, From);
                        if (From.Type == NodeType.Water) y -= (entity.Dimensions.y * World.TILE_HEIGHT) / 2f;

                        // Set new position
                        Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
                        entity.transform.position = newPosition;

                        // Check if we reach next phase
                        if (Vector2.Distance(newPosition2d, startClimbPoint2d) <= REACH_EPSILON)
                        {
                            entity.ClimbPhase = ClimbPhase.InClimb;
                            entity.transform.rotation = Entity.Get2dRotationByDirection(IsAscend ? Direction : HelperFunctions.GetOppositeDirection(Direction)); // Look at cliff wall
                        }

                        // Out params
                        finishedTransition = false;
                        currentNode = From;
                        return;
                    }

                case ClimbPhase.InClimb:
                    {
                        // Move towards climb end
                        Vector3 endClimbPoint = GetEndClimbPoint(entity);
                        Vector3 newPosition = Vector3.MoveTowards(entity.transform.position, endClimbPoint, Time.deltaTime * (IsAscend ? SPEED_ON_CLIMB_UP :SPEED_ON_CLIMB_DOWN));

                        // Set new position
                        entity.transform.position = newPosition;

                        // Check if we reach next phase
                        if (Vector3.Distance(newPosition, endClimbPoint) <= REACH_EPSILON)
                        {
                            entity.ClimbPhase = ClimbPhase.PostClimb;
                            entity.transform.rotation = Entity.Get2dRotationByDirection(Direction); // Look straight ahead
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
                        if (To.Type == NodeType.Water) y -= (entity.Dimensions.y * World.TILE_HEIGHT) / 2f;

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

        private Vector3 GetStartClimbPoint(MovingEntity entity)
        {
            return new Vector3(From.WorldCoordinates.x + 0.5f, World.GetWorldHeight(StartHeight), From.WorldCoordinates.y + 0.5f) + GetOffset(entity);
        }
        private Vector3 GetEndClimbPoint(MovingEntity entity)
        {
            return new Vector3(From.WorldCoordinates.x + 0.5f, World.GetWorldHeight(EndHeight), From.WorldCoordinates.y + 0.5f) + GetOffset(entity);
        }
        private Vector3 GetOffset(MovingEntity entity)
        {
            if(IsAscend) return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f - entity.WorldSize.x / 2f);
            else return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f + entity.WorldSize.x / 2f);
        }
    }
}
