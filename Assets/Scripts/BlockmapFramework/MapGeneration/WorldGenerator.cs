using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public abstract class WorldGenerator
    {
        private const int MAX_WORLD_SIZE = 512;

        public abstract string Name { get; }

        protected int ChunkSize;
        protected int NumChunks;
        protected int WorldSize;

        public World World { get; private set; }
        protected GenerationPhase GenerationPhase { get; set; }
        public bool IsDone => GenerationPhase == GenerationPhase.Done;

        public void InitGeneration(int chunkSize, int numChunks)
        {
            if (chunkSize != 16) throw new System.Exception("Chunk size must be 16 due to shader limitations.");
            if (chunkSize * numChunks > MAX_WORLD_SIZE) throw new System.Exception("World size can't be bigger than " + MAX_WORLD_SIZE +  ".");

            ChunkSize = chunkSize;
            NumChunks = numChunks;
            WorldSize = chunkSize * numChunks;

            GenerationPhase = GenerationPhase.InitializingGenerator;
        }

        private void StartGeneration()
        {
            // Create empty world to start with
            WorldData data = CreateEmptyWorldData();
            World = World.SimpleLoad(data);

            GenerationPhase = GenerationPhase.Generating;
            OnGenerationStart();
        }

        protected abstract void OnGenerationStart();

        /// <summary>
        /// Call this in FixedUpdate.
        /// </summary>
        public void UpdateGeneration()
        {
            switch(GenerationPhase)
            {
                case GenerationPhase.InitializingGenerator:
                    StartGeneration();
                    break;

                case GenerationPhase.Generating:
                    OnUpdate();
                    break;

                case GenerationPhase.InitializingWorld:
                    if (World.IsInitialized) GenerationPhase = GenerationPhase.Done;
                    break;
            }
        }

        /// <summary>
        /// Gets called each frame. Should be used to go through the different generation steps while not blocking the program completely.
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// Last step of world generation. When called, the world generator stops it work and the world starts being initialized.
        /// </summary>
        protected void FinishGeneration()
        {
            foreach (Chunk c in World.GetAllChunks()) World.RedrawChunk(c);

            foreach (Entity e in World.GetAllEntities()) e.UpdateVision();
            World.GenerateFullNavmesh();

            GenerationPhase = GenerationPhase.InitializingWorld;;
        }

        protected WorldData CreateEmptyWorldData()
        {
            WorldData data = new WorldData();
            data.Name = "World";
            data.ChunkSize = ChunkSize;
            data.Chunks = new List<ChunkData>();
            data.Actors = new List<ActorData>();
            data.Entities = new List<EntityData>();
            data.WaterBodies = new List<WaterBodyData>();
            data.Fences = new List<FenceData>();

            data.MaxEntityId = -1;
            data.MaxZoneId = -1;
            data.MaxWaterBodyId = -1;

            // Create players
            data.Actors.Add(CreatePlayerData(data.MaxActorId++, "Gaia", Color.white));
            data.Actors.Add(CreatePlayerData(data.MaxActorId++, "Player 1", Color.blue));
            data.Actors.Add(CreatePlayerData(data.MaxActorId++, "Player 2", Color.red));

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
            data.Type = NodeType.Ground;
            data.SubType = (int)SurfaceId.Grass;

            return data;
        }
        private ActorData CreatePlayerData(int id, string name, Color color)
        {
            ActorData data = new ActorData();
            data.Id = id;
            data.Name = name;
            data.ColorR = color.r;
            data.ColorG = color.g;
            data.ColorB = color.b;
            return data;
        }

        #region Helper Functions

        /// <summary>
        /// Spawns an entity on the ground near the given point and returns the entity instance.
        /// </summary>
        protected Entity SpawnEntityOnGroundAround(Entity prefab, Actor player, Vector2Int pos, float standard_deviation, Direction rotation, List<BlockmapNode> forbiddenNodes = null)
        {
            int maxAttempts = 100;
            int numAttempts = 0;
            while (numAttempts++ < maxAttempts) // Keep searching until we find a suitable position
            {
                Vector2Int targetPos = HelperFunctions.GetRandomNearPosition(pos, standard_deviation);

                if (!World.IsInWorld(targetPos)) continue;

                BlockmapNode targetNode = World.GetGroundNode(targetPos);
                if (forbiddenNodes != null && forbiddenNodes.Contains(targetNode)) continue;
                if (!World.CanSpawnEntity(prefab, targetNode, rotation, forceHeadspaceRecalc: true)) continue;

                return World.SpawnEntity(prefab, targetNode, rotation, player, updateWorld: false);
            }

            Debug.LogWarning("Could not spawn " + prefab.Name + " around " + pos.ToString() + " after " + maxAttempts + " attempts.");
            return null;
        }

        protected Entity GetEntityPrefab(string id)
        {
            string fullPath = "Entities/Prefabs/" + id;
            Entity prefab = Resources.Load<Entity>(fullPath);
            return prefab;
        }

        #endregion
    }
}
