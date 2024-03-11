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
        /// </summary>
        public static void DrawStandardSurface(BlockmapNode node, MeshBuilder meshBuilder)
        {
            int surfaceSubmesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.SurfaceMaterial);

            // Surface vertices
            float xStart = node.LocalCoordinates.x;
            float xEnd = node.LocalCoordinates.x + 1;
            float yStart = node.LocalCoordinates.y;
            float yEnd = node.LocalCoordinates.y + 1;
            MeshVertex v1a = meshBuilder.AddVertex(new Vector3(xStart, node.Height[Direction.SW] * World.TILE_HEIGHT, yStart), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(0f, 0f));
            MeshVertex v1b = meshBuilder.AddVertex(new Vector3(xStart, node.Height[Direction.SW] * World.TILE_HEIGHT, yStart), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(0f, 0f));
            MeshVertex v2a = meshBuilder.AddVertex(new Vector3(xEnd, node.Height[Direction.SE] * World.TILE_HEIGHT, yStart), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(1f, 0f));
            MeshVertex v2b = meshBuilder.AddVertex(new Vector3(xEnd, node.Height[Direction.SE] * World.TILE_HEIGHT, yStart), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)node.LocalCoordinates.y / node.Chunk.Size), new Vector2(1f, 0f));
            MeshVertex v3a = meshBuilder.AddVertex(new Vector3(xEnd, node.Height[Direction.NE] * World.TILE_HEIGHT, yEnd), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(1f, 1f));
            MeshVertex v3b = meshBuilder.AddVertex(new Vector3(xEnd, node.Height[Direction.NE] * World.TILE_HEIGHT, yEnd), new Vector2((float)(node.LocalCoordinates.x + 1) / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(1f, 1f));
            MeshVertex v4a = meshBuilder.AddVertex(new Vector3(xStart, node.Height[Direction.NW] * World.TILE_HEIGHT, yEnd), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(0f, 1f));
            MeshVertex v4b = meshBuilder.AddVertex(new Vector3(xStart, node.Height[Direction.NW] * World.TILE_HEIGHT, yEnd), new Vector2((float)node.LocalCoordinates.x / node.Chunk.Size, (float)(node.LocalCoordinates.y + 1) / node.Chunk.Size), new Vector2(0f, 1f));

            switch (node.Shape)
            {
                case "0000":
                case "1100":
                case "0110":
                case "0011":
                case "1001":
                case "0001":
                case "1011":
                case "0100":
                case "1110":
                case "1012":
                case "1210":
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4b, v3b);
                    break;

                case "1000":
                case "0010":
                case "0111":
                case "1101":
                case "2101":
                case "0121":
                    meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                    meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3b);
                    break;

                case "1010":
                    if (node.UseAlternativeVariant)
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3b);
                    }
                    else
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4b, v3b);
                    }
                    break;

                case "0101":
                    if (node.UseAlternativeVariant)
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v3a, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v1b, v4b, v3b);
                    }
                    else
                    {
                        meshBuilder.AddTriangle(surfaceSubmesh, v1a, v4b, v2a);
                        meshBuilder.AddTriangle(surfaceSubmesh, v2b, v4b, v3a);
                    }
                    break;
            }
        }

        /// <summary>
        /// Builds a surface that connects to adjacent nodes with the same surface and draws a curb to other surfaces.
        /// </summary>
        public static void BuildBorderedNodeSurface(World world, BlockmapNode node, MeshBuilder meshBuilder, Material mainMaterial, Material curbMaterial, float mainHeight, float curbHeight, float curbWidth)
        {
            int mainSubmesh = meshBuilder.GetSubmesh(mainMaterial);
            int curbSubmesh = meshBuilder.GetSubmesh(curbMaterial);

            // Center plane
            DrawShapePlane(meshBuilder, node, mainSubmesh, mainHeight, curbWidth, 1f - curbWidth, curbWidth, 1f - curbWidth); // top
            DrawShapePlane(meshBuilder, node, mainSubmesh, 0f, 0f, 1f, 0f, 1f, mirror: true); // bottom

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
        /// Creates a plane parrallel to the shape of this tile covering the area given by the relative values (0-1) xStart, xEnd, yStart, yEnd.
        /// </summary>
        private static void DrawShapePlane(MeshBuilder meshBuilder, BlockmapNode node, int submesh, float height, float xStart, float xEnd, float yStart, float yEnd, bool mirror = false)
        {
            Vector3 v_SW_pos = new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yStart) + height, node.LocalCoordinates.y + yStart);
            Vector2 v_SW_uv = new Vector2(xStart, yStart);
            MeshVertex v_SW = meshBuilder.AddVertex(v_SW_pos, v_SW_uv);

            Vector3 v_SE_pos = new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yStart) + height, node.LocalCoordinates.y + yStart);
            Vector2 v_SE_uv = new Vector2(xEnd, yStart);
            MeshVertex v_SE = meshBuilder.AddVertex(v_SE_pos, v_SE_uv);

            Vector3 v_NE_pos = new Vector3(node.LocalCoordinates.x + xEnd, GetVertexHeight(node, xEnd, yEnd) + height, node.LocalCoordinates.y + yEnd);
            Vector2 v_NE_uv = new Vector2(xEnd, yEnd);
            MeshVertex v_NE = meshBuilder.AddVertex(v_NE_pos, v_NE_uv);

            Vector3 v_NW_pos = new Vector3(node.LocalCoordinates.x + xStart, GetVertexHeight(node, xStart, yEnd) + height, node.LocalCoordinates.y + yEnd);
            Vector2 v_NW_uv = new Vector2(xStart, yEnd);
            MeshVertex v_NW = meshBuilder.AddVertex(v_NW_pos, v_NW_uv);

            if (mirror) meshBuilder.AddPlane(submesh, v_SW, v_NW, v_NE, v_SE);
            else meshBuilder.AddPlane(submesh, v_SW, v_SE, v_NE, v_NW);
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
                DrawShapePlane(meshBuilder, node, mainSubmesh, mainHeight, xStart, xEnd, yStart, yEnd);
            }

            // Draw curb
            else
            {
                DrawShapePlane(meshBuilder, node, curbSubmesh, curbHeight, xStart, xEnd, yStart, yEnd); // curb top
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
                DrawShapePlane(meshBuilder, node, mainSubmesh, mainHeight, xStart, xEnd, yStart, yEnd);
            }

            else // Draw curb
            {
                DrawShapePlane(meshBuilder, node, curbSubmesh, curbHeight, xStart, xEnd, yStart, yEnd); // curb top
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
