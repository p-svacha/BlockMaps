using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// AirNodes make up elevated paths and floors above ground level. They are deformable and removable.
    /// </summary>
    public class AirNode : DynamicNode
    {
        public override NodeType Type => NodeType.Air;
        public override bool SupportsEntities => true;

        public AirNode() { }
        public AirNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceDef surfaceDef) : base(world, chunk, id, localCoordinates, height, surfaceDef) { }

        #region Getters

        public bool IsStairs => IsSlope();

        public override VisibilityType GetVisibility(Actor activeVisionActor)
        {
            // Check if we need to hide because of vision cutoff
            if (Chunk.World.DisplaySettings.IsVisionCutoffEnabled && BaseAltitude > Chunk.World.DisplaySettings.VisionCutoffAltitude) return VisibilityType.Hidden;

            return base.GetVisibility(activeVisionActor);
        }

        #endregion
    }
}
    