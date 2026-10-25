# CircuitBreaker

`CircuitBreaker` protects calls to an external notification provider from repeated failures. After a configurable number of failures it opens the circuit and blocks further calls for a recovery period. Once that period has elapsed, the next call is let through as a recovery test: success closes the circuit again, failure reopens it. The types live in the `CaddyVpsToolkit.Notifications` namespace and are defined in `Notifications/CircuitBreaker.cs`.

The public surface consists of:

- `CircuitState` - the enum of breaker states (`Closed`, `Open`, `HalfOpen`).
- `ICircuitBreaker` - the breaker abstraction used by consumers.
- `CircuitBreaker` - the production implementation.
- `ICircuitBreakerFactory` / `CircuitBreakerFactory` - create one breaker per provider with shared defaults.
- `NoOpCircuitBreaker`, `AlwaysOpenCircuitBreaker`, `NoOpCircuitBreakerFactory` - test doubles.

## API

### `enum CircuitState`

The current state of a circuit breaker.

| Value | Meaning |
|-------|---------|
| `Closed` | Normal operation. Calls are executed and failures are counted. |
| `Open` | Calls are blocked and `ExecuteAsync` returns `false` without running the operation. |
| `HalfOpen` | The recovery timeout has elapsed. Calls are executed as recovery tests. |

---

### `interface ICircuitBreaker`

Contract for a breaker that guards one provider.

**Members:**  
- `CircuitState CurrentState { get; }` - current state.  
- `int FailureCount { get; }` - failures counted since the last state transition.  
- `int SuccessCount { get; }` - successes counted while `Closed` since the last state transition.  
- `Task<bool> ExecuteAsync(Func<Task<bool>> operation)` - runs `operation` under breaker protection.  
- `void RecordSuccess()` - records a successful operation without running one.  
- `void RecordFailure()` - records a failed operation without running one.  
- `void Reset()` - returns the breaker to `Closed`.

---

### `class CircuitBreaker`

Sealed implementation of `ICircuitBreaker`. All state transitions are guarded by an internal lock, so one instance can be shared by concurrent callers.

#### Constructor

```csharp
public CircuitBreaker(
    string providerName,
    ILogger logger,
    int failureThreshold = 5,
    int recoveryTimeoutSeconds = 60)
```

Creates a breaker in the `Closed` state.

**Parameters:**  
- `providerName` - name of the provider being monitored. Used in log messages.  
- `logger` - an `ILogger` (`CaddyVpsToolkit.Middleware`) that receives breaker events.  
- `failureThreshold` - failures required to open the circuit from `Closed`. Values `<= 0` are replaced with `5`.  
- `recoveryTimeoutSeconds` - how long the circuit stays `Open` before a recovery test is allowed. Values `<= 0` are replaced with `60`.

**Throws:**  
- `ArgumentNullException` if `providerName` or `logger` is `null`.  
- `ArgumentException` if `providerName` is empty.

#### Properties

- `CurrentState` - the current `CircuitState`.  
- `FailureCount` - failures counted since the last state transition.  
- `SuccessCount` - successes counted while `Closed` since the last state transition.

#### `async Task<bool> ExecuteAsync`

Runs `operation` if the circuit allows it and records the result.

Behavior, by state:

- **`Closed`** - `operation` runs. `true` records a success; `false` records a failure. When the failure count reaches the threshold, the circuit opens.  
- **`Open`** - if the recovery timeout has not elapsed, `operation` is not run and the method returns `false`. If it has elapsed, the circuit moves to `HalfOpen` and `operation` runs.  
- **`HalfOpen`** - `operation` runs as a recovery test. `true` closes the circuit; `false` reopens it.

If `operation` throws, the exception is logged, the call is recorded as a failure, and the method returns `false`. The exception is not rethrown.

**Parameters:**  
- `operation` - a delegate that performs the work and returns `true` on success.

**Returns:**  
`true` if the operation succeeded. `false` if the operation reported failure, threw, or was blocked by an open circuit.

**Throws:**  
- `ArgumentNullException` if `operation` is `null`.

#### `void RecordSuccess`

Records a success without running an operation. In `HalfOpen`, this closes the circuit. In `Closed`, it increments `SuccessCount`. In `Open`, it has no effect.

**Parameters:** None.

**Returns:** `void`.

**Throws:** None.

#### `void RecordFailure`

Records a failure without running an operation. In `Closed`, the circuit opens when the failure count reaches the threshold. In `HalfOpen`, the circuit reopens.

**Parameters:** None.

**Returns:** `void`.

**Throws:** None.

#### `void Reset`

Forces the breaker to `Closed` and clears both counters. Use this for operator actions such as a manual retry after a provider outage has been fixed.

**Parameters:** None.

**Returns:** `void`.

**Throws:** None.

#### `double GetFailureRate`

Returns the failure ratio since the last state transition: `FailureCount / (FailureCount + SuccessCount)`. This method exists only on the concrete `CircuitBreaker` class, not on `ICircuitBreaker`.

**Parameters:** None.

**Returns:** A value from `0.0` to `1.0`. Returns `0.0` when no outcomes have been recorded.

**Throws:** None.

---

### `interface ICircuitBreakerFactory`

**Members:**  
- `ICircuitBreaker Create(string providerName)` - creates a breaker for one provider.

---

### `class CircuitBreakerFactory`

Sealed implementation of `ICircuitBreakerFactory`. It holds shared thresholds and creates a new `CircuitBreaker` for each provider.

#### Constructor

```csharp
public CircuitBreakerFactory(ILogger logger, int failureThreshold = 5, int recoveryTimeoutSeconds = 60)
```

**Parameters:**  
- `logger` - passed to every breaker it creates.  
- `failureThreshold` - default failure threshold. Values `<= 0` are replaced with `5`.  
- `recoveryTimeoutSeconds` - default recovery timeout. Values `<= 0` are replaced with `60`.

**Throws:**  
- `ArgumentNullException` if `logger` is `null`.

#### `ICircuitBreaker Create`

Creates a new breaker for `providerName`. Each call returns a new instance, so providers do not share state.

**Parameters:**  
- `providerName` - name of the provider. Must not be empty.

**Returns:**  
A new `ICircuitBreaker` (a `CircuitBreaker`) in the `Closed` state.

**Throws:**  
- `ArgumentNullException` if `providerName` is `null`.  
- `ArgumentException` if `providerName` is empty.

---

### `class NoOpCircuitBreaker`

Test double that never opens. `ExecuteAsync` runs the operation and returns its result. `CurrentState` is always `Closed`, and `FailureCount` and `SuccessCount` are always `0`. `RecordSuccess`, `RecordFailure`, and `Reset` do nothing.

**Throws:** `ArgumentNullException` from `ExecuteAsync` if `operation` is `null`.

---

### `class AlwaysOpenCircuitBreaker`

Test double that is always open. `ExecuteAsync` returns `Task.FromResult(false)` without running the operation. `CurrentState` is always `Open`, and `FailureCount` is `int.MaxValue`.

---

### `class NoOpCircuitBreakerFactory`

Test double for `ICircuitBreakerFactory`. `Create` returns a new `NoOpCircuitBreaker`.

**Throws:** `ArgumentNullException` if `providerName` is `null`. `ArgumentException` if `providerName` is empty.

## Usage

### Example 1: Guarding a single provider call

```csharp
using CaddyVpsToolkit.Middleware;
using CaddyVpsToolkit.Notifications;

ILogger logger = new FileLogger("/var/log/caddy-vps-toolkit/app.log");
var breaker = new CircuitBreaker("telegram", logger, failureThreshold: 3, recoveryTimeoutSeconds: 120);

bool delivered = await breaker.ExecuteAsync(async () =>
{
    // Return true on success, false on failure
    return await telegramClient.SendAsync(message);
});

if (!delivered)
{
    // Either the send failed or the circuit is Open and the call was skipped
    Console.WriteLine($"Notification not delivered; state={breaker.CurrentState}");
}
```

### Example 2: One breaker per provider via the factory

```csharp
var factory = new CircuitBreakerFactory(logger, failureThreshold: 5, recoveryTimeoutSeconds: 60);

// Each provider gets its own independent state
ICircuitBreaker telegramBreaker = factory.Create("telegram");
ICircuitBreaker emailBreaker = factory.Create("email");

await telegramBreaker.ExecuteAsync(() => telegramClient.SendAsync(message));
await emailBreaker.ExecuteAsync(() => emailClient.SendAsync(message));
```

### Example 3: Handling exceptions without a try/catch

```csharp
// Exceptions thrown inside the operation are logged and counted as failures.
// ExecuteAsync returns false instead of rethrowing.
bool ok = await breaker.ExecuteAsync(async () =>
{
    await httpClient.PostAsync(url, content); // may throw HttpRequestException
    return true;
});
```

### Example 4: Recording outcomes that happen outside ExecuteAsync

```csharp
// Useful when the call is made elsewhere but the breaker should still track it
try
{
    await externalSend();
    breaker.RecordSuccess();
}
catch (Exception)
{
    breaker.RecordFailure();
}
```

### Example 5: Manual reset and failure-rate inspection

```csharp
var concrete = new CircuitBreaker("telegram", logger);

if (concrete.CurrentState == CircuitState.Open)
{
    // Operator confirmed the provider is back; close the circuit now
    concrete.Reset();
}

double rate = concrete.GetFailureRate(); // 0.0 to 1.0
Console.WriteLine($"Failure rate since last transition: {rate:P0}");
```

### Example 6: Using test doubles

```csharp
// Never-opening breaker: the operation always runs and its result is returned
ICircuitBreakerFactory noOpFactory = new NoOpCircuitBreakerFactory();
ICircuitBreaker noop = noOpFactory.Create("test-provider");

bool result = await noop.ExecuteAsync(() => Task.FromResult(true)); // true; never blocked

ICircuitBreaker open = new AlwaysOpenCircuitBreaker();
bool blocked = await open.ExecuteAsync(() => Task.FromResult(true)); // false; operation not run
```

## Notes

- **Thread safety:** `ExecuteAsync`, `RecordSuccess`, `RecordFailure`, `Reset`, and `GetFailureRate` take the same internal lock. `CurrentState`, `FailureCount`, and `SuccessCount` read fields without the lock, so they may be momentarily stale under concurrency.
- **Logging under lock:** `RecordSuccess`, `RecordFailure`, and `Reset` call the logger synchronously (`GetAwaiter().GetResult()`) while holding the lock. Keep the `ILogger` implementation fast and non-blocking.
- **Lazy transition to `HalfOpen`:** the circuit does not change state on a timer. It moves from `Open` to `HalfOpen` only when `ExecuteAsync` is called after the recovery timeout has elapsed.
- **`FailureCount` semantics:** the counter is reset only on state transitions (and by `Reset`). Successes recorded while `Closed` do not reset it, so failures accumulate across successes until the circuit opens. This differs from the word "consecutive" in the XML comment.
- **`HalfOpen` admission:** despite the log message "allowing test request", every call that arrives while the circuit is `HalfOpen` is executed. There is no single-probe gate, so concurrent callers can all run as recovery tests.
- **Invalid settings:** `failureThreshold` and `recoveryTimeoutSeconds` values of `0` or less are silently replaced with the defaults (`5` and `60`) rather than rejected.
- **Provider names in logs:** log messages are built with interpolation and then passed through `string.Format`. Avoid provider names containing `{` or `}`.
- **Interface scope:** `GetFailureRate` is not part of `ICircuitBreaker`. Code that needs it must depend on `CircuitBreaker` directly.
