# ServiceCollectionExtensions

Provides a set of extension methods for `IServiceCollection` that register commonly used infrastructure concerns (caching, HTTP client, webhook handling, logging, event bus, rate limiting, and service discovery) together with configurable fields that control the behavior of those services.

## API

### `public static IServiceCollection AddCachingServices(this IServiceCollection services)`
- **Purpose**: Registers the default caching implementations (e.g., `IMemoryCache` or `IDistributedCache`) required by the toolkit.
- **Parameters**: 
  - `services`: The service collection to which caching services are added.
- **Return value**: The same `IServiceCollection` instance to allow method chaining.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.

### `public static IServiceCollection AddHttpClientServices(this IServiceCollection services)`
- **Purpose**: Configures and registers `IHttpClientFactory` with default timeout and retry policies derived from the `HttpTimeoutMs` and `MaxRetries` fields.
- **Parameters**: 
  - `services`: The service collection to receive the HTTP client services.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.
  - `ArgumentOutOfRangeException` if `HttpTimeoutMs` is less than or equal to zero or `MaxRetries` is negative (checked internally when the factory is built).

### `public static IServiceCollection AddWebhookServices(this IServiceCollection services)`
- **Purpose**: Registers services needed to receive, validate, and process webhook payloads (e.g., background workers, signature verifiers).
- **Parameters**: 
  - `services`: The target service collection.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.

### `public static IServiceCollection AddLoggingServices(this IServiceCollection services)`
- **Purpose**: Adds logging providers configured with the `LogPath` and `MinLogLevel` fields (e.g., file logger with minimum level filtering).
- **Parameters**: 
  - `services`: The service collection to augment.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.
  - `ArgumentException` if `LogPath` is null, empty, or points to an inaccessible directory (checked when the logger is instantiated).

### `public static IServiceCollection AddEventBus(this IServiceCollection services)`
- **Purpose**: Registers the in‑process event bus implementation (`IEventBus`) used for publishing and subscribing to domain events.
- **Parameters**: 
  - `services`: The service collection to receive the event bus.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.

### `public static IServiceCollection AddRateLimiting(this IServiceCollection services)`
- **Purpose**: Adds ASP.NET Core rate‑limiting middleware with limits defined by `RateLimitCapacity` and `RateLimitRefillRate`.
- **Parameters**: 
  - `services`: The service collection to configure.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.
  - `ArgumentOutOfRangeException` if `RateLimitCapacity` or `RateLimitRefillRate` is less than or equal to zero.

### `public static IServiceCollection AddServiceDiscovery(this IServiceCollection services)`
- **Purpose**: Registers service discovery clients (e.g., Consul, Eureka) that allow the application to locate other services at runtime.
- **Parameters**: 
  - `services`: The service collection to extend.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - `ArgumentNullException` if `services` is `null`.

### `public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)`
- **Purpose**: Convenience method that calls all the above registration methods in a prescribed order, ensuring dependencies are satisfied.
- **Parameters**: 
  - `services`: The service collection to populate.
- **Return value**: The same `IServiceCollection` instance.
- **Exceptions**: 
  - Propagates any `ArgumentNullException` or validation exceptions thrown by the individual methods it invokes.

### Fields

| Field | Type | Purpose |
|-------|------|---------|
| `HttpTimeoutMs` | `int` | Timeout (in milliseconds) applied to `HttpClient` instances created by `AddHttpClientServices`. Must be > 0. |
| `MaxRetries` | `int` | Maximum number of retry attempts for failed HTTP requests in `AddHttpClientServices`. Must be ≥ 0. |
| `LogPath` | `string` | File system path where log files are written by the logger added via `AddLoggingServices`. Must be a valid, writable directory. |
| `MinLogLevel` | `LogLevel` | Minimum `Microsoft.Extensions.Logging.LogLevel` that will be logged; lower levels are ignored. |
| `RateLimitCapacity` | `int` | Maximum number of requests permitted within a refill window for the rate limiter added by `AddRateLimiting`. Must be > 0. |
| `RateLimitRefillRate` | `int` | Number of tokens added to the rate limiter’s bucket per second; defines the sustained request rate. Must be > 0. |

*All fields are `public static` members of the `ServiceCollectionExtensions` class and are read by the extension methods at the time they are invoked.*

## Usage

### Basic registration with default values
```csharp
using Microsoft.Extensions.DependencyInjection;
using CaddyVpsToolkit.Extensions; // namespace containing ServiceCollectionExtensions

var services = new ServiceCollection();

// Apply default infrastructure services
services.AddInfrastructureServices();

// Build the provider
var provider = services.BuildServiceProvider();

// Resolve an HTTP client factory, for example
var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
```

### Customizing configuration before registration
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CaddyVpsToolkit.Extensions;

var services = new ServiceCollection();

// Adjust settings that affect the registered services
ServiceCollectionExtensions.HttpTimeoutMs = 10_000;   // 10 seconds
ServiceCollectionExtensions.MaxRetries   = 3;
ServiceCollectionExtensions.LogPath      = @"C:\Logs\MyApp";
ServiceCollectionExtensions.MinLogLevel  = LogLevel.Warning;
ServiceCollectionExtensions.RateLimitCapacity = 100;
ServiceCollectionExtensions.RateLimitRefillRate = 20;

// Register all infrastructure services with the customized values
services.AddInfrastructureServices();

var provider = services.BuildServiceProvider();

// The logger will now write to the specified path and ignore Info/Debug messages
var logger = provider.GetRequiredService<ILogger<Program>>();
```

## Notes

- **Null argument handling**: Every extension method throws `ArgumentNullException` if the supplied `IServiceCollection` is `null`. Callers should ensure the collection is instantiated before invoking any method.
- **Field validation**: The extension methods do not validate the fields at registration time; invalid values (e.g., negative timeouts, empty log path) may cause exceptions later when the corresponding service is resolved or used. It is advisable to set the fields to valid values before calling the registration methods.
- **Mutability and thread‑safety**: The configuration fields are mutable static members. If they are modified after the service provider has been built, already‑created services will retain the original configuration, while subsequently resolved services will see the new values. Concurrent modification of these fields from multiple threads without synchronization can lead to race conditions; treat the fields as configuration that is set once during application start‑up, or protect writes with appropriate locking.
- **Order of registration**: `AddInfrastructureServices` calls the individual methods in a specific order to satisfy internal dependencies (e.g., logging is registered before HTTP client services that may emit logs). Calling the methods manually in a different order may still work but is not guaranteed.
- **Underlying implementations**: The exact concrete types registered (e.g., caching provider, event bus) are internal to the toolkit and may change between versions; rely only on the abstractions (`IMemoryCache`, `IHttpClientFactory`, `IEventBus`, etc.) when consuming these services.
