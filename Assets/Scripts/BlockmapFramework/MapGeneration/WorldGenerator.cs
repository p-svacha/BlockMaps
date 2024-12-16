using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public abstract class WorldGenerator
    {
        public const int MAX_WORLD_SIZE = 512;

        public abstract string Name { get; }

        /// <summary>
        /// The amount of chunks on each side of the map. (A world with 2x2 chunks has NumChunksPerSide = 2).
        /// </summary>
        protected int NumChunksPerSide;

        /// <summary>
        /// The amount of nodes on each side of the map. (A world with 2x2 chunks has WorldSize = 32).
        /// </summary>
        protected int WorldSize { get; private set; }

        /// <summary>
        /// Amount of ground nodes on the map. (A world with 2x2 chunks has WorldArea = 32 * 32 = 1024).
        /// </summary>
        protected int WorldArea => WorldSize * WorldSize;

        protected int TotalNumChunks => NumChunksPerSide * NumChunksPerSide;



        public World World { get; private set; }
        protected GenerationPhase GenerationPhase { get; set; }
        public bool IsDone => GenerationPhase == GenerationPhase.Done;

        /// <summary>
        /// Starts a new world generation process with this generator that is continued every time UpdateGeneration() is called until the GenerationPhase is Done.
        /// </summary>
        public void StartGeneration(int numChunks)
        {
            Debug.Log($"Starting world generation '{Name}' with {numChunks}x{numChunks} chunks.");

            if (World.ChunkSize * numChunks > MAX_WORLD_SIZE) throw new System.Exception("World size can't be bigger than " + MAX_WORLD_SIZE +  ".");

            NumChunksPerSide = numChunks;
            WorldSize = World.ChunkSize * numChunks;

            GenerationPhase = GenerationPhase.InitializingGenerator;
        }

        /// <summary>
        /// First step of the world generation process.
        /// </summary>
        private void CreateEmptyWorld()
        {
            // Create empty world to start with
            World = new World(NumChunksPerSide);

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
                    CreateEmptyWorld();
                    break;

                case GenerationPhase.Generating: // Generator-specific steps
                    OnUpdate();
                    break;
            }
        }

        /// <summary>
        /// Gets called each frame. Should be used to go through the different generation steps while not blocking the program completely.
        /// <br/>When done, call FinalizeGeneration().
        /// </summary>
        protected abstract void OnUpdate();

        /// <summary>
        /// Last step of world generation. Initiates the world initialization which redraws the finished world, updates the vision of all entities and generates the full navmesh.
        /// </summary>
        protected void FinalizeGeneration()
        {
            Debug.Log($"Finalizing world generation.");
            GenerationPhase = GenerationPhase.Done;
        }

        #region Helper Functions

        /// <summary>
        /// Spawns an entity on the ground near the given point and returns the entity instance.
        /// </summary>
        protected Entity SpawnEntityOnGroundAround(EntityDef def, Actor player, Vector2Int pos, float standard_deviation, Direction rotation, List<BlockmapNode> forbiddenNodes = null)
        {
            int maxAttempts = 50;
            if (standard_deviation == 0f) maxAttempts = 1;
            int numAttempts = 0;

            while (numAttempts++ < maxAttempts) // Keep searching until we find a suitable position
            {
                Vector2Int targetPos = HelperFunctions.GetRandomNearPosition(pos, standard_deviation);

                if (!World.IsInWorld(targetPos)) continue;

                BlockmapNode targetNode = World.GetGroundNode(targetPos);
                if (forbiddenNodes != null && forbiddenNodes.Contains(targetNode)) continue;
                if (!World.CanSpawnEntity(def, targetNode, rotation, forceHeadspaceRecalc: true)) continue;

                return World.SpawnEntity(def, targetNode, rotation, player, updateWorld: false);
            }

            Debug.LogWarning("Could not spawn " + def.Label + " around " + pos.ToString() + " after " + maxAttempts + " attempts.");
            return null;
        }

        protected Vector2Int GetRandomWorldCoordinates() => new Vector2Int(Random.Range(0, WorldSize), Random.Range(0, WorldSize));
        protected Vector2 GetRandomWorldPosition2d() => new Vector2(Random.Range(0f, WorldSize), Random.Range(0f, WorldSize));

        protected void Log(string message)
        {
            Debug.Log($"[Map Generation] {message}");
        }

        #endregion
    }
}
