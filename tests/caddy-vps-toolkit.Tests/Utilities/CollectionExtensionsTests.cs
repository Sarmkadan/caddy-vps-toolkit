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
    public sealed class CollectionExtensionsTests
    {
        // ── SafeGet ──────────────────────────────────────────────────────────

        [Fact]
        public void SafeGet_ValidIndex_ReturnsElement()
        {
            var list = new List<string> { "a", "b", "c" };

            list.SafeGet(1).Should().Be("b");
        }

        [Fact]
        public void SafeGet_IndexOutOfRange_ReturnsDefault()
        {
            var list = new List<int> { 1, 2, 3 };

            list.SafeGet(10).Should().Be(0);
        }

        [Fact]
        public void SafeGet_NegativeIndex_ReturnsDefault()
        {
            var list = new List<int> { 1, 2 };

            list.SafeGet(-1).Should().Be(0);
        }

        [Fact]
        public void SafeGet_NullList_ReturnsProvidedDefault()
        {
            List<string>? list = null;

            list!.SafeGet(0, "fallback").Should().Be("fallback");
        }

        // ── IsNullOrEmpty ────────────────────────────────────────────────────

        [Fact]
        public void IsNullOrEmpty_NullCollection_ReturnsTrue()
        {
            IEnumerable<int>? collection = null;

            collection!.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
        {
            var collection = Enumerable.Empty<int>();

            collection.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_NonEmptyCollection_ReturnsFalse()
        {
            var collection = new[] { 1, 2, 3 };

            collection.IsNullOrEmpty().Should().BeFalse();
        }

        // ── Batch ────────────────────────────────────────────────────────────

        [Fact]
        public void Batch_EvenDivision_ProducesCorrectBatches()
        {
            var items = Enumerable.Range(1, 6);

            var batches = items.Batch(2);

            batches.Should().HaveCount(3);
            batches[0].Should().Equal(1, 2);
            batches[1].Should().Equal(3, 4);
            batches[2].Should().Equal(5, 6);
        }

        [Fact]
        public void Batch_WithRemainder_LastBatchHasFewerItems()
        {
            var items = Enumerable.Range(1, 5);

            var batches = items.Batch(2);

            batches.Should().HaveCount(3);
            batches[2].Should().Equal(5);
        }

        [Fact]
        public void Batch_ZeroBatchSize_ThrowsArgumentException()
        {
            var items = new[] { 1, 2, 3 };

            Action act = () => items.Batch(0);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Batch_NullCollection_ThrowsArgumentNullException()
        {
            IEnumerable<int>? items = null;

            Action act = () => items!.Batch(2);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Batch_EmptyCollection_ReturnsEmptyList()
        {
            var items = Enumerable.Empty<int>();

            var batches = items.Batch(3);

            batches.Should().BeEmpty();
        }

        // ── Partition ────────────────────────────────────────────────────────

        [Fact]
        public void Partition_SplitsIntoMatchingAndNotMatching()
        {
            var items = new[] { 1, 2, 3, 4, 5, 6 };

            var (evens, odds) = items.Partition(x => x % 2 == 0);

            evens.Should().Equal(2, 4, 6);
            odds.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void Partition_AllMatch_NotMatchingIsEmpty()
        {
            var items = new[] { 2, 4, 6 };

            var (matching, notMatching) = items.Partition(x => x % 2 == 0);

            matching.Should().HaveCount(3);
            notMatching.Should().BeEmpty();
        }

        [Fact]
        public void Partition_NoneMatch_MatchingIsEmpty()
        {
            var items = new[] { 1, 3, 5 };

            var (matching, notMatching) = items.Partition(x => x % 2 == 0);

            matching.Should().BeEmpty();
            notMatching.Should().HaveCount(3);
        }

        [Fact]
        public void Partition_NullCollection_ReturnsTwoEmptyLists()
        {
            IEnumerable<int>? items = null;

            var (matching, notMatching) = items!.Partition(x => x > 0);

            matching.Should().BeEmpty();
            notMatching.Should().BeEmpty();
        }

        // ── RemoveWhere ──────────────────────────────────────────────────────

        [Fact]
        public void RemoveWhere_MatchingPredicate_RemovesItems()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.RemoveWhere(x => x % 2 == 0);

            list.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void RemoveWhere_NoMatches_LeavesListUnchanged()
        {
            var list = new List<int> { 1, 3, 5 };

            list.RemoveWhere(x => x % 2 == 0);

            list.Should().Equal(1, 3, 5);
        }

        // ── AddRangeIfNotExists ──────────────────────────────────────────────

        [Fact]
        public void AddRangeIfNotExists_NewItems_AddsAll()
        {
            var list = new List<int> { 1, 2 };

            list.AddRangeIfNotExists(new[] { 3, 4 });

            list.Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void AddRangeIfNotExists_DuplicateItems_SkipsDuplicates()
        {
            var list = new List<int> { 1, 2, 3 };

            list.AddRangeIfNotExists(new[] { 2, 3, 4 });

            list.Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void AddRangeIfNotExists_NullItems_DoesNotThrow()
        {
            var list = new List<int> { 1 };

            Action act = () => list.AddRangeIfNotExists(null!);

            act.Should().NotThrow();
            list.Should().Equal(1);
        }

        // ── ToTupleList ──────────────────────────────────────────────────────

        [Fact]
        public void ToTupleList_NormalDictionary_ReturnsTuples()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

            var tuples = dict.ToTupleList();

            tuples.Should().HaveCount(2);
            tuples.Should().Contain(("a", 1));
            tuples.Should().Contain(("b", 2));
        }

        [Fact]
        public void ToTupleList_NullDictionary_ReturnsEmptyList()
        {
            Dictionary<string, int>? dict = null;

            var tuples = dict!.ToTupleList();

            tuples.Should().BeEmpty();
        }
    }
}
