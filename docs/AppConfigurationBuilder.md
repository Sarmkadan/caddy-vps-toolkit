# AppConfigurationBuilder

`AppConfigurationBuilder` is a fluent builder for constructing and retrieving configuration values from multiple sources, including JSON files, environment variables, and in-memory settings. It provides a unified interface for loading and accessing configuration data with type-safe retrieval methods.

## API

### `WithJsonFile`
Adds a JSON file as a configuration source. The file is read and merged into the configuration hierarchy.

- **Parameters**
  - `path` (string): The file system path to the JSON file.
- **Return Value**
  - Returns the builder instance (`AppConfigurationBuilder`) for method chaining.
- **Exceptions**
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `JsonException` if the file contains invalid JSON.

### `WithEnvironmentVariables`
Adds environment variables as a configuration source. Variables are flattened and merged into the configuration hierarchy.

- **Parameters**
  - None.
- **Return Value**
  - Returns the builder instance (`AppConfigurationBuilder`) for method chaining.
- **Exceptions**
  - None.

### `WithSetting`
Adds an in-memory key-value pair to the configuration.

- **Parameters**
  - `key` (string): The configuration key.
  - `value` (string): The configuration value.
- **Return Value**
  - Returns the builder instance (`AppConfigurationBuilder`) for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` or `value` is `null`.

### `WithDefaults`
Configures default values for common configuration keys. These defaults are used if no other source provides a value.

- **Parameters**
  - None.
- **Return Value**
  - Returns the builder instance (`AppConfigurationBuilder`) for method chaining.
- **Exceptions**
  - None.

### `Build`
Finalizes the configuration and returns an immutable `AppConfiguration` instance.

- **Parameters**
  - None.
- **Return Value**
  - Returns an `AppConfiguration` instance populated from all added sources.
- **Exceptions**
  - None.

### `AppConfiguration`
The immutable configuration object produced by `Build`. Exposes methods for retrieving values.

### `GetString`
Retrieves a configuration value as a string.

- **Parameters**
  - `key` (string): The configuration key.
- **Return Value**
  - Returns the value as a string, or `null` if the key does not exist.
- **Exceptions**
  - None.

### `GetInt`
Retrieves a configuration value as an integer.

- **Parameters**
  - `key` (string): The configuration key.
- **Return Value**
  - Returns the parsed integer value, or `0` if the key does not exist or parsing fails.
- **Exceptions**
  - None.

### `GetBool`
Retrieves a configuration value as a boolean.

- **Parameters**
  - `key` (string): The configuration key.
- **Return Value**
  - Returns the parsed boolean value, or `false` if the key does not exist or parsing fails.
- **Exceptions**
  - None.

### `GetObject<T>`
Retrieves a configuration value as a strongly-typed object.

- **Parameters**
  - `key` (string): The configuration key.
- **Return Value**
  - Returns the deserialized object of type `T`, or `default(T)` if the key does not exist or deserialization fails.
- **Exceptions**
  - None.

### `Exists`
Checks whether a configuration key exists.

- **Parameters**
  - `key` (string): The configuration key.
- **Return Value**
  - Returns `true` if the key exists; otherwise, `false`.
- **Exceptions**
  - None.

### `GetAll`
Retrieves all configuration key-value pairs as a flattened dictionary.

- **Parameters**
  - None.
- **Return Value**
  - Returns a `Dictionary<string, object>` containing all configuration entries.
- **Exceptions**
  - None.

## Usage

### Example 1: Loading from JSON and Environment
