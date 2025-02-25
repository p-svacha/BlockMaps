using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A transition for when walking from one node to another that is adjacent in a straight direction.
    /// </summary>
    public class AdjacentWalkTransition : Transition
    {
        public AdjacentWalkTransition(BlockmapNode from, BlockmapNode to, Direction dir, int maxHeight) : base(from, to, dir, maxHeight) { }

        public override float GetMovementCost(MovingEntity entity)
        {
            float value = (0.5f * From.GetMovementCost(entity, from: Direction.None, to: Direction)) + (0.5f * To.GetMovementCost(entity, from: OppositeDirection, to: Direction.None));
            if(HelperFunctions.IsCorner(Direction)) value *= 1.4142f; // Because diagonal
            return value;
        }

        public override bool CanPass(MovingEntity entity)
        {
            if (!From.IsPassable(Direction, entity)) return false;
            if (!To.IsPassable(HelperFunctions.GetOppositeDirection(Direction), entity)) return false;

            return base.CanPass(entity);
        }

        public override void OnTransitionStart(MovingEntity entity)
        {
            entity.SetRotation(Direction);
        }
        public override void EntityMovementTick(MovingEntity entity, out bool finishedTransition, out BlockmapNode originNode)
        {
            // Get current 2d position of entity
            Vector2 oldPosition2d = new Vector2(entity.WorldPosition.x, entity.WorldPosition.z);

            // Get 2d position of next node
            Vector3 nextNodePosition = To.MeshCenterWorldPosition;
            Vector2 nextNodePosition2d = new Vector2(nextNodePosition.x, nextNodePosition.z);

            // Calculate new 2d world position and coordinates by moving towards next node in 2d
            Direction fromDirection = entity.OriginNode == From ? Direction.None : OppositeDirection;
            Direction toDirection = entity.OriginNode == From ? Direction : Direction.None;
            Vector2 newPosition2d = Vector2.MoveTowards(oldPosition2d, nextNodePosition2d, entity.GetCurrentWalkingSpeed(fromDirection, toDirection));
            Vector2Int newWorldCoordinates = World.GetWorldCoordinates(newPosition2d);

            // Set origin node according to 2d coordinates
            if (newWorldCoordinates == From.WorldCoordinates) originNode = From;
            else originNode = To;

            // Calculate altitude (y-coordinate) on new position
            float y = World.GetWorldAltitudeAt(newPosition2d, originNode);
            if (originNode.Type == NodeType.Water && entity.Def.WaterBehaviour == WaterBehaviour.HalfBelowWaterSurface) y -= entity.WorldHeight / 2f;

            // Set new position
            Vector3 newPosition = new Vector3(newPosition2d.x, y, newPosition2d.y);
            entity.SetWorldPosition(newPosition);

            // Return true if character is very close to next node
            if (Vector2.Distance(oldPosition2d, nextNodePosition2d) <= REACH_EPSILON) finishedTransition = true;
            else finishedTransition = false;
        }

        public override List<Vector3> GetPreviewPath()
        {
            return new List<Vector3>() { From.MeshCenterWorldPosition, To.MeshCenterWorldPosition };
        }
    }
}
