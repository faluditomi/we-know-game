using System.Collections.Generic;
using UnityEngine;

public static class CollectionExtensions
{
    public static Stack<T> ToShuffledStack<T>(this IEnumerable<T> source)
    {
        List<T> shuffledItems = new List<T>(source);
        for (int i = shuffledItems.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temporary = shuffledItems[i];
            shuffledItems[i] = shuffledItems[randomIndex];
            shuffledItems[randomIndex] = temporary;
        }
        return new Stack<T>(shuffledItems);
    }
}