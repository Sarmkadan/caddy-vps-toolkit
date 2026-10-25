#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Read-only query extension methods for <see cref="MaintenanceWindow"/>.
    /// </summary>
    public static class MaintenanceWindowQueryExtensions
    {
        /// <summary>
        /// Gets the days of the week on which the maintenance window applies, parsed from
        /// <see cref="MaintenanceWindow.DaysOfWeek"/>. Unknown entries are ignored, and a blank
        /// value means every day of the week (matching <see cref="MaintenanceWindow.IsInWindow"/>).
        /// </summary>
        /// <param name="window">The maintenance window to inspect.</param>
        /// <returns>The distinct allowed days, in the order they are listed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is null.</exception>
        public static IReadOnlyList<DayOfWeek> GetAllowedDays(this MaintenanceWindow window)
        {
            ArgumentNullException.ThrowIfNull(window);

            if (string.IsNullOrWhiteSpace(window.DaysOfWeek))
            {
                return Enum.GetValues<DayOfWeek>();
            }

            var days = new List<DayOfWeek>();
            foreach (var entry in window.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<DayOfWeek>(entry, true, out var day) && !days.Contains(day))
                {
                    days.Add(day);
                }
            }

            return days;
        }

        /// <summary>
        /// Gets the length of the maintenance window from <see cref="MaintenanceWindow.StartTime"/> to
        /// <see cref="MaintenanceWindow.EndTime"/>. Windows that cross midnight are measured across the day boundary.
        /// </summary>
        /// <param name="window">The maintenance window to measure.</param>
        /// <returns>The window length, or <c>null</c> if either time cannot be parsed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is null.</exception>
        public static TimeSpan? GetDuration(this MaintenanceWindow window)
        {
            ArgumentNullException.ThrowIfNull(window);

            if (!TimeSpan.TryParse(window.StartTime, out var start) || !TimeSpan.TryParse(window.EndTime, out var end))
            {
                return null;
            }

            var duration = end - start;
            return duration < TimeSpan.Zero ? duration + TimeSpan.FromDays(1) : duration;
        }

        /// <summary>
        /// Determines whether the given moment falls inside the maintenance window. The check is the
        /// same as <see cref="MaintenanceWindow.IsInWindow"/> but evaluated for <paramref name="moment"/>
        /// instead of the current time. Disabled windows never contain a moment. Pass UTC values, since
        /// <see cref="MaintenanceWindow.IsInWindow"/> evaluates against UTC.
        /// </summary>
        /// <param name="window">The maintenance window to check.</param>
        /// <param name="moment">The moment to test.</param>
        /// <returns><c>true</c> if the window is enabled, the time of day is within its range and the day is allowed; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is null.</exception>
        public static bool ContainsTime(this MaintenanceWindow window, DateTime moment)
        {
            ArgumentNullException.ThrowIfNull(window);

            if (!window.IsEnabled)
            {
                return false;
            }

            if (!TimeSpan.TryParse(window.StartTime, out var start) || !TimeSpan.TryParse(window.EndTime, out var end))
            {
                return false;
            }

            var time = moment.TimeOfDay;
            var isInTimeRange = end < start
                ? time >= start || time <= end
                : time >= start && time <= end;

            return isInTimeRange && window.GetAllowedDays().Contains(moment.DayOfWeek);
        }
    }
}
