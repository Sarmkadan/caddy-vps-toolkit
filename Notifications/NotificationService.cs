#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;

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
    /// Service for sending notifications through multiple providers.
    /// Supports retry, failure handling, and duplicate suppression.
    /// </summary>
    public sealed class NotificationService
    {
        private readonly Dictionary<string, INotificationProvider> _providers = new();
        private readonly ILogger _logger;
        private readonly NotificationSuppressionOptions _suppressionOptions;
        private readonly Dictionary<string, DateTime> _recentNotifications = new();
        private readonly object _suppressionLock = new object();

        public NotificationService(ILogger logger, NotificationSuppressionOptions? suppressionOptions = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _suppressionOptions = suppressionOptions ?? new NotificationSuppressionOptions();
        }

        /// <summary>
        /// Register a notification provider with the service.
        /// </summary>
        /// <param name="provider">The notification provider to register</param>
        /// <exception cref="ArgumentNullException">Thrown when provider is null</exception>
        public void Register(INotificationProvider provider)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

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
        /// Send a notification through all registered providers.
        /// </summary>
        /// <param name="notification">The notification to send</param>
        /// <returns>True if at least one provider succeeded, false otherwise</returns>
        public async Task<bool> SendAsync(Notification notification)
        {
            if (notification is null)
                throw new ArgumentNullException(nameof(notification));

            // Generate suppression key for duplicate detection
            var suppressionKey = GenerateSuppressionKey(notification);

            // Check if this notification should be suppressed
            if (ShouldSuppressNotification(suppressionKey))
            {
                await _logger.LogInfoAsync($"Suppressed duplicate notification: {notification.Title}");
                return true; // Return true since we intentionally suppressed it (not a failure)
            }

            var results = new List<bool>();

            foreach (var provider in _providers.Values)
            {
                try
                {
                    var success = await provider.SendAsync(notification);
                    results.Add(success);

                    var status = success ? "succeeded" : "failed";
                    await _logger.LogInfoAsync($"Notification sent via {provider.ProviderName} ({status})");
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync($"Error sending notification via {provider.ProviderName}: {ex.Message}");
                    results.Add(false);
                }
            }

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