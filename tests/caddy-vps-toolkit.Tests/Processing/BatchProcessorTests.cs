using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Processing;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Processing
{
    public class BatchProcessorTests
    {
        [Fact]
        public async Task ProcessAsync_WithExactBatchSize_ProcessesAllItemsInCompleteBatches()
        {
            // Arrange
            var processedBatches = new List<List<int>>();
            var batchSize = 5;
            var totalItems = 20;

            var processor = new BatchProcessor<int>(
                batchSize,
                batch =>
                {
                    processedBatches.Add(new List<int>(batch));
                    return Task.CompletedTask;
                }
            );

            var items = new List<int>();
            for (int i = 0; i < totalItems; i++)
            {
                items.Add(i);
            }

            // Act
            await processor.ProcessAsync(items);

            // Assert
            processedBatches.Should().HaveCount(4, "Should create exactly 4 batches for 20 items with batch size 5");
            processedBatches[0].Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4 });
            processedBatches[1].Should().BeEquivalentTo(new[] { 5, 6, 7, 8, 9 });
            processedBatches[2].Should().BeEquivalentTo(new[] { 10, 11, 12, 13, 14 });
            processedBatches[3].Should().BeEquivalentTo(new[] { 15, 16, 17, 18, 19 });
        }

        [Fact]
        public async Task ProcessAsync_WithPartialFinalBatch_ProcessesRemainingItemsInFinalBatch()
        {
            // Arrange
            var processedBatches = new List<List<string>>();
            var batchSize = 4;
            var totalItems = 10;

            var processor = new BatchProcessor<string>(
                batchSize,
                batch =>
                {
                    processedBatches.Add(new List<string>(batch));
                    return Task.CompletedTask;
                }
            );

            var items = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

            // Act
            await processor.ProcessAsync(items);

            // Assert
            processedBatches.Should().HaveCount(3, "Should create 3 batches for 10 items with batch size 4");
            processedBatches[0].Should().BeEquivalentTo(new[] { "a", "b", "c", "d" });
            processedBatches[1].Should().BeEquivalentTo(new[] { "e", "f", "g", "h" });
            processedBatches[2].Should().BeEquivalentTo(new[] { "i", "j" });
        }

        [Fact]
        public async Task ProcessAsync_WithSingleItem_CreatesSingleItemBatch()
        {
            // Arrange
            var processedBatch = new List<int>();
            var batchSize = 10;
            var totalItems = 1;

            var processor = new BatchProcessor<int>(
                batchSize,
                batch =>
                {
                    processedBatch.AddRange(batch);
                    return Task.CompletedTask;
                }
            );

            var items = new List<int> { 42 };

            // Act
            await processor.ProcessAsync(items);

            // Assert
            processedBatch.Should().BeEquivalentTo(new[] { 42 });
        }

        [Fact]
        public async Task ProcessAsync_WithEmptyCollection_DoesNotThrow()
        {
            // Arrange
            var processedBatchCount = 0;

            var processor = new BatchProcessor<int>(
                5,
                _ =>
                {
                    processedBatchCount++;
                    return Task.CompletedTask;
                }
            );

            var items = new List<int>();

            // Act
            await processor.ProcessAsync(items);

            // Assert
            processedBatchCount.Should().Be(0, "Should not process any batches for empty collection");
        }

        [Fact]
        public async Task ProcessAsync_WithCustomObjectType_ProcessesCorrectly()
        {
            // Arrange
            var processedItems = new List<TestItem>();
            var batchSize = 3;

            var processor = new BatchProcessor<TestItem>(
                batchSize,
                batch =>
                {
                    processedItems.AddRange(batch);
                    return Task.CompletedTask;
                }
            );

            var items = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Item1" },
                new TestItem { Id = 2, Name = "Item2" },
                new TestItem { Id = 3, Name = "Item3" },
                new TestItem { Id = 4, Name = "Item4" },
            };

            // Act
            await processor.ProcessAsync(items);

            // Assert
            processedItems.Should().HaveCount(4);
            processedItems[0].Should().BeEquivalentTo(new { Id = 1, Name = "Item1" });
            processedItems[3].Should().BeEquivalentTo(new { Id = 4, Name = "Item4" });
        }

        [Fact]
        public void Constructor_WithZeroBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var processFunction = new Func<List<int>, Task>(_ => Task.CompletedTask);

            // Act
            Action act = () => new BatchProcessor<int>(0, processFunction);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Batch size must be positive (Parameter 'batchSize')");
        }

        [Fact]
        public void Constructor_WithNegativeBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var processFunction = new Func<List<int>, Task>(_ => Task.CompletedTask);

            // Act
            Action act = () => new BatchProcessor<int>(-1, processFunction);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Batch size must be positive (Parameter 'batchSize')");
        }

        [Fact]
        public void Constructor_WithNullProcessFunction_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new BatchProcessor<int>(5, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'processFunction')");
        }

        private class TestItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
