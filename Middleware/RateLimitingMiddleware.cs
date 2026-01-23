#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Middleware
{
    /// <summary>
    /// Rate limiting implementation to prevent abuse of operations.
    /// Uses token bucket algorithm for flexible rate limiting with burst capacity.
    /// </summary>
    public interface IRateLimiter
    {
        Task<bool> AllowAsync(string key);
    }

    /// <summary>
    /// Token bucket rate limiter - allows burst traffic up to bucket size,
    /// then enforces per-second rate limit. Common in production systems.
    /// </summary>
    public sealed class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly Dictionary<string, TokenBucket> _buckets = new();
        private readonly int _capacity;
        private readonly int _refillRatePerSecond;
        private readonly object _lockObject = new();

        public TokenBucketRateLimiter(int capacity, int refillRatePerSecond)
        {
            _capacity = capacity;
            _refillRatePerSecond = refillRatePerSecond;
        }

        public async Task<bool> AllowAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key required", nameof(key));

            lock (_lockObject)
            {
                if (!_buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new TokenBucket(_capacity, _refillRatePerSecond);
                    _buckets[key] = bucket;
                }

                return bucket.TryConsume();
            }
        }

        private class TokenBucket
        {
            private double _tokens;
            private readonly int _capacity;
            private readonly int _refillRate;
            private DateTime _lastRefillTime;

            public TokenBucket(int capacity, int refillRate)
            {
                _capacity = capacity;
                _refillRate = refillRate;
                _tokens = capacity;
                _lastRefillTime = DateTime.UtcNow;
            }

            public bool TryConsume()
            {
                RefillTokens();

                if (_tokens >= 1)
                {
                    _tokens--;
                    return true;
                }

                return false;
            }

            private void RefillTokens()
            {
                var now = DateTime.UtcNow;
                var timePassed = (now - _lastRefillTime).TotalSeconds;
                var tokensToAdd = timePassed * _refillRate;

                _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                _lastRefillTime = now;
            }
        }
    }

    /// <summary>
    /// Simple fixed-window rate limiter - counts requests in fixed time windows.
    /// Simpler than token bucket but can allow brief bursts at window boundaries.
    /// </summary>
    public sealed class FixedWindowRateLimiter : IRateLimiter
    {
        private readonly Dictionary<string, RequestWindow> _windows = new();
        private readonly int _maxRequestsPerWindow;
        private readonly int _windowSizeSeconds;
        private readonly object _lockObject = new();

        public FixedWindowRateLimiter(int maxRequests, int windowSeconds = 60)
        {
            _maxRequestsPerWindow = maxRequests;
            _windowSizeSeconds = windowSeconds;
        }

        public async Task<bool> AllowAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key required", nameof(key));

            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                if (!_windows.TryGetValue(key, out var window))
                {
                    window = new RequestWindow();
                    _windows[key] = window;
                }

                // Check if window has expired
                if ((now - window.StartTime).TotalSeconds >= _windowSizeSeconds)
                {
                    window.StartTime = now;
                    window.RequestCount = 0;
                }

                if (window.RequestCount < _maxRequestsPerWindow)
                {
                    window.RequestCount++;
                    return true;
                }

                return false;
            }
        }

        private class RequestWindow
        {
            public DateTime StartTime { get; set; } = DateTime.UtcNow;
            public int RequestCount { get; set; }
        }
    }
}
