using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class WhiteNoise : GradientNoise
    {
        public override string Name => "White";

        public WhiteNoise() : base() { }
        public WhiteNoise(int seed) : base(seed) { }


        public override float GetValue(float x, float y)
        {
            return GetRandomFloat(0f, 1f);
        }

        public override GradientNoise GetCopy()
        {
            return new WhiteNoise(Seed);
        }
    }
}
