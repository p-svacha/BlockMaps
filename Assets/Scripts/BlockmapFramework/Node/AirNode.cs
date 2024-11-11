using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class AirNode : DynamicNode
    {
        public override NodeType Type => NodeType.Air;
        public override bool IsSolid => true;

        public AirNode(World world, Chunk chunk, int id, Vector2Int localCoordinates, Dictionary<Direction, int> height, SurfaceDef surfaceDef) : base(world, chunk, id, localCoordinates, height, surfaceDef) { }
    }
}
