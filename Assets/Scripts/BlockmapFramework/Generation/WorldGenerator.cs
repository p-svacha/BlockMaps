using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WorldGenerator
    {
        public abstract string Name { get; }

        protected int ChunkSize;
        protected int NumChunks;
        protected int WorldSize;

        public World GeneratedWorld { get; private set; }
        public GenerationPhase GenerationPhase { get; private set; }

        public void StartGeneration(int chunkSize, int numChunks)
        {
            if (chunkSize > 16) throw new System.Exception("Chunk size can't be bigger than 16 due to shader limitations.");
            if (chunkSize * numChunks > 512) throw new System.Exception("World size can't be bigger than 512.");

            ChunkSize = chunkSize;
            NumChunks = numChunks;
            WorldSize = chunkSize * numChunks;

            // Create empty world to start with
            WorldData data = CreateEmptyWorldData();
            GeneratedWorld = World.SimpleLoad(data);

            GenerationPhase = GenerationPhase.Generating;
            OnGenerationStart();
        }
        protected abstract void OnGenerationStart();
        public void UpdateGeneration()
        {
            if (GenerationPhase == GenerationPhase.Generating) OnUpdate();
            else if (GenerationPhase == GenerationPhase.Initializing)
            {
                if (GeneratedWorld.IsInitialized) GenerationPhase = GenerationPhase.Done;
            }
        }
        protected abstract void OnUpdate();
        protected void FinishGeneration()
        {
            GenerationPhase = GenerationPhase.Initializing;
            GeneratedWorld.GenerateFullNavmesh();
        }

        protected WorldData CreateEmptyWorldData()
        {
            WorldData data = new WorldData();
            data.Name = "World";
            data.ChunkSize = ChunkSize;
            data.Chunks = new List<ChunkData>();
            data.Players = new List<PlayerData>();
            data.Entities = new List<EntityData>();
            data.WaterBodies = new List<WaterBodyData>();
            data.Walls = new List<WallData>();

            // Create players
            data.Players.Add(CreatePlayerData(World.GAIA_ID, "Gaia", Color.white));
            data.Players.Add(CreatePlayerData(data.MaxPlayerId++, "Player 1", Color.blue));
            data.Players.Add(CreatePlayerData(data.MaxPlayerId++, "Player 2", Color.red));

            // Create chunks
            for (int x = 0; x < NumChunks; x++)
                for (int y = 0; y < NumChunks; y++)
                    data.Chunks.Add(CreateEmptyChunkData(data, new Vector2Int(x, y)));

            return data;
        }
        private ChunkData CreateEmptyChunkData(WorldData worldData, Vector2Int coordinates)
        {
            ChunkData chunkData = new ChunkData();
            chunkData.ChunkCoordinateX = coordinates.x;
            chunkData.ChunkCoordinateY = coordinates.y;
            chunkData.Nodes = new List<NodeData>();

            for (int y = 0; y < ChunkSize; y++)
                for (int x = 0; x < ChunkSize; x++)
                    chunkData.Nodes.Add(CreateEmptyNodeData(worldData, coordinates, new Vector2Int(x, y)));

            return chunkData;
        }
        private NodeData CreateEmptyNodeData(WorldData worldData, Vector2Int chunkCoordinates, Vector2Int localCoordinates)
        {
            NodeData data = new NodeData();
            data.Id = worldData.MaxNodeId++;
            data.LocalCoordinateX = localCoordinates.x;
            data.LocalCoordinateY = localCoordinates.y;
            data.Height = new int[] { 5, 5, 5, 5 };
            data.Surface = SurfaceId.Grass;
            data.Type = NodeType.Surface;

            return data;
        }
        private PlayerData CreatePlayerData(int id, string name, Color color)
        {
            PlayerData data = new PlayerData();
            data.Id = id;
            data.Name = name;
            data.ColorR = color.r;
            data.ColorG = color.g;
            data.ColorB = color.b;
            return data;
        }
    }
}
