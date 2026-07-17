#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Extension methods for test scenarios that provide custom default values for collection operations.
    /// These methods complement the standard LINQ methods by allowing callers to specify custom default values
    /// instead of relying on the type's default value.
    /// </summary>
    public static class CollectionExtensionsTestsExtensions
    {
        /// <summary>
        /// Returns the first element of a sequence or a custom default value if the sequence is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="source">The sequence to get the first element from.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The first element or the custom default value if the sequence is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static T FirstOrDefault<T>(this IEnumerable<T> source, T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(source);

            using var enumerator = source.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : defaultValue;
        }

        /// <summary>
        /// Returns the last element of a sequence or a custom default value if the sequence is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="source">The sequence to get the last element from.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <returns>The last element or the custom default value if the sequence is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static T LastOrDefault<T>(this IEnumerable<T> source, T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source is IList<T> list)
            {
                return list.Count > 0 ? list[^1] : defaultValue;
            }

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return defaultValue;
            }

            T last = enumerator.Current;
            while (enumerator.MoveNext())
            {
                last = enumerator.Current;
            }

            return last;
        }

        /// <summary>
        /// Returns the element at the specified index or a custom default value if the index is out of range.
        /// This is similar to <see cref="CollectionExtensions.SafeGet{T}(IList{T},int,T)"/> but uses a different method signature
        /// for test scenarios where a custom default value is preferred.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to access.</param>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="defaultValue">The value to return if the index is out of range.</param>
        /// <returns>The element at the specified index or the default value if out of range.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
        public static T ElementAtOrDefault<T>(this IReadOnlyList<T> list, int index, T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(list);

            return index >= 0 && index < list.Count ? list[index] : defaultValue;
        }
    }
}