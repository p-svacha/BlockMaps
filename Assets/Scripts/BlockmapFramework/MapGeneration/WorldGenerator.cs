using BlockmapFramework.Profiling;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltimateNoiseLibrary;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class WorldGenerator
    {
        public const int MAX_WORLD_SIZE = 512;

        public abstract string Label { get; }
        public abstract string Description { get; }
        public abstract bool StartAsVoid { get; }

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
            Profiler.Begin("Create Empty World");
            // Create empty world to start with
            World = new World(NumChunksPerSide, StartAsVoid);
            Profiler.End("Create Empty World");

            GenerationPhase = GenerationPhase.Generating;
            CurrentGenerationStep = 0;
            GenerationSteps = GetGenerationSteps();
            OnGenerationStart();
        }

        /// <summary>
        /// Called once at the start of the world generation to retrieve all steps (actions) that need to be executed for the full generation.
        /// <br/>Should only contain generator-specific actions.
        /// </summary>
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

            Debug.Log($"Starting World Generation Step: {GenerationSteps[CurrentGenerationStep].Method.Name}");
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
        /// Sets the corner altitudes of all ground nodes according to the given height map.
        /// </summary>
        protected static void ApplyHeightmap(World world, int[,] heightMap)
        {
            foreach (GroundNode n in world.GetAllGroundNodes())
            {
                ApplyHeightmap(world, heightMap, n);
            }
        }

        /// <summary>
        /// Sets the corner altitudes of a ground nodes according to the given height map.
        /// <br/>Returns if anything has been changed on the node.
        /// </summary>
        protected static bool ApplyHeightmap(World world, int[,] heightMap, GroundNode node, bool raiseOnly = false)
        {
            if (raiseOnly)
            {
                if (node.Altitude[Direction.SW] >= heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y] &&
                    node.Altitude[Direction.SE] >= heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y] &&
                    node.Altitude[Direction.NE] >= heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y + 1] &&
                    node.Altitude[Direction.NW] >= heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y + 1]) return false; // no change needed

                Dictionary<Direction, int> newNodeAltitudes = new Dictionary<Direction, int>()
                {
                    { Direction.SW, Mathf.Max(node.Altitude[Direction.SW], heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y]) },
                    { Direction.SE, Mathf.Max(node.Altitude[Direction.SE], heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y]) },
                    { Direction.NE, Mathf.Max(node.Altitude[Direction.NE], heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y + 1]) },
                    { Direction.NW, Mathf.Max(node.Altitude[Direction.NW], heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y + 1]) },
                };
                node.SetAltitude(newNodeAltitudes);
                return true;
            }
            else
            {
                Dictionary<Direction, int> newNodeAltitudes = new Dictionary<Direction, int>()
                {
                    { Direction.SW, heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y] },
                    { Direction.SE, heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y] },
                    { Direction.NE, heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y + 1] },
                    { Direction.NW, heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y + 1] },
                };
                node.SetAltitude(newNodeAltitudes);
                return true;
            }
        }

        /// <summary>
        /// Adds the altitudes on all corners of all ground nodes according to the given height map.
        /// </summary>
        protected static void AddHeightmap(World world, int[,] heightMap)
        {
            foreach (GroundNode n in world.GetAllGroundNodes())
            {
                AddHeightmap(world, heightMap, n);
            }
        }

        /// <summary>
        /// Adds the altitudes on all corners of a ground node according to the given height map.
        /// </summary>
        protected static void AddHeightmap(World world, int[,] heightMap, GroundNode node)
        {
            Dictionary<Direction, int> newNodeHeights = new Dictionary<Direction, int>()
                {
                    { Direction.SW, node.Altitude[Direction.SW] + heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y] },
                    { Direction.SE, node.Altitude[Direction.SE] + heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y] },
                    { Direction.NE, node.Altitude[Direction.NE] + heightMap[node.WorldCoordinates.x + 1, node.WorldCoordinates.y + 1] },
                    { Direction.NW, node.Altitude[Direction.NW] + heightMap[node.WorldCoordinates.x, node.WorldCoordinates.y + 1] },
                };
            node.SetAltitude(newNodeHeights);
        }

        protected Vector2Int GetRandomWorldCoordinates() => new Vector2Int(Random.Range(0, WorldSize), Random.Range(0, WorldSize));
        protected Vector2 GetRandomWorldPosition2d() => new Vector2(Random.Range(0f, WorldSize), Random.Range(0f, WorldSize));

        /// <summary>
        /// Generates a height map from a noise, given the min and max altitudes referring to the noises 0 and 1 values.
        /// <br/>The int[,] height map can then be used with add/apply heightmap to create smooth terrain.
        /// </summary>
        protected static int[,] GetHeightMapFromNoise(World world, GradientNoise noise, int minAltitude, int maxAltitude)
        {
            int[,] heightMap = new int[world.NumNodesPerSide + 1, world.NumNodesPerSide + 1];
            for (int x = 0; x < world.NumNodesPerSide + 1; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide + 1; y++)
                {
                    int value = (int)((noise.GetValue(x, y) * (maxAltitude - minAltitude)) + minAltitude);
                    heightMap[x, y] = (int)value;
                }
            }
            return heightMap;
        }

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
