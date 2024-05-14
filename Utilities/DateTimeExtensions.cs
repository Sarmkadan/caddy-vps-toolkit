#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Globalization;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension methods for DateTime operations.
    /// Provides human-readable time formatting and common datetime calculations.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Get human-readable time difference (e.g., "2 hours ago")
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static string ToRelativeTime(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);

            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";

            return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format datetime as ISO 8601 string
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static string ToIso8601(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime.ToString("o", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format datetime as readable date string (e.g., "2026-05-03 14:30:45")
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static string ToReadableString(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get the start of the current day
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static DateTime StartOfDay(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime.Date;
        }

        /// <summary>
        /// Get the end of the current day
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static DateTime EndOfDay(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Get the start of the week (Monday)
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static DateTime StartOfWeek(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            int diff = (int)dateTime.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return dateTime.AddDays(-diff).StartOfDay();
        }

        /// <summary>
        /// Get the start of the month
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static DateTime StartOfMonth(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// Check if datetime is in the past
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static bool IsPast(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime < DateTime.UtcNow;
        }

        /// <summary>
        /// Check if datetime is in the future
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static bool IsFuture(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime > DateTime.UtcNow;
        }

        /// <summary>
        /// Check if datetime is today
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if dateTime is null.</exception>
        public static bool IsToday(this DateTime dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return dateTime.Date == DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Get the number of working days between two dates
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>The number of working days (Monday-Friday) between the dates.</returns>
        /// <exception cref="ArgumentNullException">Thrown if startDate or endDate is null.</exception>
        public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate)
        {
            ArgumentNullException.ThrowIfNull(startDate);
            ArgumentNullException.ThrowIfNull(endDate);

            // Ensure startDate is before or equal to endDate for correct calculation
            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            int count = 0;
            DateTime current = startDate;

            while (current <= endDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                    count++;
                current = current.AddDays(1);
            }

            return count;
        }

        /// <summary>
        /// Get human-readable duration between two times (e.g., "2h 30m")
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if timeSpan is not applicable, but TimeSpan is a struct so this is defensive.</exception>
        public static string ToDurationString(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{(int)timeSpan.TotalSeconds}s";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
        }
    }
}