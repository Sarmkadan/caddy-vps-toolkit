// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Thrown when all upstreams in a pool are unhealthy and FailFast policy is configured
// =============================================================================

#nullable enable

using System;

namespace CaddyVpsToolkit.Core.Exceptions
{
    /// <summary>
    /// Thrown when all upstreams in a pool are unhealthy and the configured policy is FailFast.
    /// Indicates that no upstream server is available to handle the request.
    /// </summary>
    public sealed class NoHealthyUpstreamException : Exception
    {
        /// <summary>
        /// Gets the ID of the pool that has no healthy upstreams.
        /// </summary>
        public string PoolId { get; }

        /// <summary>
        /// Gets the collection of all upstream server IDs in the pool.
        /// </summary>
        public IReadOnlyCollection<string> UpstreamIds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoHealthyUpstreamException"/> class.
        /// </summary>
        /// <param name="poolId">The ID of the pool with no healthy upstreams.</param>
        /// <param name="upstreamIds">The collection of all upstream server IDs in the pool.</param>
        public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds)
            : base($"No healthy upstreams available in pool '{poolId}'. All {upstreamIds.Count} upstreams are unhealthy.")
        {
            PoolId = poolId ?? throw new ArgumentNullException(nameof(poolId));
            UpstreamIds = upstreamIds ?? throw new ArgumentNullException(nameof(upstreamIds));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoHealthyUpstreamException"/> class with a custom message.
        /// </summary>
        /// <param name="poolId">The ID of the pool with no healthy upstreams.</param>
        /// <param name="upstreamIds">The collection of all upstream server IDs in the pool.</param>
        /// <param name="message">The custom error message.</param>
        public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds, string message)
            : base(message)
        {
            PoolId = poolId ?? throw new ArgumentNullException(nameof(poolId));
            UpstreamIds = upstreamIds ?? throw new ArgumentNullException(nameof(upstreamIds));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoHealthyUpstreamException"/> class with a custom message and inner exception.
        /// </summary>
        /// <param name="poolId">The ID of the pool with no healthy upstreams.</param>
        /// <param name="upstreamIds">The collection of all upstream server IDs in the pool.</param>
        /// <param name="message">The custom error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public NoHealthyUpstreamException(string poolId, IReadOnlyCollection<string> upstreamIds, string message, Exception innerException)
            : base(message, innerException)
        {
            PoolId = poolId ?? throw new ArgumentNullException(nameof(poolId));
            UpstreamIds = upstreamIds ?? throw new ArgumentNullException(nameof(upstreamIds));
        }
    }
}
