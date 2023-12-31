using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WaterMeshGenerator
    {
        public static void BuildFullWaterMesh(MeshBuilder meshBuilder, WaterBody water, World world)
        {
            foreach(SurfaceNode node in water.CoveredNodes)
            {
                float waterWorldHeight = ((water.ShoreHeight - 1) * World.TILE_HEIGHT) + (World.WATER_HEIGHT * World.TILE_HEIGHT);

                MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, waterWorldHeight, node.WorldCoordinates.y), new Vector2(0f, 0f));
                MeshVertex se = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, waterWorldHeight, node.WorldCoordinates.y), new Vector2(1f, 0f));
                MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, waterWorldHeight, node.WorldCoordinates.y + 1), new Vector2(1f, 1f));
                MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, waterWorldHeight, node.WorldCoordinates.y + 1), new Vector2(0f, 1f));

                meshBuilder.AddTriangle(0, sw, ne, se);
                meshBuilder.AddTriangle(0, sw, nw, ne);
            }
        }

        public static void BuildWaterMeshForSingleNode(MeshBuilder meshBuilder, BlockmapNode node)
        {
            float waterWorldHeight = ((node.WaterBody.ShoreHeight - 1) * World.TILE_HEIGHT) + (World.WATER_HEIGHT * World.TILE_HEIGHT);

            MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y), new Vector2(0f, 0f));
            MeshVertex se = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y), new Vector2(1f, 0f));
            MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(1f, 1f));
            MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(0f, 1f));

            meshBuilder.AddTriangle(0, sw, ne, se);
            meshBuilder.AddTriangle(0, sw, nw, ne);
        }
    }
}
