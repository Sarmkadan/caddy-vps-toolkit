# CoreExceptions

`Core/Exceptions.cs` defines the exception hierarchy used across `caddy-vps-toolkit`, all in the `CaddyVpsToolkit.Core` namespace. Every type derives from [CaddyVpsException](CaddyVpsException.md), which adds a machine-readable `ErrorCode` and an optional `Details` payload to the standard `Exception` members. Each derived type is `sealed` and fixes its own `ErrorCode`, so callers can catch either a specific type or the base class.

The base class is not sealed. The derived types are.

## Type Summary

| Type | Error code | Default message | Typical use |
|------|------------|-----------------|-------------|
| `CaddyVpsException` | `GENERAL_ERROR` (when no code is given) | Supplied by caller | Base type, or a generic failure |
| `ServiceNotFoundException` | `SERVICE_NOT_FOUND` | `Service '<serviceId>' not found` | A managed service ID does not exist |
| `ServiceConfigurationException` | `SERVICE_CONFIG_ERROR` | Supplied by caller | A service definition is invalid |
| `SystemdOperationException` | `SYSTEMD_ERROR` | Supplied by caller | A systemd unit operation failed |
| `CaddyOperationException` | `CADDY_ERROR` | Supplied by caller | A Caddy operation failed |
| `HealthCheckException` | `HEALTH_CHECK_ERROR` | Supplied by caller | A health check failed |
| `DatabaseException` | `DATABASE_ERROR` | Supplied by caller | A database operation failed |
| `ValidationException` | `VALIDATION_ERROR` | Supplied by caller | Input or state validation failed |
| `NotSupportedException` | `NOT_SUPPORTED` | Supplied by caller | An operation or feature is not supported |

## API

### `CaddyVpsException`
```csharp
public class CaddyVpsException : Exception
```

The base type. It holds the error code and details shared by all toolkit exceptions.

**Properties:**
- `string ErrorCode { get; set; }`: A stable identifier for the error category, such as `SERVICE_NOT_FOUND`. Callers can switch on it without parsing messages.
- `object Details { get; set; }`: Optional context about the failure, such as the offending service ID or a list of validation problems. Its type depends on the derived class.

**Constructors:**
- `CaddyVpsException(string message, string errorCode = null, Exception innerException = null)`: Sets `ErrorCode` to `"GENERAL_ERROR"` when `errorCode` is `null`. `Details` stays `null`.
- `CaddyVpsException(string message, string errorCode, object details, Exception innerException = null)`: Stores `errorCode` exactly as passed. If `errorCode` is `null`, `ErrorCode` is `null`, not `"GENERAL_ERROR"`.

**Overrides:**
- `string ToString()`: Returns `CaddyVpsException { ErrorCode = <code>, Details = <details> }`. It does not include the message, the inner exception, or the stack trace. See [Notes](#notes).

---

### `ServiceNotFoundException`
```csharp
public sealed class ServiceNotFoundException : CaddyVpsException
public ServiceNotFoundException(string serviceId)
```

Thrown when a managed service cannot be found by its identifier.

**Parameters:**
- `serviceId` (`string`): The identifier that was looked up. It is used in the message and stored in `Details`.

**Result:**
- `Message`: `Service '<serviceId>' not found`
- `ErrorCode`: `SERVICE_NOT_FOUND`
- `Details`: the `serviceId` string

---

### `ServiceConfigurationException`
```csharp
public sealed class ServiceConfigurationException : CaddyVpsException
public ServiceConfigurationException(string message, object details = null)
```

Thrown when a service definition fails validation or is otherwise invalid.

**Parameters:**
- `message` (`string`): A description of the problem.
- `details` (`object`, optional): Extra context, such as a list of validation errors. Defaults to `null`.

**Result:**
- `ErrorCode`: `SERVICE_CONFIG_ERROR`
- `Details`: the `details` value as passed

---

### `SystemdOperationException`
```csharp
public sealed class SystemdOperationException : CaddyVpsException
public SystemdOperationException(string message, Exception innerException = null)
```

Thrown when a systemd operation such as start, stop, or enable fails.

**Parameters:**
- `message` (`string`): A description of the failed operation.
- `innerException` (`Exception`, optional): The underlying exception, if there is one.

**Result:**
- `ErrorCode`: `SYSTEMD_ERROR`
- `Details`: `null`

---

### `CaddyOperationException`
```csharp
public sealed class CaddyOperationException : CaddyVpsException
public CaddyOperationException(string message, Exception innerException = null)
```

Thrown when a Caddy operation fails, such as a configuration reload or certificate issuance.

**Parameters:**
- `message` (`string`): A description of the failed operation.
- `innerException` (`Exception`, optional): The underlying exception, if there is one.

**Result:**
- `ErrorCode`: `CADDY_ERROR`
- `Details`: `null`

---

### `HealthCheckException`
```csharp
public sealed class HealthCheckException : CaddyVpsException
public HealthCheckException(string serviceId, string message)
```

Thrown when a health check fails for a service.

**Parameters:**
- `serviceId` (`string`): The service whose health check failed. It is stored in `Details`.
- `message` (`string`): A description of the failure. Note the order: the service ID comes first, the message second.

**Result:**
- `Message`: the `message` value as passed
- `ErrorCode`: `HEALTH_CHECK_ERROR`
- `Details`: the `serviceId` string

---

### `DatabaseException`
```csharp
public sealed class DatabaseException : CaddyVpsException
public DatabaseException(string message, Exception innerException = null)
```

Thrown when a database operation fails.

**Parameters:**
- `message` (`string`): A description of the failed operation.
- `innerException` (`Exception`, optional): The underlying exception, if there is one.

**Result:**
- `ErrorCode`: `DATABASE_ERROR`
- `Details`: `null`

---

### `ValidationException`
```csharp
public sealed class ValidationException : CaddyVpsException
public ValidationException(string message, object details = null)
```

Thrown when input or state validation fails. It is a toolkit type and has the same name as `System.ComponentModel.DataAnnotations.ValidationException`. See [Notes](#notes).

**Parameters:**
- `message` (`string`): A description of the validation failure.
- `details` (`object`, optional): Extra context, such as a list of problems. Defaults to `null`.

**Result:**
- `ErrorCode`: `VALIDATION_ERROR`
- `Details`: the `details` value as passed

---

### `NotSupportedException`
```csharp
public sealed class NotSupportedException : CaddyVpsException
public NotSupportedException(string message)
```

Thrown when an operation or feature is not supported. It has the same name as `System.NotSupportedException`. See [Notes](#notes).

**Parameters:**
- `message` (`string`): A description of what is not supported.

**Result:**
- `ErrorCode`: `NOT_SUPPORTED`
- `Details`: `null`

## Usage

### Example 1: Throwing with an inner exception
Wrap a lower-level failure so that the toolkit error keeps the original cause:

```csharp
using System;
using CaddyVpsToolkit.Core;

try
{
    // Call into the systemd layer here
}
catch (InvalidOperationException ex)
{
    throw new SystemdOperationException("Failed to start unit 'caddy.service'", ex);
}
```

### Example 2: Reporting a missing service
```csharp
using CaddyVpsToolkit.Core;

// Thrown when the service ID is not in the repository
throw new ServiceNotFoundException("blog-api");
// Message: Service 'blog-api' not found
// ErrorCode: SERVICE_NOT_FOUND
// Details: "blog-api"
```

### Example 3: Attaching validation problems to a configuration error
```csharp
using System.Collections.Generic;
using CaddyVpsToolkit.Core;

var problems = new List<string>
{
    "Service name is required",
    "Port must be between 1 and 65535, got 70000"
};

throw new ServiceConfigurationException("Service definition is invalid", problems);
```

### Example 4: Branching on the error code
Catch the base type and switch on `ErrorCode` when the response depends on the category:

```csharp
using System;
using CaddyVpsToolkit.Core;

try
{
    // Run a service operation here
}
catch (CaddyVpsException ex)
{
    switch (ex.ErrorCode)
    {
        case "SERVICE_NOT_FOUND":
            Console.Error.WriteLine($"Unknown service: {ex.Details}");
            break;
        case "HEALTH_CHECK_ERROR":
            Console.Error.WriteLine($"Health check failed for {ex.Details}: {ex.Message}");
            break;
        default:
            Console.Error.WriteLine($"[{ex.ErrorCode}] {ex.Message}");
            break;
    }
}
```

### Example 5: Using the base type directly
For a one-off error without a dedicated type, use the base constructor. Omit the code to get `GENERAL_ERROR`:

```csharp
using CaddyVpsToolkit.Core;

throw new CaddyVpsException("Unexpected state while reloading routes");
// ErrorCode: GENERAL_ERROR
```

## Notes

- **Stable codes**: Match on `ErrorCode`, not on `Message`. Messages are for people and may change.
- **Details types**: `Details` is typed `object`. Cast it only where the derived type documents what it holds. For example, `ServiceNotFoundException` holds a `string`, and `ServiceConfigurationException` holds whatever the caller passed.
- **ToString is short**: `CaddyVpsException.ToString()` returns only the error code and details. It omits the message, the inner exception, and the stack trace. When logging, use `ex.Message`, `ex.ErrorCode`, and `ex.InnerException` explicitly, or log `ex.ToString()` only after checking that this is what you want.
- **Name collisions**: `ValidationException` and `NotSupportedException` have the same names as types in `System.ComponentModel.DataAnnotations` and `System`. If both namespaces are imported, qualify the name, for example `CaddyVpsToolkit.Core.ValidationException`. Some domain models in `Domain/Models/` alias `ValidationException` to the DataAnnotations type, so the validation failures they raise are `System.ComponentModel.DataAnnotations.ValidationException`, not the Core type. Catch both if you need to handle validation failures from those models.
- **Base constructor defaults**: The first base constructor replaces a `null` code with `GENERAL_ERROR`. The second base constructor does not, so a `null` code stays `null`. The derived types always pass a code, so this only matters when you call the base type directly.
- **Related**: [CaddyVpsException](CaddyVpsException.md) describes the base type. [ExceptionContractUnification](ExceptionContractUnification.md) covers how collection-processing components choose exception types.
