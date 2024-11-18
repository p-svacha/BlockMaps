using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WaterMeshGenerator
    {
        public static void BuildFullWaterMesh(MeshBuilder meshBuilder, WaterBody water)
        {
            int waterSubmesh = meshBuilder.GetSubmesh(MaterialManager.BuildPreviewMaterial);

            foreach (GroundNode node in water.CoveredGroundNodes)
            {
                MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y), new Vector2(0f, 0f));
                MeshVertex se = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y), new Vector2(1f, 0f));
                MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x + 1, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y + 1), new Vector2(1f, 1f));
                MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.WorldCoordinates.x, water.WaterSurfaceWorldHeight, node.WorldCoordinates.y + 1), new Vector2(0f, 1f));

                meshBuilder.AddTriangle(waterSubmesh, sw, ne, se);
                meshBuilder.AddTriangle(waterSubmesh, sw, nw, ne);
            }
        }

        public static void BuildWaterMeshForSingleNode(MeshBuilder meshBuilder, WaterNode node)
        {
            int waterSubmesh = meshBuilder.GetSubmesh(MaterialManager.LoadMaterial("Water"));

            float waterWorldHeight = node.WaterBody.WaterSurfaceWorldHeight;

            MeshVertex sw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y), new Vector2(0f, 0f));
            MeshVertex se = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y), new Vector2(1f, 0f));
            MeshVertex ne = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x + 1, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(1f, 1f));
            MeshVertex nw = meshBuilder.AddVertex(new Vector3(node.LocalCoordinates.x, waterWorldHeight, node.LocalCoordinates.y + 1), new Vector2(0f, 1f));

            meshBuilder.AddTriangle(waterSubmesh, sw, ne, se);
            meshBuilder.AddTriangle(waterSubmesh, sw, nw, ne);

            // Map edge plane
            if (node.World.GetAdjacentGroundNode(node, Direction.N) == null)
                meshBuilder.BuildPlane(waterSubmesh, nw.Position, ne.Position, new Vector3(ne.Position.x, 0f, ne.Position.z), new Vector3(nw.Position.x, 0f, nw.Position.z), Vector2.zero, Vector2.one);
            if (node.World.GetAdjacentGroundNode(node, Direction.E) == null)
                meshBuilder.BuildPlane(waterSubmesh, ne.Position, se.Position, new Vector3(se.Position.x, 0f, se.Position.z), new Vector3(ne.Position.x, 0f, ne.Position.z), Vector2.zero, Vector2.one);
            if (node.World.GetAdjacentGroundNode(node, Direction.S) == null)
                meshBuilder.BuildPlane(waterSubmesh, se.Position, sw.Position, new Vector3(sw.Position.x, 0f, sw.Position.z), new Vector3(se.Position.x, 0f, se.Position.z), Vector2.zero, Vector2.one);
            if (node.World.GetAdjacentGroundNode(node, Direction.W) == null)
                meshBuilder.BuildPlane(waterSubmesh, sw.Position, nw.Position, new Vector3(nw.Position.x, 0f, nw.Position.z), new Vector3(sw.Position.x, 0f, sw.Position.z), Vector2.zero, Vector2.one);
        }
    }
}
