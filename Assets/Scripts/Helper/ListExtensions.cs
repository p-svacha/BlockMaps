using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    /// <summary>
    /// Returns a random element from the list using UnityEngine.Random. Optionally removes the selected element from the list.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to select a random element from.</param>
    /// <param name="removeElement">If true, the selected element is removed from the list.</param>
    /// <returns>A random element from the list.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the list is null.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown if the list is empty.</exception>
    public static T RandomElement<T>(this List<T> list, bool removeElement = false)
    {
        if (list == null)
            throw new System.ArgumentNullException(nameof(list), "The list cannot be null.");

        if (list.Count == 0)
            throw new System.InvalidOperationException("Cannot select a random element from an empty list.");

        int index = Random.Range(0, list.Count);
        T element = list[index];

        if (removeElement)
        {
            list.RemoveAt(index);
        }

        return element;
    }

    /// <summary>
    /// Returns a new list with all elements shuffled randomly without modifying the original list.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    /// <returns>A new list with all elements shuffled randomly.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the list is null.</exception>
    public static List<T> GetShuffledList<T>(this List<T> list)
    {
        if (list == null)
            throw new System.ArgumentNullException(nameof(list), "The list cannot be null.");

        List<T> shuffledList = new List<T>(list);
        for (int i = 0; i < shuffledList.Count; i++)
        {
            int randomIndex = Random.Range(0, shuffledList.Count);
            T temp = shuffledList[i];
            shuffledList[i] = shuffledList[randomIndex];
            shuffledList[randomIndex] = temp;
        }

        return shuffledList;
    }
}