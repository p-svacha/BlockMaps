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
    }
}
