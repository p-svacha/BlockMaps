using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class BaseWorldGenerator
    {
        private static int NodeId;
        private static WorldData WorldData;
        private static int[,] HeightMap;

        public static WorldData GenerateWorld(string name, int chunkSize, int numChunks)
        {
            int worldSize = chunkSize * numChunks;
            NodeId = 0;

            WorldData = new WorldData();
            WorldData.Name = name;
            WorldData.ChunkSize = chunkSize;
            WorldData.Chunks = new List<ChunkData>();

            // Create height map
            Vector2Int perlinOffset = new Vector2Int(Random.Range(0, 20000), Random.Range(0, 20000));
            float perlinScale = 0.1f;
            float perlinHeightScale = 5f;
            HeightMap = new int[worldSize + 1, worldSize + 1];
            for(int x = 0; x < worldSize + 1; x++)
            {
                for(int y = 0; y < worldSize + 1; y++)
                {
                    HeightMap[x,y] = (int)(perlinHeightScale * Mathf.PerlinNoise(perlinOffset.x + x * perlinScale, perlinOffset.y + y * perlinScale));
                }
            }

            // Create chunks
            for(int x = 0; x < numChunks; x++)
            {
                for(int y = 0; y < numChunks; y++)
                {
                    ChunkData chunkData = GenerateChunk(new Vector2Int(x, y));
                    WorldData.Chunks.Add(chunkData);
                }
            }

            WorldData.MaxNodeId = NodeId;
            return WorldData;
        }
        
        public static ChunkData GenerateChunk(Vector2Int coordinates)
        {
            ChunkData chunkData = new ChunkData();
            chunkData.ChunkCoordinateX = coordinates.x;
            chunkData.ChunkCoordinateY = coordinates.y;
            chunkData.Nodes = new List<NodeData>();

            for (int y = 0; y < WorldData.ChunkSize; y++)
            {
                for (int x = 0; x < WorldData.ChunkSize; x++)
                {
                    chunkData.Nodes.Add(GenerateNode(coordinates, new Vector2Int(x, y)));
                }
            }

            return chunkData;
        }

        private static NodeData GenerateNode(Vector2Int chunkCoordinates, Vector2Int localCoordinates)
        {
            int worldX = chunkCoordinates.x * WorldData.ChunkSize + localCoordinates.x;
            int worldY = chunkCoordinates.y * WorldData.ChunkSize + localCoordinates.y;

            NodeData data = new NodeData();
            data.Id = NodeId++;
            data.LocalCoordinateX = localCoordinates.x;
            data.LocalCoordinateY = localCoordinates.y;
            data.Height = new int[] { HeightMap[worldX, worldY], HeightMap[worldX + 1, worldY], HeightMap[worldX + 1, worldY + 1], HeightMap[worldX, worldY + 1] };
            data.Surface = SurfaceId.Grass;
            data.Type = NodeType.Surface;

            return data;
        }
    }
}
