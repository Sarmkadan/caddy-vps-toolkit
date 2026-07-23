#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Interface for storing notification state to enable duplicate suppression and escalation detection.
    /// Implementations can provide in-memory or persistent storage.
    /// </summary>
    public interface INotificationStateStore : IDisposable
    {
        /// <summary>
        /// Record a notification and check if it should be suppressed.
        /// </summary>
        /// <param name="key">The suppression key for the notification</param>
        /// <param name="severity">The notification priority/severity</param>
        /// <param name="suppressionWindow">Time window in seconds for suppressing duplicates</param>
        /// <returns>Tuple of (shouldSuppress, shouldEscalate)</returns>
        (bool ShouldSuppress, bool ShouldEscalate) RecordNotification(string key, NotificationPriority severity, int suppressionWindow);

        /// <summary>
        /// Clear all tracked notifications (for testing or manual reset).
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// In-memory implementation of notification state store.
    /// </summary>
    public sealed class InMemoryNotificationStateStore : INotificationStateStore
    {
        private readonly Dictionary<string, (DateTime LastSent, NotificationPriority Severity)> _notificationState = new();
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>
        /// Record a notification and check if it should be suppressed.
        /// </summary>
        /// <param name="key">The suppression key for the notification</param>
        /// <param name="severity">The notification priority/severity</param>
        /// <param name="suppressionWindow">Time window in seconds for suppressing duplicates</param>
        /// <returns>Tuple of (shouldSuppress, shouldEscalate)</returns>
        public (bool ShouldSuppress, bool ShouldEscalate) RecordNotification(string key, NotificationPriority severity, int suppressionWindow)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InMemoryNotificationStateStore));
            }

            lock (_lock)
            {
                // Check if we have a previous notification with this key
                if (_notificationState.TryGetValue(key, out var previousState))
                {
                    var timeSinceLastSent = DateTime.UtcNow - previousState.LastSent;
                    var isWithinWindow = timeSinceLastSent.TotalSeconds < suppressionWindow;

                    // Escalation detection: always send if severity increased
                    bool shouldEscalate = severity > previousState.Severity;

                    if (isWithinWindow && !shouldEscalate)
                    {
                        // Within suppression window and severity didn't increase - suppress
                        return (true, false);
                    }

                    // Either outside window or severity increased - update state and send
                    _notificationState[key] = (DateTime.UtcNow, severity);
                    return (false, shouldEscalate);
                }

                // First time seeing this notification - record it and send
                _notificationState[key] = (DateTime.UtcNow, severity);
                return (false, false);
            }
        }

        /// <summary>
        /// Clear all tracked notifications.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _notificationState.Clear();
            }
        }

        /// <summary>
        /// Dispose the state store and release resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~InMemoryNotificationStateStore()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Factory for creating notification state stores.
    /// </summary>
    public interface INotificationStateStoreFactory
    {
        /// <summary>
        /// Create a new notification state store.
        /// </summary>
        INotificationStateStore Create();
    }

    /// <summary>
    /// Factory for creating in-memory notification state stores.
    /// </summary>
    public sealed class InMemoryNotificationStateStoreFactory : INotificationStateStoreFactory
    {
        /// <summary>
        /// Create a new notification state store.
        /// </summary>
        public INotificationStateStore Create()
        {
            return new InMemoryNotificationStateStore();
        }
    }
}
