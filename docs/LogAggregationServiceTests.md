# LogAggregationServiceTests

Test suite for the `LogAggregationService` component in the **caddy-vps-toolkit** project. It verifies the behavior of log retrieval, parsing, filtering, and ordering under various conditions.

## API

| Member | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `GetLogsAsync_WithEmptyDirectory_ReturnsEmptyList` | Confirms that when the log directory contains no files, the service returns an empty collection. | none | `Task` (completes when the assertion finishes) | Throws if the returned collection is not empty or if an unexpected exception occurs during execution. |
| `GetLogsAsync_ParsesStandardLogFormat` | Validates that a log line conforming to the standard format is correctly parsed into a log entry model. | none | `Task` | Throws if parsing fails, the resulting model does not match expected values, or an exception is raised. |
| `GetLogsAsync_FiltersbyMinLevel` | Ensures that logs whose level is below the supplied minimum level are excluded from the result set. | none | `Task` | Throws if any log entry below the minimum level appears in the output or if the filtering logic fails. |
| `GetLogsAsync_RespectsLinesLimit` | Checks that the service honors the `linesLimit` argument, returning no more than the requested number of entries. | none | `Task` | Throws when more than the specified limit are returned or when the limit is incorrectly applied. |
| `GetLogsAsync_ReturnsMostRecentFirst` | Verifies that logs are sorted in descending timestamp order, with the most recent entry first. | none | `Task` | Throws if the ordering is incorrect or if sorting logic throws. |
| `GetLogsAsync_FiltersBySince` | Asserts that logs older than the provided `since` DateTime are omitted from the results. | none | `Task` | Throws if any entry predating the `since` boundary is present or if the filter misbehaves. |
| `GetLogSources_ReturnsLogFiles` | Tests that the helper method enumerates all log files (matching the expected pattern) within the target directory. | none | `void` (synchronous test) | Throws if the returned collection does not match the actual files on disk or if an exception occurs during enumeration. |

## Usage

```csharp
// Example 1: Running the empty‑directory test
var testSuite = new LogAggregationServiceTests();
await testSuite.GetLogsAsync_WithEmptyDirectory_ReturnsEmptyList();
// If the test passes, execution continues; otherwise an exception is propagated.

// Example 2: Executing a parameterised filter test in a loop
foreach (var minLevel in new[] { LogLevel.Info, LogLevel.Warn, LogLevel.Error })
{
    await testSuite.GetLogsAsync_FiltersbyMinLevel();
    // Each iteration validates that logs below the current minLevel are excluded.
}
```

## Notes

- The test class relies on a temporary directory fixture that is created and torn down per test method; therefore, tests are not safe to run concurrently without isolating their file‑system state.
- If the underlying `LogAggregationService` throws (e.g., due to an inaccessible directory), the corresponding test will surface that exception as a failure.
- No members of this class accept parameters; all test data is supplied via internal test helpers or pre‑configured test assets.
- The class itself holds no mutable state that is shared across test methods, but it does depend on the test framework’s lifecycle (setup/teardown) to ensure a clean environment for each test. Consequently, invoking test methods manually outside of a test runner may produce unpredictable results.
