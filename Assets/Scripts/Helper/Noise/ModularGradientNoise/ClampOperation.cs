using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class ClampOperation : NoiseOperation
    {
        public float Min;
        public float Max;

        public ClampOperation(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public override int NumInputs => 1;
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            float value = inputs[0].GetValue(x, y);
            if (value < Min) return Min;
            if (value > Max) return Max;
            return value;
        }
    }
}