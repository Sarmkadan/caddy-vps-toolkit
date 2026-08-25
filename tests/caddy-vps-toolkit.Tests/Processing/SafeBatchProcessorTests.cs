using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Processing;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Processing
{
    /// <summary>
    /// Tests for the SafeBatchProcessor class.
    /// </summary>
    public class SafeBatchProcessorTests
    {
        /// <summary>
        /// Verifies that when the total number of items is an exact multiple of the batch size, all items are processed in complete batches.
        /// </summary>
        /// <summary>
        /// Verifies that when the total number of items is an exact multiple of the batch size, all items are processed in complete batches.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithExactBatchSize_ProcessesAllItemsInCompleteBatches()
        {
            // Arrange
            var processedBatches = new List<List<int>>();
            var batchSize = 5;
            var totalItems = 20;

            var processor = new SafeBatchProcessor<int>(
                batchSize,
                async item =>
                {
                    await Task.Delay(1); // Simulate async work
                    processedBatches.Add(new List<int> { item });
                }
            );

            var items = new List<int>();
            for (int i = 0; i < totalItems; i++)
            {
                items.Add(i);
            }

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(20);
            result.SuccessCount.Should().Be(20);
            result.FailureCount.Should().Be(0);
            result.AllSucceeded.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that when the total number of items is not a multiple of the batch size, the remaining items are processed in a final partial batch.
        /// </summary>
        /// <summary>
        /// Verifies that when the total number of items is not a multiple of the batch size, the remaining items are processed in a final partial batch.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithPartialFinalBatch_ProcessesRemainingItemsInFinalBatch()
        {
            // Arrange
            var processedItems = new List<int>();
            var batchSize = 4;
            var totalItems = 10;

            var processor = new SafeBatchProcessor<int>(
                batchSize,
                async item =>
                {
                    await Task.Delay(1);
                    processedItems.Add(item);
                }
            );

            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(10);
            result.SuccessCount.Should().Be(10);
            result.FailureCount.Should().Be(0);
            processedItems.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        /// <summary>
        /// Verifies that when an item throws an exception, the processing stops and the failed item is tracked.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithErrorInOneItem_ContinuesProcessingAndTracksFailure()
        {
            // Arrange
            var processedItems = new List<int>();
            var failedItems = new List<(int item, Exception error)>();
            var batchSize = 3;
            var errorItem = 5;

            var processor = new SafeBatchProcessor<int>(
                batchSize,
                async item =>
                {
                    await Task.Delay(1);
                    if (item == errorItem)
                    {
                        throw new InvalidOperationException($"Error processing item {item}");
                    }
                    processedItems.Add(item);
                }
            );

            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(8);
            result.SuccessCount.Should().Be(7);
            result.FailureCount.Should().Be(1);
            result.AllSucceeded.Should().BeFalse();

            result.SuccessfulItems.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 6, 7, 8 });
            result.FailedItems.Should().HaveCount(1);
            result.FailedItems[0].item.Should().Be(errorItem);
            result.FailedItems[0].error.Should().BeOfType<InvalidOperationException>();
        }

        /// <summary>
        /// Verifies that when an item throws an exception and continueOnError is false, the processing stops and the failed item is tracked.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithErrorInOneItemAndContinueOnErrorFalse_StopsProcessingAndThrows()
        {
            // Arrange
            var processedItems = new List<int>();
            var batchSize = 3;
            var errorItem = 5;

            var processor = new SafeBatchProcessor<int>(
                batchSize,
                async item =>
                {
                    await Task.Delay(1);
                    if (item == errorItem)
                    {
                        throw new InvalidOperationException($"Error processing item {item}");
                    }
                    processedItems.Add(item);
                },
                continueOnError: false
            );

            var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            Func<Task> act = async () => await processor.ProcessAsync(items);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            processedItems.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        }

        /// <summary>
        /// Verifies that when multiple items throw exceptions, all failed items are tracked and processing continues for successful items.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithMultipleErrors_TracksAllFailures()
        {
            // Arrange
            var batchSize = 2;
            var errorItems = new List<int> { 3, 7, 11 };

            var processor = new SafeBatchProcessor<int>(
                batchSize,
                async item =>
                {
                    await Task.Delay(1);
                    if (errorItems.Contains(item))
                    {
                        throw new InvalidOperationException($"Error processing item {item}");
                    }
                }
            );

            var items = new List<int>();
            for (int i = 1; i <= 12; i++)
            {
                items.Add(i);
            }

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(12);
            result.SuccessCount.Should().Be(9);
            result.FailureCount.Should().Be(3);
            result.FailedItems.Should().HaveCount(3);
            result.FailedItems[0].item.Should().Be(3);
            result.FailedItems[1].item.Should().Be(7);
            result.FailedItems[2].item.Should().Be(11);
        }

        /// <summary>
        /// Verifies that when an empty collection is processed, the result contains zero processed items and empty success/failure lists.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithEmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var processor = new SafeBatchProcessor<int>(
                5,
                _ => Task.CompletedTask
            );

            var items = new List<int>();

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(0);
            result.SuccessCount.Should().Be(0);
            result.FailureCount.Should().Be(0);
            result.AllSucceeded.Should().BeTrue();
            result.SuccessfulItems.Should().BeEmpty();
            result.FailedItems.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that when a single item is processed, the result contains one processed item and the item is in the successful list.
        /// </summary>
        [Fact]
        public async Task ProcessAsync_WithSingleItem_ReturnsSingleItemResult()
        {
            // Arrange
            var processor = new SafeBatchProcessor<string>(
                10,
                async item => await Task.Delay(1)
            );

            var items = new List<string> { "single" };

            // Act
            var result = await processor.ProcessAsync(items);

            // Assert
            result.TotalProcessed.Should().Be(1);
            result.SuccessCount.Should().Be(1);
            result.FailureCount.Should().Be(0);
            result.SuccessfulItems.Should().BeEquivalentTo(new[] { "single" });
        }

        /// <summary>
        /// Verifies that when the batch size is zero, an ArgumentException is thrown.
        /// </summary>
        [Fact]
        public void Constructor_WithZeroBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var processFunction = new Func<int, Task>(_ => Task.CompletedTask);

            // Act
            Action act = () => new SafeBatchProcessor<int>(0, processFunction);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Batch size must be positive (Parameter 'batchSize')");
        }

        [Fact]
        public void Constructor_WithNegativeBatchSize_ThrowsArgumentException()
        {
            // Arrange
            var processFunction = new Func<int, Task>(_ => Task.CompletedTask);

            // Act
            Action act = () => new SafeBatchProcessor<int>(-1, processFunction);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Batch size must be positive (Parameter 'batchSize')");
        }

        [Fact]
        public void Constructor_WithNullProcessFunction_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new SafeBatchProcessor<int>(5, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("Value cannot be null. (Parameter 'processFunction')");
        }

        [Fact]
        public async Task GetReport_ReturnsCorrectSummary()
        {
            // Arrange
            var processor = new SafeBatchProcessor<int>(
                5,
                _ => Task.CompletedTask
            );

            var items = new List<int> { 1, 2, 3 };
            var result = await processor.ProcessAsync(items);

            // Act
            var report = result.GetReport();

            // Assert
            report.Should().Be("Processed: 3, Successful: 3, Failed: 0");
        }

        [Fact]
        public async Task BatchResult_Properties_WorkCorrectly()
        {
            // Arrange
            var result = new BatchResult<int>();

            // Act & Assert
            result.TotalProcessed.Should().Be(0);
            result.SuccessCount.Should().Be(0);
            result.FailureCount.Should().Be(0);
            result.AllSucceeded.Should().BeTrue();

            // Add some items
            result.SuccessfulItems.Add(1);
            result.SuccessfulItems.Add(2);

            result.TotalProcessed.Should().Be(2);
            result.SuccessCount.Should().Be(2);
            result.FailureCount.Should().Be(0);
            result.AllSucceeded.Should().BeTrue();

            // Add a failure
            result.FailedItems.Add((3, new Exception("Error")));

            result.TotalProcessed.Should().Be(3);
            result.SuccessCount.Should().Be(2);
            result.FailureCount.Should().Be(1);
            result.AllSucceeded.Should().BeFalse();
        }
    }
}
