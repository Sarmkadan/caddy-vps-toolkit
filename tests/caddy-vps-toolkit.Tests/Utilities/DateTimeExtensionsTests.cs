using System;
using Xunit;
using FluentAssertions;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Tests for the <see cref="CaddyVpsToolkit.Utilities.DateTimeExtensions"/> utility methods.
    /// </summary>
    public class DateTimeExtensionsTests
    {
        /// <summary>
        /// Verifies that <c>ToRelativeTime</c> returns "just now" for times less than a minute ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnJustNow_WhenLessThanMinute()
        {
            var dateTime = DateTime.UtcNow.AddSeconds(-30);
            dateTime.ToRelativeTime().Should().Be("just now");
        }

        /// <summary>
        /// Verifies that <c>ToRelativeTime</c> returns the correct minutes ago string for times less than an hour ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnMinutesAgo_WhenLessThanHour()
        {
            var dateTime = DateTime.UtcNow.AddMinutes(-10);
            dateTime.ToRelativeTime().Should().Be("10 minutes ago");
        }

        /// <summary>
        /// Verifies that <c>ToRelativeTime</c> returns the correct hours ago string for times less than a day ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnHoursAgo_WhenLessThanDay()
        {
            var dateTime = DateTime.UtcNow.AddHours(-5);
            dateTime.ToRelativeTime().Should().Be("5 hours ago");
        }

        /// <summary>
        /// Verifies that <c>ToIso8601</c> returns a string containing the ISO 8601 representation of the date and time.
        /// </summary>
        [Fact]
        public void ToIso8601_ShouldReturnCorrectFormat()
        {
            var dateTime = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
            dateTime.ToIso8601().Should().Contain("2026-06-26T12:00:00.0000000Z");
        }

        /// <summary>
        /// Verifies that <c>StartOfDay</c> returns a <see cref="DateTime"/> set to midnight of the same day.
        /// </summary>
        [Fact]
        public void StartOfDay_ShouldReturnCorrectTime()
        {
            var dateTime = new DateTime(2026, 6, 26, 15, 30, 0);
            dateTime.StartOfDay().Should().Be(new DateTime(2026, 6, 26, 0, 0, 0));
        }

        /// <summary>
        /// Verifies that <c>IsPast</c> returns <c>true</c> when the date is in the past.
        /// </summary>
        [Fact]
        public void IsPast_ShouldReturnTrue_WhenDateIsInPast()
        {
            var dateTime = DateTime.UtcNow.AddDays(-1);
            dateTime.IsPast().Should().BeTrue();
        }

        /// <summary>
        /// Verifies that <c>WorkingDaysBetween</c> correctly counts the number of working days between two dates.
        /// </summary>
        [Fact]
        public void WorkingDaysBetween_ShouldReturnCorrectCount()
        {
            // Monday to Friday should be 5 days
            var start = new DateTime(2026, 6, 22); // Monday
            var end = new DateTime(2026, 6, 26); // Friday
            start.WorkingDaysBetween(end).Should().Be(5);
        }
    }
}
