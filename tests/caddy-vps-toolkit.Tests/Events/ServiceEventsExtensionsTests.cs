using System;
using CaddyVpsToolkit.Events;
using Xunit;

namespace CaddyVpsToolkit.Tests.Events
{
    public class ServiceEventsExtensionsTests
    {
        [Fact]
        public void Describe_ServiceCreatedEvent_ReturnsExpectedString()
        {
            var ev = new ServiceCreatedEvent
            {
                ServiceName = "TestService",
                ServiceType = default,
                Port = 8080,
                ExecutablePath = "/usr/bin/test"
            };

            var description = ev.Describe();

            Assert.Contains("TestService", description);
            Assert.Contains("8080", description);
        }

        [Fact]
        public void IsCritical_ServiceHealthCheckFailedEvent_ReturnsTrue()
        {
            var ev = new ServiceHealthCheckFailedEvent
            {
                ServiceName = "UnhealthyService",
                ErrorMessage = "Timeout",
                ConsecutiveFailures = 3
            };

            Assert.True(ev.IsCritical());
        }

        [Fact]
        public void IsCritical_ServiceCreatedEvent_ReturnsFalse()
        {
            var ev = new ServiceCreatedEvent
            {
                ServiceName = "HealthyService",
                ServiceType = default,
                Port = 1234,
                ExecutablePath = "/bin/healthy"
            };

            Assert.False(ev.IsCritical());
        }

        [Fact]
        public void ToLogString_IncludesTimestampAndDescription()
        {
            var ev = new ServiceDeletedEvent
            {
                ServiceName = "ObsoleteService",
                ServiceType = default
            };

            var log = ev.ToLogString();

            // The log should start with a UTC timestamp in ISO‑8601 format
            Assert.Matches(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z\] ", log);
            Assert.Contains("ObsoleteService", log);
        }
    }
}
