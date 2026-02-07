#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Data;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Data
{
    file sealed class Item
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public sealed class PaginationHelperTests
    {
        // ── Paginate ─────────────────────────────────────────────────────────

        [Fact]
        public void Paginate_FirstPage_ReturnsCorrectSlice()
        {
            var items = Enumerable.Range(1, 25).ToList();

            var result = PaginationHelper.Paginate(items, page: 1, pageSize: 10);

            result.Items.Should().Equal(Enumerable.Range(1, 10));
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(25);
        }

        [Fact]
        public void Paginate_LastPage_ReturnsRemainingItems()
        {
            var items = Enumerable.Range(1, 25).ToList();

            var result = PaginationHelper.Paginate(items, page: 3, pageSize: 10);

            result.Items.Should().Equal(21, 22, 23, 24, 25);
        }

        [Fact]
        public void Paginate_PageBeyondTotal_ReturnsEmptyItems()
        {
            var items = Enumerable.Range(1, 5).ToList();

            var result = PaginationHelper.Paginate(items, page: 10, pageSize: 5);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(5);
        }

        [Fact]
        public void Paginate_NullCollection_TreatsAsEmpty()
        {
            var result = PaginationHelper.Paginate<int>(null!, page: 1, pageSize: 10);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public void Paginate_PageLessThanOne_ClampsToOne()
        {
            var items = Enumerable.Range(1, 5).ToList();

            var result = PaginationHelper.Paginate(items, page: -1, pageSize: 5);

            result.Page.Should().Be(1);
            result.Items.Should().Equal(1, 2, 3, 4, 5);
        }

        [Fact]
        public void Paginate_PageSizeLessThanOne_ClampsToTen()
        {
            var items = Enumerable.Range(1, 20).ToList();

            var result = PaginationHelper.Paginate(items, page: 1, pageSize: 0);

            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(10);
        }

        // ── SortBy ───────────────────────────────────────────────────────────

        [Fact]
        public void SortBy_Ascending_SortsCorrectly()
        {
            var items = new List<Item>
            {
                new() { Name = "c", Value = 3 },
                new() { Name = "a", Value = 1 },
                new() { Name = "b", Value = 2 }
            };

            var sorted = PaginationHelper.SortBy(items, nameof(Item.Value), ascending: true);

            sorted.Select(i => i.Value).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void SortBy_Descending_SortsCorrectly()
        {
            var items = new List<Item>
            {
                new() { Name = "a", Value = 1 },
                new() { Name = "b", Value = 2 },
                new() { Name = "c", Value = 3 }
            };

            var sorted = PaginationHelper.SortBy(items, nameof(Item.Value), ascending: false);

            sorted.Select(i => i.Value).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void SortBy_UnknownProperty_ReturnsUnsortedList()
        {
            var items = new List<Item> { new() { Name = "b" }, new() { Name = "a" } };

            var sorted = PaginationHelper.SortBy(items, "NonExistent");

            sorted.Select(i => i.Name).Should().Equal("b", "a");
        }

        [Fact]
        public void SortBy_NullCollection_ReturnsEmptyList()
        {
            var sorted = PaginationHelper.SortBy<Item>(null!, "Name");

            sorted.Should().BeEmpty();
        }

        // ── FilterBy ─────────────────────────────────────────────────────────

        [Fact]
        public void FilterBy_ExistingPropertyValue_ReturnsMatchingItems()
        {
            var items = new List<Item>
            {
                new() { Name = "api", Value = 1 },
                new() { Name = "web", Value = 2 },
                new() { Name = "api", Value = 3 }
            };

            var filtered = PaginationHelper.FilterBy(items, nameof(Item.Name), "api");

            filtered.Should().HaveCount(2);
            filtered.All(i => i.Name == "api").Should().BeTrue();
        }

        [Fact]
        public void Filter_WithPredicate_ReturnsMatchingItems()
        {
            var items = Enumerable.Range(1, 10).ToList();

            var filtered = PaginationHelper.Filter(items, x => x > 5);

            filtered.Should().Equal(6, 7, 8, 9, 10);
        }

        [Fact]
        public void Filter_NullCollection_ReturnsEmptyList()
        {
            var filtered = PaginationHelper.Filter<int>(null!, x => x > 0);

            filtered.Should().BeEmpty();
        }
    }

    public sealed class QueryBuilderTests
    {
        [Fact]
        public void Execute_WithPageAndPageSize_ReturnsPaginatedResult()
        {
            var items = Enumerable.Range(1, 30).ToList();

            var result = new QueryBuilder<int>(items)
                .Page(2)
                .PageSize(10)
                .Execute();

            result.Items.Should().Equal(Enumerable.Range(11, 10));
            result.Page.Should().Be(2);
        }

        [Fact]
        public void Execute_WithWhereFilter_FiltersBeforePagination()
        {
            var items = Enumerable.Range(1, 20).ToList();

            var result = new QueryBuilder<int>(items)
                .Where(x => x % 2 == 0)
                .Page(1)
                .PageSize(5)
                .Execute();

            result.Items.Should().Equal(2, 4, 6, 8, 10);
            result.TotalCount.Should().Be(10); // 10 even numbers
        }

        [Fact]
        public void ExecuteUnpaged_ReturnsAllFilteredItems()
        {
            var items = Enumerable.Range(1, 10).ToList();

            var result = new QueryBuilder<int>(items)
                .Where(x => x > 5)
                .ExecuteUnpaged();

            result.Should().Equal(6, 7, 8, 9, 10);
        }

        [Fact]
        public void Execute_WithNullData_ReturnsEmptyResult()
        {
            var result = new QueryBuilder<int>(null!)
                .Execute();

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public void Execute_ChainedWhereFilters_AppliesBoth()
        {
            var items = Enumerable.Range(1, 20).ToList();

            var result = new QueryBuilder<int>(items)
                .Where(x => x % 2 == 0)
                .Where(x => x > 10)
                .ExecuteUnpaged();

            result.Should().Equal(12, 14, 16, 18, 20);
        }
    }
}
