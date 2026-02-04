#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Retry policies for handling transient failures.
    /// Implements exponential backoff and jitter to prevent thundering herd.
    /// </summary>
    public interface IRetryPolicy
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
        Task ExecuteAsync(Func<Task> operation);
    }

    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialDelayMs;
        private readonly double _backoffMultiplier;
        private readonly int _maxDelayMs;
        private readonly Random _random;

        public ExponentialBackoffRetryPolicy(
            int maxRetries = 3,
            int initialDelayMs = 100,
            double backoffMultiplier = 2.0,
            int maxDelayMs = 10000)
        {
            _maxRetries = maxRetries;
            _initialDelayMs = initialDelayMs;
            _backoffMultiplier = backoffMultiplier;
            _maxDelayMs = maxDelayMs;
            _random = new Random();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            int delayMs = _initialDelayMs;
            Exception lastException = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt >= _maxRetries)
                        throw;

                    // Add jitter to prevent thundering herd
                    int jitter = _random.Next((int)(delayMs * 0.1), (int)(delayMs * 1.1));
                    await Task.Delay(jitter);

                    // Calculate next delay
                    delayMs = (int)Math.Min(_maxDelayMs, delayMs * _backoffMultiplier);
                }
            }

            throw lastException;
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            await ExecuteAsync(async () =>
            {
                await operation();
                return (object)null;
            });
        }
    }

    /// <summary>
    /// Linear backoff retry policy - increases delay by fixed amount
    /// </summary>
    public sealed class LinearBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _delayIncrement;

        public LinearBackoffRetryPolicy(int maxRetries = 3, int delayIncrementMs = 500)
        {
            _maxRetries = maxRetries;
            _delayIncrement = delayIncrementMs;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception) when (attempt < _maxRetries)
                {
                    await Task.Delay(_delayIncrement * (attempt + 1));
                }
            }

            // This should not be reached, but satisfies compiler
            throw new InvalidOperationException("Retry policy exhausted");
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return (object)null;
            });
        }
    }

    /// <summary>
    /// No retry policy - execute once only
    /// </summary>
    public sealed class NoRetryPolicy : IRetryPolicy
    {
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await operation();
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await operation();
        }
    }
}
