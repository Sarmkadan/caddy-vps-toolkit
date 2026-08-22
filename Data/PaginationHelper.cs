#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// Helper for paginating collections.
    /// Supports sorting and filtering on collections.
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Paginate a collection.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="items">The collection to paginate.</param>
        /// <param name="page">The page number (1-based). Defaults to 1.</param>
        /// <param name="pageSize">The number of items per page. Defaults to 10.</param>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing the paginated items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        public static PaginatedResult<T> Paginate<T>(
            IEnumerable<T> items,
            int page = 1,
            int pageSize = 10)
        {
            ArgumentNullException.ThrowIfNull(items);

            // Normalise paging parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            // Calculate how many items to skip before we start collecting the page
            int skip = (page - 1) * pageSize;

            var pageItems = new List<T>(pageSize);
            int totalCount = 0;
            int index = 0;

            // Single-pass enumeration: count total items and collect only the items that belong to the requested page
            foreach (var item in items)
            {
                totalCount++;

                if (index >= skip && pageItems.Count < pageSize)
                {
                    pageItems.Add(item);
                }

                index++;
            }

            return new PaginatedResult<T>
            {
                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Sort collection by property name.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="items">The collection to sort.</param>
        /// <param name="propertyName">The name of the property to sort by.</param>
        /// <param name="ascending">Whether to sort in ascending order. Defaults to true.</param>
        /// <returns>A <see cref="List{T}"/> containing the sorted items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
        public static List<T> SortBy<T>(
            IEnumerable<T> items,
            string propertyName,
            bool ascending = true)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            var list = items.ToList();

            var property = typeof(T).GetProperty(propertyName);
            if (property is null)
                return list;

            return ascending
                ? list.OrderBy(x => property.GetValue(x)).ToList()
                : list.OrderByDescending(x => property.GetValue(x)).ToList();
        }

        /// <summary>
        /// Filter collection by property value.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="items">The collection to filter.</param>
        /// <param name="propertyName">The name of the property to filter by.</param>
        /// <param name="value">The value to filter for.</param>
        /// <returns>A <see cref="List{T}"/> containing the filtered items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/>, <paramref name="propertyName"/>, or <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
        public static List<T> FilterBy<T>(
            IEnumerable<T> items,
            string propertyName,
            object value)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            ArgumentNullException.ThrowIfNull(value);

            var property = typeof(T).GetProperty(propertyName);
            if (property is null)
                return items.ToList();

            return items
                .Where(x => property.GetValue(x)?.Equals(value) ?? false)
                .ToList();
        }

        /// <summary>
        /// Filter collection with predicate.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="items">The collection to filter.</param>
        /// <param name="predicate">The predicate to test each item against.</param>
        /// <returns>A <see cref="List{T}"/> containing the filtered items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> or <paramref name="predicate"/> is null.</exception>
        public static List<T> Filter<T>(
            IEnumerable<T> items,
            Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(predicate);

            return items
                .Where(predicate)
                .ToList();
        }
    }

    /// <summary>
    /// Query builder for fluent data querying.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public sealed class QueryBuilder<T>
    {
        private IEnumerable<T> _data;
        private int _page = 1;
        private int _pageSize = 10;
        private string _sortBy;
        private bool _ascending = true;
        private List<Func<T, bool>> _filters = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder{T}"/> class.
        /// </summary>
        /// <param name="data">The data to query.</param>
        public QueryBuilder(IEnumerable<T> data)
        {
            _data = data ?? new List<T>();
        }

        /// <summary>
        /// Sets the page number.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <returns>The same <see cref="QueryBuilder{T}"/> instance for chaining.</returns>
        public QueryBuilder<T> Page(int page)
        {
            _page = page;
            return this;
        }

        /// <summary>
        /// Sets the page size.
        /// </summary>
        /// <param name="size">The number of items per page.</param>
        /// <returns>The same <see cref="QueryBuilder{T}"/> instance for chaining.</returns>
        public QueryBuilder<T> PageSize(int size)
        {
            _pageSize = size;
            return this;
        }

        /// <summary>
        /// Sets the sorting property and order.
        /// </summary>
        /// <param name="property">The name of the property to sort by.</param>
        /// <param name="ascending">Whether to sort in ascending order. Defaults to true.</param>
        /// <returns>The same <see cref="QueryBuilder{T}"/> instance for chaining.</returns>
        public QueryBuilder<T> SortBy(string property, bool ascending = true)
        {
            _sortBy = property;
            _ascending = ascending;
            return this;
        }

        /// <summary>
        /// Adds a filter predicate.
        /// </summary>
        /// <param name="predicate">The predicate to test each item against.</param>
        /// <returns>The same <see cref="QueryBuilder{T}"/> instance for chaining.</returns>
        public QueryBuilder<T> Where(Func<T, bool> predicate)
        {
            _filters.Add(predicate);
            return this;
        }

        /// <summary>
        /// Executes the query and returns a paginated result.
        /// </summary>
        /// <returns>A <see cref="PaginatedResult{T}"/> containing the paginated items.</returns>
        public PaginatedResult<T> Execute()
        {
            var result = _data;

            // Apply filters
            foreach (var filter in _filters)
                result = result.Where(filter);

            // Apply sorting
            if (!string.IsNullOrEmpty(_sortBy))
                result = PaginationHelper.SortBy(result, _sortBy, _ascending);

            // Apply pagination
            return PaginationHelper.Paginate(result, _page, _pageSize);
        }

        /// <summary>
        /// Executes the query and returns all items as a list.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> containing all items that match the query.</returns>
        public List<T> ExecuteUnpaged()
        {
            var result = _data;

            // Apply filters
            foreach (var filter in _filters)
                result = result.Where(filter);

            // Apply sorting
            if (!string.IsNullOrEmpty(_sortBy))
                result = PaginationHelper.SortBy(result, _sortBy, _ascending);

            return result.ToList();
        }
    }
}