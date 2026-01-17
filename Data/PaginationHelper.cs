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
        /// Paginate a collection
        /// </summary>
        public static PaginatedResult<T> Paginate<T>(
            IEnumerable<T> items,
            int page = 1,
            int pageSize = 10)
        {
            if (items is null)
                items = new List<T>();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var list = items.ToList();
            var totalCount = list.Count;
            var skipCount = (page - 1) * pageSize;

            var paginatedItems = list
                .Skip(skipCount)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<T>
            {
                Items = paginatedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Sort collection by property name
        /// </summary>
        public static List<T> SortBy<T>(
            IEnumerable<T> items,
            string propertyName,
            bool ascending = true)
        {
            if (items is null)
                return new List<T>();

            var list = items.ToList();
            if (string.IsNullOrEmpty(propertyName))
                return list;

            var property = typeof(T).GetProperty(propertyName);
            if (property is null)
                return list;

            return ascending
                ? list.OrderBy(x => property.GetValue(x)).ToList()
                : list.OrderByDescending(x => property.GetValue(x)).ToList();
        }

        /// <summary>
        /// Filter collection by property value
        /// </summary>
        public static List<T> FilterBy<T>(
            IEnumerable<T> items,
            string propertyName,
            object value)
        {
            if (items is null)
                return new List<T>();

            if (string.IsNullOrEmpty(propertyName) || value is null)
                return items.ToList();

            var property = typeof(T).GetProperty(propertyName);
            if (property is null)
                return items.ToList();

            return items
                .Where(x => property.GetValue(x)?.Equals(value) ?? false)
                .ToList();
        }

        /// <summary>
        /// Filter collection with predicate
        /// </summary>
        public static List<T> Filter<T>(
            IEnumerable<T> items,
            Func<T, bool> predicate)
        {
            return items?
                .Where(predicate)
                .ToList() ?? new List<T>();
        }
    }

    /// <summary>
    /// Query builder for fluent data querying
    /// </summary>
    public sealed class QueryBuilder<T>
    {
        private IEnumerable<T> _data;
        private int _page = 1;
        private int _pageSize = 10;
        private string _sortBy;
        private bool _ascending = true;
        private List<Func<T, bool>> _filters = new();

        public QueryBuilder(IEnumerable<T> data)
        {
            _data = data ?? new List<T>();
        }

        public QueryBuilder<T> Page(int page)
        {
            _page = page;
            return this;
        }

        public QueryBuilder<T> PageSize(int size)
        {
            _pageSize = size;
            return this;
        }

        public QueryBuilder<T> SortBy(string property, bool ascending = true)
        {
            _sortBy = property;
            _ascending = ascending;
            return this;
        }

        public QueryBuilder<T> Where(Func<T, bool> predicate)
        {
            _filters.Add(predicate);
            return this;
        }

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
