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

        public bool IsStairs => IsStairShape(Altitude);

        protected override bool IsValidShape(Dictionary<Direction, int> altitude)
        {
            // Not allowed that the altitude change between 2 corners is greater than 1
            // Except stairs
            if ((!IsStairShape(altitude) || !IsStairShapeAllowed()) && (
            Mathf.Abs(altitude[Direction.SE] - altitude[Direction.SW]) > 1 ||
            Mathf.Abs(altitude[Direction.SW] - altitude[Direction.NW]) > 1 ||
            Mathf.Abs(altitude[Direction.NW] - altitude[Direction.NE]) > 1 ||
            Mathf.Abs(altitude[Direction.NE] - altitude[Direction.SE]) > 1))
                return false;

            return base.IsValidShape(altitude);
        }


        private bool IsStairShapeAllowed()
        {
            return true;
        }
        private bool IsStairShape(Dictionary<Direction, int> altitude)
        {
            string shape = GetShape(altitude);

            return shape == "0022" || shape == "0220" || shape == "2200" || shape == "2002";
        }

        #endregion
    }
}
    