using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default implementation of the Unity Perlin Noise.
/// </summary>
public class PerlinNoise : GradientNoise
{
    public override string Name => "Perlin Noise";

    // Parameters
    public float Scale;

    // Random Values
    private float XShift;
    private float YShift;

    public PerlinNoise() : base() { }
    public PerlinNoise(int seed) : base(seed) { }

    protected override void OnNewSeed()
    {
        Scale = 0.05f;
        XShift = Random.Range(-10000f, 10000f);
        YShift = Random.Range(-10000f, 10000f);
    }

    public override float GetValue(float x, float y)
    {
        return Mathf.PerlinNoise((XShift + x) * Scale, (YShift + y) * Scale);
    }

    public override GradientNoise GetCopy()
    {
        return new PerlinNoise(Seed);
    }
}
