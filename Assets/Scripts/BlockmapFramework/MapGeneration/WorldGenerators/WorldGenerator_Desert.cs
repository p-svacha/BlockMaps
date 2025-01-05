using System.Collections;
using System.Collections.Generic;
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
                GenerateHeightmapNoise,
                ApplyHeightmap,
                SetSurface,
            };
        }

        // Cache
        private int[,] HeightMap;

        #region HeightMap

        private void GenerateHeightmapNoise()
        {
            var noise1 = new PerlinNoise()
            {
                Scale = 0.05f
            };
            var noise0_op_layer = new LayerOperation(5, 1f, 1f);
            var noise0 = new ModularGradientNoise(
                new GradientNoise[] { noise1 },
                noise0_op_layer
            );
            // This is the final noise reference:
            GradientNoise finalNoise = noise0;

            float heightModifier = Random.Range(3f, 6f);
            HeightMap = new int[World.NumNodesPerSide + 1, World.NumNodesPerSide + 1];
            for (int x = 0; x < World.NumNodesPerSide + 1; x++)
            {
                for (int y = 0; y < World.NumNodesPerSide + 1; y++)
                {
                    float value = finalNoise.GetValue(x, y);
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
                n.SetAltitude(nodeHeights);
            }
        }

        #endregion

        #region Surface

        private void SetSurface()
        {
            foreach(GroundNode node in World.GetAllGroundNodes())
            {
                node.SetSurface(SurfaceDefOf.Sand);
            }
        }

        #endregion
    }
}
