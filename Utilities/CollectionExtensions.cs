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
        /// Safely get item at index or return default
        /// </summary>
        public static T SafeGet<T>(this IList<T> list, int index, T defaultValue = default)
        {
            return (list is not null && index >= 0 && index < list.Count) ? list[index] : defaultValue;
        }

        /// <summary>
        /// Check if collection is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection is null || !collection.Any();
        }

        /// <summary>
        /// Get first or default with null safety
        /// </summary>
        public static T FirstOrDefault<T>(this IEnumerable<T> collection)
        {
            return collection is null ? default! : collection.FirstOrDefault()!;
        }

        /// <summary>
        /// Split collection into batches of specified size
        /// </summary>
        public static List<List<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));

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
        public static (List<T> matching, List<T> notMatching) Partition<T>(
            this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            var matching = new List<T>();
            var notMatching = new List<T>();

            foreach (var item in collection ?? Enumerable.Empty<T>())
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
        public static List<(K key, V value)> ToTupleList<K, V>(this Dictionary<K, V> dict)
        {
            return dict?.Select(kvp => (kvp.Key, kvp.Value)).ToList() ?? new();
        }

        /// <summary>
        /// Get intersection of multiple collections
        /// </summary>
        public static List<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> collections)
        {
            if (collections.IsNullOrEmpty())
                return new();

            var enumerator = collections.GetEnumerator();
            enumerator.MoveNext();
            var result = enumerator.Current.ToHashSet();

            while (enumerator.MoveNext())
                result.IntersectWith(enumerator.Current);

            return result.ToList();
        }

        /// <summary>
        /// Remove items matching predicate from collection
        /// </summary>
        public static void RemoveWhere<T>(this List<T> list, Func<T, bool> predicate)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                    list.RemoveAt(i);
            }
        }

        /// <summary>
        /// Add range of items to collection if they don't already exist
        /// </summary>
        public static void AddRangeIfNotExists<T>(this List<T> list, IEnumerable<T> items)
        {
            foreach (var item in items ?? Enumerable.Empty<T>())
            {
                if (!list.Contains(item))
                    list.Add(item);
            }
        }

        /// <summary>
        /// Shuffle collection using Fisher-Yates algorithm
        /// </summary>
        public static List<T> Shuffle<T>(this IEnumerable<T> collection)
        {
            var list = collection?.ToList() ?? new();
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
