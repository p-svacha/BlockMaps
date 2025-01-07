using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// Default implementation of the Unity Perlin Noise.
    /// </summary>
    public class PerlinNoise : GradientNoise
    {
        public override string Name => "Perlin";

        // Random Values
        private float XShift;
        private float YShift;

        public PerlinNoise(float scale) : base(scale) { }
        public PerlinNoise(int seed, float scale) : base(seed, scale) { }

        protected override void OnNewSeed()
        {
            XShift = GetRandomFloat(-10000f, 10000f);
            YShift = GetRandomFloat(-10000f, 10000f);
        }

        public override float GetValue(float x, float y)
        {
            return Mathf.PerlinNoise((XShift + x) * Scale, (YShift + y) * Scale);
        }

        public override GradientNoise GetCopy()
        {
            return new PerlinNoise(Seed, Scale);
        }
    }
}
