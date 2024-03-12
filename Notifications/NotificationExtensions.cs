using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Extension methods that add convenient functionality to <see cref="Notification"/>.
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// Adds or updates a metadata entry and returns the notification for fluent chaining.
        /// </summary>
        public static Notification AddMetadata(this Notification notification, string key, string value)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            if (key == null) throw new ArgumentNullException(nameof(key));

            // Ensure the dictionary is instantiated.
            if (notification.Metadata == null)
                notification.Metadata = new Dictionary<string, string>();

            notification.Metadata[key] = value ?? string.Empty;
            return notification;
        }

        /// <summary>
        /// Removes a metadata entry if it exists and returns the notification for fluent chaining.
        /// </summary>
        public static Notification RemoveMetadata(this Notification notification, string key)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            if (key == null) throw new ArgumentNullException(nameof(key));

            notification.Metadata?.Remove(key);
            return notification;
        }

        /// <summary>
        /// Retrieves a metadata value by key. Returns <c>null</c> if the key does not exist.
        /// </summary>
        public static string? GetMetadataValue(this Notification notification, string key)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            if (key == null) throw new ArgumentNullException(nameof(key));

            if (notification.Metadata != null && notification.Metadata.TryGetValue(key, out var value))
                return value;

            return null;
        }

        /// <summary>
        /// Returns a concise, human‑readable summary of the notification.
        /// </summary>
        public static string ToSummaryString(this Notification notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));

            return $"[Id:{notification.Id}] \"{notification.Title}\" " +
                   $"(Priority:{notification.Priority}, Created:{notification.CreatedAt:u})";
        }
    }
}
