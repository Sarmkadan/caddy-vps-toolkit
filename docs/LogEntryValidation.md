# LogEntryValidation

`LogEntryValidation` is a static class of extension methods that check a [LogEntry](LogEntry.md) for invalid values and returns a list of problems. It offers three entry points: `Validate` returns every problem found, `IsValid` returns a simple boolean, and `EnsureValid` throws an `ArgumentException` that lists all problems. All three are defined in the `CaddyVpsToolkit.Domain.Models` namespace, so the calling code needs `using CaddyVpsToolkit.Domain.Models;`.

Validation checks the `Timestamp` kind and value, that `Level` is one of the known levels, that `Message` and `Source` are not whitespace, and that `ServiceId` is not whitespace when it is set. It does not check the length or format of any string.

## API

### `Validate(this LogEntry value)`

```csharp
public static IReadOnlyList<string> Validate(this LogEntry value)
```

Validates a `LogEntry` and collects every problem found. Validation does not stop at the first failure.

The following checks run, in this order:

| Property | Rule | Message |
|----------|------|---------|
| `Timestamp` | Kind must be `DateTimeKind.Utc`. | `Timestamp must be a UTC DateTime value.` |
| `Timestamp` | Must not equal `default`. Runs only when the kind is UTC, so it catches `DateTime.MinValue` with UTC kind. | `Timestamp must be set to a non-default DateTime value.` |
| `Level` | Must not be null, empty, or whitespace. | `Level cannot be null, empty, or whitespace.` |
| `Level` | Must be `Debug`, `Info`, `Warning`, or `Error`. The comparison ignores case. Runs only when `Level` is not blank. | `Level '<value>' is not a valid log level. Valid levels are: Debug, Info, Warning, Error.` |
| `Message` | Must not be null, empty, or whitespace. | `Message cannot be null, empty, or whitespace.` |
| `Source` | Must not be null, empty, or whitespace. | `Source cannot be null, empty, or whitespace.` |
| `ServiceId` | If not `null`, must not be whitespace. An empty string counts as whitespace. | `ServiceId cannot be an empty or whitespace string when set.` |

**Parameters:**
- `value` (`LogEntry`) – The log entry to validate.

**Returns:**
`IReadOnlyList<string>` – A read-only view of the validation messages. The list is empty if the entry is valid.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `IsValid(this LogEntry value)`

```csharp
public static bool IsValid(this LogEntry value)
```

Returns whether the entry passes every check in `Validate`. It is equivalent to `value.Validate().Count == 0`.

**Parameters:**
- `value` (`LogEntry`) – The log entry to check.

**Returns:**
`bool` – `true` if the entry has no validation problems; otherwise, `false`.

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

### `EnsureValid(this LogEntry value)`

```csharp
public static void EnsureValid(this LogEntry value)
```

Validates the entry and throws if any problem is found. Use it at boundaries where an invalid entry should be rejected rather than handled.

**Parameters:**
- `value` (`LogEntry`) – The log entry to validate.

**Returns:**
`void`

**Throws:**
`ArgumentNullException` – If `value` is `null`. The parameter name is `value`.

`ArgumentException` – If the entry is invalid. The message has the form `LogEntry is not valid. Problems: <problem 1> <problem 2>`. The problems are joined with a single space, in the order `Validate` reports them. The parameter name is `value`.

## Usage

The examples assume the namespace directive shown below:

```csharp
using CaddyVpsToolkit.Domain.Models;
```

### Example 1: Collecting all problems

```csharp
using System;

var entry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Verbose",
    Message = "",
    Source = "/var/log/caddy/access.log"
};

foreach (var problem in entry.Validate())
{
    Console.WriteLine(problem);
}
// Level 'Verbose' is not a valid log level. Valid levels are: Debug, Info, Warning, Error.
// Message cannot be null, empty, or whitespace.
```

### Example 2: Checking validity with a boolean

```csharp
using System;

if (!entry.IsValid())
{
    Console.Error.WriteLine("Skipping invalid log entry.");
    return;
}

// Safe to store or forward the entry.
```

### Example 3: Rejecting an invalid entry with an exception

```csharp
using System;

var entry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Info",
    Message = "upstream recovered",
    Source = " "
};

try
{
    entry.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    // LogEntry is not valid. Problems: Source cannot be null, empty, or whitespace.
}
```

### Example 4: Creating a UTC timestamp that passes validation

`LogEntry.Timestamp` defaults to `default(DateTime)`, which has `Unspecified` kind and fails the UTC check. Set the timestamp explicitly with a UTC value:

```csharp
using System;

var entry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Warning",
    Message = "certificate expires in 7 days",
    Source = "caddy",
    ServiceId = "api-gateway"
};

Console.WriteLine(entry.IsValid()); // True
```

### Example 5: Calling the static methods directly

The extension methods are also static methods on `LogEntryValidation`, so they can be called without the `this` syntax:

```csharp
bool valid = LogEntryValidation.IsValid(entry);
```

## Notes

- `Timestamp` values with `DateTimeKind.Local` or `DateTimeKind.Unspecified` always fail, even when they represent a valid moment. Convert with `ToUniversalTime()` or create the value with `DateTimeKind.Utc` before validating.
- A `default` `Timestamp` reports the UTC-kind message, not the non-default message, because `default(DateTime)` has `Unspecified` kind. The non-default message appears only for `DateTime.MinValue` with `Utc` kind.
- `Level` is checked only against the four fixed names. `LogEntry.Level` defaults to `"Info"`, so the check passes unless the level is changed.
- `Message` and `Source` have no default checks in `LogEntry` itself. A new `LogEntry` with only `Timestamp` set fails on both `Message` and `Source`.
- `ServiceId` may be `null`. Only a non-null value that is empty or whitespace is reported.
- `Validate` creates a new list on every call. Callers cannot change the result of a later call through the returned list.
- The class holds no state, and its methods only read the entry. Concurrent calls on the same entry are safe as long as nothing else mutates it at the same time.
- `LogEntry` has public settable properties, so `Validate` is the only guard against invalid values. Nothing in the type prevents an invalid entry from being created.
