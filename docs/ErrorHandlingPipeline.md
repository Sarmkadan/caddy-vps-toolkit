# ErrorHandlingPipeline

`ErrorHandlingPipeline` is the sealed default implementation of the `IErrorHandler` abstraction in the `caddy-vps-toolkit` project. It runs an ordered list of handlers against an exception, logs the failure through an `ILogger`, and converts the exception into a structured `ErrorResponse` that carries an exit code, a machine-readable code, a message, and optional details. The first handler whose predicate matches wins; if none matches, a generic fallback response is produced.

The supporting types live in the same namespace (`CaddyVpsToolkit.Middleware`):
- `IErrorHandler` - the interface that consumers should depend on. It declares a single method, `HandleAsync(Exception ex)`.
- `ErrorResponse` - the structured result returned by `HandleAsync` (defined in `Middleware/ErrorHandlingPipeline.cs`).
- `ILogger` - the logging abstraction the pipeline writes to (defined in `Middleware/LoggingMiddleware.cs`).

The exception types matched by the default handlers come from `CaddyVpsToolkit.Core` (`ServiceNotFoundException` and `ServiceConfigurationException`, both derived from `CaddyVpsException`).

## API

### ErrorHandlingPipeline(ILogger logger)
- **Signature:** `public ErrorHandlingPipeline(ILogger logger)`
- **Purpose:** Creates a pipeline that writes to `logger` and registers the three default handlers, in this order:
  1. `ServiceNotFoundException` → exit code `1`, code `SERVICE_NOT_FOUND`
  2. `ServiceConfigurationException` → exit code `2`, code `CONFIGURATION_ERROR`
  3. `ArgumentException` (including its subclasses such as `ArgumentNullException`) → exit code `3`, code `INVALID_ARGUMENT`
- **Parameters:**
  - `logger` - the logger that receives one error line per handled exception.
- **Throws:** `ArgumentNullException` if `logger` is `null`.
- **Remarks:** The default handlers cannot be removed or replaced. Because they are registered first, a handler added later with `AddHandler` never sees an exception that one of the defaults already matched (see [Handler ordering](#handler-ordering)).

### AddHandler(Func\<Exception, bool\> predicate, Func\<Exception, Task\<ErrorResponse\>\> handler)
- **Signature:** `public void AddHandler(Func<Exception, bool> predicate, Func<Exception, Task<ErrorResponse>> handler)`
- **Purpose:** Appends a custom handler to the end of the pipeline. The handler runs only for exceptions for which `predicate` returns `true`.
- **Parameters:**
  - `predicate` - decides whether this handler applies to an exception.
  - `handler` - produces the `ErrorResponse` for a matching exception. It is responsible for any logging it wants to perform.
- **Remarks:**
  - `AddHandler` is not synchronized. Register all handlers during startup, before the pipeline is shared between threads.
  - `null` arguments are not checked. A `null` predicate or handler is not reported when added; it fails with `NullReferenceException` the first time an exception reaches that slot in `HandleAsync`.
  - If `predicate` throws, the exception propagates out of `HandleAsync` and the remaining handlers are not run.

### HandleAsync(Exception ex)
- **Signature:** `public async Task<ErrorResponse> HandleAsync(Exception ex)`
- **Purpose:** Walks the registered handlers in order and returns the response of the first one that matches. If no handler matches, it logs the error and returns the fallback response.
- **Parameters:**
  - `ex` - the exception to convert.
- **Returns:** `Task<ErrorResponse>`. The result is never `null`.
- **Behavior:**
  - A handler is considered to have matched when its predicate returns `true` and it returns a non-`null` `ErrorResponse`. A handler that returns `null` is treated as "not handled", and the search continues with the next handler.
  - If no handler matches, the pipeline calls `LogErrorAsync("Unexpected error: {ex.Message}")` and returns the fallback response described below.
- **Throws:** Any exception thrown by a predicate or handler is not caught. `ex` is not checked for `null`; passing `null` throws `NullReferenceException`.

### Fallback response
When no handler matches, `HandleAsync` returns:

| Property | Value |
|----------|-------|
| `ExitCode` | `255` |
| `Code` | `UNEXPECTED_ERROR` |
| `Message` | `An unexpected error occurred` |
| `Details` | `ex.ToString()`, including the stack trace and inner exceptions |

`Message` is deliberately generic, so it is safe to show to users. `Details` is intended for logs and support, not for end-user display.

### ErrorResponse
A `sealed` class with settable properties.

| Property | Type | Description |
|----------|------|-------------|
| `ExitCode` | `int` | The process exit code that corresponds to this error. Built-in values are `1`, `2`, `3`, and `255`. |
| `Code` | `string` | A stable, machine-readable identifier such as `SERVICE_NOT_FOUND`. |
| `Message` | `string` | A concise, human-readable description. The built-in handlers copy it from the exception message. |
| `Details` | `string` | Optional diagnostic text. `null` for the built-in handlers, and `ex.ToString()` for the fallback. |

The properties are not initialized by the constructor, so set every property a caller depends on in the handler that creates the response.

### IErrorHandler
- **Signature:** `public interface IErrorHandler { Task<ErrorResponse> HandleAsync(Exception ex); }`
- **Purpose:** The contract that consumers should depend on. `ErrorHandlingPipeline` implements it. Registration (`AddHandler`) is available only on the concrete class, so configure the pipeline before exposing it as an `IErrorHandler`.

## Handler ordering

Handlers are evaluated strictly in registration order, and the default handlers always come first. This has two practical consequences:

- An exception that matches a default handler is never handled by a custom handler. For example, a custom predicate for `ServiceNotFoundException` will never run. To change how such an exception is reported, handle it at the call site before it reaches the pipeline.
- More specific custom handlers should be registered before broader ones. Since the default `ArgumentException` handler matches every subclass of `ArgumentException`, a custom handler for `ArgumentNullException` must catch that case before the pipeline does, not after.

## Usage

### Example 1: Creating the pipeline and handling an error
```csharp
ILogger logger = new ConsoleLogger();
var pipeline = new ErrorHandlingPipeline(logger);

try
{
    await serviceManager.StartAsync("api");
}
catch (Exception ex)
{
    ErrorResponse response = await pipeline.HandleAsync(ex);

    Console.Error.WriteLine($"[{response.Code}] {response.Message}");
    Environment.ExitCode = response.ExitCode;
}
```

### Example 2: Mapping a custom exception to its own response
```csharp
public sealed class UpstreamUnavailableException : CaddyVpsException
{
    public UpstreamUnavailableException(string upstream)
        : base($"Upstream '{upstream}' is unavailable", "UPSTREAM_UNAVAILABLE")
    {
    }
}

pipeline.AddHandler(
    ex => ex is UpstreamUnavailableException,
    async ex =>
    {
        await logger.LogWarningAsync(ex.Message);
        return new ErrorResponse
        {
            ExitCode = 4,
            Code = "UPSTREAM_UNAVAILABLE",
            Message = ex.Message
        };
    });
```

### Example 3: Declining an exception from a handler
A handler can return `null` to pass the exception on to the next handler. This is useful when the predicate is too coarse to decide on its own.
```csharp
pipeline.AddHandler(
    ex => ex is HttpRequestException,
    async ex =>
    {
        // Only treat connection refusals as errors here; let other transport failures fall through
        if (ex.InnerException is not System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
            return null;

        await logger.LogErrorAsync($"Connection refused: {ex.Message}");
        return new ErrorResponse
        {
            ExitCode = 5,
            Code = "CONNECTION_REFUSED",
            Message = "The service refused the connection"
        };
    });
```

### Example 4: Inspecting the fallback response
```csharp
ErrorResponse response = await pipeline.HandleAsync(new InvalidOperationException("boom"));

// response.ExitCode == 255
// response.Code == "UNEXPECTED_ERROR"
// response.Message == "An unexpected error occurred"
// response.Details contains the full ToString() output of the exception
```

Use `Details` for logs and diagnostics only. Do not show it to end users.

### Example 5: Registering the pipeline in DI
The pipeline depends only on `ILogger`, so it can be registered as a singleton. Register handlers in the same place, before the instance is resolved for the first time:
```csharp
services.AddSingleton<ILogger>(logger);
services.AddSingleton<ErrorHandlingPipeline>(sp =>
{
    var pipeline = new ErrorHandlingPipeline(sp.GetRequiredService<ILogger>());
    pipeline.AddHandler(
        ex => ex is UpstreamUnavailableException,
        ex => Task.FromResult(new ErrorResponse
        {
            ExitCode = 4,
            Code = "UPSTREAM_UNAVAILABLE",
            Message = ex.Message
        }));
    return pipeline;
});

// Consumers depend on the abstraction:
public sealed class CommandRunner(IErrorHandler errors)
{
    // ...
}
```

## Notes

- `ErrorHandlingPipeline` is `sealed`. Extend behavior by adding handlers, not by subclassing.
- Logging is per handler. The three built-in handlers and the fallback always log one error line. Custom handlers log only if they call the logger themselves, as in Examples 2 and 3.
- `HandleAsync` does not accept a `CancellationToken`. Handlers that perform long-running work should manage their own timeouts.
