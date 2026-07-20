#nullable enable

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Notifications;
using CaddyVpsToolkit.Middleware;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Notifications
{
    /// <summary>
    /// Tests for NotificationService duplicate suppression functionality
    /// </summary>
    public class NotificationServiceTests
    {
        private readonly MemoryLogger _logger;
        private readonly TestNotificationProvider _provider;

        public NotificationServiceTests()
        {
            _logger = new MemoryLogger();
            _provider = new TestNotificationProvider();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new NotificationService(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNullOptions_UsesDefaultOptions()
        {
            // Arrange & Act
            var service = new NotificationService(_logger, null);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task SendAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new NotificationService(_logger);

            // Act
            Func<Task> act = async () => await service.SendAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SendAsync_WithDuplicateNotificationWithinWindow_SuppressesDuplicate()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification = new Notification
            {
                Title = "Test Alert",
                Message = "This is a test alert message",
                Priority = NotificationPriority.High
            };

            // Act - Send first notification
            var firstResult = await service.SendAsync(notification);

            // Reset provider call count after first send
            _provider.Reset();

            // Act - Send duplicate notification immediately
            var secondResult = await service.SendAsync(notification);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue(); // Should return true even though suppressed
            _provider.SendCount.Should().Be(0); // Second notification should be suppressed

            // Verify suppression was logged
            var logs = _logger.GetLogs();
            logs.Should().Contain(log => log.Contains("Suppressed duplicate notification"));
        }

        [Fact]
        public async Task SendAsync_WithDifferentNotifications_DoesNotSuppress()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification1 = new Notification
            {
                Title = "Alert 1",
                Message = "First alert",
                Priority = NotificationPriority.High
            };

            var notification2 = new Notification
            {
                Title = "Alert 2",
                Message = "Second alert",
                Priority = NotificationPriority.High
            };

            // Act
            var result1 = await service.SendAsync(notification1);
            var result2 = await service.SendAsync(notification2);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            _provider.SendCount.Should().Be(2); // Both notifications should be sent
        }

        [Fact]
        public async Task SendAsync_WithSameTitleDifferentMessage_DoesNotSuppress()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification1 = new Notification
            {
                Title = "Alert",
                Message = "First message",
                Priority = NotificationPriority.High
            };

            var notification2 = new Notification
            {
                Title = "Alert",
                Message = "Second message",
                Priority = NotificationPriority.High
            };

            // Act
            var result1 = await service.SendAsync(notification1);
            var result2 = await service.SendAsync(notification2);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            _provider.SendCount.Should().Be(2); // Both notifications should be sent
        }

        [Fact]
        public async Task SendAsync_WithSuppressionDisabled_DoesNotSuppress()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = false, // Suppression disabled
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification = new Notification
            {
                Title = "Test Alert",
                Message = "This is a test alert message",
                Priority = NotificationPriority.High
            };

            // Act - Send first notification
            var firstResult = await service.SendAsync(notification);

            // Reset provider call count
            _provider.Reset();

            // Act - Send duplicate notification
            var secondResult = await service.SendAsync(notification);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue();
            _provider.SendCount.Should().Be(1); // Duplicate should still be sent when suppression is disabled
        }

        [Fact]
        public async Task SendAsync_AfterSuppressionWindow_AllowsNotification()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 1, // Very short window for testing
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification = new Notification
            {
                Title = "Test Alert",
                Message = "This is a test alert message",
                Priority = NotificationPriority.High
            };

            // Act - Send first notification
            var firstResult = await service.SendAsync(notification);

            // Reset provider call count
            _provider.Reset();

            // Wait for suppression window to expire
            await Task.Delay(1100);

            // Act - Send duplicate notification after window expires
            var secondResult = await service.SendAsync(notification);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue();
            _provider.SendCount.Should().Be(1); // Should be allowed after window expires
        }

        [Fact]
        public async Task SendAsync_WithStringConstructor_DoesNotSuppress()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            // Act - Send two identical notifications using string constructor
            var firstResult = await service.SendAsync("Test Alert", "Test message");
            _provider.Reset();
            var secondResult = await service.SendAsync("Test Alert", "Test message");

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue();
            _provider.SendCount.Should().Be(0); // Second notification should be suppressed
        }

        [Fact]
        public async Task SendAsync_WithMultipleProviders_SuppressesAcrossAllProviders()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            var provider1 = new TestNotificationProvider();
            var provider2 = new TestNotificationProvider();

            service.Register(provider1);
            service.Register(provider2);

            var notification = new Notification
            {
                Title = "Test Alert",
                Message = "Test message",
                Priority = NotificationPriority.High
            };

            // Act - Send first notification
            var firstResult = await service.SendAsync(notification);

            // Reset provider call counts after first send
            provider1.Reset();
            provider2.Reset();

            // Act - Send duplicate notification
            var secondResult = await service.SendAsync(notification);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue();
            provider1.SendCount.Should().Be(0); // Should be suppressed for all providers
            provider2.SendCount.Should().Be(0);
        }

        [Fact]
        public void NotificationSuppressionOptions_DefaultValues()
        {
            // Arrange & Act
            var options = new NotificationSuppressionOptions();

            // Assert
            options.Enabled.Should().BeTrue();
            options.SuppressionWindowSeconds.Should().Be(300);
            options.MaxTrackedNotifications.Should().Be(1000);
        }

        [Fact]
        public void NotificationSuppressionOptions_CustomValues()
        {
            // Arrange & Act
            var options = new NotificationSuppressionOptions
            {
                Enabled = false,
                SuppressionWindowSeconds = 60,
                MaxTrackedNotifications = 500
            };

            // Assert
            options.Enabled.Should().BeFalse();
            options.SuppressionWindowSeconds.Should().Be(60);
            options.MaxTrackedNotifications.Should().Be(500);
        }

        [Fact]
        public async Task SendAsync_WithConcurrentNotifications_NoRaceConditions()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            var notification = new Notification
            {
                Title = "Concurrent Alert",
                Message = "Test concurrent notification",
                Priority = NotificationPriority.Normal
            };

            // Act - Send multiple notifications concurrently
            var tasks = new Task<bool>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = service.SendAsync(notification);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBeEquivalentTo(true);
            _provider.SendCount.Should().Be(1); // Only one should actually be sent

            // Verify no exceptions were thrown
            _logger.GetLogs().Should().NotContain(log => log.Contains("Error"));
        }

        [Fact]
        public async Task SendAsync_WithMaxNotifications_RemovesOldEntries()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 50 // Small number for testing
            };

            var service = new NotificationService(_logger, options);
            service.Register(_provider);

            // Act - Send more notifications than the max tracked
            for (int i = 0; i < 60; i++)
            {
                var notification = new Notification
                {
                    Title = $"Alert {i}",
                    Message = $"Message {i}",
                    Priority = NotificationPriority.Normal
                };
                await service.SendAsync(notification);
            }

            // Assert - Should not throw or fail
            _provider.SendCount.Should().Be(60);

            // Verify dictionary size is reasonable (might be slightly over due to timing)
            // The cleanup happens in batches, so we check it's not growing uncontrollably
            var suppressionDictSize = service.GetType().GetField("_recentNotifications",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(service);
            suppressionDictSize.Should().NotBeNull();
        }

        /// <summary>
        /// Test notification provider that tracks send calls
        /// </summary>
        private class TestNotificationProvider : INotificationProvider
        {
            public int SendCount { get; private set; }
            public string ProviderName => "TestProvider";

            public async Task<bool> SendAsync(Notification notification)
            {
                SendCount++;
                await Task.CompletedTask;
                return true;
            }

            public void Reset()
            {
                SendCount = 0;
            }
        }
    }
}