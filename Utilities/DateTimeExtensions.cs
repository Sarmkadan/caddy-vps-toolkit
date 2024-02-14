#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

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
        public static string ToRelativeTime(this DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour(s) ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day(s) ago";

            return dateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Format datetime as ISO 8601 string
        /// </summary>
        public static string ToIso8601(this DateTime dateTime)
        {
            return dateTime.ToString("o");
        }

        /// <summary>
        /// Format datetime as readable date string (e.g., "2026-05-03 14:30:45")
        /// </summary>
        public static string ToReadableString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Get the start of the current day
        /// </summary>
        public static DateTime StartOfDay(this DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// Get the end of the current day
        /// </summary>
        public static DateTime EndOfDay(this DateTime dateTime)
        {
            return dateTime.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Get the start of the week (Monday)
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dateTime)
        {
            int diff = (int)dateTime.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return dateTime.AddDays(-diff).StartOfDay();
        }

        /// <summary>
        /// Get the start of the month
        /// </summary>
        public static DateTime StartOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// Check if datetime is in the past
        /// </summary>
        public static bool IsPast(this DateTime dateTime)
        {
            return dateTime < DateTime.UtcNow;
        }

        /// <summary>
        /// Check if datetime is in the future
        /// </summary>
        public static bool IsFuture(this DateTime dateTime)
        {
            return dateTime > DateTime.UtcNow;
        }

        /// <summary>
        /// Check if datetime is today
        /// </summary>
        public static bool IsToday(this DateTime dateTime)
        {
            return dateTime.Date == DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Get the number of working days between two dates
        /// </summary>
        public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate)
        {
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
