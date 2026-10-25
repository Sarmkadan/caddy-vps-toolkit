# SystemdUnitServiceValidation

`SystemdUnitServiceValidation` is a static class of extension methods that check a `SystemdUnitStatus` for valid values and return a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Services` namespace, so add `using CaddyVpsToolkit.Services;` before calling them.

Validation checks that `UnitName`, `ActiveState`, and `Type` are not blank, that `ActiveState` and `Type` are known systemd values, that `MainPid` is not negative, and that `TimeoutStartUsec` is either `0` or at least one second. It does not check that the unit exists or that its state is consistent with `MainPid`.

## API

### `Validate(this SystemdUnitStatus status)`
Validates a `SystemdUnitStatus` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `UnitName` | Must not be null, empty, or whitespace. | `UnitName cannot be null or whitespace` |
| `ActiveState` | Must not be null, empty, or whitespace. Must be one of `active`, `inactive`, `activating`, `deactivating`, or `failed`, compared without regard to case. | `ActiveState cannot be null or whitespace` or `ActiveState '<value>' is not a valid systemd state` |
| `Type` | Must not be null, empty, or whitespace. Must be one of `simple`, `forking`, `oneshot`, `dbus`, `notify`, or `idle`, compared without regard to case. | `Type cannot be null or whitespace` or `Type '<value>' is not a valid systemd service type` |
| `MainPid` | Must not be negative. | `MainPid cannot be negative` |
| `TimeoutStartUsec` | `0` is allowed and means the default. Any other value must be at least `1000000` (one second). | `TimeoutStartUsec should be at least 1 second (1000000 microseconds) for proper timeout configuration` |

Messages for `ActiveState` and `Type` include the rejected value in quotes. None of the messages end with a period.

**Parameters:**
- `status` (`SystemdUnitStatus`) – The unit status to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the status is valid.

**Throws:**
`ArgumentNullException` – If `status` is `null`. The parameter name is `status`.

### `IsValid(this SystemdUnitStatus status)`
Returns whether the status passes every check in `Validate`. It is equivalent to `status.Validate().Count == 0`.

**Parameters:**
- `status` (`SystemdUnitStatus`) – The unit status to check.

**Returns:**
`bool` – `true` if the status has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `status` is `null`. The parameter name is `status`.

### `EnsureValid(this SystemdUnitStatus status)`
Validates the status and throws if any problem is found. Use it at boundaries where an invalid status should be rejected rather than handled.

**Parameters:**
- `status` (`SystemdUnitStatus`) – The unit status to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `status` is `null`. The parameter name is `status`.

`ArgumentException` – If the status is invalid. The message starts with `SystemdUnitStatus validation failed:` on its own line. Each problem from `Validate` follows on its own line, prefixed with `- `, in the same order. The parameter name is not set.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Services;

var status = new SystemdUnitStatus
{
    UnitName = "caddy.service",
    ActiveState = "running",
    Type = "simple",
    MainPid = 1234,
    TimeoutStartUsec = 0
};

IReadOnlyList<string> problems = status.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// ActiveState 'running' is not a valid systemd state
```

`running` is a substate, not an `ActiveState` value. The valid values are `active`, `inactive`, `activating`, `deactivating`, and `failed`. Use `active` here to make the status valid.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Services;

if (!status.IsValid())
{
    Console.Error.WriteLine($"Ignoring invalid status for unit '{status.UnitName}'.");
    return;
}

// Safe to publish the status.
```

### Example 3: Rejecting an invalid status with an exception

```csharp
using System;
using CaddyVpsToolkit.Services;

var status = new SystemdUnitStatus
{
    UnitName = "",
    ActiveState = "bogus",
    Type = "simple",
    MainPid = -1,
    TimeoutStartUsec = 0
};

try
{
    status.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
}
```

The message printed by the `catch` block has this form:

```
SystemdUnitStatus validation failed:
- UnitName cannot be null or whitespace
- ActiveState 'bogus' is not a valid systemd state
- MainPid cannot be negative
```

### Example 4: Catching a too-short start timeout

`TimeoutStartUsec` is a `ulong` measured in microseconds. A value between `1` and `999999` is rejected, because it is shorter than one second:

```csharp
using CaddyVpsToolkit.Services;

var status = new SystemdUnitStatus
{
    UnitName = "api.service",
    ActiveState = "active",
    Type = "notify",
    MainPid = 4321,
    TimeoutStartUsec = 500000
};

foreach (var problem in status.Validate())
{
    Console.WriteLine(problem);
}
// TimeoutStartUsec should be at least 1 second (1000000 microseconds) for proper timeout configuration
```

### Example 5: Calling the static methods directly

The extension methods are also static methods on `SystemdUnitServiceValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Services;

bool valid = SystemdUnitServiceValidation.IsValid(status);
```

## Notes

- `ActiveState` and `Type` are compared with `StringComparison.OrdinalIgnoreCase`, so `Active` and `SIMPLE` are accepted. The value is not normalized; the stored property keeps its original casing.
- `TimeoutStartUsec == 0` is valid and is not reported. Only values from `1` through `999999` produce the one-second warning.
- `MainPid` is a signed `int`, so only negative values are rejected. `0` is accepted and is not checked against `ActiveState`.
- `UnitName` is checked only for blank values. Any non-whitespace string passes, including names without a `.service` suffix.
- `Validate` creates a new list on every call. The returned list is a read-only view over that list, so callers cannot change the result of a later call.
- The class holds no state, and its methods only read the status. Concurrent calls on the same status are safe as long as nothing else mutates it at the same time.
- `SystemdUnitStatus` is a `sealed` class with public settable properties, so `Validate` is the only guard against invalid values. Nothing in the type prevents an invalid status from being created.
