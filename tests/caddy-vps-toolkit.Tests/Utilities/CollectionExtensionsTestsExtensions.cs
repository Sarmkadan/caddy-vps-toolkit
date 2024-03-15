#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Utilities
{
    public static class CollectionExtensionsTestsExtensions
    {
        /// <summary>
        /// Returns the first element of a collection or a default value if the collection is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The collection to get the first element from.</param>
        /// <param name="defaultValue">The default value to return if the collection is empty.</param>
        /// <returns>The first element or the default value if empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static T FirstOrDefault<T>(this IEnumerable<T> source, T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(source);

            using var enumerator = source.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : defaultValue;
        }

        /// <summary>
        /// Returns the last element of a collection or a default value if the collection is empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The collection to get the last element from.</param>
        /// <param name="defaultValue">The default value to return if the collection is empty.</param>
        /// <returns>The last element or the default value if empty.</returns>
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
        /// Returns the element at the specified index or a default value if the index is out of range.
        /// Similar to SafeGet but returns the provided default value instead of the type's default.
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

        /// <summary>
        /// Returns a new collection containing only the distinct elements from the source.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <returns>A new collection with distinct elements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return System.Linq.Enumerable.Distinct(source);
        }

        /// <summary>
        /// Returns a new collection with elements that satisfy the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="predicate">The predicate to filter elements.</param>
        /// <returns>A new collection with elements that match the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return System.Linq.Enumerable.Where(source, predicate);
        }

        /// <summary>
        /// Returns a new collection with elements transformed by the specified selector.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
        /// <typeparam name="TResult">The type of elements in the result collection.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="selector">The selector function to transform elements.</param>
        /// <returns>A new collection with transformed elements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is null.</exception>
        public static IEnumerable<TResult> Select<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);

            return System.Linq.Enumerable.Select(source, selector);
        }
    }
}