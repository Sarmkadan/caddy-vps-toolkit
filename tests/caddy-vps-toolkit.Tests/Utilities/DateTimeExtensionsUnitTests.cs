using System;
using Xunit;
using FluentAssertions;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Unit tests for the DateTimeExtensions class.
    /// </summary>
    public class DateTimeExtensionsUnitTests
    {
        /// <summary>
        /// Tests that ToRelativeTime returns "just now" for times less than a minute ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnJustNow_WhenLessThanMinute()
        {
            var dateTime = DateTime.UtcNow.AddSeconds(-30);
            dateTime.ToRelativeTime().Should().Be("just now");
        }

        /// <summary>
        /// Tests that ToRelativeTime returns minutes ago format for times less than an hour ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnMinutesAgo_WhenLessThanHour()
        {
            var dateTime = DateTime.UtcNow.AddMinutes(-10);
            dateTime.ToRelativeTime().Should().Be("10 minutes ago");
        }

        /// <summary>
        /// Tests that ToRelativeTime returns hours ago format for times less than a day ago.
        /// </summary>
        [Fact]
        public void ToRelativeTime_ShouldReturnHoursAgo_WhenLessThanDay()
        {
            var dateTime = DateTime.UtcNow.AddHours(-5);
            dateTime.ToRelativeTime().Should().Be("5 hours ago");
        }

        /// <summary>
        /// Tests that ToIso8601 returns the correct ISO 8601 format string.
        /// </summary>
        [Fact]
        public void ToIso8601_ShouldReturnCorrectFormat()
        {
            var dateTime = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
            dateTime.ToIso8601().Should().Contain("2026-06-26T12:00:00.0000000Z");
        }

        /// <summary>
        /// Tests that StartOfDay returns the start of the day (00:00:00) for a given DateTime.
        /// </summary>
        [Fact]
        public void StartOfDay_ShouldReturnCorrectTime()
        {
            var dateTime = new DateTime(2026, 6, 26, 15, 30, 0);
            dateTime.StartOfDay().Should().Be(new DateTime(2026, 6, 26, 0, 0, 0));
        }

        /// <summary>
        /// Tests that IsPast returns true for dates that are in the past.
        /// </summary>
        [Fact]
        public void IsPast_ShouldReturnTrue_WhenDateIsInPast()
        {
            var dateTime = DateTime.UtcNow.AddDays(-1);
            dateTime.IsPast().Should().BeTrue();
        }

        /// <summary>
        /// Tests that WorkingDaysBetween returns the correct count of working days between two dates.
        /// </summary>
        [Fact]
        public void WorkingDaysBetween_ShouldReturnCorrectCount()
        {
            // Monday to Friday should be 5 days
            var start = new DateTime(2026, 6, 22); // Monday
            var end = new DateTime(2026, 6, 26); // Friday
            start.WorkingDaysBetween(end).Should().Be(5);
        }

        /// <summary>
        /// Tests that IsToday returns true for dates that are today.
        /// </summary>
        [Fact]
        public void IsToday_ShouldReturnTrue_WhenDateIsToday()
        {
            var dateTime = DateTime.UtcNow.Date;
            dateTime.IsToday().Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsToday returns false for dates that are not today.
        /// </summary>
        [Fact]
        public void IsToday_ShouldReturnFalse_WhenDateIsNotToday()
        {
            var dateTime = DateTime.UtcNow.AddDays(-1);
            dateTime.IsToday().Should().BeFalse();
        }

        /// <summary>
        /// Tests that IsFuture returns true for dates that are in the future.
        /// </summary>
        [Fact]
        public void IsFuture_ShouldReturnTrue_WhenDateIsInFuture()
        {
            var dateTime = DateTime.UtcNow.AddDays(1);
            dateTime.IsFuture().Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsFuture returns false for dates that are not in the future.
        /// </summary>
        [Fact]
        public void IsFuture_ShouldReturnFalse_WhenDateIsNotInFuture()
        {
            var dateTime = DateTime.UtcNow;
            dateTime.IsFuture().Should().BeFalse();
        }

        /// <summary>
        /// Tests that ToDurationString returns the correct format string for various TimeSpan values.
        /// </summary>
        [Fact]
        public void ToDurationString_ShouldReturnCorrectFormat()
        {
            var timeSpan = TimeSpan.FromSeconds(30);
            timeSpan.ToDurationString().Should().Be("30s");

            timeSpan = TimeSpan.FromMinutes(10);
            timeSpan.ToDurationString().Should().Be("10m 0s");

            timeSpan = TimeSpan.FromHours(5);
            timeSpan.ToDurationString().Should().Be("5h 0m");

            timeSpan = TimeSpan.FromDays(2);
            timeSpan.ToDurationString().Should().Be("2d 0h");
        }
    }
}
