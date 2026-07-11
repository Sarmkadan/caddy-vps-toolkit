# IRateLimiter

`IRateLimiter` defines the contract for rate-limiting strategies in the `caddy-vps-toolkit` project. It exposes two concrete implementations—`TokenBucketRateLimiter` and `FixedWindowRateLimiter`—along with a shared asynchronous permission-checking method. Implementations govern how callers are throttled, either by a token bucket that refills over time or by a fixed time window that resets request counts at a known boundary.

## API

### TokenBucketRateLimiter
A rate limiter based on the token bucket algorithm. It holds a configurable bucket of tokens that depletes with each allowed request and refills at a steady rate. This type is exposed as a public property or field for direct access to its internal state and consumption methods.

- **`TokenBucket`**  
  The underlying token bucket instance that tracks current token count, capacity, and refill rate. Consumers can inspect this to determine available capacity or to adjust parameters at runtime.

- **`bool TryConsume`**  
  Attempts to consume a single token from the bucket synchronously. Returns `true` if a token was available and the request should proceed; `false` if the bucket is empty and the request must be denied or delayed. Does not throw.

### FixedWindowRateLimiter
A rate limiter that divides time into fixed-size windows. It counts requests within the current window and resets the count when the window expires. This type is exposed as a public property or field for direct inspection of window state.

- **`DateTime StartTime`**  
  The UTC timestamp marking the beginning of the current fixed window. All requests occurring between `StartTime` and `StartTime + windowDuration` are counted against the same limit.

- **`int RequestCount`**  
  The number of requests that have been allowed so far within the current window. Resets to zero when the window advances.

### async Task<bool> AllowAsync
Asynchronously determines whether a request is permitted under the active rate-limiting policy. Both `TokenBucketRateLimiter` and `FixedWindowRateLimiter` expose this method with their respective internal logic.

- **Parameters**  
  None. The method evaluates state internal to the limiter instance.

- **Returns**  
  A `Task<bool>` that resolves to `true` if the request is allowed, or `false` if the limit has been exceeded and the caller should back off.

- **Exceptions**  
  Implementations may throw `ObjectDisposedException` if the limiter has been disposed before the call completes. Other exceptions depend on internal timer or synchronization failures and are implementation-specific.

## Usage

### Example 1: Token bucket rate limiting for outgoing API calls
```csharp
var bucketLimiter = new TokenBucketRateLimiter(
    maxTokens: 10,
    refillRate: TimeSpan.FromSeconds(1));

// Inspect current bucket state
Console.WriteLine($"Tokens available: {bucketLimiter.TokenBucket.CurrentTokens}");

for (int i = 0; i < 15; i++)
{
    if (await bucketLimiter.AllowAsync())
    {
        await CallExternalApiAsync();
    }
    else
    {
        Console.WriteLine("Rate limit hit; backing off.");
        await Task.Delay(200);
    }
}
```

### Example 2: Fixed window rate limiting for a web endpoint
```csharp
var windowLimiter = new FixedWindowRateLimiter(
    maxRequests: 100,
    windowDuration: TimeSpan.FromMinutes(1));

// Log window state for monitoring
Console.WriteLine(
    $"Window starts at {windowLimiter.StartTime:O}, " +
    $"requests so far: {windowLimiter.RequestCount}");

while (true)
{
    var request = await ListenForRequestAsync();
    if (await windowLimiter.AllowAsync())
    {
        await ProcessRequestAsync(request);
    }
    else
    {
        await RejectWithStatusCodeAsync(429);
        // Window will eventually roll over and reset RequestCount automatically
    }
}
```

## Notes

- **Thread safety**  
  Both `TokenBucketRateLimiter` and `FixedWindowRateLimiter` are designed for concurrent use. `AllowAsync` and `TryConsume` (on the token bucket) employ internal synchronization to ensure that token counts and window counters remain consistent under parallel callers. Reading `StartTime` or `RequestCount` without external locking yields a point-in-time snapshot that may already be stale.

- **Edge cases**  
  - `FixedWindowRateLimiter`: At the exact boundary between windows, a burst of requests that arrive before the internal window rollover completes may be counted against the old window, the new window, or rejected depending on implementation timing. Callers should not assume perfectly sharp cutoffs.  
  - `TokenBucketRateLimiter`: If the bucket is configured with zero max tokens or a zero refill rate, `AllowAsync` and `TryConsume` will always return `false`. A negative refill rate is invalid and constructor behavior is implementation-defined.  
  - `AllowAsync` may complete synchronously (with `Task.FromResult`) when no delay or I/O is required; callers should not rely on it yielding.

- **Disposal**  
  Limiters that own timers or other disposable resources should be disposed when no longer needed. Calling `AllowAsync` after disposal results in undefined behavior, typically an `ObjectDisposedException`.
