# ServiceCollectionExtensionsValidation

`ServiceCollectionExtensionsValidation` is a static class of extension methods that check an `InfrastructureOptions` instance for invalid values and return a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Extensions` namespace, the same namespace as `InfrastructureOptions` and `ServiceCollectionExtensions`, so no extra `using` is needed when those types are already in scope.

Validation checks the numeric limits (`HttpTimeoutMs`, `MaxRetries`, `RateLimitCapacity`, `RateLimitRefillRate`) and the `LogPath` string. It does not check `MinLogLevel`, because that property is an enum and every value is valid. Because the methods are extensions on `InfrastructureOptions`, they can be called on any instance, whether it was built with an object initializer, bound from configuration, or changed through the `configure` callback of `AddInfrastructureServices`.

## API

### `Validate(this InfrastructureOptions value)`
Validates an `InfrastructureOptions` instance and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `HttpTimeoutMs` | Must be greater than zero. | `HttpTimeoutMs must be positive, but was {value}.` |
| `MaxRetries` | Must not be negative. | `MaxRetries must be non-negative, but was {value}.` |
| `LogPath` | Must not be null or whitespace. | `LogPath must not be null or whitespace.` |
| `LogPath` | Must not exceed 1024 characters. Checked only when the path is not null or whitespace. | `LogPath must not exceed 1024 characters.` |
| `RateLimitCapacity` | Must be greater than zero. | `RateLimitCapacity must be positive, but was {value}.` |
| `RateLimitRefillRate` | Must be greater than zero. | `RateLimitRefillRate must be positive, but was {value}.` |

**Parameters:**
- `value` (`InfrastructureOptions`) – The options instance to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the options are valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this InfrastructureOptions value)`
Returns whether the options pass every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`InfrastructureOptions`) – The options instance to check.

**Returns:**
`bool` – `true` if the options have no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `EnsureValid(this InfrastructureOptions value)`
Validates the options and throws if any problem is found. Use it at startup, before the options are passed to service registration, so that a bad configuration fails fast instead of producing a service that misbehaves later.

**Parameters:**
- `value` (`InfrastructureOptions`) – The options instance to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the options are invalid. The message starts with `InfrastructureOptions validation failed:`, followed by one line per problem. Each line begins with `- ` and the problems are separated by `Environment.NewLine`. The parameter name is not set, because the exception is thrown without one.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Extensions;

var options = new InfrastructureOptions
{
    HttpTimeoutMs = 0,
    MaxRetries = -1,
    LogPath = " "
};

IReadOnlyList<string> problems = options.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// HttpTimeoutMs must be positive, but was 0.
// MaxRetries must be non-negative, but was -1.
// LogPath must not be null or whitespace.
```

The example sets three properties to invalid values. The rate limit defaults (`100` and `10`) are valid, so they produce no message.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Extensions;

var options = new InfrastructureOptions();

if (!options.IsValid())
{
    Console.Error.WriteLine("Infrastructure options are invalid; using defaults.");
    return;
}

// Safe to pass to AddInfrastructureServices.
```

An `InfrastructureOptions` created with its default values is valid, so `IsValid` returns `true` for it.

### Example 3: Throwing on invalid options

```csharp
using System;
using CaddyVpsToolkit.Extensions;

var options = new InfrastructureOptions
{
    RateLimitRefillRate = 0
};

try
{
    options.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // InfrastructureOptions validation failed:
    // - RateLimitRefillRate must be positive, but was 0.
}
```

### Example 4: Validating before registering services

`AddInfrastructureServices` builds an `InfrastructureOptions` instance, passes it to the `configure` callback, and then registers the services. It does not call any validation method, so a bad value is only caught if you check it yourself. Call `EnsureValid` inside the callback to reject the configuration before any service is registered:

```csharp
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Extensions;

var services = new ServiceCollection();

services.AddInfrastructureServices(options =>
{
    options.HttpTimeoutMs = 5000;
    options.MaxRetries = 2;
    options.LogPath = "/var/log/caddy-vps-toolkit/app.log";

    options.EnsureValid();
});
```

If the callback throws, `AddInfrastructureServices` propagates the `ArgumentException` and the service collection is left without the infrastructure registrations.

### Example 5: Calling the static methods directly

The extension methods are also static methods on `ServiceCollectionExtensionsValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Extensions;

bool valid = ServiceCollectionExtensionsValidation.IsValid(options);
```

## Notes

- `Validate` checks only the six rules in the table. Other properties, including `MinLogLevel`, are not inspected.
- `LogPath` is checked for the 1024-character limit only when it is non-empty. A whitespace-only path is reported once, with the "must not be null or whitespace" message.
- `Validate` creates a new list on every call. The returned value is typed as `IReadOnlyList<string>`, so callers should not rely on it being mutable or immutable beyond that interface.
- The class holds no state, and its methods only read the options. Concurrent calls on the same instance are safe as long as nothing else mutates it at the same time.
- `InfrastructureOptions` is a sealed class with public settable properties, so `Validate` is the only guard against invalid values. Nothing in the type prevents an invalid instance from being created.
- `AddInfrastructureServices` does not call `Validate` or `EnsureValid`. Validation is opt-in and must be called by the code that configures the options.
- The class name includes `ServiceCollection`, but its methods extend `InfrastructureOptions`, not `IServiceCollection`.
