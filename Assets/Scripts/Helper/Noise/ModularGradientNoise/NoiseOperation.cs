using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoiseOperation
{
    public abstract float DoOperation(GradientNoise[] inputs, float x, float y);
}
