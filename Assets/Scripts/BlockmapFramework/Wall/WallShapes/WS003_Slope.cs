using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WS003_Slope : WallShape
    {
        private const float WIDTH = 0.1f;

        public override WallShapeId Id => WallShapeId.Slope;
        public override string Name => "Slope";
        public override bool BlocksVision => true;
        public override bool IsClimbable => false;
        public override float Width => WIDTH;

        public override void GenerateMesh(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, Material material, bool isMirrored)
        {
            int submesh = meshBuilder.GetSubmesh(material);

            float startX = isMirrored ? 1f : 0f;
            float endX = isMirrored ? 0f : 1f;

            // Front triangle
            Vector3 ft1 = new Vector3(startX, 0f, 0f);
            Vector3 ft2 = new Vector3(endX, World.TILE_HEIGHT, 0f);
            Vector3 ft3 = new Vector3(endX, 0f, 0f);
            meshBuilder.BuildTriangle(localCellPosition, side, submesh, ft1, ft2, ft3, !isMirrored);

            // Back triangle
            Vector3 bt1 = new Vector3(startX, 0f, WIDTH);
            Vector3 bt2 = new Vector3(endX, World.TILE_HEIGHT, WIDTH);
            Vector3 bt3 = new Vector3(endX, 0f, WIDTH);
            meshBuilder.BuildTriangle(localCellPosition, side, submesh, bt1, bt2, bt3, isMirrored);

            // Side plane
            Vector3 sp1 = new Vector3(endX, 0f, 0f);
            Vector3 sp2 = new Vector3(endX, World.TILE_HEIGHT, 0f);
            Vector3 sp3 = new Vector3(endX, World.TILE_HEIGHT, WIDTH);
            Vector3 sp4 = new Vector3(endX, 0f, WIDTH);
            meshBuilder.BuildPlane(localCellPosition, side, submesh, sp1, sp2, sp3, sp4, !isMirrored);

            // Top sloped plane
            Vector3 tsp1 = new Vector3(startX, 0f, 0f);
            Vector3 tsp2 = new Vector3(endX, World.TILE_HEIGHT, 0f);
            Vector3 tsp3 = new Vector3(endX, World.TILE_HEIGHT, WIDTH);
            Vector3 tsp4 = new Vector3(startX, 0f, WIDTH);
            meshBuilder.BuildPlane(localCellPosition, side, submesh, tsp1, tsp2, tsp3, tsp4, isMirrored);
        }
    }
}
