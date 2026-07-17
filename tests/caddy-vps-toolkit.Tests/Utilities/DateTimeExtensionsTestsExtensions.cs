using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/> to provide date/time utilities.
    /// </summary>
    public static class DateTimeExtensionsTestsExtensions
    {
        /// <summary>
        /// Converts the date to a Unix timestamp (seconds since epoch).
        /// </summary>
        /// <param name="dateTime">The date to convert.</param>
        /// <returns>The Unix timestamp in seconds.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the date is before the Unix epoch.</exception>
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(dateTime, DateTime.UnixEpoch);
            return (long)(dateTime - DateTime.UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Gets the first day of the month for the given date.
        /// </summary>
        /// <param name="dateTime">The date to get the first day from.</param>
        /// <returns>A new DateTime representing the first day of the month.</returns>
        public static DateTime FirstDayOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, dateTime.Kind);
        }

        /// <summary>
        /// Gets the last day of the month for the given date.
        /// </summary>
        /// <param name="dateTime">The date to get the last day from.</param>
        /// <returns>A new DateTime representing the last day of the month.</returns>
        public static DateTime LastDayOfMonth(this DateTime dateTime)
        {
            var firstDayOfNextMonth = dateTime.FirstDayOfMonth().AddMonths(1);
            return firstDayOfNextMonth.AddDays(-1);
        }

        /// <summary>
        /// Determines whether the given date is a weekend day (Saturday or Sunday).
        /// </summary>
        /// <param name="dateTime">The date to check.</param>
        /// <returns>True if the date is a weekend day; otherwise, false.</returns>
        public static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Gets all dates between two dates (inclusive).
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>An enumerable of dates between the start and end dates.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when start date is after end date.</exception>
        public static IEnumerable<DateTime> GetDatesBetween(this DateTime startDate, DateTime endDate)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(startDate, endDate);

            var date = startDate;
            while (date <= endDate)
            {
                yield return date;
                date = date.AddDays(1);
            }
        }

        /// <summary>
        /// Calculates the difference in business days between two dates.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>The number of business days between the dates.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when start date is after end date.</exception>
        public static int BusinessDaysBetween(this DateTime startDate, DateTime endDate)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(startDate, endDate);

            var businessDays = 0;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (!currentDate.IsWeekend())
                {
                    businessDays++;
                }

                currentDate = currentDate.AddDays(1);
            }

            return businessDays;
        }
    }
}