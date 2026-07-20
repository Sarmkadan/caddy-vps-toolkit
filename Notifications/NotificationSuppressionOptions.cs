#nullable enable

using System;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Configuration options for duplicate notification suppression.
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
    }
}