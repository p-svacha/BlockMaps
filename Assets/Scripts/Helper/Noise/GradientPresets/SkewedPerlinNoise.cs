using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// A perlin-like noise that applies a directional bias (skew) to the coordinates,
    /// generating asymmetrical "dune-like" shapes.
    /// </summary>
    public class SkewedPerlinNoise : GradientNoise
    {
        public override string Name => "Skewed Perlin";

        // Parameters
        public float Slope;     // How strongly we skew one axis based on the other

        // Internal random shifts
        private float XShift;
        private float YShift;

        public SkewedPerlinNoise(float scale) : base(scale) { }
        public SkewedPerlinNoise(int seed, float scale) : base(seed, scale) { }

        /// <summary>
        /// Called automatically when a new Seed is set or when we randomize it.
        /// We choose random shifts for x/y, scale, and slope.
        /// </summary>
        protected override void OnNewSeed()
        {
            // Random shifts, so each seed yields a new "location" in Perlin space.
            XShift = GetRandomFloat(-10000f, 10000f);
            YShift = GetRandomFloat(-10000f, 10000f);

            // A slope factor that biases one axis by the other.
            // You can tweak the range: smaller => subtle slope, bigger => more extreme skew
            Slope = GetRandomFloat(0.01f, 0.1f);
        }
        public override float GetValue(float x, float y)
        {
            float xPrime = (XShift + x) * Scale + (Slope * (YShift + y));
            float yPrime = (YShift + y) * Scale;
            return Mathf.PerlinNoise(xPrime, yPrime);
        }

        public override GradientNoise GetCopy()
        {
            return new SkewedPerlinNoise(Seed, Scale);
        }
    }
}