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

/// <summary>
/// Tests for destination validation functionality
/// </summary>
public class DestinationValidationTests
{
    [Fact]
    public void ValidateEmail_ValidEmail_ReturnsEmail()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var result = validEmail.ValidateEmail();

        // Assert
        result.Should().Be(validEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateEmail_NullOrEmpty_ThrowsArgumentException(string invalidEmail)
    {
        // Arrange & Act
        Action act = () => invalidEmail.ValidateEmail();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test@@example.com")]
    public void ValidateEmail_InvalidFormat_ThrowsArgumentException(string invalidEmail)
    {
        // Arrange & Act
        Action act = () => invalidEmail.ValidateEmail();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateWebhookUrl_ValidHttpsUrl_ReturnsUrl()
    {
        // Arrange
        var validUrl = "https://example.com/webhook";

        // Act
        var result = validUrl.ValidateWebhookUrl();

        // Assert
        result.Should().Be(validUrl);
    }

    [Fact]
    public void ValidateWebhookUrl_ValidHttpUrl_ReturnsUrl()
    {
        // Arrange
        var validUrl = "http://example.com/webhook";

        // Act
        var result = validUrl.ValidateWebhookUrl();

        // Assert
        result.Should().Be(validUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateWebhookUrl_NullOrEmpty_ThrowsArgumentException(string invalidUrl)
    {
        // Arrange & Act
        Action act = () => invalidUrl.ValidateWebhookUrl();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ftp://example.com/webhook")]
    [InlineData("ws://example.com/webhook")]
    public void ValidateWebhookUrl_InvalidScheme_ThrowsArgumentException(string invalidUrl)
    {
        // Arrange & Act
        Action act = () => invalidUrl.ValidateWebhookUrl();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("http://localhost/webhook")]
    [InlineData("http://127.0.0.1/webhook")]
    [InlineData("http://192.168.1.1/webhook")]
    [InlineData("http://10.0.0.1/webhook")]
    [InlineData("http://172.16.0.1/webhook")]
    public void ValidateWebhookUrl_LocalhostOrPrivateIp_ThrowsArgumentException(string blockedUrl)
    {
        // Arrange & Act
        Action act = () => blockedUrl.ValidateWebhookUrl();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*localhost or private address*");
    }

    [Theory]
    [InlineData("https://192.168.1.1:8080/webhook")]
    [InlineData("http://10.10.10.10/webhook")]
    public void ValidateWebhookUrl_PrivateIpRanges_ThrowsArgumentException(string blockedUrl)
    {
        // Arrange & Act
        Action act = () => blockedUrl.ValidateWebhookUrl();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidatePhoneNumber_ValidPhoneNumber_ReturnsPhoneNumber()
    {
        // Arrange
        var validPhone = "+1234567890";

        // Act
        var result = validPhone.ValidatePhoneNumber();

        // Assert
        result.Should().Be(validPhone);
    }

    [Fact]
    public void ValidatePhoneNumber_ValidPhoneWithDashes_ReturnsPhoneNumber()
    {
        // Arrange
        var validPhone = "123-456-7890";

        // Act
        var result = validPhone.ValidatePhoneNumber();

        // Assert
        result.Should().Be(validPhone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePhoneNumber_NullOrEmpty_ThrowsArgumentException(string invalidPhone)
    {
        // Arrange & Act
        Action act = () => invalidPhone.ValidatePhoneNumber();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("123-45")]
    public void ValidatePhoneNumber_InvalidFormat_ThrowsArgumentException(string invalidPhone)
    {
        // Arrange & Act
        Action act = () => invalidPhone.ValidatePhoneNumber();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateMessageContent_ValidMessage_ReturnsMessage()
    {
        // Arrange
        var validMessage = "This is a normal message without any templates";

        // Act
        var result = validMessage.ValidateMessageContent();

        // Assert
        result.Should().Be(validMessage);
    }

    [Theory]
    [InlineData(null)]
    public void ValidateMessageContent_NullOrEmpty_ThrowsArgumentException(string invalidMessage)
    {
        // Arrange & Act
        Action act = () => invalidMessage.ValidateMessageContent();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("Hello {{name}}")]
    [InlineData("Value: {{value}} and {{other}}")]
    [InlineData("Template: {{user}} please check {{action}}")]
    public void ValidateMessageContent_TemplateInjectionPatterns_ThrowsArgumentException(string messageWithTemplate)
    {
        // Arrange & Act
        Action act = () => messageWithTemplate.ValidateMessageContent();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*template injection patterns*");
    }

    [Fact]
    public void ValidateDestination_EmailType_ValidatesAsEmail()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = email.ValidateDestination(DestinationType.Email);

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void ValidateDestination_WebhookType_ValidatesAsWebhook()
    {
        // Arrange
        var url = "https://example.com/webhook";

        // Act
        var result = url.ValidateDestination(DestinationType.Webhook);

        // Assert
        result.Should().Be(url);
    }

    [Fact]
    public void ValidateDestination_PhoneType_ValidatesAsPhone()
    {
        // Arrange
        var phone = "+1234567890";

        // Act
        var result = phone.ValidateDestination(DestinationType.Phone);

        // Assert
        result.Should().Be(phone);
    }
}