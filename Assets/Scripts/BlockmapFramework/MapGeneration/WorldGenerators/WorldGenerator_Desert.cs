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

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>()
            {
                ApplyBaseHeightmap,
                SetBaseSurfaces,
                AddDunes,
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
            AddDunes(World);
        }



        public static void ApplyBaseHeightmap(World world)
        {
            float BASE_HEIGHT = 5f;

            var basePerlin = new PerlinNoise(0.008f);
            var layeredPerlin = new ModularGradientNoise(new GradientNoise[] { basePerlin }, new LayerOperation(5, 2f, 0.5f));

            int[,] heightMap = new int[world.NumNodesPerSide + 1, world.NumNodesPerSide + 1];
            for (int x = 0; x < world.NumNodesPerSide + 1; x++)
            {
                for (int y = 0; y < world.NumNodesPerSide + 1; y++)
                {
                    float value = layeredPerlin.GetValue(x, y);
                    value *= BASE_HEIGHT;
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
        public static void AddDunes(World world)
        {
            float DUNE_HEIGHT = 20f;

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
                }
            }
        }
    }
}