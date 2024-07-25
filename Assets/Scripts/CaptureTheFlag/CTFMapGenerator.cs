using BlockmapFramework;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFMapGenerator : WorldGenerator
    {
        public const string FLAG_ID = "flag";

        // Rules
        private const int SPAWN_MAP_EDGE_OFFSET = 10;
        private const int SPAWN_VARIATION = 5;
        private const int NUM_HUMANS_PER_PLAYER = 6;
        private const int NUM_DOGS_PER_PLAYER = 2;

        // Generation
        public override string Name => "CTF Map";

        private int GenerationStep;
        private int[,] HeightMap;
        private Actor LocalPlayer;
        private Actor Opponent;

        protected override void OnGenerationStart()
        {
            GenerationStep = 0;
            LocalPlayer = World.GetActor(1);
            Opponent = World.GetActor(2);
        }
        protected override void OnUpdate()
        {
            if (GenerationStep == 0) GenerateNoise();
            else if (GenerationStep == 1) ApplyHeightmap();
            else if (GenerationStep == 2) CreateMapZones();
            else if (GenerationStep == 3) CreatePlayerBases();
            else if (GenerationStep == 4) AddWaterBodies();
            else if (GenerationStep == 5) AddBridges();
            else if (GenerationStep == 6) AddForests();
            else if (GenerationStep == 7) FinishGeneration();
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

            foreach (GroundNode n in World.GetAllGroundNodes())
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

            World.DrawNodes();
            GenerationStep++;
        }

        private void CreatePlayerBases()
        {
            int p1X = SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(LocalPlayer, p1X, Direction.E);

            int p2X = WorldSize - SPAWN_MAP_EDGE_OFFSET;
            CreatePlayerSpawn(Opponent, p2X, Direction.W);

            World.DrawNodes();
            GenerationStep++;
        }
        /// <summary>
        /// Spawns the flag, characters and jail zone for a player.
        /// </summary>
        private void CreatePlayerSpawn(Actor player, int spawnX, Direction faceDirection)
        {
            // Position
            int spawnY = Random.Range(SPAWN_MAP_EDGE_OFFSET, WorldSize - SPAWN_MAP_EDGE_OFFSET);
            Vector2Int spawnAreaCenter = new Vector2Int(spawnX, spawnY);

            // Flag
            Entity spawnedFlag = null;
            Entity flagPrefab = GetEntityPrefab(FLAG_ID);
            while (spawnedFlag == null) spawnedFlag = SpawnEntityAround(flagPrefab, player, spawnAreaCenter, 0f, HelperFunctions.GetRandomSideDirection());
            
            // Jail zone
            HashSet<Vector2Int> jailZoneCoords = new HashSet<Vector2Int>();
            int flagDistanceX, flagDistanceY;
            do
            {
                flagDistanceX = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                flagDistanceY = Random.Range(CTFGame.JAIL_ZONE_MIN_FLAG_DISTANCE, CTFGame.JAIL_ZONE_MAX_FLAG_DISTANCE + 1);
                if (Random.value < 0.5f) flagDistanceX *= -1;
                if (Random.value < 0.5f) flagDistanceY *= -1;
            }
            while (spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX < World.MinX + CTFGame.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.x + flagDistanceX > World.MaxX - CTFGame.JAIL_ZONE_RADIUS - 1 ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY < World.MinY + CTFGame.JAIL_ZONE_RADIUS ||
                spawnedFlag.OriginNode.WorldCoordinates.y + flagDistanceY > World.MaxY - CTFGame.JAIL_ZONE_RADIUS - 1);

            Vector2Int jailZoneCenter = spawnedFlag.OriginNode.WorldCoordinates + new Vector2Int(flagDistanceX, flagDistanceY);
            for (int x = -(CTFGame.JAIL_ZONE_RADIUS - 1); x < CTFGame.JAIL_ZONE_RADIUS; x++)
            {
                for (int y = -(CTFGame.JAIL_ZONE_RADIUS - 1); y < CTFGame.JAIL_ZONE_RADIUS; y++)
                {
                    jailZoneCoords.Add(jailZoneCenter + new Vector2Int(x, y));
                }
            }
            World.AddZone(jailZoneCoords, player, providesVision: false, showBorders: true);

            // Flag zone
            HashSet<Vector2Int> flagZoneCoords = new HashSet<Vector2Int>();
            for(int x = -(int)(CTFGame.FLAG_ZONE_RADIUS + 1); x <= CTFGame.FLAG_ZONE_RADIUS + 1; x++)
            {
                for (int y = -(int)(CTFGame.FLAG_ZONE_RADIUS + 1); y <= CTFGame.FLAG_ZONE_RADIUS + 1; y++)
                {
                    Vector2Int offsetCoords = new Vector2Int(x, y);
                    float distance = offsetCoords.magnitude;
                    if(distance <= CTFGame.FLAG_ZONE_RADIUS)
                    {
                        flagZoneCoords.Add(spawnedFlag.OriginNode.WorldCoordinates + offsetCoords);
                    }
                }
            }
            Zone flagZone = World.AddZone(flagZoneCoords, player, providesVision: true, showBorders: true);

            // Humans
            int humansSpawned = 0;
            Entity humanPrefab = GetCharacterPrefab("human");
            while (humansSpawned < NUM_HUMANS_PER_PLAYER)
            {
                if (SpawnEntityAround(humanPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes)) humansSpawned++;
            }

            // Dogs
            int dogsSpawned = 0;
            Entity dogPrefab = GetCharacterPrefab("dog");
            while (dogsSpawned < NUM_DOGS_PER_PLAYER)
            {
                if (SpawnEntityAround(dogPrefab, player, spawnAreaCenter, SPAWN_VARIATION, faceDirection, forbiddenNodes: flagZone.Nodes)) dogsSpawned++;
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
                GroundNode n = World.GetRandomGroundNode();
                WaterBody b = World.CanAddWater(n, 3);
                if (b != null)
                {
                    World.AddWaterBody(b, updateNavmesh: false);
                    numWaterBodies++;
                }
            }

            // End
            //Debug.Log("Generated " + numWaterBodies + " water bodies after " + attempts + " attempts.");

            World.DrawNodes();
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
                GroundNode startNode = World.GetRandomGroundNode();
                Direction dir1 = HelperFunctions.GetRandomSideDirection();
                Direction dir2 = HelperFunctions.GetOppositeDirection(dir1);
                int bridgeHeight = startNode.MaxAltitude + Random.Range(1, 7);
                List<Vector2Int> bridgeCoordinates = new List<Vector2Int>() { startNode.WorldCoordinates };

                // Go into first direction until the bridge ends
                GroundNode nextNode = World.GetAdjacentGroundNode(startNode, dir1);
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
                    else if(nextNode.IsFlat(dir2) && nextNode.GetMaxAltitude(dir2) == bridgeHeight)
                    {
                        isDone = true;
                        isValid = true;
                    }

                    // Build and end
                    else if(nextNode.IsFlat(dir1) && nextNode.GetMaxAltitude(dir1) == bridgeHeight)
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        isDone = true;
                        isValid = true;
                    }

                    // Build and continue
                    else
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        nextNode = World.GetAdjacentGroundNode(nextNode, dir1);
                    }
                }
                if (!isValid) continue;

                // Go into first direction until the bridge ends
                nextNode = World.GetAdjacentGroundNode(startNode, dir2);
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
                    else if (nextNode.IsFlat(dir1) && nextNode.GetMaxAltitude(dir1) == bridgeHeight)
                    {
                        isDone = true;
                        isValid = true;
                    }

                    // Build and end
                    else if (nextNode.IsFlat(dir2) && nextNode.GetMaxAltitude(dir2) == bridgeHeight)
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        isDone = true;
                        isValid = true;
                    }

                    // Build and continue
                    else
                    {
                        bridgeCoordinates.Add(nextNode.WorldCoordinates);
                        nextNode = World.GetAdjacentGroundNode(nextNode, dir2);
                    }
                }
                if (!isValid) continue;

                // Build bridge
                if (bridgeCoordinates.Any(x => !World.CanBuildAirPath(x, bridgeHeight))) continue; // Don't build if any bridge node can't be built
                foreach (Vector2Int coords in bridgeCoordinates)
                {
                    World.BuildAirPath(coords, bridgeHeight, SurfaceId.Concrete);
                }
                numBridges++;
            }

            // End
            //Debug.Log("Generated " + numBridges + " bridges after " + attempts + " attempts.");

            World.DrawNodes();
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

            //GeneratedWorld.DrawNodes();
            GenerationStep++;
        }

        private void CreateMapZones()
        {
            int neutralZoneSize = (int)(World.Dimensions.x * CTFGame.NEUTRAL_ZONE_SIZE);
            int playerZoneSize = (World.Dimensions.x / 2) - (neutralZoneSize / 2);
            HashSet<Vector2Int> ownZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> neutralZoneNodes = new HashSet<Vector2Int>();
            HashSet<Vector2Int> opponentZoneNodes = new HashSet<Vector2Int>();
            foreach (BlockmapNode node in World.GetAllGroundNodes())
            {
                if (node.WorldCoordinates.x < playerZoneSize) ownZoneNodes.Add(node.WorldCoordinates);
                else if (node.WorldCoordinates.x < playerZoneSize + neutralZoneSize) neutralZoneNodes.Add(node.WorldCoordinates);
                else opponentZoneNodes.Add(node.WorldCoordinates);
            }
            Zone localPlayerZone = World.AddZone(ownZoneNodes, LocalPlayer, providesVision: false, showBorders: true);
            Zone neutralZone = World.AddZone(neutralZoneNodes, World.Gaia, providesVision: false, showBorders: true);
            Zone opponentZone = World.AddZone(opponentZoneNodes, Opponent, providesVision: false, showBorders: true);

            GenerationStep++;
        }

        private void SpawnRandomTree(int x, int y)
        {
            BlockmapNode targetNode = World.GetGroundNode(new Vector2Int(x, y));
            Direction rotation = HelperFunctions.GetRandomSideDirection();

            Entity prefab = GetEntityPrefab(GetRandomTreeId());

            if (World.CanSpawnEntity(prefab, targetNode, rotation))
            {
                World.SpawnEntity(prefab, targetNode, rotation, World.Gaia, updateWorld: false);
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
