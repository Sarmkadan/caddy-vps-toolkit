#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly int? _maxDegreeOfParallelism;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessor{T}"/> class.
        /// </summary>
        /// <param name="batchSize">The maximum number of items per batch. Must be positive.</param>
        /// <param name="processFunction">The function to process each batch. Must not be null.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of batches to process concurrently. If null, processes batches sequentially. Must be positive if not null.</param>
        /// <exception cref="ArgumentException">Thrown if batchSize is not positive or maxDegreeOfParallelism is not positive.</exception>
        /// <exception cref="ArgumentNullException">Thrown if processFunction is null.</exception>
        public BatchProcessor(int batchSize, Func<List<T>, Task> processFunction, int? maxDegreeOfParallelism = null)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            _batchSize = batchSize;
            _processFunction = processFunction ?? throw new ArgumentNullException(nameof(processFunction));
            _maxDegreeOfParallelism = maxDegreeOfParallelism.HasValue && maxDegreeOfParallelism <= 0
                ? throw new ArgumentException("Max degree of parallelism must be positive or null", nameof(maxDegreeOfParallelism))
                : maxDegreeOfParallelism;
        }

        /// <summary>
        /// Process items in batches using bounded channel for efficient producer-consumer pattern
        /// </summary>
        /// <param name="items">The items to process. Must not be null.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for items to be processed.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if items is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        public async Task ProcessAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);

            // Create a bounded channel to coordinate batch processing
            // Capacity is set to batchSize to prevent unbounded memory growth
            var channel = Channel.CreateBounded<List<T>>(new BoundedChannelOptions(_batchSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
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
                        cancellationToken.ThrowIfCancellationRequested();
                        batch.Add(item);

                        if (batch.Count >= _batchSize)
                        {
                            await channel.Writer.WriteAsync(batch, cancellationToken);
                            batch = new List<T>(_batchSize);
                        }
                    }

                    // Write remaining items
                    if (batch.Count > 0)
                        await channel.Writer.WriteAsync(batch, cancellationToken);

                    // Signal completion
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    channel.Writer.Complete(ex);
                }
            }, cancellationToken);

            // Consumer: Process batches from channel with optional parallelism
            if (_maxDegreeOfParallelism.HasValue && _maxDegreeOfParallelism > 1)
            {
                await ProcessBatchesInParallelAsync(channel.Reader, cancellationToken);
            }
            else
            {
                await foreach (var batch in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await _processFunction(batch);
                }
            }
        }

        private async Task ProcessBatchesInParallelAsync(ChannelReader<List<T>> reader, CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism.Value, _maxDegreeOfParallelism.Value);
            var tasks = new List<Task>();

            await foreach (var batch in reader.ReadAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _processFunction(batch);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
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

        /// <summary>
        /// Gets a report summarizing the batch processing results.
        /// </summary>
        /// <returns>A string containing the processing summary.</returns>
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
        private readonly int? _maxDegreeOfParallelism;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeBatchProcessor{T}"/> class.
        /// </summary>
        /// <param name="batchSize">The maximum number of items per batch. Must be positive.</param>
        /// <param name="processFunction">The function to process each item. Must not be null.</param>
        /// <param name="continueOnError">Whether to continue processing remaining items after an error. Defaults to true.</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of items to process concurrently. If null, processes items sequentially. Must be positive if not null.</param>
        /// <exception cref="ArgumentException">Thrown if batchSize is not positive or maxDegreeOfParallelism is not positive.</exception>
        /// <exception cref="ArgumentNullException">Thrown if processFunction is null.</exception>
        public SafeBatchProcessor(int batchSize, Func<T, Task> processFunction, bool continueOnError = true, int? maxDegreeOfParallelism = null)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be positive", nameof(batchSize));

            _batchSize = batchSize;
            _processFunction = processFunction ?? throw new ArgumentNullException(nameof(processFunction));
            _continueOnError = continueOnError;
            _maxDegreeOfParallelism = maxDegreeOfParallelism.HasValue && maxDegreeOfParallelism <= 0
                ? throw new ArgumentException("Max degree of parallelism must be positive or null", nameof(maxDegreeOfParallelism))
                : maxDegreeOfParallelism;
        }

        /// <summary>
        /// Process items with error handling using bounded channel for efficient producer-consumer pattern
        /// </summary>
        /// <param name="items">The items to process. Must not be null.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for items to be processed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the batch processing results.</returns>
        /// <exception cref="ArgumentNullException">Thrown if items is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        public async Task<BatchResult<T>> ProcessAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);

            var result = new BatchResult<T>();

            // Create a bounded channel to coordinate batch processing
            var channel = Channel.CreateBounded<List<T>>(new BoundedChannelOptions(_batchSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
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
                        cancellationToken.ThrowIfCancellationRequested();
                        batch.Add(item);

                        if (batch.Count >= _batchSize)
                        {
                            await channel.Writer.WriteAsync(batch, cancellationToken);
                            batch = new List<T>(_batchSize);
                        }
                    }

                    // Write remaining items
                    if (batch.Count > 0)
                        await channel.Writer.WriteAsync(batch, cancellationToken);

                    // Signal completion
                    channel.Writer.Complete();
                }
                catch (Exception ex)
                {
                    channel.Writer.Complete(ex);
                }
            }, cancellationToken);

            // Consumer: Process batches from channel with optional parallelism
            if (_maxDegreeOfParallelism.HasValue && _maxDegreeOfParallelism > 1)
            {
                await ProcessBatchesInParallelAsync(channel.Reader, result, cancellationToken);
            }
            else
            {
                await foreach (var batch in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await ProcessBatchAsync(batch, result);
                }
            }

            return result;
        }

        private async Task ProcessBatchesInParallelAsync(ChannelReader<List<T>> reader, BatchResult<T> result, CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism.Value, _maxDegreeOfParallelism.Value);
            var tasks = new List<Task>();

            await foreach (var batch in reader.ReadAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ProcessBatchAsync(batch, result);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
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
