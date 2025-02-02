using System;
using System.Collections.Generic;

/// <summary>
/// A simple generic min-heap based priority queue for (T, float) pairs.
/// </summary>
public class PriorityQueue<T>
{
    private List<(T Item, float Priority)> _elements = new List<(T, float)>();

    public int Count => _elements.Count;

    /// <summary>
    /// Adds an item with the given priority to the queue.
    /// </summary>
    public void Enqueue(T item, float priority)
    {
        _elements.Add((item, priority));
        // "Bubble up" the newly added element to maintain the heap property
        BubbleUp(_elements.Count - 1);
    }

    /// <summary>
    /// Removes and returns the item with the smallest priority.
    /// </summary>
    public T Dequeue()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty.");

        // The root of the heap (index 0) is the item with smallest priority
        T result = _elements[0].Item;

        // Move the last item to the root
        _elements[0] = _elements[_elements.Count - 1];
        _elements.RemoveAt(_elements.Count - 1);

        // "Bubble down" the new root to restore the heap property
        if (_elements.Count > 0)
            BubbleDown(0);

        return result;
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_elements[index].Priority >= _elements[parentIndex].Priority)
                break;

            // Swap child and parent
            var temp = _elements[index];
            _elements[index] = _elements[parentIndex];
            _elements[parentIndex] = temp;

            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        int lastIndex = _elements.Count - 1;

        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallestIndex = index;

            // Check left child
            if (leftChildIndex <= lastIndex &&
                _elements[leftChildIndex].Priority < _elements[smallestIndex].Priority)
            {
                smallestIndex = leftChildIndex;
            }

            // Check right child
            if (rightChildIndex <= lastIndex &&
                _elements[rightChildIndex].Priority < _elements[smallestIndex].Priority)
            {
                smallestIndex = rightChildIndex;
            }

            // If no change, we are done
            if (smallestIndex == index)
                break;

            // Otherwise, swap and continue
            var temp = _elements[index];
            _elements[index] = _elements[smallestIndex];
            _elements[smallestIndex] = temp;

            index = smallestIndex;
        }
    }
}