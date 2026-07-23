#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Middleware;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Circuit breaker states
    /// </summary>
    public enum CircuitState
    {
        Closed,  // Normal operation - requests are allowed
        Open,    // Circuit is open - requests are blocked
        HalfOpen // Circuit is testing if the failure has been resolved
    }

    /// <summary>
    /// Circuit breaker for tracking failures of a specific notification provider.
    /// Opens the circuit after a configurable number of consecutive failures,
    /// then periodically allows test requests to check if the provider has recovered.
    /// </summary>
    public interface ICircuitBreaker
    {
        CircuitState CurrentState { get; }
        int FailureCount { get; }
        int SuccessCount { get; }

        Task<bool> ExecuteAsync(Func<Task<bool>> operation);
        void Reset();
        void RecordSuccess();
        void RecordFailure();
    }

    /// <summary>
    /// Circuit breaker implementation with configurable thresholds and recovery behavior.
    /// </summary>
    public sealed class CircuitBreaker : ICircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly int _recoveryTimeoutSeconds;
        private readonly string _providerName;
        private readonly ILogger _logger;

        private CircuitState _state;
        private DateTime _lastFailureTime;
        private DateTime _stateChangedTime;
        private int _failureCount;
        private int _successCount;
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new circuit breaker instance.
        /// </summary>
        /// <param name="providerName">Name of the provider this circuit breaker monitors</param>
        /// <param name="logger">Logger for circuit breaker events</param>
        /// <param name="failureThreshold">Number of consecutive failures before opening the circuit</param>
        /// <param name="recoveryTimeoutSeconds">Time to wait before attempting a recovery check when circuit is open</param>
        /// <exception cref="ArgumentNullException">Thrown when providerName or logger is null</exception>
        /// <exception cref="ArgumentException">Thrown when providerName is null or empty</exception>
        public CircuitBreaker(
            string providerName,
            ILogger logger,
            int failureThreshold = 5,
            int recoveryTimeoutSeconds = 60)
        {
            ArgumentNullException.ThrowIfNull(providerName);
            ArgumentException.ThrowIfNullOrEmpty(providerName);
            ArgumentNullException.ThrowIfNull(logger);

            _providerName = providerName;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _failureThreshold = failureThreshold > 0 ? failureThreshold : 5;
            _recoveryTimeoutSeconds = recoveryTimeoutSeconds > 0 ? recoveryTimeoutSeconds : 60;

            _state = CircuitState.Closed;
            _failureCount = 0;
            _successCount = 0;
        }

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public CircuitState CurrentState => _state;

        /// <summary>
        /// Number of consecutive failures since last state change
        /// </summary>
        public int FailureCount => _failureCount;

        /// <summary>
        /// Number of consecutive successes since last state change
        /// </summary>
        public int SuccessCount => _successCount;

        /// <summary>
        /// Execute an operation with circuit breaker protection.
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <returns>True if operation succeeded or circuit allowed it, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when operation is null</exception>
        public async Task<bool> ExecuteAsync(Func<Task<bool>> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);

            try
            {
                var result = await TryExecuteAsync(operation);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Circuit breaker for {_providerName} caught unexpected exception: {ex.Message}");
                RecordFailure();
                return false;
            }
        }

        private async Task<bool> TryExecuteAsync(Func<Task<bool>> operation)
        {
            bool shouldLog = false;
            string logMessage = null;
            bool shouldReturnFalse = false;

            lock (_lock)
            {
                switch (_state)
                {
                    case CircuitState.Open:
                        // Check if recovery timeout has elapsed
                        var timeInOpenState = DateTime.UtcNow - _stateChangedTime;
                        if (timeInOpenState.TotalSeconds >= _recoveryTimeoutSeconds)
                        {
                            // Transition to half-open state to test recovery
                            _state = CircuitState.HalfOpen;
                            _failureCount = 0;
                            _successCount = 0;
                            shouldLog = true;
                            logMessage = $"Circuit breaker for {_providerName} transitioning from Open to HalfOpen for recovery test";
                        }
                        else
                        {
                            shouldLog = true;
                            logMessage = $"Circuit breaker for {_providerName} is Open - blocking request (will retry in {_recoveryTimeoutSeconds - timeInOpenState.TotalSeconds:F0}s)";
                            shouldReturnFalse = true;
                        }
                        break;

                    case CircuitState.HalfOpen:
                        // Allow one test request through
                        shouldLog = true;
                        logMessage = $"Circuit breaker for {_providerName} in HalfOpen state - allowing test request";
                        break;
                }
            }

            if (shouldLog && !string.IsNullOrEmpty(logMessage))
            {
                await _logger.LogInfoAsync(string.Format(logMessage, _providerName));
            }

            if (shouldReturnFalse)
            {
                return false;
            }

            // Execute the operation
            var success = await operation();

            if (success)
            {
                RecordSuccess();
            }
            else
            {
                RecordFailure();
            }

            return success;
        }

        /// <summary>
        /// Record a successful operation
        /// </summary>
        public void RecordSuccess()
        {
            lock (_lock)
            {
                if (_state == CircuitState.HalfOpen)
                {
                    // In half-open state, a success means circuit should close
                    _state = CircuitState.Closed;
                    _failureCount = 0;
                    _successCount = 0;
                    _stateChangedTime = DateTime.UtcNow;
                    _logger.LogInfoAsync($"Circuit breaker for {_providerName} received success in HalfOpen state - closing circuit").GetAwaiter().GetResult();
                }
                else if (_state == CircuitState.Closed)
                {
                    _successCount++;
                }
            }
        }

        /// <summary>
        /// Record a failed operation
        /// </summary>
        public void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitState.Closed && _failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _stateChangedTime = DateTime.UtcNow;
                    _logger.LogErrorAsync($"Circuit breaker for {_providerName} reached failure threshold ({_failureThreshold}) - opening circuit").GetAwaiter().GetResult();
                }
                else if (_state == CircuitState.HalfOpen)
                {
                    // In half-open state, a failure means circuit should reopen
                    _state = CircuitState.Open;
                    _stateChangedTime = DateTime.UtcNow;
                    _failureCount = 1; // Keep failure count for next attempt
                    _logger.LogWarningAsync($"Circuit breaker for {_providerName} received failure in HalfOpen state - reopening circuit").GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Reset the circuit breaker to its initial closed state
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _successCount = 0;
                _stateChangedTime = DateTime.UtcNow;
                _logger.LogInfoAsync($"Circuit breaker for {_providerName} manually reset to Closed state").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Get the current failure rate (0.0 to 1.0)
        /// </summary>
        /// <returns>Failure rate as a percentage</returns>
        public double GetFailureRate()
        {
            lock (_lock)
            {
                if (_failureCount + _successCount == 0)
                    return 0.0;

                return (double)_failureCount / (_failureCount + _successCount);
            }
        }
    }

    /// <summary>
    /// Factory for creating circuit breakers
    /// </summary>
    public interface ICircuitBreakerFactory
    {
        ICircuitBreaker Create(string providerName);
    }

    /// <summary>
    /// Circuit breaker factory implementation
    /// </summary>
    public sealed class CircuitBreakerFactory : ICircuitBreakerFactory
    {
        private readonly int _failureThreshold;
        private readonly int _recoveryTimeoutSeconds;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new circuit breaker factory.
        /// </summary>
        /// <param name="logger">Logger for circuit breaker events</param>
        /// <param name="failureThreshold">Default failure threshold for all circuit breakers</param>
        /// <param name="recoveryTimeoutSeconds">Default recovery timeout for all circuit breakers</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public CircuitBreakerFactory(ILogger logger, int failureThreshold = 5, int recoveryTimeoutSeconds = 60)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _failureThreshold = failureThreshold > 0 ? failureThreshold : 5;
            _recoveryTimeoutSeconds = recoveryTimeoutSeconds > 0 ? recoveryTimeoutSeconds : 60;
        }

        /// <summary>
        /// Create a circuit breaker for a specific provider
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <returns>New circuit breaker instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when providerName is null</exception>
        /// <exception cref="ArgumentException">Thrown when providerName is null or empty</exception>
        public ICircuitBreaker Create(string providerName)
        {
            ArgumentNullException.ThrowIfNull(providerName);
            ArgumentException.ThrowIfNullOrEmpty(providerName);

            return new CircuitBreaker(
                providerName,
                _logger,
                _failureThreshold,
                _recoveryTimeoutSeconds
            );
        }
    }

    /// <summary>
    /// No-op circuit breaker that never opens (for testing)
    /// </summary>
    public sealed class NoOpCircuitBreaker : ICircuitBreaker
    {
        public CircuitState CurrentState => CircuitState.Closed;
        public int FailureCount => 0;
        public int SuccessCount => 0;

        public Task<bool> ExecuteAsync(Func<Task<bool>> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation();
        }

        public void RecordSuccess() { }
        public void RecordFailure() { }
        public void Reset() { }
    }

    /// <summary>
    /// Always-open circuit breaker that always fails (for testing circuit breaker behavior)
    /// </summary>
    public sealed class AlwaysOpenCircuitBreaker : ICircuitBreaker
    {
        public CircuitState CurrentState => CircuitState.Open;
        public int FailureCount => int.MaxValue;
        public int SuccessCount => 0;

        public Task<bool> ExecuteAsync(Func<Task<bool>> operation)
        {
            return Task.FromResult(false);
        }

        public void RecordSuccess() { }
        public void RecordFailure() { }
        public void Reset() { }
    }

    /// <summary>
    /// Factory for creating no-op circuit breakers (for testing)
    /// </summary>
    public sealed class NoOpCircuitBreakerFactory : ICircuitBreakerFactory
    {
        public ICircuitBreaker Create(string providerName)
        {
            ArgumentNullException.ThrowIfNull(providerName);
            ArgumentException.ThrowIfNullOrEmpty(providerName);

            return new NoOpCircuitBreaker();
        }
    }
}