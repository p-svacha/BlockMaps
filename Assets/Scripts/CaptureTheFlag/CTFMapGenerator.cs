using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFMapGenerator : WorldGenerator
    {
        // Rules
        private const int SPAWN_MAP_EDGE_OFFSET = 10;
        private const int SPAWN_VARIATION = 5;
        private const int NUM_HUMANS_PER_PLAYER = 6;
        private const int NUM_DOGS_PER_PLAYER = 2;

        // Generation
        public override string Name => "CTF Map";

        private int GenerationStep;
        private int[,] HeightMap;

        protected override void OnGenerationStart()
        {
            GenerationStep = 0;
        }
        protected override void OnUpdate()
        {
            if (GenerationStep == 0) GenerateNoise();
            else if (GenerationStep == 1) ApplyHeightmap();
            else if (GenerationStep == 2) CreatePlayerBases();
            else if (GenerationStep == 3) AddWaterBodies();
            else if (GenerationStep == 4) AddBridges();
            else if (GenerationStep == 5) AddForests();
        }

        private void GenerateNoise()
        {
            PerlinNoise noise = new PerlinNoise();
            noise.Scale = 0.02f;
            LayerOperation op = new LayerOperation(4, 2, 0.5f);

            float heightModifier = 20f;
            HeightMap = new int[WorldSize + 1, WorldSize + 1];
            for (int x = 0; x < WorldSize + 1; x++)
            {
                for (int y = 0; y < WorldSize + 1; y++)
                {
                    float value = op.DoOperation(new GradientNoise[] { noise }, x, y);
                    value *= heightModifier;
                    HeightMap[x, y] = (int)(value);
                }
            }

            GenerationStep++;
        }

        private void ApplyHeightmap()
        {

            foreach (SurfaceNode n in GeneratedWorld.GetAllSurfaceNodes())
            {
                Dictionary<Direction, int> nodeHeights = new Dictionary<Direction, int>()
                    {
                        { Direction.SW, HeightMap[n.WorldCoordinates.x, n.WorldCoordinates.y] },
                        { Direction.SE, HeightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y] },
                        { Direction.NE, HeightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y + 1] },
                        { Direction.NW, HeightMap[n.WorldCoordinates.x, n.WorldCoordinates.y + 1] },
                    };
                n.SetHeight(nodeHeights);
            }

            GeneratedWorld.DrawNodes();
            GenerationStep++;
        }

        private void CreatePlayerBases()
        {
            int p1X = SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(GeneratedWorld.Actors[0], p1X, Direction.E);

            int p2X = WorldSize - SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(GeneratedWorld.Actors[1], p2X, Direction.W);

            GeneratedWorld.DrawNodes();
            GenerationStep++;
        }
        /// <summary>
        /// Spawns the flag and characters for a player.
        /// </summary>
        private void CreatePlayerSpawn(Actor player, int x, Direction faceDirection)
        {
            // Position
            int y = Random.Range(SPAWN_MAP_EDGE_OFFSET, WorldSize - SPAWN_MAP_EDGE_OFFSET);
            Vector2Int spawnAreaCenter = new Vector2Int(x, y);

            // Flag
            bool flagSpawned = false;
            Entity flagPrefab = GetEntityPrefab("flag");
            while (!flagSpawned) flagSpawned = SpawnEntityAround(flagPrefab, player, spawnAreaCenter, SPAWN_VARIATION, HelperFunctions.GetRandomSideDirection());

            // Humans
            int humansSpawned = 0;
            Entity humanPrefab = GetCharacterPrefab("human");
            while (humansSpawned < NUM_HUMANS_PER_PLAYER)
            {
                if (SpawnEntityAround(humanPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection)) humansSpawned++;
            }

            // Dogs
            int dogsSpawned = 0;
            Entity dogPrefab = GetCharacterPrefab("dog");
            while (dogsSpawned < NUM_DOGS_PER_PLAYER)
            {
                if (SpawnEntityAround(dogPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection)) dogsSpawned++;
            }
        }

        private void AddWaterBodies()
        {
            int numWaterBodies = 0;
            int attempts = 0;

            int targetAttempts = WorldSize / 5;

            while (attempts < targetAttempts)
            {
                attempts++;
                SurfaceNode n = GeneratedWorld.GetRandomSurfaceNode();
                WaterBody b = GeneratedWorld.CanAddWater(n, 3);
                if (b != null)
                {
                    GeneratedWorld.AddWaterBody(b, updateNavmesh: false);
                    numWaterBodies++;
                }
            }

            // End
            Debug.Log("Generated " + numWaterBodies + " water bodies after " + attempts + " attempts.");

            GeneratedWorld.DrawNodes();
            GenerationStep++;
        }

        private void AddBridges()
        {
            int numBridges = 0;
            int attempts = 0;

            int targetAttempts = WorldSize / 4;

            while (attempts < targetAttempts)
            {
                attempts++;

                // Take a random surface node, direction and bridge height
                SurfaceNode startNode = GeneratedWorld.GetRandomSurfaceNode();
                Direction dir1 = HelperFunctions.GetRandomSideDirection();
                Direction dir2 = HelperFunctions.GetOppositeDirection(dir1);
                int bridgeHeight = startNode.MaxHeight + Random.Range(1, 7);
                List<Vector2Int> bridgeCoordinates = new List<Vector2Int>() { startNode.WorldCoordinates };

                // Go into first direction until the bridge ends
                SurfaceNode nextNode = GeneratedWorld.GetAdjacentSurfaceNode(startNode, dir1);
                bool isDone = false;
                bool isValid = false;
                while(!isDone)
                {
                    // End at world edge
                    if(nextNode == null)
                    {
                        isDone = true;
                        isValid = false;
                    }

                    // End
                    else if(nextNode.IsFlat(dir2) && nextNode.GetMaxHeight(dir2) == bridgeHeight)
                    {
                        isDone = true;
                        isValid = true;
                    }

                    // Build and end
                    else if(nextNode.IsFlat(dir1) && nextNode.GetMaxHeight(dir1) == bridgeHeight)
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        isDone = true;
                        isValid = true;
                    }

                    // Build and continue
                    else
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        nextNode = GeneratedWorld.GetAdjacentSurfaceNode(nextNode, dir1);
                    }
                }
                if (!isValid) continue;

                // Go into first direction until the bridge ends
                nextNode = GeneratedWorld.GetAdjacentSurfaceNode(startNode, dir2);
                isDone = false;
                isValid = false;
                while (!isDone)
                {
                    // End at world edge
                    if (nextNode == null)
                    {
                        isDone = true;
                        isValid = false;
                    }

                    // End
                    else if (nextNode.IsFlat(dir1) && nextNode.GetMaxHeight(dir1) == bridgeHeight)
                    {
                        isDone = true;
                        isValid = true;
                    }

                    // Build and end
                    else if (nextNode.IsFlat(dir2) && nextNode.GetMaxHeight(dir2) == bridgeHeight)
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        isDone = true;
                        isValid = true;
                    }

                    // Build and continue
                    else
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        nextNode = GeneratedWorld.GetAdjacentSurfaceNode(nextNode, dir2);
                    }
                }
                if (!isValid) continue;

                // Build bridge
                if (bridgeCoordinates.Any(x => !GeneratedWorld.CanBuildAirPath(x, bridgeHeight))) continue; // Don't build if any bridge node can't be built
                foreach (Vector2Int coords in bridgeCoordinates)
                {
                    GeneratedWorld.BuildAirPath(coords, bridgeHeight);
                }
                numBridges++;
            }

            // End
            Debug.Log("Generated " + numBridges + " bridges after " + attempts + " attempts.");

            GeneratedWorld.DrawNodes();
            GenerationStep++;
        }

        private void AddForests()
        {
            PerlinNoise forestDensityMap = new PerlinNoise();
            forestDensityMap.Scale = 0.1f;
            float densityModifier = 0.15f; // max tree chance per tile

            for (int x = 0; x < WorldSize; x++)
            {
                for (int y = 0; y < WorldSize; y++)
                {
                    float densityMapValue = forestDensityMap.GetValue(x, y);
                    if (densityMapValue > 0.3f && Random.value < densityMapValue * densityModifier)
                    {
                        SpawnRandomTree(x, y);
                    }
                }
            }

            GeneratedWorld.DrawNodes();
            GenerationStep++;
            FinishGeneration();
        }

        private void SpawnRandomTree(int x, int y)
        {
            BlockmapNode targetNode = GeneratedWorld.GetSurfaceNode(new Vector2Int(x, y));
            Direction rotation = HelperFunctions.GetRandomSideDirection();

            Entity prefab = GetEntityPrefab(GetRandomTreeId());

            if (GeneratedWorld.CanSpawnEntity(prefab, targetNode, rotation))
            {
                GeneratedWorld.SpawnEntity(prefab, targetNode, rotation, GeneratedWorld.Gaia, updateWorld: false);
            }
        }
        private string GetRandomTreeId()
        {
            Dictionary<string, float> ids = new Dictionary<string, float>()
            {
                { "tree01", 1f },
                { "tree02", 3f },
                { "tree03", 1f },
                { "log_2x1", 0.3f },
            };
            return HelperFunctions.GetWeightedRandomElement(ids);
        }

        private Entity GetCharacterPrefab(string id)
        {
            string fullPath = "CaptureTheFlag/Characters/" + id;
            Entity prefab = Resources.Load<Entity>(fullPath);
            return prefab;
        }
    }
}
