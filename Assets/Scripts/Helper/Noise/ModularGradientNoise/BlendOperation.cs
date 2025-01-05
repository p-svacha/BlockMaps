using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class BlendOperation : NoiseOperation
    {
        public float BlendRatio;

        public BlendOperation(float blendRatio)
        {
            BlendRatio = blendRatio;
        }

        public override int NumInputs => 2;
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            float value1 = inputs[0].GetValue(x, y);
            float value2 = inputs[1].GetValue(x, y);
            return (BlendRatio * value1) + ((1f - BlendRatio) * value2);
        }
    }
}
