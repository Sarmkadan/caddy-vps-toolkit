# LogEntryFluentExtensions

`LogEntryFluentExtensions` is a static class of extension methods for [LogEntry](LogEntry.md). The source lives in `Domain/Models/LogEntryFluentExtensions.cs` under the `CaddyVpsToolkit.Domain.Models` namespace. The methods let callers test and format log entries in a single chained expression, without repeating null checks or string comparisons at each call site.

Every method extends `LogEntry?`, so each one accepts a `null` receiver and handles it without throwing. The class holds no state and its methods have no side effects, so they can be called from any thread.

The class defines three public methods:

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsError` | `bool` | Checks whether the entry's `Level` is `"Error"`. |
| `Matches` | `bool` | Checks whether the entry's `Message` matches a regular expression. |
| `ToCompactString` | `string` | Formats the entry as one line of text. |

## API

### `IsError`

```csharp
public static bool IsError(this LogEntry? entry)
```

Determines whether the log entry represents an error.

- **Parameters**
  - `entry`: The `LogEntry` to evaluate. May be `null`.
- **Return value**: `true` if `entry` is not `null` and its `Level` equals `"Error"`. The comparison ignores case, so `"ERROR"` and `"error"` also match. Otherwise the method returns `false`.
- **Exceptions**: None.

The check is an exact match on `Level`. It does not treat higher severities such as `"Fatal"` or `"Critical"` as errors, and it does not match abbreviations such as `"ERR"`. Callers that need a broader definition should test `Level` directly.

### `Matches`

```csharp
public static bool Matches(this LogEntry? entry, string pattern)
```

Determines whether the entry's `Message` matches a regular expression.

- **Parameters**
  - `entry`: The `LogEntry` whose `Message` is tested. May be `null`.
  - `pattern`: A regular expression pattern, passed directly to `Regex.IsMatch`.
- **Return value**: `true` if `entry` is not `null`, `pattern` is not `null` or empty, and the `Message` matches the pattern. Otherwise the method returns `false`. A `null` `Message` is treated as an empty string.
- **Exceptions**: `ArgumentException` (or its subclass `RegexParseException`) if `pattern` is not a valid regular expression.

An empty or `null` pattern returns `false` before the regex is evaluated. This differs from `Regex.IsMatch`, which would match any input against an empty pattern. Matching is unanchored unless the pattern contains anchors, so `"timeout"` matches any message that contains the word.

### `ToCompactString`

```csharp
public static string ToCompactString(this LogEntry? entry)
```

Returns a compact, single-line string representation of the log entry.

- **Parameters**
  - `entry`: The `LogEntry` to format. May be `null`.
- **Return value**: For a non-`null` entry, a string in the form `"{Timestamp:o} [{Level}] {Source}: {Message}"`. The `Timestamp` is formatted with the round-trip `"o"` specifier (ISO 8601). If `Source` is empty or whitespace, the `Source:` part is omitted, so the result has the form `"{Timestamp:o} [{Level}] {Message}"`. If `entry` is `null`, the method returns `string.Empty`.
- **Exceptions**: None.

The `Timestamp` is written as stored. The method does not convert it to UTC. A `DateTime` with `DateTimeKind.Utc` ends in `Z`, while one with `DateTimeKind.Unspecified` does not. Because `Message` is not escaped, a message that contains line breaks produces a result that spans more than one line.

## Usage

The following examples show common ways to combine these methods. They assume the usual `using` directive for the namespace:

```csharp
using CaddyVpsToolkit.Domain.Models;
```

### Example 1: Counting errors in a batch

`IsError` works as a predicate in LINQ, so a list of entries can be filtered without a separate null check:

```csharp
using System.Collections.Generic;
using System.Linq;

public static class ErrorReport
{
    public static int CountErrors(IEnumerable<LogEntry> entries)
    {
        return entries.Count(e => e.IsError());
    }
}
```

Because the receiver is `LogEntry?`, `e.IsError()` also returns `false` for a `null` element in the sequence.

### Example 2: Finding entries with a message pattern

`Matches` tests one pattern against a single entry. The example below keeps entries whose message reports an upstream timeout or a connection refusal:

```csharp
using System.Collections.Generic;
using System.Linq;

public static class UpstreamDiagnostics
{
    private const string FailurePattern = @"upstream (timed out|connection refused)";

    public static IReadOnlyList<LogEntry> FindUpstreamFailures(IEnumerable<LogEntry> entries)
    {
        return entries
            .Where(e => e.IsError() || e.Matches(FailurePattern))
            .ToList();
    }
}
```

The pattern is a constant, so it is valid for every call. If a pattern comes from configuration or user input, validate it first. An invalid pattern makes `Matches` throw, and this method does not catch the exception.

### Example 3: Writing a one-line log summary

`ToCompactString` gives a consistent line format for console output or simple log files:

```csharp
using System;

var entry = new LogEntry
{
    Timestamp = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc),
    Level = "Error",
    Source = "/var/log/caddy/access.log",
    Message = "upstream timed out after 30s"
};

Console.WriteLine(entry.ToCompactString());
// 2026-10-25T12:00:00.0000000Z [Error] /var/log/caddy/access.log: upstream timed out after 30s

var noSource = new LogEntry
{
    Timestamp = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc),
    Level = "Info",
    Message = "service restarted"
};

Console.WriteLine(noSource.ToCompactString());
// 2026-10-25T12:00:00.0000000Z [Info] service restarted
```

### Example 4: Handling a missing entry

Since the methods accept `null`, a lookup that returns `null` can be passed straight through:

```csharp
using System;
using System.Collections.Generic;

public static class EntryLookup
{
    public static string Describe(IDictionary<string, LogEntry> index, string key)
    {
        index.TryGetValue(key, out var entry);

        // Returns string.Empty when the key is absent, so no null check is needed.
        return entry.ToCompactString();
    }
}
```

## Notes

*   **Null handling:** All three methods accept a `null` receiver. `IsError` and `Matches` return `false`, and `ToCompactString` returns `string.Empty`. A caller does not need to test for `null` before using them, and the methods never throw because of a `null` entry.
*   **Pattern safety:** `Matches` passes the pattern to `Regex.IsMatch` with no timeout. A pattern with nested quantifiers can run for a long time on a long message. Use only trusted patterns, or add a timeout in a separate helper if the pattern comes from outside the application.
*   **Level comparison:** `IsError` compares against the literal `"Error"`. The `Level` values in the toolkit are free-form strings whose exact spelling depends on the source, so entries written with other spellings will not be detected.
*   **Stateless:** None of the methods read or write shared state, so they can be used concurrently on the same entry as long as the `LogEntry` itself is not being modified during the call. See [LogEntry](LogEntry.md) for the thread-safety notes on the data type.
