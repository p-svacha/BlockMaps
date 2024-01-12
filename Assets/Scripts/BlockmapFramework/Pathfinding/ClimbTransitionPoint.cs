using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Invisible nodes for climbing. ClimbNodes are generated and destroyed dynamically when the navmesh is re-calculated.
    /// </summary>
    public class ClimbTransitionPoint
    {
        /*
        public Direction Side { get; private set; }
        public float DistanceFromNodeEdge { get; private set; }
        /// <summary>
        /// Other end of this climb
        /// </summary>
        public ClimbTransitionPoint ClimbConnection { get; private set; }

        public ClimbTransitionPoint(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height)

        public void Init(Direction side, float distance, ClimbTransitionPoint climbConnection)
        {
            Side = side;
            DistanceFromNodeEdge = distance;
            ClimbConnection = climbConnection;
        }

        #region Getters

        public Vector3 GetWorldPosition()
        {
            Vector3 centerWorldPos = new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight), WorldCoordinates.y + 0.5f);
            Vector3 offset = new Vector3(World.GetDirectionVector(Side).x, 0f, World.GetDirectionVector(Side).y) * (0.5f - DistanceFromNodeEdge);
            return centerWorldPos + offset;
        }

        // Transfer this to ClimbTransition
        public override bool IsPassable(Direction dir, Entity entity = null)
        {
            // Check if the side has enough head space for the entity
            int headSpace = GetFreeHeadSpace(dir);
            if (headSpace <= 0) return false; // Another node above this one is blocking this(by overlapping in at least 1 corner)
            if (entity != null && entity.Dimensions.y > headSpace) return false; // A node above is blocking the space for the entity

            return true;
        }
        

        #endregion

        */

    }
}
