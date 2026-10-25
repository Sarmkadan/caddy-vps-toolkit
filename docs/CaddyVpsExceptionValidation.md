# CaddyVpsExceptionValidation

`CaddyVpsExceptionValidation` is a static class of extension methods that check a `CaddyVpsException` for required values and return a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Core` namespace, the same namespace as `CaddyVpsException`, so no extra `using` is needed when the exception type is already in scope.

Validation only checks that `ErrorCode` is not null, empty, or whitespace, and that `Details` is not `null`. It does not check the format of the error code or the type of the details payload. Because the methods are extensions on `CaddyVpsException`, they also apply to every derived exception such as `ServiceNotFoundException` or `DatabaseException`.

## API

### `Validate(this CaddyVpsException value)`
Validates a `CaddyVpsException` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `ErrorCode` | Must not be null, empty, or whitespace. | `ErrorCode must not be null or whitespace.` |
| `Details` | Must not be `null`. | `Details must not be null.` |

**Parameters:**
- `value` (`CaddyVpsException`) – The exception to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the exception is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this CaddyVpsException value)`
Returns whether the exception passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`CaddyVpsException`) – The exception to check.

**Returns:**
`bool` – `true` if the exception has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `EnsureValid(this CaddyVpsException value)`
Validates the exception and throws if any problem is found. Use it at boundaries where an invalid exception should be rejected rather than handled.

**Parameters:**
- `value` (`CaddyVpsException`) – The exception to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the exception is invalid. The message starts with `The CaddyVpsException instance is invalid. Problems:`, followed by one line per problem from `Validate`, separated by `Environment.NewLine`. The parameter name is `value`.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Core;

var error = new CaddyVpsException("Reload failed", errorCode: " ");

IReadOnlyList<string> problems = error.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// ErrorCode must not be null or whitespace.
// Details must not be null.
```

The two-argument constructor leaves `Details` as `null`, so the example fails on both checks.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Core;

if (!error.IsValid())
{
    Console.Error.WriteLine("Skipping exception with incomplete metadata.");
    return;
}

// Safe to log or serialize the exception.
```

### Example 3: Throwing on an invalid exception

```csharp
using System;
using CaddyVpsToolkit.Core;

var error = new CaddyVpsException("Health check failed", "CADDY_HEALTH_CHECK_FAILED");
error.ErrorCode = null;

try
{
    error.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // The CaddyVpsException instance is invalid. Problems:
    // ErrorCode must not be null or whitespace.
    // Details must not be null.
}
```

`ErrorCode` is set to `GENERAL_ERROR` when the constructor receives a `null` code, so the example clears it through the property setter to trigger the check.

### Example 4: Providing a complete exception

Passing both an error code and details through the three-argument constructor produces a valid exception:

```csharp
using CaddyVpsToolkit.Core;

var error = new CaddyVpsException(
    "Certificate issuance failed",
    "CADDY_OPERATION_FAILED",
    details: "api.example.com");

bool valid = error.IsValid();
// true
```

### Example 5: Calling the static methods directly

The extension methods are also static methods on `CaddyVpsExceptionValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Core;

bool valid = CaddyVpsExceptionValidation.IsValid(error);
```

## Notes

- `Validate` only inspects `ErrorCode` and `Details`. The `Message` and `InnerException` properties are not checked, so an exception with an empty message still passes.
- `ErrorCode` is `null` only if it is set through the property setter or passed as `null` to the three-argument constructor. The two-argument constructor defaults a `null` code to `GENERAL_ERROR`.
- `Validate` creates a new list on every call. The returned list is a read-only view over that list, so callers cannot change the result of a later call.
- The class holds no state, and its methods only read the exception. Concurrent calls on the same exception are safe as long as nothing else mutates it at the same time.
- `ErrorCode` and `Details` are public settable properties, so `Validate` is the only guard against invalid values. Nothing in the type prevents an invalid exception from being created.
