# RateLimitingMiddleware

`RateLimitingMiddleware.cs` defines the rate-limiting primitives used by the `caddy-vps-toolkit` project to throttle operations per caller. The file contains one interface, `IRateLimiter`, and two implementations: `TokenBucketRateLimiter`, which allows bursts up to a bucket size and then refills at a steady rate, and `FixedWindowRateLimiter`, which counts requests in fixed time windows. All three types live in the `CaddyVpsToolkit.Middleware` namespace.

Both implementations track limits separately for each string key (for example a client IP, a service name, or an API route), so one noisy caller does not exhaust the budget of another. Each limiter instance is safe to share across threads.

## API

### IRateLimiter
- **Signature:** `public interface IRateLimiter`
- **Purpose:** The contract that consumers should depend on. Code that needs throttling takes an `IRateLimiter` so the strategy can be swapped without changing call sites.
- **Members:**

| Member | Description |
|--------|-------------|
| `Task<bool> AllowAsync(string key)` | Records one request for `key` and returns `true` if it is within the limit, or `false` if it must be rejected. |

### TokenBucketRateLimiter(int capacity, int refillRatePerSecond)
- **Signature:** `public sealed class TokenBucketRateLimiter : IRateLimiter`
- **Purpose:** Token bucket limiter. Each key gets its own bucket that starts full, so an idle caller can send up to `capacity` requests immediately. Afterwards, tokens return at `refillRatePerSecond` per second, capped at `capacity`.
- **Parameters:**
  - `capacity` - the maximum number of tokens a bucket can hold, which is also the largest burst allowed for a key.
  - `refillRatePerSecond` - the number of tokens added to each bucket per second. Fractional refill accumulates between calls.
- **Remarks:** A bucket is created on the first `AllowAsync` call for a key. Each allowed request consumes exactly one token. Refill is calculated lazily on each call from the elapsed UTC time, so no background timer runs.

### FixedWindowRateLimiter(int maxRequests, int windowSeconds = 60)
- **Signature:** `public sealed class FixedWindowRateLimiter : IRateLimiter`
- **Purpose:** Fixed window limiter. Each key may make up to `maxRequests` requests within a window. When the window expires, the count resets.
- **Parameters:**
  - `maxRequests` - the number of requests allowed per window for each key.
  - `windowSeconds` - the length of the window in seconds. Defaults to `60`.
- **Remarks:** A key's window opens on its first request, or on the first request after the previous window expired. Windows are not aligned to the clock, so a window that starts at `12:00:37` ends at `12:01:37`. Expiry is checked only when a request arrives.

### AllowAsync(string key)
- **Signature:** `public async Task<bool> AllowAsync(string key)` (implemented by both limiters)
- **Purpose:** Checks the limit for `key` and consumes one unit of budget when the request is allowed.
- **Parameters:** `key` - the identifier of the caller or resource being limited. Keys are compared with the default `string` equality, so they are case-sensitive.
- **Returns:** `Task<bool>`. `true` when the request is allowed, `false` when the limit is reached. A rejected request does not consume budget.
- **Throws:** `ArgumentException` (parameter name `key`) when `key` is `null` or empty. Whitespace-only keys are accepted.

## Usage

### Example 1: Per-client burst limiting with a token bucket
```csharp
// Allow bursts of 20 requests per client, refilling 5 tokens per second
IRateLimiter limiter = new TokenBucketRateLimiter(capacity: 20, refillRatePerSecond: 5);

async Task HandleRequestAsync(string clientIp, Func<Task> processRequest)
{
    if (await limiter.AllowAsync(clientIp))
    {
        await processRequest();
    }
    else
    {
        // Reject the request; the caller should retry after a short delay
        Console.WriteLine($"Rate limit exceeded for {clientIp}");
    }
}
```

### Example 2: Limiting an operation per service with a fixed window
```csharp
// At most 5 reloads per service every 60 seconds
IRateLimiter reloadLimiter = new FixedWindowRateLimiter(maxRequests: 5, windowSeconds: 60);

async Task<bool> TryReloadServiceAsync(string serviceName)
{
    if (!await reloadLimiter.AllowAsync(serviceName))
    {
        return false;
    }

    // Perform the reload here
    return true;
}
```

### Example 3: Registering the limiter with dependency injection
```csharp
// Registers IRateLimiter as a singleton backed by TokenBucketRateLimiter
services.AddRateLimiting(capacity: 50, refillRate: 10);

// Later, resolve it where needed
public sealed class WebhookThrottle(IRateLimiter limiter)
{
    public Task<bool> ShouldAcceptAsync(string sender) => limiter.AllowAsync(sender);
}
```

`AddRateLimiting` (in `ServiceCollectionExtensions`) uses the default values `capacity: 100` and `refillRate: 10` when arguments are omitted. To use the fixed window strategy with DI, register it directly with `services.AddSingleton<IRateLimiter>(_ => new FixedWindowRateLimiter(maxRequests: 100))`.

### Example 4: Handling invalid keys
```csharp
string? key = GetCallerKey();

if (string.IsNullOrEmpty(key))
{
    // AllowAsync would throw ArgumentException for a null or empty key
    return false;
}

return await limiter.AllowAsync(key);
```

## Notes

- **Thread safety**  
  Both implementations guard their dictionaries and counters with a private lock, so a single instance can be shared across threads and across requests. The lock is held only for the bookkeeping, and the returned task is already completed.

- **Memory growth**  
  Per-key state is stored in an in-memory dictionary and is never evicted. Limiters keyed by unbounded values (for example, every client IP ever seen) grow over the life of the process. Use a bounded set of keys, or recreate the limiter periodically, when that is a concern.

- **Fixed window bursts**  
  `FixedWindowRateLimiter` can allow up to twice `maxRequests` within a short span. For example, with `maxRequests: 10` and `windowSeconds: 60`, a caller can send 10 requests at second 59 of one window and 10 more at second 61. `TokenBucketRateLimiter` does not have this boundary effect.

- **Argument validation**  
  The constructors do not validate their arguments. A `capacity` or `maxRequests` of `0` rejects every request. A `refillRatePerSecond` of `0` means a bucket never refills after it is drained. Negative values are accepted without error and produce meaningless behaviour, so validate configuration before constructing a limiter (`ServiceCollectionExtensions` validates `RateLimitCapacity` and `RateLimitRefillRate` as positive values in `ServiceCollectionExtensionsValidation`).

- **Clock source**  
  Both limiters use `DateTime.UtcNow`. Clock adjustments on the host can shift when tokens refill or windows expire.

- **Disposal**  
  Neither implementation holds unmanaged resources or timers, so they do not implement `IDisposable`.
