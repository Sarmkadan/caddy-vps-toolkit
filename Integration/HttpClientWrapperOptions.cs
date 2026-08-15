#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Integration
{
    /// <summary>
    /// Strongly‑typed options for <see cref="HttpClientWrapper"/>.
    /// These values can be bound from configuration (e.g., appsettings.json) and
    /// injected via DI. Keeping them in a dedicated class makes the wrapper
    /// configurable without hard‑coded literals.
    /// </summary>
    public sealed class HttpClientWrapperOptions
    {
        /// <summary>
        /// Timeout for HTTP requests in milliseconds.
        /// Default matches the previous hard‑coded value of 30 000 ms.
        /// </summary>
        public int TimeoutMs { get; set; } = 30_000;

        /// <summary>
        /// Retry policy to apply to HTTP operations.
        /// If <c>null</c>, a <see cref="NoRetryPolicy"/> will be used.
        /// </summary>
        public IRetryPolicy? RetryPolicy { get; set; }
    }
}
