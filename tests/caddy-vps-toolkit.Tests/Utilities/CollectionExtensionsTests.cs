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
    /// <summary>
    /// Provides unit tests for the <see cref="CollectionExtensions"/> class extension methods.
    /// Tests various collection operations including safe access, batching, partitioning, and conditional removal.
    /// </summary>
    public sealed class CollectionExtensionsTests
    {
        // ── SafeGet ──────────────────────────────────────────────────────────

        /// <summary>
        /// Tests that SafeGet returns the element at a valid index.
        /// </summary>
        [Fact]
        public void SafeGet_ValidIndex_ReturnsElement()
        {
            var list = new List<string> { "a", "b", "c" };

            list.SafeGet(1).Should().Be("b");
        }

        /// <summary>
        /// Tests that SafeGet returns the default value when the index is out of range.
        /// </summary>
        [Fact]
        public void SafeGet_IndexOutOfRange_ReturnsDefault()
        {
            var list = new List<int> { 1, 2, 3 };

            list.SafeGet(10).Should().Be(0);
        }

        /// <summary>
        /// Tests that SafeGet returns the default value when the index is negative.
        /// </summary>
        [Fact]
        public void SafeGet_NegativeIndex_ReturnsDefault()
        {
            var list = new List<int> { 1, 2 };

            list.SafeGet(-1).Should().Be(0);
        }

        /// <summary>
        /// Tests that SafeGet returns the provided default value when the collection is null.
        /// <param name="list">The null collection to test</param>
        /// <param name="fallback">The fallback value to return</param>
        /// <returns>The fallback value when collection is null</returns>
        /// </summary>
        [Fact]
        public void SafeGet_NullList_ReturnsProvidedDefault()
        {
            List<string>? list = null;

            list!.SafeGet(0, "fallback").Should().Be("fallback");
        }

        // ── IsNullOrEmpty ────────────────────────────────────────────────────

        /// <summary>
        /// Tests that IsNullOrEmpty returns true for a null collection.
        /// </summary>
        [Fact]
        public void IsNullOrEmpty_NullCollection_ReturnsTrue()
        {
            IEnumerable<int>? collection = null;

            collection!.IsNullOrEmpty().Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsNullOrEmpty returns true for an empty collection.
        /// </summary>
        [Fact]
        public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
        {
            var collection = Enumerable.Empty<int>();

            collection.IsNullOrEmpty().Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsNullOrEmpty returns false for a non-empty collection.
        /// </summary>
        [Fact]
        public void IsNullOrEmpty_NonEmptyCollection_ReturnsFalse()
        {
            var collection = new[] { 1, 2, 3 };

            collection.IsNullOrEmpty().Should().BeFalse();
        }

        // ── Batch ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tests that Batch produces correct batches when dividing evenly.
        /// <param name="items">The collection to batch</param>
        /// <param name="batchSize">The size of each batch</param>
        /// <returns>A collection of batches</returns>
        /// </summary>
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

        /// <summary>
        /// Tests that Batch handles remainder correctly with the last batch having fewer items.
        /// <param name="items">The collection to batch</param>
        /// <param name="batchSize">The size of each batch</param>
        /// <returns>A collection of batches with the last batch having fewer items</returns>
        /// </summary>
        [Fact]
        public void Batch_WithRemainder_LastBatchHasFewerItems()
        {
            var items = Enumerable.Range(1, 5);

            var batches = items.Batch(2);

            batches.Should().HaveCount(3);
            batches[2].Should().Equal(5);
        }

        /// <summary>
        /// Tests that Batch throws ArgumentException when batch size is zero.
        /// <param name="items">The collection to batch</param>
        /// </summary>
        [Fact]
        public void Batch_ZeroBatchSize_ThrowsArgumentException()
        {
            var items = new[] { 1, 2, 3 };

            Action act = () => items.Batch(0);

            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that Batch throws ArgumentNullException when the collection is null.
        /// <param name="items">The null collection to batch</param>
        /// </summary>
        [Fact]
        public void Batch_NullCollection_ThrowsArgumentNullException()
        {
            IEnumerable<int>? items = null;

            Action act = () => items!.Batch(2);

            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that Batch returns an empty list when the collection is empty.
        /// <param name="items">The empty collection to batch</param>
        /// <returns>An empty list of batches</returns>
        /// </summary>
        [Fact]
        public void Batch_EmptyCollection_ReturnsEmptyList()
        {
            var items = Enumerable.Empty<int>();

            var batches = items.Batch(3);

            batches.Should().BeEmpty();
        }

        // ── Partition ────────────────────────────────────────────────────────

        /// <summary>
        /// Tests that Partition correctly splits items into matching and not matching groups.
        /// <param name="items">The collection to partition</param>
        /// <returns>A tuple containing matching and not matching collections</returns>
        /// </summary>
        [Fact]
        public void Partition_SplitsIntoMatchingAndNotMatching()
        {
            var items = new[] { 1, 2, 3, 4, 5, 6 };

            var (evens, odds) = items.Partition(x => x % 2 == 0);

            evens.Should().Equal(2, 4, 6);
            odds.Should().Equal(1, 3, 5);
        }

        /// <summary>
        /// Tests that Partition returns an empty notMatching collection when all items match.
        /// <param name="items">The collection to partition</param>
        /// <returns>A tuple where notMatching is empty</returns>
        /// </summary>
        [Fact]
        public void Partition_AllMatch_NotMatchingIsEmpty()
        {
            var items = new[] { 2, 4, 6 };

            var (matching, notMatching) = items.Partition(x => x % 2 == 0);

            matching.Should().HaveCount(3);
            notMatching.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that Partition returns an empty matching collection when no items match.
        /// <param name="items">The collection to partition</param>
        /// <returns>A tuple where matching is empty</returns>
        /// </summary>
        [Fact]
        public void Partition_NoneMatch_MatchingIsEmpty()
        {
            var items = new[] { 1, 3, 5 };

            var (matching, notMatching) = items.Partition(x => x % 2 == 0);

            matching.Should().BeEmpty();
            notMatching.Should().HaveCount(3);
        }

        /// <summary>
        /// Tests that Partition returns two empty lists when the collection is null.
        /// <param name="items">The null collection to partition</param>
        /// <returns>A tuple of two empty lists</returns>
        /// </summary>
        [Fact]
        public void Partition_NullCollection_ReturnsTwoEmptyLists()
        {
            IEnumerable<int>? items = null;

            var (matching, notMatching) = items!.Partition(x => x > 0);

            matching.Should().BeEmpty();
            notMatching.Should().BeEmpty();
        }

        // ── RemoveWhere ──────────────────────────────────────────────────────

        /// <summary>
        /// Tests that RemoveWhere removes items matching the predicate.
        /// <param name="list">The list to modify</param>
        /// </summary>
        [Fact]
        public void RemoveWhere_MatchingPredicate_RemovesItems()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.RemoveWhere(x => x % 2 == 0);

            list.Should().Equal(1, 3, 5);
        }

        /// <summary>
        /// Tests that RemoveWhere leaves the list unchanged when no items match the predicate.
        /// <param name="list">The list to test</param>
        /// </summary>
        [Fact]
        public void RemoveWhere_NoMatches_LeavesListUnchanged()
        {
            var list = new List<int> { 1, 3, 5 };

            list.RemoveWhere(x => x % 2 == 0);

            list.Should().Equal(1, 3, 5);
        }

        // ── AddRangeIfNotExists ──────────────────────────────────────────────

        /// <summary>
        /// Tests that AddRangeIfNotExists adds all new items to the list.
        /// <param name="list">The list to modify</param>
        /// <param name="newItems">The items to add</param>
        /// </summary>
        [Fact]
        public void AddRangeIfNotExists_NewItems_AddsAll()
        {
            var list = new List<int> { 1, 2 };

            list.AddRangeIfNotExists(new[] { 3, 4 });

            list.Should().Equal(1, 2, 3, 4);
        }

        /// <summary>
        /// Tests that AddRangeIfNotExists skips duplicate items.
        /// <param name="list">The list to modify</param>
        /// <param name="newItems">The items to add (containing duplicates)</param>
        /// </summary>
        [Fact]
        public void AddRangeIfNotExists_DuplicateItems_SkipsDuplicates()
        {
            var list = new List<int> { 1, 2, 3 };

            list.AddRangeIfNotExists(new[] { 2, 3, 4 });

            list.Should().Equal(1, 2, 3, 4);
        }

        /// <summary>
        /// Tests that AddRangeIfNotExists does not throw when the items to add are null.
        /// <param name="list">The list to test</param>
        /// <param name="newItems">The null items to add</param>
        /// </summary>
        [Fact]
        public void AddRangeIfNotExists_NullItems_DoesNotThrow()
        {
            var list = new List<int> { 1 };

            Action act = () => list.AddRangeIfNotExists(null!);

            act.Should().NotThrow();
            list.Should().Equal(1);
        }

        // ── ToTupleList ──────────────────────────────────────────────────────

        /// <summary>
        /// Tests that ToTupleList converts a dictionary to a list of key-value tuples.
        /// <param name="dict">The dictionary to convert</param>
        /// <returns>A list of key-value tuples</returns>
        /// </summary>
        [Fact]
        public void ToTupleList_NormalDictionary_ReturnsTuples()
        {
            var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

            var tuples = dict.ToTupleList();

            tuples.Should().HaveCount(2);
            tuples.Should().Contain(("a", 1));
            tuples.Should().Contain(("b", 2));
        }

        /// <summary>
        /// Tests that ToTupleList returns an empty list when the dictionary is null.
        /// <param name="dict">The null dictionary to convert</param>
        /// <returns>An empty list of tuples</returns>
        /// </summary>
        [Fact]
        public void ToTupleList_NullDictionary_ReturnsEmptyList()
        {
            Dictionary<string, int>? dict = null;

            var tuples = dict!.ToTupleList();

            tuples.Should().BeEmpty();
        }
    }
}