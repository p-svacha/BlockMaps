using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateNoiseLibrary
{
    /// <summary>
    /// Base class for noise containing all relevant logic regarding seed.
    /// <br/> All noise types must inherit this class.
    /// </summary>
    public abstract class Noise
    {
        public int Seed { get; private set; }
        public abstract string Name { get; }
        public float Scale { get; protected set; }

        public Noise()
        {
            Scale = 1f;
            RandomizeSeed();
        }
        public Noise(float scale)
        {
            Scale = scale;
            RandomizeSeed();
        }
        public Noise(int seed, float scale)
        {
            Scale = scale;
            SetSeed(seed);
        }

        public void SetSeed(int newSeed)
        {
            Seed = newSeed;
            Random.InitState(Seed);
            OnNewSeed();
        }

        public void SetScale(float newScale)
        {
            Scale = newScale;
        }

        /// <summary>
        /// Gets called when the noise receives a new seed to set new random values.
        /// <br/> RNG has already been set with the new seed when this gets called.
        /// </summary>
        protected virtual void OnNewSeed() { }

        public void RandomizeSeed()
        {
            int randomSeed = Random.Range(int.MinValue / 2, int.MaxValue / 2);
            SetSeed(randomSeed);
        }

        protected float GetRandomFloat(float min, float max)
        {
            return Random.Range(min, max);
        }

        public abstract Sprite CreateTestSprite(int size = 128);
    }
}
