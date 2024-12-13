using BlockmapFramework.MeshBuilding;
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
        /// Draws a flat-shaded standard surface top that is just 2 triangles with the surface texture.
        /// <br/>Or draws a basic stairs if shape is stairs.
        /// <br/>If height is set to a value > 0, a cube will drawn instead of a plane.
        /// </summary>
        public static void DrawDefaultNodeSurface(BlockmapNode node, MeshBuilder meshBuilder, Material material, float height)
        {
            if (node is AirNode airNode && airNode.IsStairs)
            {
                BuildStairs(meshBuilder, node, 0f, material);
                return;
            }

            int surfaceSubmesh = meshBuilder.GetSubmesh(material);

            if (height == 0f)
            {
                // Surface vertices (all of them are necessary because we cannot reuse vertices if we want flat shading)
                float xStart = node.LocalCoordinates.x;
                float xEnd = node.LocalCoordinates.x + 1;
                float yStart = node.LocalCoordinates.y;
                float yEnd = node.LocalCoordinates.y + 1;
                MeshVertex v1a = meshBuilder.AddVertex(new Vector3(xStart, node.Altitude[Direction.SW] * World.NodeHeight, yStart), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(0f, 0f));
                MeshVertex v1b = meshBuilder.AddVertex(new Vector3(xStart, node.Altitude[Direction.SW] * World.NodeHeight, yStart), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(0f, 0f));
                MeshVertex v2a = meshBuilder.AddVertex(new Vector3(xEnd, node.Altitude[Direction.SE] * World.NodeHeight, yStart), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(1f, 0f));
                MeshVertex v2b = meshBuilder.AddVertex(new Vector3(xEnd, node.Altitude[Direction.SE] * World.NodeHeight, yStart), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(1f, 0f));
                MeshVertex v3a = meshBuilder.AddVertex(new Vector3(xEnd, node.Altitude[Direction.NE] * World.NodeHeight, yEnd), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(1f, 1f));
                MeshVertex v3b = meshBuilder.AddVertex(new Vector3(xEnd, node.Altitude[Direction.NE] * World.NodeHeight, yEnd), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(1f, 1f));
                MeshVertex v4a = meshBuilder.AddVertex(new Vector3(xStart, node.Altitude[Direction.NW] * World.NodeHeight, yEnd), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(0f, 1f));
                MeshVertex v4b = meshBuilder.AddVertex(new Vector3(xStart, node.Altitude[Direction.NW] * World.NodeHeight, yEnd), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(0f, 1f));

                if (node.GetTriangleMeshShapeVariant())
                {
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4b, v3b);
                }
                else
                {
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3b);
                }
            }
            else meshBuilder.BuildCube(node, surfaceSubmesh, Vector3.zero, new Vector3(1f, height, 1f), adjustToNodeShape: true);
        }

        /// <summary>
        /// Builds a surface that connects to adjacent nodes with the same surface and draws a curb to other surfaces.
        /// </summary>
        public static void BuildBorderedNodeSurface(BlockmapNode node, MeshBuilder meshBuilder, string mainMaterialSubpath, string curbMaterialSubpath, float mainHeight, float curbHeight, float curbWidth)
        {
            if(node is AirNode airNode && airNode.IsStairs)
            {
                BuildStairs(meshBuilder, node, mainHeight, MaterialManager.LoadMaterial(mainMaterialSubpath));
                return;
            }

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
            return node.GetWorldShapeAltitude(new Vector2(x, y));
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

        private static void BuildStairs(MeshBuilder meshBuilder, BlockmapNode node, float additionalStepHeight, Material material)
        {
            int submesh = meshBuilder.GetSubmesh(material);
            int numSteps = 5;
            float stepSize = (1f / numSteps);

            for (int i = 0; i < numSteps; i++)
            {
                float startPos =  stepSize * i;
                float stepHeight = stepSize + additionalStepHeight;

                if (node.Shape == "0022") meshBuilder.BuildCube(node, submesh, new Vector3(0f, startPos, startPos), new Vector3(1f, stepHeight, stepSize), adjustToNodeShape: false);
                else if (node.Shape == "2002") meshBuilder.BuildCube(node, submesh, new Vector3(startPos, 1f - startPos - stepSize, 0f), new Vector3(stepSize, stepHeight, 1f), adjustToNodeShape: false);
                else if (node.Shape == "2200") meshBuilder.BuildCube(node, submesh, new Vector3(0f, 1f - startPos - stepSize, startPos), new Vector3(1f, stepHeight, stepSize), adjustToNodeShape: false);
                else if (node.Shape == "0220") meshBuilder.BuildCube(node, submesh, new Vector3(startPos, startPos, 0f), new Vector3(stepSize, stepHeight, 1f), adjustToNodeShape: false);
            }
        }
    }
}
