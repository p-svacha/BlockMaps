using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class ScaleOperation : NoiseOperation
    {
        public float Scale;

        public ScaleOperation(float scale)
        {
            Scale = scale;
        }

        public override int NumInputs => 1;
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            return inputs[0].GetValue(x * Scale, y * Scale);
        }
    }
}
