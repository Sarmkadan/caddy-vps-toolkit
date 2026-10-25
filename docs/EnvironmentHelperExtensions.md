# EnvironmentHelperExtensions

`EnvironmentHelperExtensions` provides typed accessors and a required-variable check on top of `EnvironmentHelper` in the `CaddyVpsToolkit.Utilities` namespace. Each method reads a variable through `EnvironmentHelper.GetEnvironmentVariable` and converts it to a `bool` or `int`, or throws if the variable is missing.

The methods are declared as `public static` without a `this` receiver, so they are not called on a string or on `EnvironmentHelper`. Call them through the class name, for example `EnvironmentHelperExtensions.GetInt("TOOLKIT_PORT", 8080)`.

For the underlying get and set methods, see [EnvironmentHelper](EnvironmentHelper.md).

## API

### `GetRequiredVariable`
```csharp
public static string GetRequiredVariable(string name)
```

Returns the value of an environment variable and throws if the variable is not set.

**Parameters:**
- `name` (`string`): The name of the environment variable.

**Return value:**
- Returns the variable's value as a `string`.

**Exceptions:**
- Throws `InvalidOperationException` if the variable is not set.
- Throws `ArgumentException` if `name` is `null` or empty. This comes from `EnvironmentHelper.GetEnvironmentVariable`.

A variable that is set to an empty string is not treated as missing. The method returns `""` in that case.

---

### `GetBool`
```csharp
public static bool GetBool(string name, bool defaultValue = false)
```

Returns the value of an environment variable parsed as a `bool`.

**Parameters:**
- `name` (`string`): The name of the environment variable.
- `defaultValue` (`bool`, optional): The value returned when the variable is not set. Defaults to `false`.

**Return value:**
- Returns the parsed `bool`, or `defaultValue` if the variable is not set.

**Exceptions:**
- Throws `FormatException` if the variable is set but cannot be parsed by `bool.TryParse`. Parsing is case-insensitive, so `"true"`, `"True"`, and `"FALSE"` are all valid.
- Throws `ArgumentException` if `name` is `null` or empty.

---

### `GetInt`
```csharp
public static int GetInt(string name, int defaultValue = 0)
```

Returns the value of an environment variable parsed as an `int`.

**Parameters:**
- `name` (`string`): The name of the environment variable.
- `defaultValue` (`int`, optional): The value returned when the variable is not set. Defaults to `0`.

**Return value:**
- Returns the parsed `int`, or `defaultValue` if the variable is not set.

**Exceptions:**
- Throws `FormatException` if the variable is set but cannot be parsed by `int.TryParse`. This includes empty strings and values outside the `int` range.
- Throws `ArgumentException` if `name` is `null` or empty.

## Usage

### Example 1: Reading a Required Secret
```csharp
// Throws InvalidOperationException at startup if the variable is missing
string apiKey = EnvironmentHelperExtensions.GetRequiredVariable("TOOLKIT_API_KEY");
```

### Example 2: Optional Flags and Numeric Settings
```csharp
// Falls back to defaults when the variables are not set
bool verbose = EnvironmentHelperExtensions.GetBool("TOOLKIT_VERBOSE");
bool dryRun = EnvironmentHelperExtensions.GetBool("TOOLKIT_DRY_RUN", defaultValue: true);
int port = EnvironmentHelperExtensions.GetInt("TOOLKIT_PORT", 8080);
int retries = EnvironmentHelperExtensions.GetInt("TOOLKIT_RETRIES", 3);
```

### Example 3: Handling Invalid Values
```csharp
try
{
    int maxConnections = EnvironmentHelperExtensions.GetInt("TOOLKIT_MAX_CONNECTIONS", 100);
}
catch (FormatException ex)
{
    // Message names the variable and the rejected value
    Console.Error.WriteLine(ex.Message);
}
```

## Notes

- **Missing versus empty**: Only a variable that is not set counts as missing. An empty string is returned by `GetRequiredVariable`, but it is rejected by `GetBool` and `GetInt` with `FormatException`.
- **Defaults**: `GetBool` and `GetInt` return their default only when the variable is not set. A value that is set but invalid throws instead of falling back, so a typo in configuration fails loudly.
- **Calling convention**: The methods are static helpers, not extension methods on `string`. Calling them as `"value".GetInt()` does not compile.
- **Thread Safety**: The methods do not modify state. They read the process environment, which is shared, so changes made by other threads through `EnvironmentHelper.SetEnvironmentVariable` can affect the result.
- **Parsing Rules**: `GetBool` and `GetInt` use the parameterless `bool.TryParse` and `int.TryParse` overloads. Integer parsing uses the current culture's number format, so a value written with culture-specific digits or separators may be rejected on some machines.
