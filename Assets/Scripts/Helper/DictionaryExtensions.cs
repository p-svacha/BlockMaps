using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DictionaryExtensions
{
    public static TKey GetWeightedRandomElement<TKey>(this Dictionary<TKey, int> weightDictionary)
    {
        int probabilitySum = weightDictionary.Sum(x => x.Value);
        int rng = Random.Range(0, probabilitySum);
        int tmpSum = 0;
        foreach (var kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum)
                return kvp.Key;
        }
        throw new System.Exception("No element selected. Check the dictionary for valid weights.");
    }

    public static TKey GetWeightedRandomElement<TKey>(this Dictionary<TKey, float> weightDictionary)
    {
        float probabilitySum = weightDictionary.Sum(x => x.Value);
        float rng = Random.Range(0, probabilitySum);
        float tmpSum = 0;
        foreach (var kvp in weightDictionary)
        {
            tmpSum += kvp.Value;
            if (rng < tmpSum)
                return kvp.Key;
        }
        throw new System.Exception("No element selected. Check the dictionary for valid weights.");
    }
}