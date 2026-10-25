# HttpClientWrapperOptions

`HttpClientWrapperOptions` is a small, mutable configuration object for `HttpClientWrapper`. It holds the two settings the wrapper needs at construction time: the request timeout (`TimeoutMs`) and the retry policy applied to each HTTP operation (`RetryPolicy`). The class is designed to be bound from configuration (for example `appsettings.json`) or registered with dependency injection, so the wrapper has no hard-coded literals.

The class is `public sealed` and lives in the `CaddyVpsToolkit.Integration` namespace (`Integration/HttpClientWrapperOptions.cs`). It has a parameterless constructor and no other constructors. It has no `Validate` method and no `ToString` override.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `TimeoutMs` | `int` | Timeout for each HTTP request, in milliseconds. Defaults to `30_000` (30 seconds), which matches the wrapper's previous hard-coded value. | — | The configured timeout. | None. |
| `RetryPolicy` | `IRetryPolicy?` | Retry policy applied to HTTP operations. Defaults to `null`, which means the wrapper uses a `NoRetryPolicy` (one attempt, no retries). | — | The configured policy, or `null`. | None. |

Both properties have public getters and setters. Nothing is validated when a value is assigned, so values such as a zero or negative timeout are accepted here and only reach `HttpClient` when `HttpClientWrapper` is constructed.

`IRetryPolicy` is defined in the `CaddyVpsToolkit.Utilities` namespace, so add `using CaddyVpsToolkit.Utilities;` when you assign a policy.

## Usage

### Example 1: Using the defaults

Passing no options, or a new instance with no changes, gives a 30-second timeout and no retries.

```csharp
using System;
using CaddyVpsToolkit.Integration;

var options = new HttpClientWrapperOptions();

Console.WriteLine(options.TimeoutMs);            // 30000
Console.WriteLine(options.RetryPolicy is null);  // True
```

### Example 2: Setting a longer timeout with an object initializer

```csharp
using CaddyVpsToolkit.Integration;

var options = new HttpClientWrapperOptions
{
    TimeoutMs = 60_000
};

var client = new HttpClientWrapper(options);
```

### Example 3: Adding exponential backoff retries

Assign an `IRetryPolicy` to `RetryPolicy`. The wrapper applies it to each request it sends.

```csharp
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Utilities;

var options = new HttpClientWrapperOptions
{
    TimeoutMs = 10_000,
    RetryPolicy = new ExponentialBackoffRetryPolicy(
        maxRetries: 3,
        initialDelayMs: 200,
        backoffMultiplier: 2.0,
        maxDelayMs: 5_000)
};

var client = new HttpClientWrapper(options);
```

### Example 4: Using a linear backoff policy

```csharp
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Utilities;

var options = new HttpClientWrapperOptions
{
    RetryPolicy = new LinearBackoffRetryPolicy(maxRetries: 2, delayIncrementMs: 1_000)
};

var client = new HttpClientWrapper(options);
```

### Example 5: Binding from configuration

Because both properties have public setters, the class works with the standard options binder. The section below is an example `appsettings.json` fragment; the `RetryPolicy` value is not bound from JSON, so set it in code.

```json
{
  "HttpClientWrapper": {
    "TimeoutMs": 15000
  }
}
```

```csharp
using CaddyVpsToolkit.Integration;
using Microsoft.Extensions.Configuration;

var options = configuration
    .GetSection("HttpClientWrapper")
    .Get<HttpClientWrapperOptions>() ?? new HttpClientWrapperOptions();

options.RetryPolicy = new LinearBackoffRetryPolicy(maxRetries: 3);

var client = new HttpClientWrapper(options);
```

### Example 6: Registering with dependency injection

Register the options as a singleton and let the wrapper be created from them. The wrapper's constructor accepts a nullable `HttpClientWrapperOptions`, so the container can resolve it with the options instance in place.

```csharp
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Utilities;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton(new HttpClientWrapperOptions
{
    TimeoutMs = 20_000,
    RetryPolicy = new ExponentialBackoffRetryPolicy(maxRetries: 2)
});
services.AddSingleton<IHttpClient, HttpClientWrapper>();
```

## Notes

- **Read once at construction.** `HttpClientWrapper` copies `TimeoutMs` into a private field and stores the `RetryPolicy` reference (or a `NoRetryPolicy` when it is `null`) in its constructor. Changing the options object afterwards has no effect on an existing wrapper; create a new wrapper to apply new settings.
- **The timeout is applied to the underlying `HttpClient`.** The constructor sets `HttpClient.Timeout` to `TimeSpan.FromMilliseconds(TimeoutMs)`. A value that `HttpClient` rejects surfaces when the wrapper is constructed, not when the options are assigned.
- **No validation.** `TimeoutMs` and `RetryPolicy` are not checked by the options class. Negative or zero timeouts are accepted here.
- **Null policy means no retries.** Leaving `RetryPolicy` as `null` is the same as passing a `NoRetryPolicy`. A request that fails is returned once, with `IsSuccess = false`.
- **Retries are per request.** The retry policy wraps each `GetAsync`, `PostAsync`, `PutAsync`, and `DeleteAsync` call. Retry delays add to the total time a caller waits, so keep `maxRetries` and the delays small enough for the surrounding operation.
- **Not thread-safe while changing.** The properties are plain auto-properties with no synchronization. Configure the object before sharing it, and do not mutate it while a wrapper is being constructed from it.
- **Backward compatibility.** The `HttpClientWrapper(int timeoutMs, IRetryPolicy? retryPolicy)` constructor builds an `HttpClientWrapperOptions` internally, so both constructors behave the same for the same values.
- **Related types.** See [HttpClientWrapper](HttpClientWrapper.md) for the client itself, [IHttpClient](IHttpClient.md) for the abstraction, and [RetryPolicy](RetryPolicy.md) for the available retry policies.
