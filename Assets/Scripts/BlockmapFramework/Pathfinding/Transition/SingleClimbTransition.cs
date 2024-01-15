using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class SingleClimbTransition : Transition
    {
        public const float CLIFF_COST_UP = 2.5f;
        public const float CLIFF_COST_DOWN = 1.5f;
        public const float CLIFF_SPEED_UP = 0.3f;
        public const float CLIFF_SPEED_DOWN = 0.4f;

        public const float LADDER_COST_UP = 1.6f;
        public const float LADDER_COST_DOWN = 1.3f;
        public const float LADDER_SPEED_UP = 0.65f;
        public const float LADDER_SPEED_DOWN = 0.75f;

        public bool IsAscend { get; private set; } // true when climbing up, false when climbing down
        public int StartHeight { get; private set; }
        public int EndHeight { get; private set; }
        public int Height { get; private set; }
        public int HeadSpaceRequirement { get; private set; }

        public float ClimbCostPerTile { get; private set; }
        public float ClimbSpeed { get; private set; }

        public float TransformOffset { get; private set; }

        public SingleClimbTransition(BlockmapNode from, BlockmapNode to, Direction dir, float cost, float speed, float transformOffset) : base(from, to)
        {
            Direction = dir;
            Direction oppositeDir = HelperFunctions.GetOppositeDirection(dir);
            ClimbCostPerTile = cost;
            ClimbSpeed = speed;
            TransformOffset = transformOffset;

            StartHeight = from.GetMinHeight(dir);
            EndHeight = to.GetMaxHeight(oppositeDir);
            Height = Mathf.Abs(EndHeight - StartHeight);

            IsAscend = (EndHeight > StartHeight);

            if (StartHeight == EndHeight) throw new System.Exception("Can't create cliff climb transition on equal heights from " + From.ToString() + " to " + To.ToString() + ".");

            // Calculate head space needed to use this
            BlockmapNode lowerNode = IsAscend ? From : To;
            Direction lowerDir = IsAscend ? dir : oppositeDir;
            HeadSpaceRequirement = lowerNode.GetFreeHeadSpace(lowerDir) - Height;
        }

        public override float GetMovementCost(MovingEntity entity)
        {
            float value = (0.5f * (1f / From.GetSpeedModifier())) + (0.5f * (1f / To.GetSpeedModifier())); // Cost of moving between start and end tile

            // Add cost of climbing
            int deltaY = Mathf.Abs(EndHeight - StartHeight);
            value += deltaY * ClimbCostPerTile;

            return value;
        }

        public override bool CanPass(Entity entity)
        {
            if (entity.Dimensions.y > HeadSpaceRequirement) return false;

            return base.CanPass(entity);
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
                        if (!IsAscend && y < World.GetWorldHeight(StartHeight)) y = World.GetWorldHeight(StartHeight);

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
                        Vector3 newPosition = Vector3.MoveTowards(entity.transform.position, endClimbPoint, Time.deltaTime * ClimbSpeed);

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
            if(IsAscend) return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f - entity.WorldSize.x / 2f - TransformOffset);
            else return new Vector3(World.GetDirectionVector(Direction).x, 0f, World.GetDirectionVector(Direction).y) * (0.5f + entity.WorldSize.x / 2f + TransformOffset);
        }
    }
}
