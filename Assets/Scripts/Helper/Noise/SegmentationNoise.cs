using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// Base class for all segmentation noise patterns, meaning patterns that return an integer index for each point in space.
    /// <br/> SegmentationNoise patterns are always confined within a finite and defined space.
    /// </summary>
    public abstract class SegmentationNoise : Noise
    {
        public abstract int GetValue(float x, float y);

        public override Sprite CreateTestSprite(int size = 128)
        {
            throw new System.Exception();
        }
    }
}
