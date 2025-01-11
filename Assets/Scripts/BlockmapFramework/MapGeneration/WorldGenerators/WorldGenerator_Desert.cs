using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltimateNoiseLibrary;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class WorldGenerator_Desert : WorldGenerator
    {
        public override string Label => "Desert";
        public override string Description => "Very sparse, flat and empty map with lots of sand.";

        private HashSet<Vector2Int> DuneNodes;
        private HashSet<Vector2Int> MesaNodes;
        private HashSet<Vector2Int> MountainNodes;

        private static Dictionary<EntityDef, float> ShrubClusterProbabilities = new Dictionary<EntityDef, float>()
        {
            { EntityDefOf.Shrub_01, 0.2f },
            { EntityDefOf.Shrub_02_Small, 0.2f },
            { EntityDefOf.Shrub_02_Medium, 0.2f },
            { EntityDefOf.Shrub_Wide_01, 1f },
            { EntityDefOf.Shrub_Tall_01, 0.2f },
            { EntityDefOf.Grass_01, 0.5f },
            { EntityDefOf.Saguaro_01_Small, 0.04f },
            { EntityDefOf.Saguaro_01_Medium, 0.04f },
            { EntityDefOf.Saguaro_01_Big, 0.04f },
            { EntityDefOf.Palm_Tree_01, 0.01f },
            { EntityDefOf.Palm_Tree_02, 0.01f },
        };

        private static Dictionary<EntityDef, float> OasisPlantProbabilities = new Dictionary<EntityDef, float>()
        {
            { EntityDefOf.Shrub_01, 0.2f },
            { EntityDefOf.Shrub_02_Small, 0.2f },
            { EntityDefOf.Shrub_02_Medium, 0.2f },
            { EntityDefOf.Shrub_Wide_01, 0.3f },
            { EntityDefOf.Shrub_Tall_01, 1f },
            { EntityDefOf.Grass_01, 0.2f },
            { EntityDefOf.Saguaro_01_Small, 0.1f },
            { EntityDefOf.Saguaro_01_Medium, 0.1f },
            { EntityDefOf.Saguaro_01_Big, 0.1f },
            { EntityDefOf.Palm_Tree_01, 2f },
            { EntityDefOf.Palm_Tree_02, 2f },
        };

        private static Dictionary<EntityDef, float> MesaEdgeRockProbabilities = new Dictionary<EntityDef, float>()
        {
            { EntityDefOf.Rock_01_Small, 1f },
            { EntityDefOf.Rock_02_Small, 1f },
            { EntityDefOf.Rock_03_Small, 1f },
            { EntityDefOf.Rock_04_Small, 1f },
            { EntityDefOf.Rock_05_Small, 1f },
            { EntityDefOf.Rock_06_Small, 1f },
            { EntityDefOf.Rock_01_Medium, 0.1f },
        };

        private static Dictionary<EntityDef, float> ScatteredObjectsProbabilities = new Dictionary<EntityDef, float>()
        {
            { EntityDefOf.Rock_01_Small, 0.1f },
            { EntityDefOf.Rock_02_Small, 0.1f },
            { EntityDefOf.Rock_03_Small, 0.1f },
            { EntityDefOf.Rock_04_Small, 0.1f },
            { EntityDefOf.Rock_05_Small, 0.1f },
            { EntityDefOf.Rock_06_Small, 0.1f },
            { EntityDefOf.Rock_01_Medium, 0.1f },
            { EntityDefOf.Rock_01_Big, 0.1f },
            { EntityDefOf.Rock_01_Large, 0.1f },
            { EntityDefOf.Palm_Tree_01, 0.5f },
            { EntityDefOf.Palm_Tree_02, 0.5f },
            { EntityDefOf.Dead_Tree_01, 0.2f },
            { EntityDefOf.Dead_Tree_02, 0.2f },
            { EntityDefOf.Dead_Tree_03, 0.2f },
            { EntityDefOf.Dead_Tree_04, 0.2f },
            { EntityDefOf.Dead_Tree_05, 0.2f },
            { EntityDefOf.Saguaro_01_Small, 0.4f },
            { EntityDefOf.Saguaro_01_Medium, 0.4f },
            { EntityDefOf.Saguaro_01_Big, 0.4f },
            { EntityDefOf.Shrub_01, 0.1f },
            { EntityDefOf.Shrub_02_Small, 0.1f },
            { EntityDefOf.Shrub_02_Medium, 0.1f },
            { EntityDefOf.Shrub_Tall_01, 0.1f },
            { EntityDefOf.Shrub_Wide_01, 0.1f },
        };

        protected override void OnGenerationStart()
        {
            World.CliffMaterialPath = "Materials/NodeMaterials/Cliff_Desert";

            DuneNodes = new HashSet<Vector2Int>();
            MesaNodes = new HashSet<Vector2Int>();
            MountainNodes = new HashSet<Vector2Int>();
        }

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>()
            {
                ApplyBaseHeightmap,
                SetBaseSurfaces,
                AddDunes,
                AddShrubClusters,
                AddMesas,
                AddMountainSides,
                ScatterIndividualObjects,
                AddOasis,
            };
        }

        private void ApplyBaseHeightmap()
        {
            ApplyBaseHeightmap(World);
        }
        private void SetBaseSurfaces()
        {
            SetBaseSurfaces(World);
        }
        private void AddDunes()
        {
            AddDunes(World, DuneNodes);
        }
        private void AddShrubClusters()
        {
            AddShrubClusters(World, forbiddenCoordinates: DuneNodes);
        }
        private void AddMesas()
        {
            AddMesas(World, MesaNodes);
        }
        private void AddMountainSides()
        {
            if (Random.value < 0.2f)
            {
                AddSideMountain(World, Direction.S, MountainNodes);
            }
            if (Random.value < 0.2f)
            {
                AddSideMountain(World, Direction.N, MountainNodes);
            }
        }
        private void ScatterIndividualObjects()
        {
            ScatterIndividualObjects(World);
        }
        private void AddOasis()
        {
            HashSet<Vector2Int> forbiddenCoordinates = DuneNodes.Concat(MesaNodes).Concat(MountainNodes).ToHashSet();
            AddOasis(World, forbiddenCoordinates);
        }



        public static void ApplyBaseHeightmap(World world)
        {
            float BASE_HEIGHT = 5f;
            float HEIGHT_VARIATION = 5f;

            var basePerlin = new PerlinNoise(0.008f);
            var layeredPerlin = new ModularGradientNoise(new GradientNoise[] { basePerlin }, new LayerOperation(5, 2f, 0.5f));

            int[,] heightMap = new int[world.NumNodesPerSide + 1, world.NumNodesPerSide + 1];
            for (int x = 0; x < world.NumNodesPerSide + 1; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide + 1; y++)
                {
                    float value = layeredPerlin.GetValue(x, y);
                    value *= HEIGHT_VARIATION;
                    value += BASE_HEIGHT;
                    heightMap[x, y] = (int)value;
                }
            }

            ApplyHeightmap(world, heightMap);
        }
        public static void SetBaseSurfaces(World world)
        {
            var basePerlin = new PerlinNoise(0.05f);
            var layeredPerlin = new ModularGradientNoise(new GradientNoise[] { basePerlin }, new LayerOperation(5, 2f, 0.5f));

            foreach (GroundNode node in world.GetAllGroundNodes())
            {
                float surfaceNoiseValue = layeredPerlin.GetValue(node.WorldCoordinates.x, node.WorldCoordinates.y);

                if (surfaceNoiseValue < 0.35f) node.SetSurface(SurfaceDefOf.SandSoft);
                else node.SetSurface(SurfaceDefOf.Sand);
            }
        }
        public static void AddDunes(World world, HashSet<Vector2Int> affectedCoordinates)
        {
            float DUNE_HEIGHT = 25f;

            var noise3 = new SkewedPerlinNoise(0.1f);
            var noise2_op_layer = new LayerOperation(3, 2f, 0.5f);
            var noise2 = new ModularGradientNoise(
                new GradientNoise[] { noise3 },
                noise2_op_layer
            );
            var noise1 = new ModularGradientNoise(
                new GradientNoise[] { noise2 },
                new ScaleOperation(0.2f)
            );
            var noise5 = new PerlinNoise(1f);
            var noise4 = new ModularGradientNoise(
                new GradientNoise[] { noise5 },
                new CutoffOperation(2f, 2f)
            );
            var noise8 = new PerlinNoise(0.015f);
            var noise7_op_layer = new LayerOperation(2, 2f, 0.5f);
            var noise7 = new ModularGradientNoise(
                new GradientNoise[] { noise8 },
                noise7_op_layer
            );
            var noise6 = new ModularGradientNoise(
                new GradientNoise[] { noise7 },
                new CutoffOperation(0.6f, 0.7f)
            );
            var noise0 = new ModularGradientNoise(
                new GradientNoise[] { noise1, noise4, noise6 },
                new MaskOperation()
            );
            // This is the final noise reference:
            GradientNoise finalNoise = noise0;

            int[,] heightMap = new int[world.NumNodesPerSide + 1, world.NumNodesPerSide + 1];
            for (int x = 0; x < world.NumNodesPerSide + 1; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide + 1; y++)
                {
                    float value = finalNoise.GetValue(x, y);
                    value *= DUNE_HEIGHT;
                    heightMap[x, y] = (int)value;
                }
            }

            // Add height map
            foreach (GroundNode n in world.GetAllGroundNodes())
            {
                Dictionary<Direction, int> addAltitudes = new Dictionary<Direction, int>()
                {
                    { Direction.SW, heightMap[n.WorldCoordinates.x, n.WorldCoordinates.y] },
                    { Direction.SE, heightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y] },
                    { Direction.NE, heightMap[n.WorldCoordinates.x + 1, n.WorldCoordinates.y + 1] },
                    { Direction.NW, heightMap[n.WorldCoordinates.x, n.WorldCoordinates.y + 1] },
                };
                if (addAltitudes.Values.Any(x => x > 0))
                {
                    n.SetSurface(SurfaceDefOf.Sand);
                    n.AddAltitude(addAltitudes);

                    affectedCoordinates.Add(n.WorldCoordinates);
                }
            }
        }
        public static void AddShrubClusters(World world, HashSet<Vector2Int> forbiddenCoordinates = null)
        {
            var densityMap = new SkewedPerlinNoise(0.05f);
            float densityModifier = 0.1f; // max shrub chance per node

            for (int x = 0; x < world.NumNodesPerSide; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide; y++)
                {
                    if (forbiddenCoordinates != null && forbiddenCoordinates.Contains(new Vector2Int(x, y))) continue;

                    float densityMapValue = densityMap.GetValue(x, y);
                    if (densityMapValue > 0.65f && Random.value < densityMapValue * densityModifier)
                    {
                        TrySpawnRandomEntityDefOnGround(world, x, y, ShrubClusterProbabilities, variantName: "Desert");
                    }
                }
            }
        }
        public static void AddMesas(World world, HashSet<Vector2Int> affectedCoordinates)
        {
            int MESA_MIN_ALTITUDE = 10;
            int MESA_MAX_ALTITUDE = 17;

            // Generate mesa mask noise (binary noise deciding which ground nodes are affected)
            var noise2 = new PerlinNoise(0.02f);
            var noise1_op_layer = new LayerOperation(2, 4f, 0.1f);
            var noise1 = new ModularGradientNoise(
                new GradientNoise[] { noise2 },
                noise1_op_layer
            );
            var noise0 = new ModularGradientNoise(
                new GradientNoise[] { noise1 },
                new CutoffOperation(0.68f, 0.68f)
            );
            // This is the final noise reference:
            GradientNoise mesaMaskNoise = noise0;

            // Generate mesa noise (The new altitude of ground nodes affected by mesa mask)
            var mesaNoise = new PerlinNoise(0.01f);
            int[,] mesaHeightMap = GetHeightMapFromNoise(world, mesaNoise, MESA_MIN_ALTITUDE, MESA_MAX_ALTITUDE);

            for (int x = 0; x < world.NumNodesPerSide; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide; y++)
                {
                    bool isMesa = mesaMaskNoise.GetValue(x, y) >= 1f;
                    bool isMesaEdge = (isMesa && (mesaMaskNoise.GetValue(x + 1, y) < 1 || mesaMaskNoise.GetValue(x - 1, y) < 1 || mesaMaskNoise.GetValue(x, y + 1) < 1 || mesaMaskNoise.GetValue(x, y - 1) < 1));
                    if (isMesa)
                    {
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        int oldBaseAltitude = groundNode.BaseAltitude;
                        bool changeMade = ApplyHeightmap(world, mesaHeightMap, groundNode, raiseOnly: true);

                        affectedCoordinates.Add(groundNode.WorldCoordinates);

                        if(isMesaEdge && changeMade)
                        {
                            // Chance to randomly lower a mesa edge
                            if (Random.value < 0.3f) groundNode.SetAltitude(Random.Range(oldBaseAltitude, groundNode.BaseAltitude));

                            // Spawn a rock nearby
                            EntityDef rock = MesaEdgeRockProbabilities.GetWeightedRandomElement();
                            EntityManager.SpawnEntityAround(world, rock, world.Gaia, groundNode.WorldCoordinates, standard_deviation: 2f, randomMirror: true, variantName: "Desert");
                        }
                    }
                }
            }
        }

        public static void AddSideMountain(World world, Direction side, HashSet<Vector2Int> affectedCoordinates)
        {
            int MIN_MOUNTAIN_WIDTH = (int)(0.03f * world.NumNodesPerSide);
            int MAX_MOUNTAIN_WIDTH = (int)(0.12f * world.NumNodesPerSide);

            int MIN_MOUNTAIN_HEIGHT = 20;
            int MAX_MOUNTAIN_HEIGHT = 35;

            // Generate mountain noise (The new altitude of ground nodes turning into mountains)
            var mountainNoise = new ModularGradientNoise(new GradientNoise[] { new PerlinNoise(0.1f) }, new LayerOperation(3, 0.5f, 2f));
            int[,] mountainHeightMap = GetHeightMapFromNoise(world, mountainNoise, MIN_MOUNTAIN_HEIGHT, MAX_MOUNTAIN_HEIGHT);

            // Generate widths along map edge
            int[] mountainWidths = new int[world.NumNodesPerSide];

            float currentWidth = Random.Range(MIN_MOUNTAIN_WIDTH, MAX_MOUNTAIN_WIDTH + 1);
            float widthChange = Random.Range(-0.2f, 0.2f);
            for (int i = 0; i < world.NumNodesPerSide; i++)
            {
                mountainWidths[i] = Mathf.Clamp((int)currentWidth, MIN_MOUNTAIN_WIDTH, MAX_MOUNTAIN_WIDTH);
                float widthChangeChange = Random.Range(-0.1f, 0.1f);
                if (mountainWidths[i] == MIN_MOUNTAIN_WIDTH) widthChangeChange = 0.1f;
                if (mountainWidths[i] == MAX_MOUNTAIN_WIDTH) widthChangeChange = -0.1f;
                widthChange += widthChangeChange;
                currentWidth += widthChange;
            }

            // Elevate
            if (side == Direction.N)
            {
                for (int x = 0; x < world.NumNodesPerSide; x++)
                {
                    for (int y = world.NumNodesPerSide - 1; y > world.NumNodesPerSide - mountainWidths[x] - 1; y--)
                    {
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        ApplyHeightmap(world, mountainHeightMap, groundNode);

                        affectedCoordinates.Add(groundNode.WorldCoordinates);
                    }
                }
            }
            if (side == Direction.S)
            {
                for (int x = 0; x < world.NumNodesPerSide; x++)
                {
                    for (int y = 0; y < mountainWidths[x]; y++)
                    {
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        ApplyHeightmap(world, mountainHeightMap, groundNode);

                        affectedCoordinates.Add(groundNode.WorldCoordinates);
                    }
                }
            }
            if (side == Direction.E)
            {
                for (int y = 0; y < world.NumNodesPerSide; y++)
                {
                    for (int x = world.NumNodesPerSide - 1; x > world.NumNodesPerSide - mountainWidths[y] - 1; x--)
                    {
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        ApplyHeightmap(world, mountainHeightMap, groundNode);

                        affectedCoordinates.Add(groundNode.WorldCoordinates);
                    }
                }
            }
            if (side == Direction.W)
            {
                for (int y = 0; y < world.NumNodesPerSide; y++)
                {
                    for (int x = 0; x < mountainWidths[y]; x++)
                    {
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        ApplyHeightmap(world, mountainHeightMap, groundNode);

                        affectedCoordinates.Add(groundNode.WorldCoordinates);
                    }
                }
            }
        }

        public static void ScatterIndividualObjects(World world)
        {
            float CHANCE = 0.002f;
            foreach(GroundNode groundNode in world.GetAllGroundNodes())
            {
                if(Random.value < CHANCE)
                {
                    TrySpawnRandomEntityDefOnGround(world, groundNode.WorldCoordinates.x, groundNode.WorldCoordinates.y, ScatteredObjectsProbabilities, variantName: "Desert");
                }
            }
        }

        public static void AddOasis(World world, HashSet<Vector2Int> forbiddenCoordinates = null)
        {
            int MIN_GRASS_RADIUS = 2;
            int MAX_GRASS_RADIUS = 5;

            // Generate oasis mask noise (binary noise deciding which ground nodes are affected)
            var noise2 = new PerlinNoise(0.02f);
            var noise1_op_layer = new LayerOperation(2, 4f, 0.1f);
            var noise1 = new ModularGradientNoise(
                new GradientNoise[] { noise2 },
                noise1_op_layer
            );
            var noise0 = new ModularGradientNoise(
                new GradientNoise[] { noise1 },
                new CutoffOperation(0.71f, 0.71f)
            );
            // This is the final noise reference:
            GradientNoise oasisMaskNoise = noise0;

            HashSet<Vector2Int> oasisCoordinates = new HashSet<Vector2Int>();

            for (int x = 0; x < world.NumNodesPerSide; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide; y++)
                {
                    if (forbiddenCoordinates.Contains(new Vector2Int(x, y))) continue;

                    bool isOasis = oasisMaskNoise.GetValue(x, y) >= 1f;
                    if (isOasis)
                    {
                        // Lower ground
                        GroundNode groundNode = world.GetGroundNode(new Vector2Int(x, y));
                        groundNode.SetAltitude(1);
                        TerrainFunctions.SmoothOutside(groundNode, smoothStep: 5);
                        oasisCoordinates.Add(groundNode.WorldCoordinates);

                        // Check if we're on an edge
                        bool isOasisEdge = false;
                        foreach(Direction side in HelperFunctions.GetSides())
                        {
                            Vector2Int coords = HelperFunctions.GetCoordinatesInDirection(new Vector2Int(x, y), side);
                            if (oasisMaskNoise.GetValue(coords) < 1) isOasisEdge = true;
                        }

                        // Spawn plants if on an edge
                        if(isOasisEdge)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                EntityDef plant = OasisPlantProbabilities.GetWeightedRandomElement();
                                EntityManager.SpawnEntityAround(world, plant, world.Gaia, groundNode.WorldCoordinates, standard_deviation: 4f, randomMirror: true, variantName: "Desert");
                            }
                        }

                        // Set surface to grass around edge
                        int grassRadius = Random.Range(MIN_GRASS_RADIUS, MAX_GRASS_RADIUS + 1);
                        if (isOasisEdge)
                        {
                            for(int dx = -grassRadius; dx < grassRadius + 1; dx++ )
                            {
                                for (int dy = -grassRadius; dy < grassRadius + 1; dy++)
                                {
                                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > grassRadius) continue;
                                    world.SetSurface(world.GetGroundNode(x + dx, y + dy), SurfaceDefOf.Grass, updateWorld: false);
                                }
                            }
                        }
                    }
                }
            }

            List<HashSet<Vector2Int>> oasisClusters = HelperFunctions.GetConnectedClusters(oasisCoordinates);

            foreach(HashSet<Vector2Int> oasis in oasisClusters)
            {
                // Calculate shore height
                int minShoreAltitude = int.MaxValue;
                foreach(Vector2Int coordinate in oasis)
                {
                    foreach (Direction side in HelperFunctions.GetSides())
                    {
                        Vector2Int adjCoords = HelperFunctions.GetCoordinatesInDirection(coordinate, side);
                        if (oasisMaskNoise.GetValue(adjCoords) < 1)
                        {
                            GroundNode shoreNode = world.GetGroundNode(adjCoords);
                            if (shoreNode != null && shoreNode.MaxAltitude > 1 && shoreNode.MaxAltitude < minShoreAltitude) minShoreAltitude = shoreNode.MaxAltitude;
                        }
                    }
                }

                // Place water body
                world.AddWaterBody(world.GetGroundNode(oasis.First()), minShoreAltitude, updateWorld: false);
            }
        }
    }
}
