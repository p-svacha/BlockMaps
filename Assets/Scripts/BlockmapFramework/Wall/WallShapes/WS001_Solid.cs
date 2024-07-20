using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WS001_Solid : WallShape
    {
        public override WallShapeId Id => WallShapeId.Solid;
        public override string Name => "Solid";
        public override List<Direction> ValidSides => HelperFunctions.GetSides();

        public override void GenerateMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, Material material)
        {
            throw new System.NotImplementedException();
        }
    }
}
