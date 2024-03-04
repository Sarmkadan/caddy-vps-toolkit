# ArgumentParserBenchmarks

`ArgumentParserBenchmarks` is a performance benchmarking class designed to measure the efficiency and latency of core argument parsing routines within the `caddy-vps-toolkit`. It leverages the BenchmarkDotNet framework to provide reproducible performance metrics for various parsing scenarios, including basic command retrieval, flag value extraction across multiple syntaxes, existence checks, and bulk flag processing. This class facilitates performance regression testing and helps optimize the argument processing logic used throughout the toolkit.

## API

### GetCommand_Small
Retrieves a predefined, small command string utilized as input for benchmarking fundamental parsing operations.
*   **Parameters**: None.
*   **Returns**: `string` representing a representative small command.
*   **Throws**: None.

### GetFlagValue_EqualsSyntax
Benchmarks the extraction of a flag value where the flag and its corresponding value are separated by an equals sign (e.g., `--option=value`).
*   **Parameters**: None.
*   **Returns**: `string?` containing the extracted value if the flag is successfully parsed, or `null` if not found.
*   **Throws**: None.

### GetFlagValue_SpaceSyntax
Benchmarks the extraction of a flag value where the flag and its corresponding value are separated by a space (e.g., `--option value`).
*   **Parameters**: None.
*   **Returns**: `string?` containing the extracted value if the flag is successfully parsed, or `null` if not found.
*   **Throws**: None.

### HasFlag_Present
Benchmarks the boolean check routine for a flag that is confirmed to be present in the benchmarked input data.
*   **Parameters**: None.
*   **Returns**: `bool` indicating true if the flag exists.
*   **Throws**: None.

### HasFlag_Absent
Benchmarks the boolean check routine for a flag that is confirmed to be absent from the benchmarked input data.
*   **Parameters**: None.
*   **Returns**: `bool` indicating false if the flag does not exist.
*   **Throws**: None.

### GetAllFlags_Large
Benchmarks the performance of parsing and retrieving a comprehensive list of all flags from a large, complex input string.
*   **Parameters**: None.
*   **Returns**: `List<string>` containing all identified flags.
*   **Throws**: None.

## Usage

**Example 1: Running benchmarks via BenchmarkRunner**

```csharp
using BenchmarkDotNet.Running;
using CaddyVpsToolkit.Benchmarks;

// Execute all benchmarks within the class
var summary = BenchmarkRunner.Run<ArgumentParserBenchmarks>();
```

**Example 2: Manual invocation for performance profiling or verification**

```csharp
using CaddyVpsToolkit.Benchmarks;

var benchmarks = new ArgumentParserBenchmarks();

// Directly invoke a specific benchmark method
bool isPresent = benchmarks.HasFlag_Present();
List<string> allFlags = benchmarks.GetAllFlags_Large();

Console.WriteLine($"Flag present: {isPresent}");
Console.WriteLine($"Total flags found: {allFlags.Count}");
```

## Notes

*   **Edge Cases**: These benchmarks assume well-formed or typical inputs based on their implementation. They do not exhaustively cover error-handling paths for malformed command-line arguments, as their primary intent is measuring hot-path performance for standard scenarios.
*   **Thread Safety**: While these methods are designed to be safe for concurrent read access, they are intended for execution by the BenchmarkDotNet harness. They are not optimized for or intended for use in high-concurrency production environments; if invoked outside of a benchmark context, callers should treat them as synchronous, potentially blocking operations depending on the complexity of the underlying parsing logic being benchmarked.
