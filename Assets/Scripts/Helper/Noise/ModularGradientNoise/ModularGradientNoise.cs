using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// A type of noise that is a series of operations
    /// </summary>
    public class ModularGradientNoise : GradientNoise
    {
        public override string Name => "Modular Gradient";

        public GradientNoise[] Inputs;
        public NoiseOperation Operation;

        public ModularGradientNoise(GradientNoise[] inputs, NoiseOperation operation)
        {
            Inputs = inputs;
            Operation = operation;
        }

        public override GradientNoise GetCopy()
        {
            return new ModularGradientNoise((GradientNoise[])Inputs.Clone(), Operation);
        }

        public override float GetValue(float x, float y)
        {
            return Operation.DoOperation(Inputs, x, y);
        }
    }
}
