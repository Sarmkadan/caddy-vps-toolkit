#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Results;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Data
{
    /// <summary>
    /// Comprehensive unit tests for <see cref="PaginationHelper"/> public API.
    /// Tests happy-path, edge cases, and error conditions.
    /// </summary>
    public sealed class PaginationHelperUnitTests
    {
        private sealed class TestItem
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
            public DateTime Created { get; set; } = DateTime.Now;
        }

        // ====================================================================
        // Paginate Tests
        // ====================================================================

        [Fact]
        public void Paginate_WithValidInput_ReturnsCorrectPage()
        {
            // Arrange
            var items = Enumerable.Range(1, 100).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: 3, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(10);
            result.Page.Should().Be(3);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(100);
            result.Items.Should().Equal(21, 22, 23, 24, 25, 26, 27, 28, 29, 30);
        }

        [Fact]
        public void Paginate_WithEmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var items = new List<int>();

            // Act
            var result = PaginationHelper.Paginate(items, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public void Paginate_WithNullCollection_ReturnsEmptyResult()
        {
            // Act
            var result = PaginationHelper.Paginate<int>(null!, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public void Paginate_WithPageLessThanOne_ClampsToOne()
        {
            // Arrange
            var items = Enumerable.Range(1, 25).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: 0, pageSize: 10);

            // Assert
            result.Page.Should().Be(1);
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
        }

        [Fact]
        public void Paginate_WithNegativePage_ClampsToOne()
        {
            // Arrange
            var items = Enumerable.Range(1, 25).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: -5, pageSize: 10);

            // Assert
            result.Page.Should().Be(1);
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
        }

        [Fact]
        public void Paginate_WithPageSizeLessThanOne_ClampsToTen()
        {
            // Arrange
            var items = Enumerable.Range(1, 25).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: 1, pageSize: 0);

            // Assert
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
        }

        [Fact]
        public void Paginate_WithNegativePageSize_ClampsToTen()
        {
            // Arrange
            var items = Enumerable.Range(1, 25).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: 1, pageSize: -3);

            // Assert
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
        }

        [Fact]
        public void Paginate_WithPageBeyondTotal_ReturnsEmptyItems()
        {
            // Arrange
            var items = Enumerable.Range(1, 5).ToList();

            // Act
            var result = PaginationHelper.Paginate(items, page: 100, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.Page.Should().Be(100);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(5);
        }

        [Fact]
        public void Paginate_WithSingleItem_ReturnsSingleItem()
        {
            // Arrange
            var items = new List<int> { 42 };

            // Act
            var result = PaginationHelper.Paginate(items, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.Should().Equal(42);
            result.TotalCount.Should().Be(1);
        }

        // ====================================================================
        // SortBy Tests
        // ====================================================================

        [Fact]
        public void SortBy_WithNullCollection_ReturnsEmptyList()
        {
            // Act
            var result = PaginationHelper.SortBy<TestItem>(null!, nameof(TestItem.Value));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void SortBy_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var items = new List<TestItem>();

            // Act
            var result = PaginationHelper.SortBy(items, nameof(TestItem.Value));

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void SortBy_WithNullPropertyName_ReturnsUnsortedList()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 3 }, new() { Value = 1 } };

            // Act
            var result = PaginationHelper.SortBy(items, null!);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Value.Should().Be(3);
            result[1].Value.Should().Be(1);
        }

        [Fact]
        public void SortBy_WithEmptyPropertyName_ReturnsUnsortedList()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 3 }, new() { Value = 1 } };

            // Act
            var result = PaginationHelper.SortBy(items, string.Empty);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Value.Should().Be(3);
            result[1].Value.Should().Be(1);
        }

        [Fact]
        public void SortBy_WithUnknownPropertyName_ReturnsUnsortedList()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 3 }, new() { Value = 1 } };

            // Act
            var result = PaginationHelper.SortBy(items, "NonExistentProperty");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Value.Should().Be(3);
            result[1].Value.Should().Be(1);
        }

        [Fact]
        public void SortBy_Ascending_SortsCorrectly()
        {
            // Arrange
            var items = new List<TestItem> {
                new() { Value = 5, Name = "e" },
                new() { Value = 1, Name = "a" },
                new() { Value = 3, Name = "c" }
            };

            // Act
            var result = PaginationHelper.SortBy(items, nameof(TestItem.Value), ascending: true);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Value.Should().Be(1);
            result[1].Value.Should().Be(3);
            result[2].Value.Should().Be(5);
        }

        [Fact]
        public void SortBy_Descending_SortsCorrectly()
        {
            // Arrange
            var items = new List<TestItem> {
                new() { Value = 1, Name = "a" },
                new() { Value = 5, Name = "e" },
                new() { Value = 3, Name = "c" }
            };

            // Act
            var result = PaginationHelper.SortBy(items, nameof(TestItem.Value), ascending: false);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Value.Should().Be(5);
            result[1].Value.Should().Be(3);
            result[2].Value.Should().Be(1);
        }

        // ====================================================================
        // FilterBy Tests
        // ====================================================================

        [Fact]
        public void FilterBy_WithNullCollection_ReturnsEmptyList()
        {
            // Act
            var result = PaginationHelper.FilterBy<TestItem>(null!, nameof(TestItem.Value), 42);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void FilterBy_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var items = new List<TestItem>();

            // Act
            var result = PaginationHelper.FilterBy(items, nameof(TestItem.Value), 42);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void FilterBy_WithNullPropertyName_ReturnsAllItems()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 1 }, new() { Value = 2 } };

            // Act
            var result = PaginationHelper.FilterBy(items, null!, 1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public void FilterBy_WithEmptyPropertyName_ReturnsAllItems()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 1 }, new() { Value = 2 } };

            // Act
            var result = PaginationHelper.FilterBy(items, string.Empty, 1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public void FilterBy_WithUnknownPropertyName_ReturnsAllItems()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 1 }, new() { Value = 2 } };

            // Act
            var result = PaginationHelper.FilterBy(items, "NonExistentProperty", 1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public void FilterBy_WithMatchingValue_ReturnsFilteredItems()
        {
            // Arrange
            var items = new List<TestItem> {
                new() { Value = 1, Name = "one" },
                new() { Value = 2, Name = "two" },
                new() { Value = 1, Name = "another one" },
                new() { Value = 3, Name = "three" }
            };

            // Act
            var result = PaginationHelper.FilterBy(items, nameof(TestItem.Value), 1);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(i => i.Value == 1).Should().BeTrue();
        }

        [Fact]
        public void FilterBy_WithNullValue_ReturnsAllItems()
        {
            // Arrange
            var items = new List<TestItem> { new() { Value = 1 }, new() { Value = 2 } };

            // Act
            var result = PaginationHelper.FilterBy(items, nameof(TestItem.Value), null!);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        // ====================================================================
        // Filter Tests
        // ====================================================================

        [Fact]
        public void Filter_WithNullCollection_ReturnsEmptyList()
        {
            // Act
            var result = PaginationHelper.Filter<int>(null!, x => x > 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Filter_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var items = new List<int>();

            // Act
            var result = PaginationHelper.Filter(items, x => x > 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void Filter_WithPredicate_ReturnsMatchingItems()
        {
            // Arrange
            var items = Enumerable.Range(1, 20).ToList();

            // Act
            var result = PaginationHelper.Filter(items, x => x % 2 == 0);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(10);
            result.Should().OnlyContain(x => x % 2 == 0);
        }

        [Fact]
        public void Filter_WithComplexPredicate_ReturnsCorrectItems()
        {
            // Arrange
            var items = new List<TestItem> {
                new() { Value = 1, Name = "a" },
                new() { Value = 5, Name = "e" },
                new() { Value = 3, Name = "c" },
                new() { Value = 7, Name = "g" }
            };

            // Act
            var result = PaginationHelper.Filter(items, x => x.Value > 3);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(x => x.Value > 3);
        }
    }
}