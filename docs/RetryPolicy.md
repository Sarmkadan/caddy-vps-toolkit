# RetryPolicy

The retry policies in `Utilities/RetryPolicy.cs` re-run an asynchronous operation when it throws. They share one abstraction, `IRetryPolicy`, and three implementations: exponential backoff with jitter, linear backoff, and a no-retry pass-through. The types live in the `CaddyVpsToolkit.Utilities` namespace.

The public surface consists of:

- `IRetryPolicy` - the abstraction consumed by services such as `NotificationService` and `HttpClientWrapper`.
- `ExponentialBackoffRetryPolicy` - exponential backoff with random jitter. Use it for transient failures against remote services.
- `LinearBackoffRetryPolicy` - delay grows by a fixed increment on each retry.
- `NoRetryPolicy` - runs the operation once. It is the default when a consumer is given no policy.

## API

### `interface IRetryPolicy`

Contract for a policy that executes an operation with retries. All members return a task that completes with the operation's result, or faults with the exception of the final failed attempt.

**Members:**  
- `Task<T> ExecuteAsync<T>(Func<Task<T>> operation)` - runs `operation` with the policy applied.  
- `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)` - as above, and passes `cancellationToken` to the operation and to any delay.  
- `Task ExecuteAsync(Func<Task> operation)` - non-generic form for operations with no result.  
- `Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)` - non-generic, cancellable form.

---

### `class ExponentialBackoffRetryPolicy`

Sealed implementation of `IRetryPolicy`. After each failed attempt it waits for a jittered delay, then multiplies the nominal delay by `backoffMultiplier`, capped at `maxDelayMs`. The jitter spreads retries from many callers over time to avoid the thundering herd problem.

#### Constructor

```csharp
public ExponentialBackoffRetryPolicy(
    int maxRetries = 3,
    int initialDelayMs = 100,
    double backoffMultiplier = 2.0,
    int maxDelayMs = 10000)
```

Creates a policy with the given settings. The constructor validates its arguments and throws if any are out of range.

**Parameters:**  
- `maxRetries` - number of retries after the first attempt. Defaults to `3`, so up to 4 attempts in total. `0` disables retries.  
- `initialDelayMs` - nominal delay before the first retry, in milliseconds. Defaults to `100`.  
- `backoffMultiplier` - factor applied to the nominal delay after each retry. Defaults to `2.0`.  
- `maxDelayMs` - upper bound for the nominal delay, in milliseconds. Defaults to `10000`.

**Throws:**  
- `ArgumentOutOfRangeException` if `maxRetries` is negative.  
- `ArgumentOutOfRangeException` if `initialDelayMs` is less than `1`.  
- `ArgumentOutOfRangeException` if `backoffMultiplier` is less than `1.0`.  
- `ArgumentOutOfRangeException` if `maxDelayMs` is less than `initialDelayMs`.

#### `Task<T> ExecuteAsync<T>(Func<Task<T>> operation)`

Runs `operation` with retries. Equivalent to calling the cancellable overload with `CancellationToken.None`.

**Parameters:**  
- `operation` - the asynchronous operation to run. It may be invoked up to `maxRetries + 1` times.

**Returns:**  
A task that completes with the result of the first successful attempt.

**Throws:**  
- `ArgumentNullException` if `operation` is `null`.  
- The exception from the last attempt, if every attempt fails. The exception is rethrown as-is, not wrapped.

#### `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)`

Runs `operation` with retries and cancellation support.

Behavior:

- The token is checked before every attempt. If it is already cancelled, `OperationCanceledException` is thrown and the operation is not run.  
- Any exception thrown by `operation` triggers a retry, except when the token has been cancelled. In that case the `OperationCanceledException` is rethrown immediately.  
- The wait between attempts uses `Task.Delay` with the same token. Cancelling during the wait stops the policy.

**Parameters:**  
- `operation` - receives the `CancellationToken` and returns the result.  
- `cancellationToken` - cancels the retry loop, the pending delay, and the operation.

**Returns:**  
A task that completes with the result of the first successful attempt.

**Throws:**  
- `ArgumentNullException` if `operation` is `null`.  
- `OperationCanceledException` if `cancellationToken` is cancelled.  
- The exception from the last attempt, if every attempt fails.

#### `Task ExecuteAsync(Func<Task> operation)` and `Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)`

Non-generic forms of the methods above. They delegate to the generic implementation, so retry and cancellation behavior is identical.

**Throws:** `ArgumentNullException` if `operation` is `null`.

---

### `class LinearBackoffRetryPolicy`

Sealed implementation of `IRetryPolicy`. Before retry *n* (counting from 1), it waits `delayIncrementMs * n` milliseconds. With the defaults the waits are 500 ms, 1000 ms, and 1500 ms. There is no jitter and no upper bound.

#### Constructor

```csharp
public LinearBackoffRetryPolicy(int maxRetries = 3, int delayIncrementMs = 500)
```

**Parameters:**  
- `maxRetries` - number of retries after the first attempt. Defaults to `3`.  
- `delayIncrementMs` - delay added per retry, in milliseconds. Defaults to `500`.

**Throws:**  
- `ArgumentOutOfRangeException` if `maxRetries` is negative.  
- `ArgumentOutOfRangeException` if `delayIncrementMs` is negative.

#### `Task<T> ExecuteAsync<T>(Func<Task<T>> operation)` and `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)`

Runs `operation` with linear backoff. Every exception triggers a retry until `maxRetries` is exhausted. Cancellation is handled the same way as in `ExponentialBackoffRetryPolicy`: if the token is cancelled, the `OperationCanceledException` is rethrown without a retry.

**Parameters:** As for `ExponentialBackoffRetryPolicy`.

**Returns:** A task that completes with the result of the first successful attempt.

**Throws:**  
- `ArgumentNullException` if `operation` is `null`.  
- `OperationCanceledException` if the token is cancelled.  
- The exception from the last attempt, if every attempt fails. It is rethrown unwrapped.

#### `Task ExecuteAsync(Func<Task> operation)` and `Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)`

Non-generic forms. They delegate to the generic implementation.

---

### `class NoRetryPolicy`

Sealed implementation of `IRetryPolicy` that runs the operation exactly once. It checks the token before running and does not retry on failure. Exceptions from the operation propagate unchanged.

This is the default policy in `NotificationService` and `HttpClientWrapper` when no policy is supplied.

#### Constructor

Implicit parameterless constructor. No configuration.

#### `Task<T> ExecuteAsync<T>(Func<Task<T>> operation)` and `Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)`

**Parameters:**  
- `operation` - runs once.  
- `cancellationToken` - checked before the operation starts.

**Returns:** A task that completes with the operation's result.

**Throws:**  
- `ArgumentNullException` if `operation` is `null`.  
- `OperationCanceledException` if the token is already cancelled when the method is called. The operation is not run.  
- Any exception thrown by the operation.

#### `Task ExecuteAsync(Func<Task> operation)` and `Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)`

Non-generic forms. Same behavior as the generic forms.

## Usage

### Example 1: Retrying a transient HTTP call with defaults

```csharp
using CaddyVpsToolkit.Utilities;

IRetryPolicy policy = new ExponentialBackoffRetryPolicy(); // 3 retries, 100 ms initial delay, x2, capped at 10 s

string body = await policy.ExecuteAsync(async () =>
{
    using var response = await httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode(); // throws on 5xx, triggers a retry
    return await response.Content.ReadAsStringAsync();
});
```

### Example 2: Custom exponential backoff with cancellation

```csharp
var policy = new ExponentialBackoffRetryPolicy(
    maxRetries: 5,
    initialDelayMs: 250,
    backoffMultiplier: 1.5,
    maxDelayMs: 5000);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// The token stops the retry loop and any pending delay
await policy.ExecuteAsync(async token =>
{
    await SendNotificationAsync(message, token);
}, cts.Token);
```

### Example 3: Linear backoff for an operation with no result

```csharp
// Waits 200 ms, then 400 ms, then 600 ms between attempts
var policy = new LinearBackoffRetryPolicy(maxRetries: 3, delayIncrementMs: 200);

await policy.ExecuteAsync(() => RotateLogFileAsync());
```

### Example 4: Passing a policy into a consumer

```csharp
// NotificationService falls back to NoRetryPolicy when no policy is given
var notifications = new NotificationService(
    logger,
    retryPolicy: new ExponentialBackoffRetryPolicy(maxRetries: 2));
```

### Example 5: Running once with no retries

```csharp
// Useful in tests or when the caller handles retries itself
IRetryPolicy once = new NoRetryPolicy();

bool ok = await once.ExecuteAsync(async () =>
{
    return await probe.CheckAsync();
});
```

### Example 6: Handling the final failure

```csharp
var policy = new LinearBackoffRetryPolicy(maxRetries: 2, delayIncrementMs: 100);

try
{
    await policy.ExecuteAsync(() => ConnectToUpstreamAsync());
}
catch (Exception ex)
{
    // Thrown after the third attempt (initial + 2 retries) fails
    logger.LogError(ex, "Upstream unreachable after retries");
}
```

## Notes

- **Attempt count:** `maxRetries` counts retries, not attempts. A policy with `maxRetries: 3` runs the operation up to 4 times.
- **Exception propagation:** when all attempts fail, the exception from the last attempt is rethrown. No `AggregateException` or wrapper is created. Earlier failures are not kept.
- **Which exceptions are retried:** every `Exception` thrown by the operation is retried, including `HttpRequestException`, `TimeoutException`, and `ArgumentException`. The API has no per-exception filter. To avoid retrying a permanent error, catch it inside the operation and return a result that signals failure, or use `NoRetryPolicy` for that call.
- **Cancellation vs. operation timeouts:** an `OperationCanceledException` from the operation is retried unless the supplied token is cancelled. Only the caller's token stops the loop. A timeout thrown by the operation, such as an `HttpClient` timeout, is treated as an ordinary failure.
- **Jitter:** `ExponentialBackoffRetryPolicy` picks each wait at random between 10% and 110% of the nominal delay, using `Random.Shared`. Actual waits are not reproducible from run to run.
- **Delay cap:** the cap applies to the nominal delay before jitter, so the actual wait can reach roughly 110% of `maxDelayMs`.
- **Zero delay increment:** `LinearBackoffRetryPolicy` accepts `delayIncrementMs: 0`, which retries immediately. The XML comment on the constructor says values "less than or equal to zero" are rejected, but the code only rejects negative values.
- **Final-attempt wrapper:** `LinearBackoffRetryPolicy` ends with a defensive `InvalidOperationException("Retry policy exhausted")`. It is unreachable in practice, because the last failed attempt rethrows the original exception.
- **Operation reuse:** the delegate is invoked once per attempt. It must be safe to call repeatedly and should not depend on state changed by a previous failed attempt.
- **Thread safety:** all policy classes are immutable after construction, so one instance can be shared across concurrent callers.
