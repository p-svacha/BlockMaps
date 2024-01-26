using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutoffOperation : NoiseOperation
{
    private float Min;
    private float Max;

    public CutoffOperation(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public override float DoOperation(GradientNoise[] inputs, float x, float y)
    {
        float value = inputs[0].GetValue(x, y);
        if (value < Min) return 0f;
        else if (value > Max) return 1f;
        else
        {
            float scale = Max - Min;
            float baseValue = value - Min;
            return baseValue / scale;
        }
    }
}
