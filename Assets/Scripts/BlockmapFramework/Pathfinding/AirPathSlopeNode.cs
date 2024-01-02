using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathSlopeNode : BlockmapNode
    {
        public override NodeType Type => NodeType.AirPathSlope;
        public override bool IsSolid => true;
        public override bool IsPath => true;

        public AirPathSlopeNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceId surface) : base(world, chunk, id, localCoordinates, height, surface) { }

        public static string GetShapeFromDirection(Direction dir)
        {
            if (dir == Direction.N) return "0011";
            else if (dir == Direction.E) return "0110";
            else if (dir == Direction.S) return "1100";
            else if (dir == Direction.W) return "1001";
            else throw new System.Exception("Invalid directions for slope");
        }

        #region Draw

        public override void Draw(MeshBuilder meshBuilder)
        {
            PathMeshBuilder.BuildPath(this, meshBuilder, pathSubmesh: 0, pathCurbSubmesh: 1);
        }

        #endregion

        #region Getters

        public override Vector3 GetCenterWorldPosition()
        {
            return new Vector3(WorldCoordinates.x + 0.5f, World.GetWorldHeight(BaseHeight + 0.5f), WorldCoordinates.y + 0.5f);
        }

        #endregion
    }
}
