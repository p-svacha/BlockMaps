using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFMapGenerator_Forest : CTFMapGenerator
    {
        public override string Name => "CTF - Forest";

        private int[,] HeightMap;

        protected override void OnGenerationStart()
        {
            base.OnGenerationStart();

            GenerationSteps = new List<System.Action>()
            {
                GenerateNoise,
                ApplyHeightmap,
                CreateMapZones,
                CreatePlayerBases,
                AddWaterBodies,
                AddBridges,
                AddForests
            };
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
                while (!isDone)
                {
                    // End at world edge
                    if (nextNode == null)
                    {
                        isDone = true;
                        isValid = false;
                    }

                    // End
                    else if (nextNode.IsFlat(dir2) && nextNode.GetMaxAltitude(dir2) == bridgeHeight)
                    {
                        isDone = true;
                        isValid = true;
                    }

                    // Build and end
                    else if (nextNode.IsFlat(dir1) && nextNode.GetMaxAltitude(dir1) == bridgeHeight)
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
                if (bridgeCoordinates.Any(x => !World.CanBuildAirNode(x, bridgeHeight))) continue; // Don't build if any bridge node can't be built
                foreach (Vector2Int coords in bridgeCoordinates)
                {
                    World.BuildAirPath(coords, bridgeHeight, SurfaceId.Concrete);
                }
                numBridges++;
            }

            // End
            //Debug.Log("Generated " + numBridges + " bridges after " + attempts + " attempts.");

            World.DrawNodes();
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
        }
        private void SpawnRandomTree(int x, int y)
        {
            BlockmapNode targetNode = World.GetGroundNode(new Vector2Int(x, y));
            Direction rotation = HelperFunctions.GetRandomSideDirection();

            Entity prefab = GetEntityPrefab(GetRandomTreeId());

            if (World.CanSpawnEntity(prefab, targetNode, rotation, forceHeadspaceRecalc: true))
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
    }
}
