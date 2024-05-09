#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Tests for the LogAggregationService class.
    /// </summary>
    public sealed class LogAggregationServiceTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogAggregationServiceTests"/> class.
        /// </summary>
        public LogAggregationServiceTests()
        {
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method returns an empty list when the log directory is empty.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_WithEmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var sut = new LogAggregationService(tempDir);

            // Act
            var result = await sut.GetLogsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method parses standard log format.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_ParsesStandardLogFormat()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var logContent = string.Join(Environment.NewLine,
                "[2025-01-15T10:00:00.000+00:00] [Info] Application started",
                "[2025-01-15T10:01:00.000+00:00] [Warning] High memory usage detected",
                "[2025-01-15T10:02:00.000+00:00] [Error] Failed to connect to database");

            await File.WriteAllTextAsync(Path.Combine(tempDir, "app.log"), logContent);
            var sut = new LogAggregationService(tempDir);

            // Act
            var result = await sut.GetLogsAsync(new LogQueryOptions { Lines = 100 });

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(e => e.Level == "Info" && e.Message == "Application started");
            result.Should().Contain(e => e.Level == "Warning");
            result.Should().Contain(e => e.Level == "Error");
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method filters by minimum log level.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_FiltersbyMinLevel()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var logContent = string.Join(Environment.NewLine,
                "[2025-01-15T10:00:00.000+00:00] [Debug] Verbose trace",
                "[2025-01-15T10:01:00.000+00:00] [Info] Normal event",
                "[2025-01-15T10:02:00.000+00:00] [Error] Critical failure");

            await File.WriteAllTextAsync(Path.Combine(tempDir, "app.log"), logContent);
            var sut = new LogAggregationService(tempDir);

            // Act
            var result = await sut.GetLogsAsync(new LogQueryOptions { MinLevel = "Warning" });

            // Assert
            result.Should().HaveCount(1);
            result[0].Level.Should().Be("Error");
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method respects the lines limit.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_RespectsLinesLimit()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < 20; i++)
                lines.AppendLine($"[2025-01-{i + 1:D2}T10:00:00.000+00:00] [Info] Event {i}");

            await File.WriteAllTextAsync(Path.Combine(tempDir, "app.log"), lines.ToString());
            var sut = new LogAggregationService(tempDir);

            // Act
            var result = await sut.GetLogsAsync(new LogQueryOptions { Lines = 5 });

            // Assert
            result.Should().HaveCount(5);
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method returns the most recent logs first.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_ReturnsMostRecentFirst()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var logContent = string.Join(Environment.NewLine,
                "[2025-01-01T10:00:00.000+00:00] [Info] Older event",
                "[2025-01-02T10:00:00.000+00:00] [Info] Newer event");

            await File.WriteAllTextAsync(Path.Combine(tempDir, "app.log"), logContent);
            var sut = new LogAggregationService(tempDir);

            // Act
            var result = await sut.GetLogsAsync();

            // Assert
            result[0].Message.Should().Be("Newer event");
            result[1].Message.Should().Be("Older event");
        }

        /// <summary>
        /// Verifies that the GetLogsAsync method filters by since date.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Fact]
        public async Task GetLogsAsync_FiltersBySince()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var logContent = string.Join(Environment.NewLine,
                "[2025-01-01T08:00:00.000+00:00] [Info] Old event",
                "[2025-01-15T10:00:00.000+00:00] [Info] Recent event");

            await File.WriteAllTextAsync(Path.Combine(tempDir, "app.log"), logContent);
            var sut = new LogAggregationService(tempDir);

            // Act
            var since = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
            var result = await sut.GetLogsAsync(new LogQueryOptions { Since = since });

            // Assert
            result.Should().HaveCount(1);
            result[0].Message.Should().Be("Recent event");
        }

        /// <summary>
        /// Verifies that the GetLogSources method returns log files.
        /// </summary>
        [Fact]
        public void GetLogSources_ReturnsLogFiles()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"log-agg-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "a.log"), "");
            File.WriteAllText(Path.Combine(tempDir, "b.log"), "");
            File.WriteAllText(Path.Combine(tempDir, "readme.txt"), ""); // should not be included

            var sut = new LogAggregationService(tempDir);

            // Act
            var sources = sut.GetLogSources();

            // Assert
            sources.Should().HaveCount(2);
        }
    }
}
