#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Utilities
{
    public sealed class RetryPolicyTests
    {
        [Fact]
        public async Task ExecuteAsync_SuccessOnFirstAttempt_ReturnsResultWithoutRetry()
        {
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 3, initialDelayMs: 1);
            int callCount = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return "ok";
            });

            result.Should().Be("ok");
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_FailsThenSucceeds_ReturnsResultAfterRetry()
        {
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 3, initialDelayMs: 1);
            int callCount = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                callCount++;
                if (callCount < 3)
                    throw new InvalidOperationException("transient");
                await Task.CompletedTask;
                return 42;
            });

            result.Should().Be(42);
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_ExceedsMaxRetries_RethrowsLastException()
        {
            var policy = new ExponentialBackoffRetryPolicy(maxRetries: 2, initialDelayMs: 1);
            int callCount = 0;

            Func<Task<int>> act = () => policy.ExecuteAsync<int>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException($"attempt {callCount}");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
            callCount.Should().Be(3); // initial + 2 retries
        }

        [Fact]
        public async Task ExecuteAsync_NullOperation_ThrowsArgumentNullException()
        {
            var policy = new ExponentialBackoffRetryPolicy();

            Func<Task<int>> act = () => policy.ExecuteAsync<int>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_VoidOverload_NullOperation_ThrowsArgumentNullException()
        {
            var policy = new ExponentialBackoffRetryPolicy();

            Func<Task> act = () => policy.ExecuteAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task LinearBackoffRetryPolicy_SuccessOnFirstAttempt_ReturnsResult()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayIncrementMs: 1);

            var result = await policy.ExecuteAsync(() => Task.FromResult("linear-ok"));

            result.Should().Be("linear-ok");
        }

        [Fact]
        public async Task LinearBackoffRetryPolicy_FailsThenSucceeds_RetriesAndReturns()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayIncrementMs: 1);
            int callCount = 0;

            var result = await policy.ExecuteAsync(async () =>
            {
                callCount++;
                if (callCount < 2)
                    throw new Exception("transient");
                await Task.CompletedTask;
                return "recovered";
            });

            result.Should().Be("recovered");
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task LinearBackoffRetryPolicy_ExceedsMaxRetries_Throws()
        {
            var policy = new LinearBackoffRetryPolicy(maxRetries: 2, delayIncrementMs: 1);

            Func<Task<string>> act = () => policy.ExecuteAsync<string>(async () =>
            {
                await Task.CompletedTask;
                throw new Exception("always fails");
            });

            await act.Should().ThrowAsync<Exception>().WithMessage("always fails");
        }

        [Fact]
        public async Task NoRetryPolicy_SuccessOnFirstCall_ReturnsResult()
        {
            var policy = new NoRetryPolicy();

            var result = await policy.ExecuteAsync(() => Task.FromResult(99));

            result.Should().Be(99);
        }

        [Fact]
        public async Task NoRetryPolicy_OperationThrows_PropagatesImmediately()
        {
            var policy = new NoRetryPolicy();
            int callCount = 0;

            Func<Task<int>> act = () => policy.ExecuteAsync<int>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("no retry");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
            callCount.Should().Be(1);
        }
    }
}
