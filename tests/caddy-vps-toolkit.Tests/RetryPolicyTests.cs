#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CaddyVpsToolkit.Utilities;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public async Task ExponentialBackoff_SucceedsOnFirstTry_ExecutesOnce()
        {
            // Arrange
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 3, initialDelayMs: 1);
            int callCount = 0;
            Func<Task<int>> operation = () =>
            {
                callCount++;
                return Task.FromResult(42);
            };

            // Act
            var result = await policy.ExecuteAsync(operation);

            // Assert
            Assert.Equal(42, result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExponentialBackoff_TransientFailure_RetriesAndSucceeds()
        {
            // Arrange
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 3, initialDelayMs: 1);
            int callCount = 0;
            Func<Task<string>> operation = () =>
            {
                callCount++;
                if (callCount < 3)
                    throw new InvalidOperationException("Transient failure");
                return Task.FromResult("ok");
            };

            // Act
            var result = await policy.ExecuteAsync(operation);

            // Assert
            Assert.Equal("ok", result);
            Assert.Equal(3, callCount); // two failures + one success
        }

        [Fact]
        public async Task ExponentialBackoff_ExhaustedRetries_ThrowsFinalException()
        {
            // Arrange
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 2, initialDelayMs: 1);
            int callCount = 0;
            Func<Task<int>> operation = () =>
            {
                callCount++;
                throw new InvalidOperationException($"Fail {callCount}");
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await policy.ExecuteAsync(operation));

            // The exception message should be from the last attempt
            Assert.Equal("Fail 3", ex.Message);
            Assert.Equal(3, callCount); // attempts = maxRetries + 1
        }

        [Fact]
        public async Task ExponentialBackoff_DelayBackoff_IsRespected()
        {
            // Arrange: tiny delays to keep test fast
            var policy = new ExponentialBackoffRetryPolicy(
                maxRetries: 2,
                initialDelayMs: 10,
                backoffMultiplier: 2.0,
                maxDelayMs: 30);

            var attempts = new List<DateTime>();
            Func<Task<int>> operation = () =>
            {
                attempts.Add(DateTime.UtcNow);
                if (attempts.Count <= 2)
                    throw new InvalidOperationException("Transient");
                return Task.FromResult(1);
            };

            // Act
            var sw = Stopwatch.StartNew();
            var result = await policy.ExecuteAsync(operation);
            sw.Stop();

            // Assert result
            Assert.Equal(1, result);
            Assert.Equal(3, attempts.Count); // two failures + success

            // Calculate minimal expected total delay:
            // First failure: delay between 0.1*10 = 1ms and 1.1*10 = 11ms
            // Second failure: delay between 0.1*20 = 2ms and 1.1*20 = 22ms (but capped at maxDelayMs=30)
            // We'll assert that elapsed time is at least the sum of the lower bounds (1 + 2 = 3ms)
            // Adding a small safety margin for timer granularity.
            Assert.True(sw.ElapsedMilliseconds >= 3, $"Elapsed {sw.ElapsedMilliseconds}ms should be >= 3ms");
        }

        [Fact]
        public async Task LinearBackoff_RetriesAndSucceeds()
        {
            // Arrange
            var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayIncrementMs: 5);
            int callCount = 0;
            Func<Task<string>> operation = () =>
            {
                callCount++;
                if (callCount < 2)
                    throw new Exception("Transient");
                return Task.FromResult("done");
            };

            // Act
            var result = await policy.ExecuteAsync(operation);

            // Assert
            Assert.Equal("done", result);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task NoRetry_ExecutesOnlyOnce()
        {
            // Arrange
            var policy = new NoRetryPolicy();
            int callCount = 0;
            Func<Task<int>> operation = () =>
            {
                callCount++;
                return Task.FromResult(7);
            };

            // Act
            var result = await policy.ExecuteAsync(operation);

            // Assert
            Assert.Equal(7, result);
            Assert.Equal(1, callCount);
        }
    }
}
