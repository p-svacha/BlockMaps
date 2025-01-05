using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class LayerOperation : NoiseOperation
    {
        public int NumOctaves;
        public float Lacunarity;
        public float Persistence;
        public Vector2Int[] Offsets;

        public LayerOperation(int numOctaves, float lacunarity, float persistence)
        {
            NumOctaves = numOctaves;
            Lacunarity = lacunarity;
            Persistence = persistence;
            Offsets = new Vector2Int[numOctaves];
            for (int i = 0; i < numOctaves; i++) Offsets[i] = new Vector2Int(i * -358502, i * -997792);
            Offsets[0] = Vector2Int.zero;
        }

        public override int NumInputs => 1;
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            float value = 0f;
            float factor = 0f; // How much higher the new highest values are

            float amplitude = 1f;
            float frequency = 1f;

            for (int i = 0; i < NumOctaves; i++)
            {
                float layerValue = inputs[0].GetValue(Offsets[i].x + x * frequency, Offsets[i].y + y * frequency);
                value += amplitude * layerValue;
                factor += amplitude;

                amplitude *= Persistence;
                frequency *= Lacunarity;
            }

            value /= factor;

            return value;
        }
    }
}
