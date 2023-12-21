using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class PathMeshBuilder
    {
        private const float PATH_SIDE_MARGIN = 0.1f;
        private const float PATH_HEIGHT = 0.05f;

        /// <summary>
        /// Builds the path mesh on this node according to the node shape. Adds the mesh to an existing MeshBuilder.
        /// </summary>
        public static void BuildPath(World world, MeshBuilder meshBuilder, BlockmapNode node)
        {
            int pathSubmesh = meshBuilder.AddNewSubmesh(BlockmapResourceManager.Singleton.DefaultMaterial, SurfaceManager.Instance.GetSurface(SurfaceId.Tarmac).Color);

            // Center plane
            DrawShapePlane(node, meshBuilder, pathSubmesh, PATH_HEIGHT, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN);

            // North connector
            SurfaceNode northNode = world.GetAdjacentSurfaceNode(node, Direction.N);
            if (northNode != null && northNode.HasPath)
                DrawShapePlane(node, meshBuilder, pathSubmesh, PATH_HEIGHT, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN, 1f);

            // South connector
            SurfaceNode southNode = world.GetAdjacentSurfaceNode(node, Direction.S);
            if (southNode != null && southNode.HasPath)
                DrawShapePlane(node, meshBuilder, pathSubmesh, PATH_HEIGHT, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN, 0f, PATH_SIDE_MARGIN);

            // East connector
            SurfaceNode eastNode = world.GetAdjacentSurfaceNode(node, Direction.E);
            if (eastNode != null && eastNode.HasPath)
                DrawShapePlane(node, meshBuilder, pathSubmesh, PATH_HEIGHT, 1f - PATH_SIDE_MARGIN, 1f, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN);

            // West connector
            SurfaceNode westNode = world.GetAdjacentSurfaceNode(node, Direction.W);
            if (westNode != null && westNode.HasPath)
                DrawShapePlane(node, meshBuilder, pathSubmesh, PATH_HEIGHT, 0f, PATH_SIDE_MARGIN, PATH_SIDE_MARGIN, 1f - PATH_SIDE_MARGIN);
        }

        /// <summary>
        /// Creates a plane parrallel to the shape of this tile covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        private static void DrawShapePlane(BlockmapNode node, MeshBuilder meshBuilder, int pathSubmesh, float height, float xStart, float xEnd, float yStart, float yEnd)
        {
            Vector3 v_SW_pos = new Vector3(xStart, node.GetRelativeHeightAt(new Vector2(xStart, yStart)) + height, yStart);
            Vector2 v_SW_uv = new Vector2(xStart, yStart);
            MeshVertex v_SW = meshBuilder.AddVertex(v_SW_pos, v_SW_uv);

            Vector3 v_SE_pos = new Vector3(xEnd, node.GetRelativeHeightAt(new Vector2(xEnd, yStart)) + height, yStart);
            Vector2 v_SE_uv = new Vector2(xEnd, yStart);
            MeshVertex v_SE = meshBuilder.AddVertex(v_SE_pos, v_SE_uv);

            Vector3 v_NE_pos = new Vector3(xEnd, node.GetRelativeHeightAt(new Vector2(xEnd, yEnd)) + height, yEnd);
            Vector2 v_NE_uv = new Vector2(xEnd, yEnd);
            MeshVertex v_NE = meshBuilder.AddVertex(v_NE_pos, v_NE_uv);

            Vector3 v_NW_pos = new Vector3(xStart, node.GetRelativeHeightAt(new Vector2(xStart, yEnd)) + height, yEnd);
            Vector2 v_NW_uv = new Vector2(xStart, yEnd);
            MeshVertex v_NW = meshBuilder.AddVertex(v_NW_pos, v_NW_uv);

            meshBuilder.AddPlane(pathSubmesh, v_SW, v_SE, v_NE, v_NW);
        }

    }
}
