using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class PerlinWorldGenerator : WorldGenerator
    {
        public override string Name => "Simple Perlin";

        private int[,] HeightMap;
        private int GenerationStep;

        protected override void OnGenerationStart()
        {
            GenerationStep = 0;
        }
        protected override void OnUpdate()
        {
            if (GenerationStep == 0) // Step 1: Create heightmap data
            {
                // Create height map
                Vector2Int perlinOffset = new Vector2Int(Random.Range(0, 20000), Random.Range(0, 20000));
                float perlinScale = 0.1f;
                float perlinHeightScale = 5f;
                HeightMap = new int[WorldSize + 1, WorldSize + 1];
                for (int x = 0; x < WorldSize + 1; x++)
                {
                    for (int y = 0; y < WorldSize + 1; y++)
                    {
                        HeightMap[x, y] = (int)(perlinHeightScale * Mathf.PerlinNoise(perlinOffset.x + x * perlinScale, perlinOffset.y + y * perlinScale));
                    }
                }

                GenerationStep++;
            }

            else if (GenerationStep == 1) // Adjust height of all nodes
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

            else if (GenerationStep == 2) // Surface
            {
                foreach (GroundNode n in World.GetAllGroundNodes())
                {
                    if (n.WorldCoordinates.x * 10 * Random.value < 5f) n.SetSurface(SurfaceDefOf.Sand);
                }

                World.DrawNodes();
                GenerationStep++;
            }

            else if(GenerationStep == 3) // Done
            {
                FinishGeneration();
            }
        }
    }
}
