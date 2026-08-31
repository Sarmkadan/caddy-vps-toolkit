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
    /// Defines a retry policy for handling transient failures.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes an asynchronous operation with the retry policy applied.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of type <typeparamref name="T"/>.</returns>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// Executes an asynchronous operation with the retry policy applied.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ExecuteAsync(Func<Task> operation);
    }

    /// <summary>
    /// Implements a retry policy using exponential backoff with jitter to prevent the thundering herd problem.
    /// </summary>
    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialDelayMs;
        private readonly double _backoffMultiplier;
        private readonly int _maxDelayMs;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoffRetryPolicy"/> class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries to attempt. Defaults to 3.</param>
        /// <param name="initialDelayMs">The initial delay in milliseconds. Defaults to 100.</param>
        /// <param name="backoffMultiplier">The multiplier for the backoff interval. Defaults to 2.0.</param>
        /// <param name="maxDelayMs">The maximum allowed delay in milliseconds. Defaults to 10000.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelayMs"/> is less than or equal to zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="backoffMultiplier"/> is less than 1.0.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDelayMs"/> is less than <paramref name="initialDelayMs"/>.</exception>
        public ExponentialBackoffRetryPolicy(
            int maxRetries = 3,
            int initialDelayMs = 100,
            double backoffMultiplier = 2.0,
            int maxDelayMs = 10000)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
            ArgumentOutOfRangeException.ThrowIfLessThan(initialDelayMs, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(backoffMultiplier, 1.0);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDelayMs, initialDelayMs);

            _maxRetries = maxRetries;
            _initialDelayMs = initialDelayMs;
            _backoffMultiplier = backoffMultiplier;
            _maxDelayMs = maxDelayMs;
            _random = new Random();
        }

        /// <summary>
        /// Executes an asynchronous operation with exponential backoff retry.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            int delayMs = _initialDelayMs;
            Exception? lastException = null;

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

            throw lastException!;
        }

        /// <summary>
        /// Executes an asynchronous operation with exponential backoff retry.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
        public async Task ExecuteAsync(Func<Task> operation)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            await ExecuteAsync(async () =>
            {
                await operation();
                return (object)null!;
            });
        }
    }

    /// <summary>
    /// Implements a retry policy using linear backoff, increasing delay by a fixed amount between retries.
    /// </summary>
    public sealed class LinearBackoffRetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _delayIncrement;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearBackoffRetryPolicy"/> class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries to attempt. Defaults to 3.</param>
        /// <param name="delayIncrementMs">The delay increment in milliseconds. Defaults to 500.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delayIncrementMs"/> is less than or equal to zero.</exception>
        public LinearBackoffRetryPolicy(int maxRetries = 3, int delayIncrementMs = 500)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
            ArgumentOutOfRangeException.ThrowIfLessThan(delayIncrementMs, 1);

            _maxRetries = maxRetries;
            _delayIncrement = delayIncrementMs;
        }

        /// <summary>
        /// Executes an asynchronous operation with linear backoff retry.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when retry attempts are exhausted.</exception>
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

        /// <summary>
        /// Executes an asynchronous operation with linear backoff retry.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return (object)null!;
            });
        }
    }

    /// <summary>
    /// Implements a no-retry policy that executes an operation only once.
    /// </summary>
    public sealed class NoRetryPolicy : IRetryPolicy
    {
        /// <summary>
        /// Executes an asynchronous operation exactly once without retry.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result of type <typeparamref name="T"/>.</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await operation();
        }

        /// <summary>
        /// Executes an asynchronous operation exactly once without retry.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(Func<Task> operation)
        {
            await operation();
        }
    }
}
