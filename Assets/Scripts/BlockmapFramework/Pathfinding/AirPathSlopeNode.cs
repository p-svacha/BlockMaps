using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirPathSlopeNode : BlockmapNode
    {
        public override NodeType Type => NodeType.AirPathSlope;
        public override bool IsPath => true;

        public Direction SlopeDirection;

        public AirPathSlopeNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, int[] height, SurfaceId surface) : base(world, chunk, id, localCoordinates, height, surface)
        {
            SlopeDirection = GetDirectionFromShape(Shape);
        }

        public static string GetShapeFromDirection(Direction dir)
        {
            if (dir == Direction.N) return "0011";
            else if (dir == Direction.E) return "0110";
            else if (dir == Direction.S) return "1100";
            else if (dir == Direction.W) return "1001";
            else throw new System.Exception("Invalid directions for slope");
        }

        public static int[] GetHeightsFromDirection(int height, Direction dir)
        {
            if (dir == Direction.N) return new int[] { height, height, height + 1, height + 1 };
            else if (dir == Direction.E) return new int[] { height, height + 1, height + 1, height };
            else if (dir == Direction.S) return new int[] { height + 1, height + 1, height, height };
            else if (dir == Direction.W) return new int[] { height + 1, height, height, height + 1 };
            else throw new System.Exception("Invalid direction for slope");
        }

        private Direction GetDirectionFromShape(string shape)
        {
            if (shape == "0011") return Direction.N;
            if (shape == "0110") return Direction.E;
            if (shape == "1100") return Direction.S;
            if (shape == "1001") return Direction.W;
            throw new System.Exception("Unhandled heights");
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
