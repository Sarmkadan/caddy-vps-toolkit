# IOutputFormatter

The `IOutputFormatter` interface defines a contract for formatting objects into structured string representations, primarily used for console output or logging in the caddy-vps-toolkit project. Implementations provide consistent formatting for various data types, supporting customizable output styles such as tables or other structured formats.

## API

### `TableFormatter`

The default formatter instance used by the `IOutputFormatter` implementation. This static property provides access to the configured formatter for standard output operations.

### `string Format<T>(T value)`

Formats the provided value of type `T` into a structured string representation. The output format depends on the underlying formatter implementation and the type of `T`.

- **Parameters**:
  - `value` (`T`): The object to format.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null`.

### `string Format<T>(T value, OutputFormat format)`

Formats the provided value of type `T` into a structured string representation using the specified output format.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format (e.g., table, JSON, plaintext).
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null` or `format` is invalid.

### `string Format<T>(T value, OutputFormat format, int maxDepth)`

Formats the provided value of type `T` into a structured string representation, limiting the depth of nested object traversal.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format.
  - `maxDepth` (`int`): The maximum depth to traverse nested objects.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null` or `maxDepth` is negative.

### `string Format<T>(T value, OutputFormat format, int maxDepth, bool includeNulls)`

Formats the provided value of type `T` into a structured string representation with control over null value inclusion and traversal depth.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format.
  - `maxDepth` (`int`): The maximum depth to traverse nested objects.
  - `includeNulls` (`bool`): Whether to include null fields in the output.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null`.

### `string Format<T>(T value, OutputFormat format, int maxDepth, bool includeNulls, IFormatProvider provider)`

Formats the provided value of type `T` into a structured string representation with full control over formatting culture and other parameters.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format.
  - `maxDepth` (`int`): The maximum depth to traverse nested objects.
  - `includeNulls` (`bool`): Whether to include null fields in the output.
  - `provider` (`IFormatProvider`): The format provider to use for culture-sensitive formatting.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null`.

### `string Format<T>(T value, OutputFormat format, int maxDepth, bool includeNulls, IFormatProvider provider, string[] ignoredProperties)`

Formats the provided value of type `T` into a structured string representation while excluding specified properties from the output.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format.
  - `maxDepth` (`int`): The maximum depth to traverse nested objects.
  - `includeNulls` (`bool`): Whether to include null fields in the output.
  - `provider` (`IFormatProvider`): The format provider to use for culture-sensitive formatting.
  - `ignoredProperties` (`string[]`): An array of property names to exclude from the output.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null`.

### `string Format<T>(T value, OutputFormat format, int maxDepth, bool includeNulls, IFormatProvider provider, string[] ignoredProperties, bool sortProperties)`

Formats the provided value of type `T` into a structured string representation with sorting of properties in the output.

- **Parameters**:
  - `value` (`T`): The object to format.
  - `format` (`OutputFormat`): The desired output format.
  - `maxDepth` (`int`): The maximum depth to traverse nested objects.
  - `includeNulls` (`bool`): Whether to include null fields in the output.
  - `provider` (`IFormatProvider`): The format provider to use for culture-sensitive formatting.
  - `ignoredProperties` (`string[]`): An array of property names to exclude from the output.
  - `sortProperties` (`bool`): Whether to sort properties alphabetically in the output.
- **Return value**: A string containing the formatted representation of `value`.
- **Exceptions**: May throw `ArgumentNullException` if `value` is `null`.

## Usage

### Example 1: Basic Formatting

```csharp
var formatter = IOutputFormatter.TableFormatter;
var server = new Server { Name = "web01", Status = "Running", Uptime = TimeSpan.FromDays(30) };
string output = formatter.Format(server);
Console.WriteLine(output);
// Outputs a table with columns: Name, Status, Uptime
```

### Example 2: Advanced Formatting with Options

```csharp
var formatter = IOutputFormatter.TableFormatter;
var config = new ServerConfig
{
    Host = "example.com",
    Port = 443,
    Timeout = 30,
    Retries = 3,
    Enabled = true
};
string output = formatter.Format(
    config,
    OutputFormat.Json,
    maxDepth: 2,
    includeNulls: false,
    ignoredProperties: new[] { "InternalId" },
    sortProperties: true
);
Console.WriteLine(output);
// Outputs a JSON representation with sorted properties, excluding InternalId
```

## Notes

- Null values are handled gracefully in most cases, but passing `null` as the `value` parameter will typically result in an `ArgumentNullException`.
- Thread safety depends on the underlying formatter implementation. The default `TableFormatter` is stateless and thread-safe for concurrent calls with different inputs.
- Property sorting (`sortProperties: true`) may impact performance for objects with many properties.
- Ignored properties (`ignoredProperties`) are matched by exact name; case sensitivity depends on the formatter implementation.
- The `maxDepth` parameter prevents infinite recursion when formatting cyclic object graphs, but may truncate deeply nested structures.
