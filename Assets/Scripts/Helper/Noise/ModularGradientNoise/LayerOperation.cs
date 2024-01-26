using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerOperation : NoiseOperation
{
    private int NumOctaves;
    private float Lacunarity;
    private float Persistence;

    public LayerOperation(int numOctaves, float lacunarity, float persistence)
    {
        NumOctaves = numOctaves;
        Lacunarity = lacunarity;
        Persistence = persistence;
    }

    public override float DoOperation(GradientNoise[] inputs, float x, float y)
    {
        float value = 0f;
        float factor = 0f; // How much higher the new highest values are

        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < NumOctaves; i++)
        {
            float layerValue = inputs[0].GetValue(x * frequency, y * frequency);
            value += amplitude * layerValue;
            factor += amplitude;

            amplitude *= Persistence;
            frequency *= Lacunarity;
        }

        value /= factor;

        return value;
    }
}
