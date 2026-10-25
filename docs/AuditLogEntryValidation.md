# AuditLogEntryValidation

`AuditLogEntryValidation` is a static class of extension methods that check an `AuditLogEntry` for required values and returns a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Auditing` namespace, so add `using CaddyVpsToolkit.Auditing;` before calling them.

Validation only checks that required string fields are not null, empty, or whitespace, that the timestamp is set, and that no `Details` key is whitespace. It does not check value formats or ranges.

## API

### `Validate(this AuditLogEntry value)`
Validates an `AuditLogEntry` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `Id` | Must not be null, empty, or whitespace. | `Id cannot be null or whitespace.` |
| `Timestamp` | Must not be `default` or `DateTime.MinValue`. | `Timestamp must be a valid DateTime (cannot be default or MinValue).` |
| `Action` | Must not be null, empty, or whitespace. | `Action cannot be null or whitespace.` |
| `Actor` | Must not be null, empty, or whitespace. | `Actor cannot be null or whitespace.` |
| `Target` | Must not be null, empty, or whitespace. | `Target cannot be null or whitespace.` |
| `Result` | Must not be null, empty, or whitespace. | `Result cannot be null or whitespace.` |
| `Details` | If not `null`, no key may be null or whitespace. Only the first bad key is reported. | `Details dictionary contains a null or whitespace key.` |

**Parameters:**
- `value` (`AuditLogEntry`) – The audit log entry to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only list of validation messages. The list is empty if the entry is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this AuditLogEntry value)`
Returns whether the entry passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`AuditLogEntry`) – The audit log entry to check.

**Returns:**
`bool` – `true` if the entry has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`.

### `EnsureValid(this AuditLogEntry value)`
Validates the entry and throws if any problem is found. Use it at boundaries where an invalid entry should be rejected rather than handled.

**Parameters:**
- `value` (`AuditLogEntry`) – The audit log entry to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the entry is invalid. The exception message has the form `AuditLogEntry validation failed: <problem 1>; <problem 2>`, listing every problem from `Validate` in order. The parameter name is `value`.

## Usage

### Example 1: Collecting all problems

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Auditing;

var entry = new AuditLogEntry
{
    Action = "Create",
    Actor = "",
    Target = "api.example.com",
    Result = "Success"
};

IReadOnlyList<string> problems = entry.Validate();

foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
// Actor cannot be null or whitespace.
```

`Id` defaults to a new GUID and `Timestamp` defaults to `DateTime.UtcNow`, so an entry built this way fails only on `Actor`.

### Example 2: Checking validity with a boolean

```csharp
using System;
using CaddyVpsToolkit.Auditing;

if (!entry.IsValid())
{
    Console.Error.WriteLine($"Skipping invalid audit entry for target '{entry.Target}'.");
    return;
}

// Safe to persist the entry.
```

### Example 3: Rejecting an invalid entry with an exception

```csharp
using System;
using CaddyVpsToolkit.Auditing;

var entry = new AuditLogEntry
{
    Action = "Delete",
    Actor = "",
    Target = "api.example.com",
    Result = ""
};

try
{
    entry.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // AuditLogEntry validation failed: Actor cannot be null or whitespace.; Result cannot be null or whitespace.
}
```

Each message in the combined exception message ends with a period, so the separator appears as `.; `.

### Example 4: Catching a whitespace `Details` key

`Details` is a `Dictionary<string, object>`, which does not allow `null` keys. The key check therefore catches only whitespace keys.

```csharp
using CaddyVpsToolkit.Auditing;

var entry = new AuditLogEntry
{
    Action = "Update",
    Actor = "admin",
    Target = "upstream-pool",
    Result = "Success"
};

entry.Details[" "] = "unnamed";

foreach (var problem in entry.Validate())
{
    Console.WriteLine(problem);
}
// Details dictionary contains a null or whitespace key.
```

### Example 5: Calling the static methods directly

The extension methods are also static methods on `AuditLogEntryValidation`, so they can be called without the `this` syntax:

```csharp
using CaddyVpsToolkit.Auditing;

bool valid = AuditLogEntryValidation.IsValid(entry);
```

## Notes

- `Timestamp == default` and `Timestamp == DateTime.MinValue` are the same condition, because `default(DateTime)` is `DateTime.MinValue`. The check treats an unset timestamp as invalid.
- Only the first whitespace `Details` key is reported, even if several keys are invalid. The message says there is at least one bad key, not how many.
- `Validate` creates a new list on every call. The returned list is a read-only view over that list, so callers cannot change the result of a later call.
- The class holds no state, and its methods only read the entry. Concurrent calls on the same entry are safe as long as nothing else mutates it at the same time.
- Validation does not check `Action`, `Actor`, `Target`, or `Result` against a fixed set of values. Any non-whitespace string passes.
- `AuditLogEntry` is a `sealed` class with public settable properties, so `Validate` is the only guard against invalid values. Nothing in the type prevents an invalid entry from being created.
