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
                MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y), new Vector2(0f, 0f));
                MeshVertex se = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y), new Vector2(1f, 0f));
                MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y + 1), new Vector2(1f, 1f));
                MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y + 1), new Vector2(0f, 1f));

                meshBuilder.AddTriangle(0, sw, ne, se);
                meshBuilder.AddTriangle(0, sw, nw, ne);
            }
        }

        public static void BuildWaterMeshForSingleNode(MeshBuilder meshBuilder, WaterNode node)
        {
            float waterWorldHeight = node.WaterBody.WaterSurfaceWorldHeight;

            MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y), new Vector2(0f, 0f));
            MeshVertex se = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y), new Vector2(1f, 0f));
            MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(1f, 1f));
            MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(0f, 1f));

            meshBuilder.AddTriangle(0, sw, ne, se);
            meshBuilder.AddTriangle(0, sw, nw, ne);
        }
    }
}
