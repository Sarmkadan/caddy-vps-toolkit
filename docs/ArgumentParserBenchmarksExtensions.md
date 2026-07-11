# ArgumentParserBenchmarksExtensions

Provides extension methods for parsing and analyzing command-line arguments in benchmarking scenarios, offering utilities to inspect flag presence, count, and average values.

## API

### `HasGetCommand`

Determines whether the parsed arguments contain a command that matches "get".

```csharp
public static bool HasGetCommand(this IReadOnlyList<string> args)
```

**Returns**
`true` if the arguments list contains the exact string "get"; otherwise, `false`.

**Throws**
`ArgumentNullException` if `args` is `null`.

---

### `GetFlagCount`

Counts the number of flag arguments present in the parsed command-line arguments.

```csharp
public static int GetFlagCount(this IReadOnlyList<string> args)
```

**Returns**
The total number of flags detected (e.g., "-v", "--verbose").

**Throws**
`ArgumentNullException` if `args` is `null`.

---

### `GetFlagValueAverage`

Calculates the average numeric value of all flag arguments that represent numbers (e.g., "-t 5", "--timeout 10").

```csharp
public static double GetFlagValueAverage(this IReadOnlyList<string> args)
```

**Returns**
The arithmetic mean of all numeric flag values. Returns `0.0` if no numeric flags are present.

**Throws**
`ArgumentNullException` if `args` is `null`.
`FormatException` if any flag value cannot be parsed as a `double`.

---
### `HasAnyFlags`

Determines whether the parsed arguments contain any flag arguments (e.g., "-v", "--help").

```csharp
public static bool HasAnyFlags(this IReadOnlyList<string> args)
```

**Returns**
`true` if at least one flag is present; otherwise, `false`.

**Throws**
`ArgumentNullException` if `args` is `null`.

## Usage

### Example 1: Checking for a "get" command and counting flags
```csharp
var args = new[] { "get", "--verbose", "-t", "500" };
bool hasGet = args.HasGetCommand();          // true
int flagCount = args.GetFlagCount();          // 3
```

### Example 2: Calculating average flag value
```csharp
var args = new[] { "--timeout", "200", "-v", "--retries", "3" };
double avg = args.GetFlagValueAverage();      // 101.5
```

## Notes

- **Edge Cases**: Methods assume flags are prefixed with `-` or `--`. Non-prefixed values are ignored in flag counting and value averaging.
- **Thread Safety**: All methods are stateless and thread-safe provided the input `IReadOnlyList<string>` is not modified during concurrent access.
- **Performance**: Methods operate in O(n) time relative to the number of arguments, with minimal allocations.
