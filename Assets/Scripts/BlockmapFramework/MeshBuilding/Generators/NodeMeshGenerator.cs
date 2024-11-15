using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Static class that supports all kind of functionality for building the meshes for single nodes.
    /// </summary>
    public static class NodeMeshGenerator
    {
        /// <summary>
        /// Draws a standard surface top that is just 2 triangles with the surface texture.
        /// <br/>Uses the SurfaceMaterial that is capable of blending textures.
        /// </summary>
        public static void DrawFlatBlendableSurface(BlockmapNode node, MeshBuilder meshBuilder)
        {
            meshBuilder.DrawShapePlane(node, MaterialManager.BlendbaleSurfaceMaterial, height: 0f, 0f, 1f, 0f, 1f);
        }



        /// <summary>
        /// Builds a surface that connects to adjacent nodes with the same surface and draws a curb to other surfaces.
        /// </summary>
        public static void BuildBorderedNodeSurface(BlockmapNode node, MeshBuilder meshBuilder, string mainMaterialSubpath, string curbMaterialSubpath, float mainHeight, float curbHeight, float curbWidth)
        {
            int mainSubmesh = meshBuilder.GetSubmesh(mainMaterialSubpath);
            int curbSubmesh = meshBuilder.GetSubmesh(curbMaterialSubpath);

            // Center plane
            meshBuilder.DrawShapePlane(node, mainSubmesh, mainHeight, curbWidth, 1f - curbWidth, curbWidth, 1f - curbWidth); // top
            meshBuilder.DrawShapePlane(node, mainSubmesh, 0f, 0f, 1f, 0f, 1f, mirror: true); // bottom

            // Edge connections
            DrawEdge(meshBuilder, node, Direction.N, mainSubmesh, curbSubmesh, curbWidth, 1f - curbWidth, 1f - curbWidth, 1f, mainHeight, curbHeight);
            DrawEdge(meshBuilder, node, Direction.S, mainSubmesh, curbSubmesh, curbWidth, 1f - curbWidth, 0f, curbWidth, mainHeight, curbHeight);
            DrawEdge(meshBuilder, node, Direction.E, mainSubmesh, curbSubmesh, 1f - curbWidth, 1f, curbWidth, 1f - curbWidth, mainHeight, curbHeight);
            DrawEdge(meshBuilder, node, Direction.W, mainSubmesh, curbSubmesh, 0f, curbWidth, curbWidth, 1f - curbWidth, mainHeight, curbHeight);

            // Corner connections
            DrawCorner(meshBuilder, node, Direction.NW, mainSubmesh, curbSubmesh, 0, curbWidth, 1f - curbWidth, 1f, mainHeight, curbHeight);
            DrawCorner(meshBuilder, node, Direction.NE, mainSubmesh, curbSubmesh, 1f - curbWidth, 1f, 1f - curbWidth, 1f, mainHeight, curbHeight);
            DrawCorner(meshBuilder, node, Direction.SE, mainSubmesh, curbSubmesh, 1f - curbWidth, 1f, 0f, curbWidth, mainHeight, curbHeight);
            DrawCorner(meshBuilder, node, Direction.SW, mainSubmesh, curbSubmesh, 0, curbWidth, 0f, curbWidth, mainHeight, curbHeight);
        }

        /// <summary>
        /// Returns the world height for the current node at the given relative position.
        /// </summary>
        private static float GetVertexHeight(BlockmapNode node, float x, float y)
        {
            return node.GetWorldHeightAt(new Vector2(x, y));
        }

        private static void DrawEdge(MeshBuilder meshBuilder, BlockmapNode node, Direction dir, int mainSubmesh, int curbSubmesh, float xStart, float xEnd, float yStart, float yEnd, float mainHeight, float curbHeight)
        {
            // Check if a path is connected in that direction
            bool hasPathConnection = node.HasSurfaceConnection(dir);

            // Draw connection to adjacent path
            if (hasPathConnection)
            {
                meshBuilder.DrawShapePlane(node, mainSubmesh, mainHeight, xStart, xEnd, yStart, yEnd);
            }

            // Draw curb
            else
            {
                meshBuilder.DrawShapePlane(node, curbSubmesh, curbHeight, xStart, xEnd, yStart, yEnd); // curb top
                DrawCurbSides(meshBuilder, node, dir, curbSubmesh, xStart, xEnd, yStart, yEnd, mainHeight, curbHeight);
            }
        }

        private static void DrawCorner(MeshBuilder meshBuilder, BlockmapNode node, Direction dir, int mainSubmesh, int curbSubmesh, float xStart, float xEnd, float yStart, float yEnd, float mainHeight, float curbHeight)
        {
            // Check if a path is connected in that direction
            bool hasPathConnection = (
                node.HasSurfaceConnection(HelperFunctions.GetPreviousDirection8(dir)) &&
                node.HasSurfaceConnection(dir) &&
                node.HasSurfaceConnection(HelperFunctions.GetNextDirection8(dir)));

            // Draw connection to adjacent path corner
            if (hasPathConnection)
            {
                meshBuilder.DrawShapePlane(node, mainSubmesh, mainHeight, xStart, xEnd, yStart, yEnd);
            }

            else // Draw curb
            {
                meshBuilder.DrawShapePlane(node, curbSubmesh, curbHeight, xStart, xEnd, yStart, yEnd); // curb top
                DrawCurbSides(meshBuilder, node, dir, curbSubmesh, xStart, xEnd, yStart, yEnd, mainHeight, curbHeight);
            }
        }



        private static void DrawCurbSides(MeshBuilder meshBuilder, BlockmapNode node, Direction dir, int curbSubmesh, float xStart, float xEnd, float yStart, float yEnd, float mainHeight, float curbHeight)
        {

            if (HelperFunctions.GetAffectedDirections(dir).Contains(Direction.N) || mainHeight != curbHeight) // North curb side
            {
                meshBuilder.BuildPlane(curbSubmesh,
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yEnd), node.LocalCoordinates.y + yEnd),
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yEnd) + curbHeight, node.LocalCoordinates.y + yEnd),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yEnd) + curbHeight, node.LocalCoordinates.y + yEnd),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yEnd), node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f)
                    );
            }

            if (HelperFunctions.GetAffectedDirections(dir).Contains(Direction.S) || mainHeight != curbHeight) // South curb side
            {
                meshBuilder.BuildPlane(curbSubmesh,
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yStart), node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yStart) + curbHeight, node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yStart) + curbHeight, node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yStart), node.LocalCoordinates.y + yStart),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    mirror: true
                    );
            }

            if (HelperFunctions.GetAffectedDirections(dir).Contains(Direction.W) || mainHeight != curbHeight) // West curb side
            {

                meshBuilder.BuildPlane(curbSubmesh,
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yStart), node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yStart) + curbHeight, node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yEnd) + curbHeight, node.LocalCoordinates.y + yEnd),
                    new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yEnd), node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f)
                    );
            }

            if (HelperFunctions.GetAffectedDirections(dir).Contains(Direction.E) || mainHeight != curbHeight) // East curb side
            {
                meshBuilder.BuildPlane(curbSubmesh,
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yStart), node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yStart) + curbHeight, node.LocalCoordinates.y + yStart),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yEnd) + curbHeight, node.LocalCoordinates.y + yEnd),
                    new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yEnd), node.LocalCoordinates.y + yEnd),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    mirror: true
                    );
            }
        }
    }
}
