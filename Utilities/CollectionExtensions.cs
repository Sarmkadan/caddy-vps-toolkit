#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension methods for collections (List, IEnumerable, etc.).
    /// Provides functional utilities like batching, partitioning, and safe access.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Safely get item at index or return default value
        /// </summary>
        /// <param name="list">The list to access</param>
        /// <param name="index">The zero-based index to retrieve</param>
        /// <param name="defaultValue">The default value to return if index is out of bounds</param>
        /// <returns>The item at the specified index, or defaultValue if the list is null or the index is invalid</returns>
        public static T SafeGet<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list is null)
                return defaultValue;

            return index >= 0 && index < list.Count ? list[index] : defaultValue;
        }

        /// <summary>
        /// Check if collection is null or empty
        /// </summary>
        /// <returns>True if collection is null or empty; otherwise false</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection is null || !collection.Any();
        }

        /// <summary>
        /// Get first or default with null safety
        /// </summary>
        /// <param name="collection">The collection to search</param>
        /// <returns>The first element or default value</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null</exception>
        public static T? FirstOrDefault<T>(this IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            return System.Linq.Enumerable.FirstOrDefault(collection);
        }

        /// <summary>
        /// Split collection into batches of specified size
        /// </summary>
        /// <param name="collection">The collection to batch</param>
        /// <param name="batchSize">The size of each batch (must be positive)</param>
        /// <returns>List of batches, each containing up to batchSize elements</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="batchSize"/> is not positive</exception>
        public static List<List<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

            var batches = new List<List<T>>();
            var batch = new List<T>();

            foreach (var item in collection)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    batches.Add(batch);
                    batch = new List<T>();
                }
            }

            if (batch.Count > 0)
                batches.Add(batch);

            return batches;
        }

        /// <summary>
        /// Partition collection into two groups based on predicate
        /// </summary>
        /// <param name="collection">The collection to partition</param>
        /// <param name="predicate">The function to test each element</param>
        /// <returns>A tuple containing matching and not matching elements; both lists are empty when the collection is null</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null</exception>
        public static (List<T> matching, List<T> notMatching) Partition<T>(
            this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            var matching = new List<T>();
            var notMatching = new List<T>();

            if (collection is null)
                return (matching, notMatching);

            foreach (var item in collection)
            {
                if (predicate(item))
                    matching.Add(item);
                else
                    notMatching.Add(item);
            }

            return (matching, notMatching);
        }

        /// <summary>
        /// Convert dictionary to list of KeyValuePair with order preservation
        /// </summary>
        /// <param name="dict">The dictionary to convert</param>
        /// <returns>List of key-value tuples in dictionary order; empty when the dictionary is null</returns>
        public static List<(K key, V value)> ToTupleList<K, V>(this Dictionary<K, V> dict)
        {
            if (dict is null)
                return new List<(K key, V value)>();

            return dict.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        }

        /// <summary>
        /// Get intersection of multiple collections
        /// </summary>
        /// <param name="collections">The collections to intersect</param>
        /// <returns>List containing elements present in all collections</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="collections"/> is null</exception>
        public static List<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> collections)
        {
            ArgumentNullException.ThrowIfNull(collections);

            if (collections.IsNullOrEmpty())
                return new();

            using var enumerator = collections.GetEnumerator();
            if (!enumerator.MoveNext())
                return new();

            var result = enumerator.Current.ToHashSet();

            while (enumerator.MoveNext())
                result.IntersectWith(enumerator.Current);

            return result.ToList();
        }

        /// <summary>
        /// Remove items matching predicate from collection
        /// </summary>
        /// <param name="list">The list to modify</param>
        /// <param name="predicate">The function to test each element</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> or <paramref name="predicate"/> is null</exception>
        public static void RemoveWhere<T>(this List<T> list, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(list);
            ArgumentNullException.ThrowIfNull(predicate);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                    list.RemoveAt(i);
            }
        }

        /// <summary>
        /// Add range of items to collection if they don't already exist
        /// </summary>
        /// <param name="list">The list to modify</param>
        /// <param name="items">The items to add; a null sequence leaves the list unchanged</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null</exception>
        public static void AddRangeIfNotExists<T>(this List<T> list, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (items is null)
                return;

            foreach (var item in items)
            {
                if (!list.Contains(item))
                    list.Add(item);
            }
        }

        /// <summary>
        /// Shuffle collection using Fisher-Yates algorithm
        /// </summary>
        /// <param name="collection">The collection to shuffle</param>
        /// <returns>New list with elements in random order</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null</exception>
        public static List<T> Shuffle<T>(this IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            var list = System.Linq.Enumerable.ToList(collection);
            var random = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }

            return list;
        }
    }
}