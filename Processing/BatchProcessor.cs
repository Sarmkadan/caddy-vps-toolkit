#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Processing
{
    /// <summary>
    /// Processes items in batches for efficient bulk operations.
    /// Useful for optimizing database operations and API calls.
    /// </summary>
    public sealed class BatchProcessor<T>
    {
        private readonly int _batchSize;
        private readonly Func<List<T>, Task> _processFunction;

        public BatchProcessor(int batchSize, Func<List<T>, Task> processFunction)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            _batchSize = batchSize;
            _processFunction = processFunction ?? throw new ArgumentNullException(nameof(processFunction));
        }

        /// <summary>
        /// Process items in batches using bounded channel for efficient producer-consumer pattern
        /// </summary>
        public async Task ProcessAsync(IEnumerable<T> items)
        {
            // Create a bounded channel to coordinate batch processing
            // Capacity is set to batchSize to prevent unbounded memory growth
            var channel = Channel.CreateBounded<List<T>>(new BoundedChannelOptions(_batchSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

            // Producer: Enumerate items and create batches
            _ = Task.Run(async () =>
            {
                try
                {
                    var batch = new List<T>(_batchSize);
                    foreach (var item in items)
                    {
                        batch.Add(item);

                        if (batch.Count >= _batchSize)
                        {
                            await channel.Writer.WriteAsync(batch);
                            batch = new List<T>(_batchSize);
                        }
                    }

                    // Write remaining items
                    if (batch.Count > 0)
                        await channel.Writer.WriteAsync(batch);

                    // Signal completion
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    channel.Writer.Complete(ex);
                }
            });

            // Consumer: Process batches from channel
            await foreach (var batch in channel.Reader.ReadAllAsync())
            {
                await _processFunction(batch);
            }
        }
    }

    /// <summary>
    /// Batch result with success/failure tracking
    /// </summary>
    public sealed class BatchResult<T>
    {
        public List<T> SuccessfulItems { get; set; } = new();
        public List<(T item, Exception error)> FailedItems { get; set; } = new();

        public int TotalProcessed => SuccessfulItems.Count + FailedItems.Count;
        public int SuccessCount => SuccessfulItems.Count;
        public int FailureCount => FailedItems.Count;
        public bool AllSucceeded => FailureCount == 0;

        public string GetReport()
        {
            return $"Processed: {TotalProcessed}, Successful: {SuccessCount}, Failed: {FailureCount}";
        }
    }

    /// <summary>
    /// Batch processor with error handling and result tracking
    /// </summary>
    public sealed class SafeBatchProcessor<T>
    {
        private readonly int _batchSize;
        private readonly Func<T, Task> _processFunction;
        private readonly bool _continueOnError;

        public SafeBatchProcessor(int batchSize, Func<T, Task> processFunction, bool continueOnError = true)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            _batchSize = batchSize;
            _processFunction = processFunction ?? throw new ArgumentNullException(nameof(processFunction));
            _continueOnError = continueOnError;
        }

        /// <summary>
        /// Process items with error handling using bounded channel for efficient producer-consumer pattern
        /// </summary>
        public async Task<BatchResult<T>> ProcessAsync(IEnumerable<T> items)
        {
            var result = new BatchResult<T>();

            // Create a bounded channel to coordinate batch processing
            var channel = Channel.CreateBounded<List<T>>(new BoundedChannelOptions(_batchSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

            // Producer: Enumerate items and create batches
            _ = Task.Run(async () =>
            {
                try
                {
                    var batch = new List<T>(_batchSize);
                    foreach (var item in items)
                    {
                        batch.Add(item);

                        if (batch.Count >= _batchSize)
                        {
                            await channel.Writer.WriteAsync(batch);
                            batch = new List<T>(_batchSize);
                        }
                    }

                    // Write remaining items
                    if (batch.Count > 0)
                        await channel.Writer.WriteAsync(batch);

                    // Signal completion
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    channel.Writer.Complete(ex);
                }
            });

            // Consumer: Process batches from channel with error handling
            await foreach (var batch in channel.Reader.ReadAllAsync())
            {
                await ProcessBatchAsync(batch, result);
            }

            return result;
        }

        private async Task ProcessBatchAsync(List<T> batch, BatchResult<T> result)
        {
            foreach (var item in batch)
            {
                try
                {
                    await _processFunction(item);
                    result.SuccessfulItems.Add(item);
                }
                catch (Exception ex)
                {
                    result.FailedItems.Add((item, ex));

                    if (!_continueOnError)
                        throw;
                }
            }
        }
    }
}
