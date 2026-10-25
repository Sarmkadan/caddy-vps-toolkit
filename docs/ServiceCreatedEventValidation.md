# ServiceCreatedEventValidation

`ServiceCreatedEventValidation` is a static class of extension methods that check a `ServiceCreatedEvent` for invalid values and return a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Events` namespace, so the `using CaddyVpsToolkit.Events;` directive is enough to call them.

Validation checks `ServiceName`, `Port`, and `ExecutablePath`. It does not check `ServiceType`, because every value of that enum is accepted. It also does not check the timestamp or any other base `DomainEvent` members.

## API

### `Validate(this ServiceCreatedEvent value)`
Validates a `ServiceCreatedEvent` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `ServiceName` | Must not be null, empty, or whitespace. | `ServiceName is required and cannot be empty or whitespace.` |
| `ServiceName` | If not blank, must be 256 characters or less. | `ServiceName must be 256 characters or less. Current length: <length>.` |
| `Port` | Must be greater than 0. | `Port must be a positive integer greater than 0.` |
| `Port` | If greater than 0, must be 65535 or less. | `Port must be 65535 or less.` |
| `ExecutablePath` | Must not be null, empty, or whitespace. | `ExecutablePath is required and cannot be empty or whitespace.` |
| `ExecutablePath` | If not blank, must be 4096 characters or less. | `ExecutablePath must be 4096 characters or less. Current length: <length>.` |

The `Port` checks are mutually exclusive. A value of `0` or below produces only the first message, and a value above `65535` produces only the second. A blank `ServiceName` or `ExecutablePath` produces only the "required" message, not the length message.

**Parameters:**
- `value` (`ServiceCreatedEvent`) – The event to validate. Cannot be `null`.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the event is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this ServiceCreatedEvent value)`
Returns whether the event passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`ServiceCreatedEvent`) – The event to check. Cannot be `null`.

**Returns:**
`bool` – `true` if the event has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `EnsureValid(this ServiceCreatedEvent value)`
Validates the event and throws if any problem is found. Use it at boundaries where an invalid event should be rejected rather than handled.

**Parameters:**
- `value` (`ServiceCreatedEvent`) – The event to validate. Cannot be `null`.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the event is invalid. The message has the form `ServiceCreatedEvent is not valid. Validation errors: <problem 1> <problem 2>`, listing every problem from `Validate` in order, separated by single spaces. The exception is created without a parameter name.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Events;

var evt = new ServiceCreatedEvent
{
    ServiceName = "",
    ServiceType = ServiceType.WebApplication,
    Port = 0,
    ExecutablePath = "/usr/bin/myapp"
};

IReadOnlyList<string> problems = evt.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// ServiceName is required and cannot be empty or whitespace.
// Port must be a positive integer greater than 0.
```

`ServiceType` is not validated, so it is left at its default here. The `ExecutablePath` value is valid, so it produces no message.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Events;

if (!evt.IsValid())
{
    Console.Error.WriteLine($"Skipping invalid service event for port {evt.Port}.");
    return;
}

// Safe to publish the event.
```

### Example 3: Rejecting an invalid event with an exception

```csharp
using System;
using CaddyVpsToolkit.Events;

var evt = new ServiceCreatedEvent
{
    ServiceName = "api",
    ServiceType = ServiceType.ApiService,
    Port = 70000,
    ExecutablePath = " "
};

try
{
    evt.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // ServiceCreatedEvent is not valid. Validation errors: Port must be 65535 or less. ExecutablePath is required and cannot be empty or whitespace.
}
```

### Example 4: Checking a long name

Names longer than 256 characters are reported with their actual length:

```csharp
using System;
using CaddyVpsToolkit.Events;

var evt = new ServiceCreatedEvent
{
    ServiceName = new string('a', 300),
    ServiceType = ServiceType.Worker,
    Port = 8080,
    ExecutablePath = "/opt/worker/run.sh"
};

foreach (var problem in evt.Validate())
{
    Console.WriteLine(problem);
}
// ServiceName must be 256 characters or less. Current length: 300.
```

### Example 5: Calling the static methods directly

The extension methods are also static methods on `ServiceCreatedEventValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Events;

bool valid = ServiceCreatedEventValidation.IsValid(evt);
```

## Notes

- `Validate` creates a new list on every call. The returned list is a read-only view over that list, so callers cannot change the result of a later call.
- The class holds no state, and its methods only read the event. Concurrent calls on the same event are safe as long as nothing else mutates it at the same time.
- `ServiceCreatedEvent` has public settable properties, so nothing prevents an invalid event from being created. `Validate` is the only guard against invalid values.
- Validation checks only the three properties listed above. It does not check that `ExecutablePath` exists on disk, that the port is free, or that the name is unique.
- The length limits are fixed in the code (256 for `ServiceName`, 4096 for `ExecutablePath`) and are not configurable.
