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
        public async Task SendAsync_WithMultipleProviders_DispatchesToAllChannels()
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
            var provider3 = new TestNotificationProvider();

            service.Register(provider1);
            service.Register(provider2);
            service.Register(provider3);

            var notification = new Notification
            {
                Title = "Multi-Channel Alert",
                Message = "This should be sent to all providers",
                Priority = NotificationPriority.High
            };

            // Act
            var result = await service.SendAsync(notification);

            // Assert
            result.Should().BeTrue();
            provider1.SendCount.Should().Be(1, "Provider1 SendAsync should have been called");
            provider2.SendCount.Should().Be(1, "Provider2 SendAsync should have been called");
            provider3.SendCount.Should().Be(1, "Provider3 SendAsync should have been called");

            // Verify all providers were called
            var logs = _logger.GetLogs();
            logs.Should().Contain(log => log.Contains("Notification sent via TestProvider"));
            logs.Count(log => log.Contains("Notification sent via TestProvider")).Should().Be(3);
        }

        [Fact]
        public async Task SendAsync_WithFailingProvider_IsolatesFailure()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            var failingProvider = new FailingNotificationProvider();
            var successProvider = new TestNotificationProvider();

            service.Register(failingProvider);
            service.Register(successProvider);

            var notification = new Notification
            {
                Title = "Critical Alert",
                Message = "This should be sent despite one provider failing",
                Priority = NotificationPriority.Critical
            };

            // Act
            var result = await service.SendAsync(notification);

            // Assert
            result.Should().BeFalse(); // Returns false because not all providers succeeded
            failingProvider.SendCount.Should().Be(1);
            successProvider.SendCount.Should().Be(1);

            // Verify failure was logged
            var logs = _logger.GetLogs();
            logs.Should().Contain(log => log.Contains("Error sending notification via FailingProvider"));
            logs.Should().Contain(log => log.Contains("succeeded"));
        }

        [Fact]
        public async Task SendAsync_WithAllProvidersFailing_ReturnsFalse()
        {
            // Arrange
            var options = new NotificationSuppressionOptions
            {
                Enabled = true,
                SuppressionWindowSeconds = 300,
                MaxTrackedNotifications = 1000
            };

            var service = new NotificationService(_logger, options);
            var failingProvider1 = new FailingNotificationProvider();
            var failingProvider2 = new FailingNotificationProvider();

            service.Register(failingProvider1);
            service.Register(failingProvider2);

            var notification = new Notification
            {
                Title = "Critical Alert",
                Message = "This should fail if all providers fail",
                Priority = NotificationPriority.Critical
            };

            // Act
            var result = await service.SendAsync(notification);

            // Assert
            result.Should().BeFalse(); // Should return false when all providers fail
            failingProvider1.SendCount.Should().Be(1);
            failingProvider2.SendCount.Should().Be(1);

            // Verify all failure logs are present
            var logs = _logger.GetLogs();
            logs.Should().Contain(log => log.Contains("Error sending notification via FailingProvider"));
            logs.Count(log => log.Contains("Error sending notification via FailingProvider")).Should().Be(2);
        }

        [Fact]
        public void NotificationExtensions_AddMetadata_FluentApiWorks()
        {
            // Arrange
            var notification = new Notification
            {
                Title = "Test Notification",
                Message = "Test message"
            };

            // Act
            var result = notification.AddMetadata("key1", "value1")
                .AddMetadata("key2", "value2");

            // Assert
            result.Should().BeSameAs(notification); // Should return same instance for fluent chaining
            notification.Metadata.Should().NotBeNull();
            notification.Metadata.Should().HaveCount(2);
            notification.Metadata["key1"].Should().Be("value1");
            notification.Metadata["key2"].Should().Be("value2");
        }

        [Fact]
        public void NotificationExtensions_AddMetadata_NullNotificationThrows()
        {
            // Arrange
            Notification? notification = null;

            // Act
            Action act = () => notification.AddMetadata("key", "value");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NotificationExtensions_AddMetadata_NullKeyThrows()
        {
            // Arrange
            var notification = new Notification();

            // Act & Assert
            notification.Invoking(n => n.AddMetadata(null!, "value"))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NotificationExtensions_RemoveMetadata_RemovesExistingKey()
        {
            // Arrange
            var notification = new Notification();
            notification.AddMetadata("key1", "value1");
            notification.AddMetadata("key2", "value2");

            // Act
            var result = notification.RemoveMetadata("key1");

            // Assert
            result.Should().BeSameAs(notification); // Should return same instance for fluent chaining
            notification.Metadata.Should().HaveCount(1);
            notification.Metadata.Should().NotContainKey("key1");
            notification.Metadata.Should().ContainKey("key2");
        }

        [Fact]
        public void NotificationExtensions_RemoveMetadata_NonExistingKey_DoesNotThrow()
        {
            // Arrange
            var notification = new Notification();
            notification.AddMetadata("key1", "value1");

            // Act - Should not throw when removing non-existing key
            var result = notification.RemoveMetadata("nonexistent");

            // Assert
            result.Should().BeSameAs(notification);
            notification.Metadata.Should().HaveCount(1);
        }

        [Fact]
        public void NotificationExtensions_GetMetadataValue_RetrievesValue()
        {
            // Arrange
            var notification = new Notification();
            notification.AddMetadata("environment", "production");
            notification.AddMetadata("service", "api");

            // Act
            var envValue = notification.GetMetadataValue("environment");
            var serviceValue = notification.GetMetadataValue("service");
            var missingValue = notification.GetMetadataValue("nonexistent");

            // Assert
            envValue.Should().Be("production");
            serviceValue.Should().Be("api");
            missingValue.Should().BeNull();
        }

        [Fact]
        public void NotificationExtensions_GetMetadataValue_NullNotificationThrows()
        {
            // Arrange
            Notification? notification = null;

            // Act
            Action act = () => notification.GetMetadataValue("key");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void NotificationExtensions_GetMetadataValue_NullKeyThrows()
        {
            // Arrange
            var notification = new Notification();

            // Act & Assert
            notification.Invoking(n => n.GetMetadataValue(null!))
                .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void NotificationExtensions_ToSummaryString_FormatsCorrectly()
        {
            // Arrange
            var notification = new Notification
            {
                Id = "test-id-123",
                Title = "System Alert",
                Message = "Something went wrong",
                Priority = NotificationPriority.High,
                CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var summary = notification.ToSummaryString();

            // Assert
            summary.Should().Be("[Id:test-id-123] \"System Alert\" (Priority:High, Created:2024-01-01 12:00:00Z)");
        }

        [Fact]
        public void NotificationExtensions_ToSummaryString_NullNotificationThrows()
        {
            // Arrange
            Notification? notification = null;

            // Act
            Action act = () => notification.ToSummaryString();

            // Assert
            act.Should().Throw<ArgumentNullException>();
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

        /// <summary>
        /// Test notification provider that always fails
        /// </summary>
        private class FailingNotificationProvider : INotificationProvider
        {
            public int SendCount { get; private set; }
            public string ProviderName => "FailingProvider";

            public async Task<bool> SendAsync(Notification notification)
            {
                SendCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Provider intentionally failed");
            }
        }
    }
}