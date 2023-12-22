using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class CTFWorldGenerator
    {
        private static int NodeIdCounter;
        private static WorldData WorldData;
        private static int[,] HeightMap;

        public static World GenerateWorld(string name, int chunkSize, int numChunks)
        {
            int size = chunkSize * numChunks;
            NodeIdCounter = 0;

            WorldData = new WorldData();
            WorldData.Name = name;
            WorldData.ChunkSize = chunkSize;
            WorldData.Chunks = new List<ChunkData>();

            // Create height map
            Vector2Int perlinOffset = new Vector2Int(Random.Range(0, 20000), Random.Range(0, 20000));
            float perlinScale = 0.1f;
            float perlinHeightScale = 5f;
            HeightMap = new int[size + 1, size + 1];
            for (int x = 0; x < size + 1; x++)
            {
                for (int y = 0; y < size + 1; y++)
                {
                    HeightMap[x, y] = (int)(perlinHeightScale * Mathf.PerlinNoise(perlinOffset.x + x * perlinScale, perlinOffset.y + y * perlinScale));
                }
            }

            // Create surface nodes
            List<NodeData> nodes = new List<NodeData>();
            for (int x = 0; x < numChunks; x++)
            {
                for (int y = 0; y < numChunks; y++)
                {
                    ChunkData chunkData = GenerateChunk(new Vector2Int(x, y));
                    WorldData.Chunks.Add(chunkData);
                }
            }
            WorldData.MaxNodeId = NodeIdCounter;

            World world = World.Load(WorldData);
            world.Draw();
            return world;
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
            data.Id = NodeIdCounter++;
            data.LocalCoordinateX = localCoordinates.x;
            data.LocalCoordinateY = localCoordinates.y;
            data.Height = new int[] {
                HeightMap[worldX, worldY],
                HeightMap[worldX + 1, worldY],
                HeightMap[worldX + 1, worldY + 1],
                HeightMap[worldX, worldY + 1]
            };
            if (worldX == 50)
                for (int i = 0; i < 4; i++) data.Height[i] += 5;
            data.Surface = SurfaceId.Grass;
            data.Type = NodeType.Surface;

            return data;
        }
    }
}
