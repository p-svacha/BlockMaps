using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    /// <summary>
    /// Returns a random element from the list using UnityEngine.Random.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to select a random element from.</param>
    /// <returns>A random element from the list.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the list is null.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown if the list is empty.</exception>
    public static T RandomElement<T>(this List<T> list)
    {
        if (list == null) throw new System.ArgumentNullException(nameof(list), "The list cannot be null.");
        if (list.Count == 0) throw new System.InvalidOperationException("Cannot select a random element from an empty list.");

        int index = Random.Range(0, list.Count);
        return list[index];
    }
}