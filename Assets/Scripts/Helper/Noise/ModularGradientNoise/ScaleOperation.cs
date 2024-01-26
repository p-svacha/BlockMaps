using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleOperation : NoiseOperation
{
    private float Scale;

    public ScaleOperation(float scale)
    {
        Scale = scale;
    }

    public override float DoOperation(GradientNoise[] inputs, float x, float y)
    {
        return inputs[0].GetValue(x * Scale, y * Scale);
    }
}
