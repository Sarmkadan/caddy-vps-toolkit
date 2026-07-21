#nullable enable
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Unit tests for <see cref="CollectionExtensions"/>.
    /// Covers all public extension methods including happy-path, edge-cases and error-paths.
    /// </summary>
    public sealed class CollectionExtensionsUnitTests
    {
        [Fact]
        public void SafeGet_WithValidIndex_ReturnsCorrectElement()
        {
            // Arrange
            var list = new List<int> { 10, 20, 30, 40, 50 };

            // Act & Assert
            list.SafeGet(0).Should().Be(10);
            list.SafeGet(2).Should().Be(30);
            list.SafeGet(4).Should().Be(50);
        }

        [Fact]
        public void SafeGet_WithNegativeIndex_ReturnsDefault()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act & Assert
            list.SafeGet(-1).Should().BeNull();
            list.SafeGet(-100).Should().BeNull();
        }

        [Fact]
        public void SafeGet_WithOutOfRangeIndex_ReturnsDefault()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act & Assert
            list.SafeGet(3).Should().Be(0); // default(int)
            list.SafeGet(100).Should().Be(0);
            list.SafeGet(100, -1).Should().Be(-1); // custom default
        }

        [Fact]
        public void SafeGet_WithNullList_ReturnsDefault()
        {
            // Arrange
            List<int>? list = null;

            // Act & Assert
            list.SafeGet(0).Should().Be(0);
            list.SafeGet(5, -99).Should().Be(-99);
        }

        [Fact]
        public void IsNullOrEmpty_WithNonEmptyCollection_ReturnsFalse()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var list = new List<string> { "a" };
            var enumerable = new List<int> { 42 };

            // Act & Assert
            collection.IsNullOrEmpty().Should().BeFalse();
            list.IsNullOrEmpty().Should().BeFalse();
            enumerable.IsNullOrEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsNullOrEmpty_WithEmptyCollection_ReturnsTrue()
        {
            // Arrange
            var array = Array.Empty<int>();
            var list = new List<string>();
            IEnumerable<int> enumerable = new int[0];

            // Act & Assert
            array.IsNullOrEmpty().Should().BeTrue();
            list.IsNullOrEmpty().Should().BeTrue();
            enumerable.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_WithNullCollection_ReturnsTrue()
        {
            // Arrange
            IEnumerable<int>? collection = null;
            List<string>? list = null;

            // Act & Assert
            collection.IsNullOrEmpty().Should().BeTrue();
            list.IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void FirstOrDefault_WithNonEmptyCollection_ReturnsFirstElement()
        {
            // Arrange
            var collection = new[] { 5, 10, 15 };
            var list = new List<string> { "first", "second" };

            // Act & Assert
            CaddyVpsToolkit.Utilities.CollectionExtensions.FirstOrDefault(collection).Should().Be(5);
            CaddyVpsToolkit.Utilities.CollectionExtensions.FirstOrDefault(list).Should().Be("first");
        }

        [Fact]
        public void FirstOrDefault_WithEmptyCollection_ReturnsDefault()
        {
            // Arrange
            var array = Array.Empty<int>();
            var list = new List<string>();

            // Act & Assert
            CaddyVpsToolkit.Utilities.CollectionExtensions.FirstOrDefault(array).Should().Be(0);
            CaddyVpsToolkit.Utilities.CollectionExtensions.FirstOrDefault(list).Should().BeNull();
        }

        [Fact]
        public void FirstOrDefault_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<int>? collection = null;

            // Act
            Action act = () => CaddyVpsToolkit.Utilities.CollectionExtensions.FirstOrDefault(collection!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Batch_WithValidBatchSize_CreatesCorrectNumberOfBatches()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            // Act
            var batches = collection.Batch(3);

            // Assert
            batches.Should().HaveCount(4);
            batches[0].Should().Equal(1, 2, 3);
            batches[1].Should().Equal(4, 5, 6);
            batches[2].Should().Equal(7, 8, 9);
            batches[3].Should().Equal(10);
        }

        [Fact]
        public void Batch_WithBatchSizeOne_CreatesSingleElementBatches()
        {
            // Arrange
            var collection = new[] { "a", "b", "c" };

            // Act
            var batches = collection.Batch(1);

            // Assert
            batches.Should().HaveCount(3);
            batches[0].Should().Equal("a");
            batches[1].Should().Equal("b");
            batches[2].Should().Equal("c");
        }

        [Fact]
        public void Batch_WithBatchSizeEqualToCount_CreatesSingleBatch()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };

            // Act
            var batches = collection.Batch(3);

            // Assert
            batches.Should().HaveCount(1);
            batches[0].Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Batch_WithBatchSizeLargerThanCount_CreatesSingleBatch()
        {
            // Arrange
            var collection = new[] { 1, 2 };

            // Act
            var batches = collection.Batch(10);

            // Assert
            batches.Should().HaveCount(1);
            batches[0].Should().Equal(1, 2);
        }

        [Fact]
        public void Batch_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<int>? collection = null;

            // Act
            Action act = () => collection!.Batch(2);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Batch_WithNonPositiveBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };

            // Act
            Action act1 = () => collection.Batch(0);
            Action act2 = () => collection.Batch(-1);
            Action act3 = () => collection.Batch(-100);

            // Assert
            act1.Should().Throw<ArgumentException>();
            act2.Should().Throw<ArgumentException>();
            act3.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Partition_WithMatchingPredicate_SplitsCorrectly()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5, 6 };

            // Act
            var (matching, notMatching) = collection.Partition(x => x % 2 == 0);

            // Assert
            matching.Should().Equal(2, 4, 6);
            notMatching.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void Partition_WithAllMatchingPredicate_ReturnsAllInMatching()
        {
            // Arrange
            var collection = new[] { 2, 4, 6, 8 };

            // Act
            var (matching, notMatching) = collection.Partition(x => x % 2 == 0);

            // Assert
            matching.Should().Equal(2, 4, 6, 8);
            notMatching.Should().BeEmpty();
        }

        [Fact]
        public void Partition_WithNoMatchingPredicate_ReturnsAllInNotMatching()
        {
            // Arrange
            var collection = new[] { 1, 3, 5, 7 };

            // Act
            var (matching, notMatching) = collection.Partition(x => x % 2 == 0);

            // Assert
            matching.Should().BeEmpty();
            notMatching.Should().Equal(1, 3, 5, 7);
        }

        [Fact]
        public void Partition_WithNullCollection_ReturnsTwoEmptyLists()
        {
            // Arrange
            IEnumerable<int>? collection = null;

            // Act
            var (matching, notMatching) = collection!.Partition(x => x > 0);

            // Assert
            matching.Should().BeEmpty();
            notMatching.Should().BeEmpty();
        }

        [Fact]
        public void Partition_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            Func<int, bool>? predicate = null;

            // Act
            Action act = () => collection.Partition(predicate!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ToTupleList_WithDictionary_ReturnsListOfTuples()
        {
            // Arrange
            var dict = new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" }
            };

            // Act
            var result = dict.ToTupleList();

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Be((1, "one"));
            result[1].Should().Be((2, "two"));
            result[2].Should().Be((3, "three"));
        }

        [Fact]
        public void ToTupleList_WithEmptyDictionary_ReturnsEmptyList()
        {
            // Arrange
            var dict = new Dictionary<string, int>();

            // Act
            var result = dict.ToTupleList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ToTupleList_WithNullDictionary_ReturnsEmptyList()
        {
            // Arrange
            Dictionary<int, string>? dict = null;

            // Act
            var result = dict!.ToTupleList();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IntersectAll_WithMultipleCollections_ReturnsCommonElements()
        {
            // Arrange
            var collections = new[]
            {
                new[] { 1, 2, 3, 4 },
                new[] { 2, 3, 4, 5 },
                new[] { 2, 3, 6 }
            };

            // Act
            var result = collections.IntersectAll();

            // Assert
            result.Should().Equal(2, 3);
        }

        [Fact]
        public void IntersectAll_WithSingleCollection_ReturnsSameCollection()
        {
            // Arrange
            var collections = new[]
            {
                new[] { 10, 20, 30 }
            };

            // Act
            var result = collections.IntersectAll();

            // Assert
            result.Should().Equal(10, 20, 30);
        }

        [Fact]
        public void IntersectAll_WithEmptyCollections_ReturnsEmptyList()
        {
            // Arrange
            var collections = Array.Empty<IEnumerable<int>>();

            // Act
            var result = collections.IntersectAll();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IntersectAll_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<IEnumerable<int>>? collections = null;

            // Act
            Action act = () => collections!.IntersectAll();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemoveWhere_WithMatchingPredicate_RemovesCorrectElements()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            list.RemoveWhere(x => x % 2 == 0);

            // Assert
            list.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void RemoveWhere_WithNoMatchingPredicate_RemovesNothing()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };

            // Act
            list.RemoveWhere(x => x.Length > 10);

            // Assert
            list.Should().Equal("a", "b", "c");
        }

        [Fact]
        public void RemoveWhere_WithAllMatchingPredicate_RemovesAllElements()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act
            list.RemoveWhere(x => x > 0);

            // Assert
            list.Should().BeEmpty();
        }

        [Fact]
        public void RemoveWhere_WithNullList_ThrowsArgumentNullException()
        {
            // Arrange
            List<int>? list = null;

            // Act
            Action act = () => list!.RemoveWhere(x => x > 0);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void RemoveWhere_WithNullPredicate_ThrowsArgumentNullException()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            Func<int, bool>? predicate = null;

            // Act
            Action act = () => list.RemoveWhere(predicate!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddRangeIfNotExists_WithNewItems_AddsAllItems()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };
            var newItems = new[] { 4, 5, 6 };

            // Act
            list.AddRangeIfNotExists(newItems);

            // Assert
            list.Should().Equal(1, 2, 3, 4, 5, 6);
        }

        [Fact]
        public void AddRangeIfNotExists_WithExistingItems_AddsOnlyNewItems()
        {
            // Arrange
            var list = new List<string> { "a", "b", "c" };
            var newItems = new[] { "b", "c", "d", "e" };

            // Act
            list.AddRangeIfNotExists(newItems);

            // Assert
            list.Should().Equal("a", "b", "c", "d", "e");
        }

        [Fact]
        public void AddRangeIfNotExists_WithNullItems_DoesNotModifyList()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act
            list.AddRangeIfNotExists(null);

            // Assert
            list.Should().Equal(1, 2, 3);
        }

        [Fact]
        public void AddRangeIfNotExists_WithNullList_ThrowsArgumentNullException()
        {
            // Arrange
            List<int>? list = null;
            var items = new[] { 1, 2, 3 };

            // Act
            Action act = () => list!.AddRangeIfNotExists(items);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Shuffle_WithCollection_ReturnsShuffledList()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };

            // Act
            var shuffled = CaddyVpsToolkit.Utilities.CollectionExtensions.Shuffle(collection);

            // Assert
            shuffled.Should().HaveCount(5);
            shuffled.Should().Contain(collection);
            shuffled.Should().NotEqual(collection); // Should be different order
        }

        [Fact]
        public void Shuffle_WithNullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<int>? collection = null;

            // Act
            Action act = () => CaddyVpsToolkit.Utilities.CollectionExtensions.Shuffle(collection!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
