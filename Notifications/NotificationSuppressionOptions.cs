#nullable enable

using System;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Configuration options for duplicate notification suppression and delivery resilience.
    /// </summary>
    public sealed class NotificationSuppressionOptions
    {
        /// <summary>
        /// Gets or sets whether duplicate notification suppression is enabled.
        /// When true, notifications with the same key within the suppression window will be ignored.
        /// Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the suppression window in seconds.
        /// Notifications with the same key within this time window will be suppressed.
        /// Defaults to 300 seconds (5 minutes).
        /// </summary>
        public int SuppressionWindowSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the maximum number of recent notifications to track for suppression.
        /// Older notifications beyond this count are automatically removed to prevent memory leaks.
        /// Defaults to 1000.
        /// </summary>
        public int MaxTrackedNotifications { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether retry with exponential backoff is enabled for transient failures.
        /// When true, failed notification deliveries will be retried with increasing delays.
        /// Defaults to true.
        /// </summary>
        public bool RetryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for a single notification delivery.
        /// Defaults to 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay in milliseconds for the first retry attempt.
        /// Defaults to 100ms.
        /// </summary>
        public int InitialRetryDelayMs { get; set; } = 100;

        /// <summary>
        /// Gets or sets the backoff multiplier between retry attempts.
        /// Each retry will wait initialDelay * multiplier^(attempt-1) milliseconds.
        /// Defaults to 2.0 (exponential backoff).
        /// </summary>
        public double RetryBackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the maximum delay in milliseconds between retry attempts.
        /// Prevents excessively long delays for persistent failures.
        /// Defaults to 10000ms (10 seconds).
        /// </summary>
        public int MaxRetryDelayMs { get; set; } = 10000;

        /// <summary>
        /// Gets or sets whether circuit breaker protection is enabled per provider.
        /// When true, providers that fail repeatedly will be temporarily disabled.
        /// Defaults to true.
        /// </summary>
        public bool CircuitBreakerEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of consecutive failures before opening the circuit breaker.
        /// Defaults to 5.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time in seconds to wait before attempting recovery when circuit is open.
        /// Defaults to 60 seconds.
        /// </summary>
        public int CircuitBreakerRecoveryTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets whether dead-letter queue is enabled for failed notifications.
        /// When true, failed notifications will be stored for monitoring and analysis.
        /// Defaults to true.
        /// </summary>
        public bool DeadLetterEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of failed notifications to keep in memory.
        /// Older entries are automatically removed when this limit is reached.
        /// Defaults to 100.
        /// </summary>
        public int MaxDeadLetterEntries { get; set; } = 100;
    }
}