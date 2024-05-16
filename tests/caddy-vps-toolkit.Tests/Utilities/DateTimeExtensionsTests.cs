using System;
using Xunit;
using FluentAssertions;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests.Utilities
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void ToRelativeTime_ShouldReturnJustNow_WhenLessThanMinute()
        {
            var dateTime = DateTime.UtcNow.AddSeconds(-30);
            dateTime.ToRelativeTime().Should().Be("just now");
        }

        [Fact]
        public void ToRelativeTime_ShouldReturnMinutesAgo_WhenLessThanHour()
        {
            var dateTime = DateTime.UtcNow.AddMinutes(-10);
            dateTime.ToRelativeTime().Should().Be("10 minutes ago");
        }

        [Fact]
        public void ToRelativeTime_ShouldReturnHoursAgo_WhenLessThanDay()
        {
            var dateTime = DateTime.UtcNow.AddHours(-5);
            dateTime.ToRelativeTime().Should().Be("5 hours ago");
        }

        [Fact]
        public void ToIso8601_ShouldReturnCorrectFormat()
        {
            var dateTime = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
            dateTime.ToIso8601().Should().Contain("2026-06-26T12:00:00.0000000Z");
        }

        [Fact]
        public void StartOfDay_ShouldReturnCorrectTime()
        {
            var dateTime = new DateTime(2026, 6, 26, 15, 30, 0);
            dateTime.StartOfDay().Should().Be(new DateTime(2026, 6, 26, 0, 0, 0));
        }

        [Fact]
        public void IsPast_ShouldReturnTrue_WhenDateIsInPast()
        {
            var dateTime = DateTime.UtcNow.AddDays(-1);
            dateTime.IsPast().Should().BeTrue();
        }

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
