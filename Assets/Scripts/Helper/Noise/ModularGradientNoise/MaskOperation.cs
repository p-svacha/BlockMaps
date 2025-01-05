using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    public class MaskOperation : NoiseOperation
    {
        public override int NumInputs => 3;
        public override float DoOperation(GradientNoise[] inputs, float x, float y)
        {
            float value1 = inputs[0].GetValue(x, y);
            float value2 = inputs[1].GetValue(x, y);
            float mask = inputs[2].GetValue(x, y);

            return (value1 * mask) + (value2 * (1f - mask));
        }
    }
}
