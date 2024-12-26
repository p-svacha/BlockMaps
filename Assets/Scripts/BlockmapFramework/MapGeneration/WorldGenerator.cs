using BlockmapFramework.Profiling;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WorldGenerator
    {
        public const int MAX_WORLD_SIZE = 512;

        public abstract string Label { get; }

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

        /// <summary>
        /// The action that gets invoked when the generator is done.
        /// <br/>Note that the world is then not yet initialized (drawn/navmesh, etc), but merely the generator is finished with its part.
        /// </summary>
        public System.Action OnDoneCallback { get; private set; }



        public World World { get; private set; }
        protected GenerationPhase GenerationPhase { get; set; }
        public bool IsDone => GenerationPhase == GenerationPhase.Done;

        // Generation
        private int CurrentGenerationStep;
        private List<System.Action> GenerationSteps;

        /// <summary>
        /// Starts a new world generation process with this generator that is continued every time UpdateGeneration() is called until the GenerationPhase is Done.
        /// </summary>
        public void StartGeneration(int numChunks, int seed = -1, System.Action onDoneCallback = null)
        {
            Profiler.Begin("World Generation");

            if (seed == -1) seed = GetRandomSeed();
            Random.InitState(seed);
            Debug.Log($"Starting world generation '{Label}' with {numChunks}x{numChunks} chunks. Seed = {seed}");

            if (World.ChunkSize * numChunks > MAX_WORLD_SIZE) throw new System.Exception("World size can't be bigger than " + MAX_WORLD_SIZE +  ".");

            NumChunksPerSide = numChunks;
            WorldSize = World.ChunkSize * numChunks;
            OnDoneCallback = onDoneCallback;

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
            CurrentGenerationStep = 0;
            GenerationSteps = GetGenerationSteps();
            OnGenerationStart();
        }

        protected abstract List<System.Action> GetGenerationSteps();

        protected virtual void OnGenerationStart() { }

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

                case GenerationPhase.Generating:
                    ExecuteGenerationStep();
                    break;
            }
        }

        private void ExecuteGenerationStep()
        {
            if (CurrentGenerationStep == GenerationSteps.Count)
            {
                FinalizeGeneration();
                return;
            }

            Debug.Log("Starting World Generation Step: " + GenerationSteps[CurrentGenerationStep].Method.Name);
            Profiler.Begin(GenerationSteps[CurrentGenerationStep].Method.Name);
            GenerationSteps[CurrentGenerationStep].Invoke();
            Profiler.End(GenerationSteps[CurrentGenerationStep].Method.Name);
            CurrentGenerationStep++;
        }

        /// <summary>
        /// Last step of world generation. Initiates the world initialization which redraws the finished world, updates the vision of all entities and generates the full navmesh.
        /// </summary>
        private void FinalizeGeneration()
        {
            GenerationPhase = GenerationPhase.Done;
            Profiler.End("World Generation");
            
            OnDoneCallback?.Invoke();
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

        public static int GetRandomSeed()
        {
            return Random.Range(int.MinValue, int.MaxValue);
        }
    }
}
