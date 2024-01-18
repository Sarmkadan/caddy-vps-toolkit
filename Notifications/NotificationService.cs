// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Message { get; set; }
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
    /// Supports retry and failure handling.
    /// </summary>
    public class NotificationService
    {
        private readonly Dictionary<string, INotificationProvider> _providers = new();
        private readonly ILogger _logger;

        public NotificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Register(INotificationProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _providers[provider.ProviderName] = provider;
        }

        public async Task<bool> SendAsync(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

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
    public class ConsoleNotificationProvider : INotificationProvider
    {
        public string ProviderName => "Console";

        public async Task<bool> SendAsync(Notification notification)
        {
            Console.WriteLine($"[{notification.Priority}] {notification.Title}");
            Console.WriteLine($"  {notification.Message}");
            return await Task.FromResult(true);
        }
    }
}
