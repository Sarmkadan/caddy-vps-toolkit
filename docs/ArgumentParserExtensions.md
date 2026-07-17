# ArgumentParserExtensions

Provides extension methods for parsing and querying command-line arguments stored as a list of strings. This class offers typed accessors for positional arguments and flags, enabling callers to extract integers, booleans, and raw string values without writing repetitive parsing logic.

## API

### GetCommandSpan

```csharp
public static ReadOnlySpan<char> GetCommandSpan(this IReadOnlyList<string> args)
```

Returns the first element of the argument list as a read-only character span, representing the command name. If the list is null or empty, the returned span is empty.

**Parameters**
- `args` — The argument list to inspect.

**Returns**
A `ReadOnlySpan<char>` over the first argument, or an empty span if no arguments are present.

**Exceptions**
None.

---

### GetPositionalAsInt

```csharp
public static int? GetPositionalAsInt(this IReadOnlyList<string> args, int index)
```

Retrieves the positional argument at the specified zero-based index and parses it as an integer.

**Parameters**
- `args` — The argument list.
- `index` — The zero-based index of the positional argument.

**Returns**
The parsed `int` value, or `null` if the argument does not exist, is not a valid integer, or `args` is null.

**Exceptions**
None.

---

### GetPositionalAsBoolean

```csharp
public static bool? GetPositionalAsBoolean(this IReadOnlyList<string> args, int index)
```

Retrieves the positional argument at the specified index and interprets it as a boolean. Accepted true values include `"true"` (case-insensitive) and `"1"`; accepted false values include `"false"` (case-insensitive) and `"0"`.

**Parameters**
- `args` — The argument list.
- `index` — The zero-based index of the positional argument.

**Return**
`true` or `false` if parsing succeeds, or `null` if the argument is missing, unrecognized, or `args` is null.

**Exceptions**
None.

---

### GetFlagValueAsInt

```csharp
public static int? GetFlagValueAsInt(this IReadOnlyList<string> args, string flagName)
```

Searches for a flag in the form `--flagName=value` or `--flagName value` and parses the associated value as an integer.

**Parameters**
- `args` — The argument list.
- `flagName` — The flag name without leading dashes (e.g., `"port"` for `--port=8080`).

**Return Value**
The parsed integer value, or `null` if the flag is not found, has no value, or the value cannot be parsed as an integer.

**Exceptions**
None.

---

### GetFlagValueAsBoolean

```csharp
public static bool? GetFlagValueAsBoolean(this IReadOnlyList<string> args, string flagName)
```

Searches for a flag in the form `--flagName=value` or `--flagName value` and interprets the value as a boolean using the same rules as `GetPositionalAsBoolean`.

**Parameters**
- `args` — The argument list.
- `flagName` — The flag name without leading dashes.

**Return Value**
`true` or `false` if the flag exists and its value is a recognized boolean string; `null` otherwise.

**Exceptions**
None.

---

### HasFlagWithValue

```csharp
public static bool HasFlagWithValue(this IReadOnlyList<string> args, string flagName)
```

Determines whether a flag is present and has an associated non-empty value. A flag is considered to have a value if it appears as `--flag=...` or `--flag ...` with a subsequent argument that does not itself start with `--`.

**Parameters**
- `args` — The argument list.
- `flagName` — The flag name without leading dashes.

**Return Value**
`true` if the flag exists and has a value; `false` otherwise.

**Exceptions**
None.

---

### GetAllPositionalReadOnly

```csharp
public static IReadOnlyList<string> GetAllPositionalReadOnly(this IReadOnlyList<string> args)
```

Returns all positional arguments, excluding the command name (first element) and any flag tokens (arguments starting with `--` or `-`).

**Parameters**
- `args` — The argument list.

**Return Value**
A read-only list of positional argument strings. Returns an empty list if `args` is null or contains no positionals.

**Exceptions**
None.

---

### GetAllFlagsReadOnly

```csharp
public static IReadOnlyList<string> GetAllFlagsReadOnly(this IReadOnlyList<string> args)
```

Returns all flag tokens (arguments starting with `--` or `-`), including both standalone flags and flags with inline values (e.g., `--verbose` and `--port=8080`).

**Parameters**
- `args` — The argument list.

**Return Value**
A read-only list of flag strings. Returns an empty list if `args` is null or contains no flags.

**Exceptions**
None.

---

### GetPositionalCount

```csharp
public static int GetPositionalCount(this IReadOnlyList<string> args)
```

Counts the number of positional arguments, excluding the command name and any flag tokens.

**Parameters**
- `args` — The argument list.

**Return Value**
The count of positional arguments. Returns `0` if `args` is null or empty.

**Exceptions**
None.

---

### HasAnyFlag

```csharp
public static bool HasAnyFlag(this IReadOnlyList<string> args)
```

Determines whether the argument list contains at least one flag token (an argument starting with `--` or `-`).

**Parameters**
- `args` — The argument list.

**Return Value**
`true` if at least one flag is present; `false` otherwise.

**Exceptions**
None.

---

## Usage

### Example 1: Extracting a command and positional parameters

```csharp
IReadOnlyList<string> args = new[] { "deploy", "42", "true", "--verbose" };

ReadOnlySpan<char> command = args.GetCommandSpan(); // "deploy"
int? count = args.GetPositionalAsInt(0);            // 42
bool? dryRun = args.GetPositionalAsBoolean(1);      // true
int positionalCount = args.GetPositionalCount();    // 2
bool hasVerbose = args.HasAnyFlag();                // true
```

### Example 2: Working with flags

```csharp
IReadOnlyList<string> args = new[] { "serve", "--port=8080", "--enable-logging", "--timeout", "30" };

int? port = args.GetFlagValueAsInt("port");          // 8080
bool? logging = args.GetFlagValueAsBoolean("enable-logging"); // null (no value)
bool hasTimeout = args.HasFlagWithValue("timeout");  // true
IReadOnlyList<string> allFlags = args.GetAllFlagsReadOnly(); // ["--port=8080", "--enable-logging", "--timeout", "30"]
```

---

## Notes

- **Null safety**: All methods accept a null `args` parameter and return a sensible default (empty span, empty list, `null`, or `false`). No `ArgumentNullException` is thrown.
- **Flag syntax**: Flags are identified by a leading `--` or `-`. The flag name passed to methods like `GetFlagValueAsInt` should omit these dashes. Behavior with malformed flag names (e.g., containing dashes) is implementation-defined and should not be relied upon.
- **Boolean parsing**: Only the strings `true`, `false`, `1`, and `0` (case-insensitive) are recognized. Any other value results in `null`.
- **Integer parsing**: Uses standard integer parsing rules for the current culture. Values that overflow `Int32` or contain non-numeric characters return `null`.
- **Thread safety**: All methods are static and operate on the provided `IReadOnlyList<string>` without mutation. They are safe to call concurrently as long as the underlying list is not modified during the call.
- **Immutability**: The returned `IReadOnlyList<string>` instances from `GetAllPositionalReadOnly` and `GetAllFlagsReadOnly` may be wrappers over the original list or newly allocated collections. Callers should not assume mutability.
