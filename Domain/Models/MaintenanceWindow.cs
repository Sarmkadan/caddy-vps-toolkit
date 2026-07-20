#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Represents a maintenance window during which failing health checks should log but not trigger alerts or state transitions.
    /// </summary>
    public sealed class MaintenanceWindow
    {
        /// <summary>
        /// Gets or sets the start time of the maintenance window in HH:mm format (24-hour clock).
        /// </summary>
        [Required]
        [RegularExpression(@"^([01]?[0-9]|2[0-3]):([0-5][0-9])$", ErrorMessage = "Start time must be in HH:mm format (24-hour clock)")]
        public string StartTime { get; set; } = "02:00";

        /// <summary>
        /// Gets or sets the end time of the maintenance window in HH:mm format (24-hour clock).
        /// </summary>
        [Required]
        [RegularExpression(@"^([01]?[0-9]|2[0-3]):([0-5][0-9])$", ErrorMessage = "End time must be in HH:mm format (24-hour clock)")]
        public string EndTime { get; set; } = "04:00";

        /// <summary>
        /// Gets or sets the days of the week when this maintenance window applies.
        /// Comma-separated list of day names (e.g., "Monday,Tuesday,Wednesday").
        /// </summary>
        public string DaysOfWeek { get; set; } = "Monday,Tuesday,Wednesday,Thursday,Friday";

        /// <summary>
        /// Gets or sets whether the maintenance window is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional description for the maintenance window.
        /// </summary>
        public string Description { get; set; } = "Regular maintenance window";

        /// <summary>
        /// Determines if the current time falls within this maintenance window.
        /// </summary>
        /// <returns>True if current time is within the maintenance window, otherwise false.</returns>
        public bool IsInWindow()
        {
            var now = DateTime.UtcNow;
            var currentTime = now.TimeOfDay;
            var currentDay = now.DayOfWeek;

            // Parse start and end times
            if (!TimeSpan.TryParse(StartTime, out var start) || !TimeSpan.TryParse(EndTime, out var end))
            {
                return false;
            }

            // Check if current time is within the time range
            bool isInTimeRange = currentTime >= start && currentTime <= end;

            // If end time crosses midnight (e.g., 22:00 to 02:00)
            if (end < start)
            {
                isInTimeRange = currentTime >= start || currentTime <= end;
            }

            // Check if current day is in the allowed days
            if (string.IsNullOrWhiteSpace(DaysOfWeek))
            {
                return isInTimeRange;
            }

            var allowedDays = DaysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var currentDayName = currentDay.ToString();
            bool isAllowedDay = Array.Exists(allowedDays, day => string.Equals(day.Trim(), currentDayName, StringComparison.OrdinalIgnoreCase));

            return isInTimeRange && isAllowedDay && IsEnabled;
        }

        /// <summary>
        /// Validates the maintenance window configuration.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (!TimeSpan.TryParse(StartTime, out _))
            {
                throw new ValidationException($"Invalid StartTime format: {StartTime}. Expected HH:mm format (24-hour clock).");
            }

            if (!TimeSpan.TryParse(EndTime, out _))
            {
                throw new ValidationException($"Invalid EndTime format: {EndTime}. Expected HH:mm format (24-hour clock).");
            }

            var start = TimeSpan.Parse(StartTime);
            var end = TimeSpan.Parse(EndTime);

            // Validate time range
            if (start == end)
            {
                throw new ValidationException("StartTime and EndTime cannot be the same.");
            }
        }
    }
}