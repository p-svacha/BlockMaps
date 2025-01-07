using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class WhiteNoise : GradientNoise
    {
        public override string Name => "White";

        public WhiteNoise(float scale) : base(scale) { }
        public WhiteNoise(int seed, float scale) : base(seed, scale) { }


        public override float GetValue(float x, float y)
        {
            return GetRandomFloat(0f, 1f);
        }

        public override GradientNoise GetCopy()
        {
            return new WhiteNoise(Seed, Scale);
        }
    }
}
