using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public abstract class NoiseOperation
    {
        public abstract int NumInputs { get; }
        public abstract float DoOperation(GradientNoise[] inputs, float x, float y);
    }
}
