# AppConfigurationBuilderExtensions

`AppConfigurationBuilderExtensions` provides extension methods for configuring `IConfigurationBuilder` with common configuration sources in a fluent API style. These methods simplify adding JSON files, environment variables, and in-memory configurations to the builder chain while maintaining consistent behavior across the `caddy-vps-toolkit` project.

## API

### `WithJsonFile`
Adds a JSON configuration file to the builder from the specified file path.

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.
- `path` (`string`): The file path to the JSON configuration file.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` or `path` is `null`.
- Throws `FileNotFoundException` if the file at `path` does not exist.

---

### `WithEnvironmentVariables`
Adds environment variables to the configuration builder.

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.
- `prefix` (`string?`): Optional prefix to filter environment variables. If `null`, all environment variables are included.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` is `null`.

---

### `WithSettings`
Adds a JSON configuration file named `appsettings.json` from the application's base directory.

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` is `null`.
- Throws `FileNotFoundException` if `appsettings.json` does not exist in the base directory.

---

### `WithDefaultSettings`
Adds the default configuration sources in the following order:
1. `appsettings.json` (from base directory)
2. Environment variables (with no prefix)

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` is `null`.
- Throws `FileNotFoundException` if `appsettings.json` does not exist in the base directory.

---
### `WithJsonString`
Adds an in-memory JSON configuration from a string.

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.
- `json` (`string`): The JSON string to parse and add to the configuration.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` or `json` is `null`.
- Throws `JsonException` if `json` is not valid JSON.

---
### `WithObjectConfiguration`
Adds an in-memory configuration from an object using JSON serialization.

**Parameters:**
- `builder` (`IConfigurationBuilder`): The configuration builder instance.
- `configuration` (`object`): The object to serialize and add to the configuration.

**Return value:**
- Returns the `IConfigurationBuilder` for method chaining.

**Exceptions:**
- Throws `ArgumentNullException` if `builder` or `configuration` is `null`.
- Throws `JsonException` if the object cannot be serialized to JSON.

## Usage

### Example 1: Basic Configuration Setup
```csharp
var builder = new ConfigurationBuilder()
    .WithDefaultSettings()
    .Build();
```

### Example 2: Custom JSON and Environment Variables
```csharp
var builder = new ConfigurationBuilder()
    .WithJsonFile("config/appsettings.custom.json")
    .WithEnvironmentVariables("MYAPP_")
    .Build();
```

## Notes

- **Thread Safety**: All methods are thread-safe as they do not modify shared state and only operate on the provided `IConfigurationBuilder` instance.
- **File Paths**: `WithJsonFile` and `WithSettings` assume the file paths are valid and accessible. Ensure proper file permissions are granted when running in restricted environments.
- **JSON Parsing**: `WithJsonString` and `WithObjectConfiguration` rely on `System.Text.Json` for parsing. Invalid JSON or non-serializable objects will throw `JsonException`.
- **Order of Operations**: Configuration sources are added in the order the methods are called. Later sources override earlier ones for the same keys.
