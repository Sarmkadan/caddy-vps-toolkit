#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Notification priority levels
    /// </summary>
    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Notification object with metadata
    /// </summary>
    public sealed class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Interface for notification providers (email, SMS, Slack, etc.)
    /// </summary>
    public interface INotificationProvider
    {
        Task<bool> SendAsync(Notification notification);
        string ProviderName { get; }
    }

    /// <summary>
    /// Dead-letter entry for failed notifications
    /// </summary>
    public sealed class DeadLetterEntry
    {
        public string NotificationId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; } = string.Empty;
        public int AttemptCount { get; set; } = 1;
    }

    /// <summary>
    /// Service for sending notifications through multiple providers.
    /// Supports retry with exponential backoff, circuit breaker protection,
    /// duplicate suppression, and dead-letter queue for failed deliveries.
    /// </summary>
    public sealed class NotificationService
    {
        private readonly Dictionary<string, INotificationProvider> _providers = new();
        private readonly ILogger _logger;
        private readonly NotificationSuppressionOptions _suppressionOptions;
        private readonly Dictionary<string, DateTime> _recentNotifications = new();
        private readonly object _suppressionLock = new();
        private readonly List<DeadLetterEntry> _deadLetterQueue = new();
        private readonly object _deadLetterLock = new();
        private readonly IRetryPolicy _retryPolicy;
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;

        public NotificationService(
            ILogger logger,
            NotificationSuppressionOptions? suppressionOptions = null,
            IRetryPolicy? retryPolicy = null,
            ICircuitBreakerFactory? circuitBreakerFactory = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _suppressionOptions = suppressionOptions ?? new NotificationSuppressionOptions();
            _retryPolicy = retryPolicy ?? new NoRetryPolicy();
            _circuitBreakerFactory = circuitBreakerFactory ?? new NoOpCircuitBreakerFactory();
        }

        /// <summary>
        /// Register a notification provider with the service.
        /// </summary>
        /// <param name="provider">The notification provider to register</param>
        /// <exception cref="ArgumentNullException">Thrown when provider is null</exception>
        public void Register(INotificationProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            _providers[provider.ProviderName] = provider;
        }

        /// <summary>
        /// Generate a suppression key for a notification based on its content.
        /// This key is used to detect duplicate notifications.
        /// </summary>
        /// <param name="notification">The notification to generate a key for</param>
        /// <returns>A string key that uniquely identifies this notification type</returns>
        private string GenerateSuppressionKey(Notification notification)
        {
            // Use a combination of title, message, and priority as the suppression key
            // This ensures that different notifications have different keys
            return $"{notification.Title}|{notification.Message}|{notification.Priority}";
        }

        /// <summary>
        /// Check if a notification should be suppressed due to being a duplicate.
        /// </summary>
        /// <param name="key">The suppression key for the notification</param>
        /// <returns>True if the notification should be suppressed, false otherwise</returns>
        private bool ShouldSuppressNotification(string key)
        {
            if (!_suppressionOptions.Enabled)
            {
                return false;
            }

            lock (_suppressionLock)
            {
                // Check if we've seen this notification recently
                if (_recentNotifications.TryGetValue(key, out var lastSentTime))
                {
                    var timeSinceLastSent = DateTime.UtcNow - lastSentTime;
                    if (timeSinceLastSent.TotalSeconds < _suppressionOptions.SuppressionWindowSeconds)
                    {
                        return true; // Suppress duplicate
                    }
                }

                // Add/update the notification in our tracking dictionary
                _recentNotifications[key] = DateTime.UtcNow;

                // Clean up old entries to prevent memory leaks
                if (_recentNotifications.Count > _suppressionOptions.MaxTrackedNotifications)
                {
                    // Remove the oldest entries
                    var oldestKeys = new List<string>();
                    foreach (var entry in _recentNotifications)
                    {
                        oldestKeys.Add(entry.Key);
                        if (oldestKeys.Count >= 100) // Remove in batches
                            break;
                    }

                    foreach (var oldestKey in oldestKeys)
                    {
                        _recentNotifications.Remove(oldestKey);
                    }
                }

                return false; // Don't suppress
            }
        }

        /// <summary>
        /// Add a failed notification to the dead-letter queue for monitoring.
        /// </summary>
        /// <param name="notification">The notification that failed</param>
        /// <param name="providerName">Name of the provider that failed</param>
        /// <param name="errorMessage">Error message describing the failure</param>
        private void AddToDeadLetterQueue(Notification notification, string providerName, string errorMessage)
        {
            if (!_suppressionOptions.DeadLetterEnabled)
            {
                return;
            }

            lock (_deadLetterLock)
            {
                var entry = new DeadLetterEntry
                {
                    NotificationId = notification.Id,
                    ProviderName = providerName,
                    Title = notification.Title,
                    Message = notification.Message,
                    Priority = notification.Priority,
                    ErrorMessage = errorMessage,
                    FailedAt = DateTime.UtcNow
                };

                _deadLetterQueue.Add(entry);

                // Clean up old entries if we exceed the limit
                if (_deadLetterQueue.Count > _suppressionOptions.MaxDeadLetterEntries)
                {
                    _deadLetterQueue.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Send a notification to a single provider with retry and circuit breaker protection.
        /// </summary>
        /// <param name="provider">The notification provider to use</param>
        /// <param name="notification">The notification to send</param>
        /// <returns>True if the notification was successfully delivered, false otherwise</returns>
        private async Task<bool> SendToProviderWithResilienceAsync(INotificationProvider provider, Notification notification)
        {
            ICircuitBreaker circuitBreaker = null;

            if (_suppressionOptions.CircuitBreakerEnabled)
            {
                circuitBreaker = _circuitBreakerFactory.Create(provider.ProviderName);
            }
            else
            {
                circuitBreaker = new NoOpCircuitBreaker();
            }

            try
            {
                // Use circuit breaker to execute the send operation
                var success = await circuitBreaker.ExecuteAsync(async () =>
                {
                    // Apply retry policy if enabled
                    if (_suppressionOptions.RetryEnabled)
                    {
                        return await _retryPolicy.ExecuteAsync(async () =>
                        {
                            var result = await provider.SendAsync(notification);
                            return result;
                        });
                    }
                    else
                    {
                        return await provider.SendAsync(notification);
                    }
                });

                if (success)
                {
                    circuitBreaker.RecordSuccess();
                    return true;
                }
                else
                {
                    circuitBreaker.RecordFailure();
                    return false;
                }
            }
            catch (Exception ex)
            {
                circuitBreaker.RecordFailure();
                await _logger.LogErrorAsync($"Error sending notification via {provider.ProviderName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a notification through all registered providers.
        /// </summary>
        /// <param name="notification">The notification to send</param>
        /// <returns>True if at least one provider succeeded, false otherwise</returns>
        public async Task<bool> SendAsync(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);

            // Generate suppression key for duplicate detection
            var suppressionKey = GenerateSuppressionKey(notification);

            // Check if this notification should be suppressed
            if (ShouldSuppressNotification(suppressionKey))
            {
                await _logger.LogInfoAsync($"Suppressed duplicate notification: {notification.Title}");
                return true; // Return true since we intentionally suppressed it (not a failure)
            }

            var results = new List<bool>();
            var successfulProviders = new List<string>();
            var failedProviders = new List<(string Name, string Error)>();

            foreach (var provider in _providers.Values)
            {
                try
                {
                    var success = await SendToProviderWithResilienceAsync(provider, notification);
                    results.Add(success);

                    var status = success ? "succeeded" : "failed";
                    await _logger.LogInfoAsync($"Notification sent via {provider.ProviderName} ({status})");

                    if (success)
                    {
                        successfulProviders.Add(provider.ProviderName);
                    }
                    else
                    {
                        failedProviders.Add((provider.ProviderName, "Delivery failed"));
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error sending notification via {provider.ProviderName}: {ex.Message}");
                    results.Add(false);
                    failedProviders.Add((provider.ProviderName, ex.Message));
                }
            }

            // Log dead-letter entries for failed deliveries
            if (failedProviders.Count > 0 && _suppressionOptions.DeadLetterEnabled)
            {
                foreach (var (providerName, error) in failedProviders)
                {
                    AddToDeadLetterQueue(notification, providerName, error);
                    await _logger.LogWarningAsync($"Failed to deliver notification to {providerName}: {error}");
                }
            }

            // Return true if at least one provider succeeded
            return results.Count > 0 && results.TrueForAll(r => r);
        }

        /// <summary>
        /// Send a notification with title and message.
        /// </summary>
        /// <param name="title">The notification title</param>
        /// <param name="message">The notification message</param>
        /// <param name="priority">The notification priority (default: Normal)</param>
        /// <returns>True if at least one provider succeeded, false otherwise</returns>
        public async Task<bool> SendAsync(string title, string message, NotificationPriority priority = NotificationPriority.Normal)
        {
            return await SendAsync(new Notification
            {
                Title = title,
                Message = message,
                Priority = priority
            });
        }

        /// <summary>
        /// Get all dead-letter entries for monitoring failed notifications.
        /// </summary>
        /// <returns>List of failed notification deliveries</returns>
        public List<DeadLetterEntry> GetDeadLetterEntries()
        {
            lock (_deadLetterLock)
            {
                return new List<DeadLetterEntry>(_deadLetterQueue);
            }
        }

        /// <summary>
        /// Clear all dead-letter entries.
        /// </summary>
        public void ClearDeadLetterQueue()
        {
            lock (_deadLetterLock)
            {
                _deadLetterQueue.Clear();
            }
        }
    }

    /// <summary>
    /// Console notification provider for testing/development
    /// </summary>
    public sealed class ConsoleNotificationProvider : INotificationProvider
    {
        public string ProviderName => "Console";

        public async Task<bool> SendAsync(Notification notification)
        {
            Console.WriteLine($"[{notification.Priority}] {notification.Title}");
            Console.WriteLine($" {notification.Message}");
            return await Task.FromResult(true);
        }
    }
}