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
                AddForests,
                AddPaths
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

        private const float MIN_PATH_SEGMENT_LENGTH = 3f;
        private const float MAX_PATH_SEGMENT_LENGTH = 10f;
        private const float MAX_PATH_ANGLE_CHANGE = 30f;
        private const int MIN_PATH_THICKNESS = 1;
        private const int MAX_PATH_THICKNESS = 3;
        /// <summary>
        /// Adds some dirt paths to the map with a random walker algorithm.
        /// </summary>
        private void AddPaths()
        {
            int numStartPoints = NumChunksPerSide / 2;

            for(int i = 0; i < numStartPoints; i++)
            {
                Vector2 startPosition = new Vector2(Random.Range(0f, WorldSize), Random.Range(0f, WorldSize));
                float startAngle = Random.Range(0, 360);

                // Create a path going to both directions
                int thickness = Random.Range(MIN_PATH_THICKNESS, MAX_PATH_THICKNESS + 1);
                List<GroundNode> path = DrawPath(startPosition, startAngle, thickness);
                DrawPath(startPosition, startAngle + 180, thickness, existingPath: path);
            }
        }
        /// <summary>
        /// Draws a path until reaching another path or the map edge
        /// </summary>
        private List<GroundNode> DrawPath(Vector2 startPosition, float startAngle, int thickness, List<GroundNode> existingPath = null)
        {
            Vector2 currentPosition = startPosition;
            float currentAngle = startAngle;

            List<GroundNode> allPathNodes = new List<GroundNode>();
            if (existingPath != null) allPathNodes.AddRange(existingPath);

            bool continuePath = true;
            while (continuePath)
            {
                Vector2 segmentStartPosition = currentPosition;

                float angleChange = Random.Range(-MAX_PATH_ANGLE_CHANGE, MAX_PATH_ANGLE_CHANGE);
                currentAngle += angleChange;

                float segmentLength = Random.Range(MIN_PATH_SEGMENT_LENGTH, MAX_PATH_SEGMENT_LENGTH);

                Vector2 segmentEndPosition = segmentStartPosition + new Vector2(
                    Mathf.Sin(Mathf.Deg2Rad * currentAngle) * segmentLength,
                    Mathf.Cos(Mathf.Deg2Rad * currentAngle) * segmentLength);

                List<Vector2Int> pathCoordinates = HelperFunctions.RasterizeLine(segmentStartPosition, segmentEndPosition, thickness);

                foreach (Vector2Int pc in pathCoordinates)
                {
                    GroundNode node = World.GetGroundNode(pc);
                    if (node == null) continue;
                    if (!allPathNodes.Contains(node))
                    {
                        if (node.Surface.Id == SurfaceId.DirtPath)
                        {
                            continuePath = false; // reached other path
                            break;
                        }
                        World.SetSurface(node, SurfaceId.DirtPath, updateWorld: false);
                        allPathNodes.Add(node);

                        // Remove entity on path
                        World.RemoveEntities(node, updateWorld: false);
                    }

                }

                currentPosition = segmentEndPosition;

                if (currentPosition.x < 0 || currentPosition.x > WorldSize || currentPosition.y < 0 || currentPosition.y > WorldSize) continuePath = false; // Reached world edge
            }

            return allPathNodes;
        }
    }
}
