# StringExtensionsBenchmarks

The `StringExtensionsBenchmarks` class provides a suite of performance benchmarks designed to evaluate the efficiency and computational overhead of various string manipulation and validation routines used within the `caddy-vps-toolkit` library. By leveraging industry-standard benchmarking practices, this class establishes baseline performance metrics for common string extension operations.

## API

- `public string ToKebabCase`
  - **Purpose**: Measures the execution time for converting strings to kebab-case format.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `string`.
  - **Exceptions**: Throws `ArgumentNullException` if the input string is null.

- `public string ToCamelCase`
  - **Purpose**: Measures the execution time for converting strings to camelCase format.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `string`.
  - **Exceptions**: Throws `ArgumentNullException` if the input string is null.

- `public string Truncate`
  - **Purpose**: Measures the execution time for truncating a string to a specified maximum length.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `string`.
  - **Exceptions**: Throws `ArgumentOutOfRangeException` if the specified length is invalid.

- `public bool IsNumeric_Digits`
  - **Purpose**: Measures the execution time for validating if a string consists entirely of numerical digits.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `bool`.
  - **Exceptions**: None.

- `public bool IsNumeric_NonDigits`
  - **Purpose**: Measures the execution time for validating if a string contains non-numeric characters.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `bool`.
  - **Exceptions**: None.

- `public bool StartsWithAny_Match`
  - **Purpose**: Measures the execution time for checking if a string begins with any element from a defined set, focusing on successful match scenarios.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `bool`.
  - **Exceptions**: None.

- `public bool StartsWithAny_NoMatch`
  - **Purpose**: Measures the execution time for checking if a string begins with any element from a defined set, focusing on scenarios where no match occurs.
  - **Parameters**: Determined by the underlying extension method.
  - **Return Value**: `bool`.
  - **Exceptions**: None.

## Usage

To execute the benchmarks and generate performance reports, ensure the project is configured with BenchmarkDotNet and run the application in Release mode:

```csharp
using BenchmarkDotNet.Running;

// Run all benchmarks within the class
var summary = BenchmarkRunner.Run<StringExtensionsBenchmarks>();
```

To run specific benchmarks by filtering the class members:

```csharp
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;

// Run only the numeric validation benchmarks
var summary = BenchmarkRunner.Run<StringExtensionsBenchmarks>(
    ManualConfig.CreateEmpty().AddFilter(new NameFilter(name => name.Contains("IsNumeric"))));
```

## Notes

### Performance Considerations
All benchmark methods in this class are intended exclusively for performance analysis and should not be invoked as part of standard application logic. The results are highly dependent on the input data set size and character distribution.

### Thread Safety
The methods within `StringExtensionsBenchmarks` are not designed for concurrent execution. When running these benchmarks, ensure the execution environment is isolated to prevent resource contention or thread interference from affecting the accuracy of the measurements.

### Edge Cases
Each benchmark method includes scenarios for common edge cases, such as `null` or empty string inputs. These are essential for capturing the overhead associated with defensive programming checks implemented within the underlying string extension methods.
