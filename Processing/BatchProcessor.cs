#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
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
        /// Process items in batches
        /// </summary>
        public async Task ProcessAsync(IEnumerable<T> items)
        {
            var batch = new List<T>(_batchSize);

            foreach (var item in items)
            {
                batch.Add(item);

                if (batch.Count >= _batchSize)
                {
                    await _processFunction(batch);
                    batch.Clear();
                }
            }

            // Process remaining items
            if (batch.Count > 0)
                await _processFunction(batch);
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
        /// Process items with error handling
        /// </summary>
        public async Task<BatchResult<T>> ProcessAsync(IEnumerable<T> items)
        {
            var result = new BatchResult<T>();
            var batch = new List<T>(_batchSize);

            foreach (var item in items)
            {
                batch.Add(item);

                if (batch.Count >= _batchSize)
                {
                    await ProcessBatchAsync(batch, result);
                    batch.Clear();
                }
            }

            // Process remaining
            if (batch.Count > 0)
                await ProcessBatchAsync(batch, result);

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
