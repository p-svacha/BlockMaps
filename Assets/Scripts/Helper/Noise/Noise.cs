using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for noise containing all relevant logic regarding seed.
/// <br/> All noise types must inherit this class.
/// </summary>
public abstract class Noise
{
    public int Seed { get; private set; }
    public abstract string Name { get; }

    public Noise()
    {
        RandomizeSeed();
    }
    public Noise(int seed)
    {
        SetSeed(seed);
    }

    public void SetSeed(int newSeed)
    {
        Seed = newSeed;
        Random.InitState(Seed);
        OnNewSeed();
    }

    /// <summary>
    /// Gets called when the noise receives a new seed to set new random values.
    /// <br/> Unitys Random has already been reset with the new seed when this gets called.
    /// </summary>
    protected virtual void OnNewSeed() { }

    public void RandomizeSeed()
    {
        int randomSeed = Random.Range(int.MinValue, int.MaxValue);
        SetSeed(randomSeed);
    }

    public abstract Sprite CreateTestSprite(int size = 128);
}
