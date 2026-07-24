#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Core
{
    /// <summary>
    /// Exception thrown when collection validation or processing fails.
    /// Provides structured information about the failure including the offending item and its index.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public sealed class CollectionValidationException<T> : CaddyVpsException
    {
        /// <summary>
        /// Gets the collection of failed items with their error details.
        /// </summary>
        public IReadOnlyList<(T Item, Exception Error, int? Index)> FailedItems { get; }

        /// <summary>
        /// Gets the total number of items that were processed or attempted.
        /// </summary>
        public int TotalItems { get; }

        /// <summary>
        /// Gets the number of successful items.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets the number of failed items.
        /// </summary>
        public int FailureCount => FailedItems.Count;

        /// <summary>
        /// Gets a value indicating whether all items were processed successfully.
        /// </summary>
        public bool AllSucceeded => FailureCount == 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionValidationException{T}"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="failedItems">The collection of failed items with their errors and indices.</param>
        /// <param name="totalItems">The total number of items that were processed.</param>
        /// <param name="successCount">The number of successfully processed items.</param>
        public CollectionValidationException(
            string message,
            IReadOnlyList<(T Item, Exception Error, int? Index)> failedItems,
            int totalItems,
            int successCount)
            : base(message, "COLLECTION_VALIDATION_ERROR", new { FailedCount = failedItems.Count, TotalItems = totalItems })
        {
            FailedItems = failedItems ?? throw new ArgumentNullException(nameof(failedItems));
            TotalItems = totalItems;
            SuccessCount = successCount;
        }

        /// <summary>
        /// Creates a formatted error message from the exception.
        /// </summary>
        /// <returns>A formatted error message.</returns>
        public override string ToString()
        {
            var message = base.ToString();
            if (FailedItems.Count > 0)
            {
                message += $"\nFailed items ({FailedItems.Count}/{TotalItems}):";
                foreach (var (item, error, index) in FailedItems)
                {
                    var indexInfo = index.HasValue ? $" at index [{index.Value}]" : string.Empty;
                    message += $"\n- Item: {item}, Error: {error.Message}{indexInfo}";
                }
            }
            return message;
        }
    }
}