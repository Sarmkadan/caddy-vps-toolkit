# LogAggregationServiceTestsExtensions

The `LogAggregationServiceTestsExtensions` static class provides a suite of helper and assertion methods designed to streamline the testing of the `LogAggregationService`. These extensions facilitate the creation of temporary log files for test scenarios and provide fluent assertion capabilities to verify the contents and ordering of log records.

## API

### CreateTempLogFile
Creates a new temporary log file on the file system for use in unit tests.
- **Returns**: A `string` containing the full path to the created temporary file.
- **Throws**: May throw `IOException` if the file cannot be created or accessed due to file system restrictions.

### CreateTempLogFileWithMultipleEntries
Creates a new temporary log file and populates it with multiple predefined log entries to simulate a non-empty log state.
- **Returns**: A `string` containing the full path to the created and populated temporary file.
- **Throws**: May throw `IOException` if file creation or writing fails.

### ShouldBeInDescendingChronologicalOrder
Asserts that the log entries within a given log source are ordered correctly from newest to oldest.
- **Parameters**: The log source or collection to be validated.
- **Returns**: `void`.
- **Throws**: An assertion exception if the logs are not in descending chronological order.

### ShouldContainExactly
Asserts that a given log source contains exactly the expected set of log entries, matching both content and count.
- **Parameters**: The actual logs to check, and the expected set of logs.
- **Returns**: `void`.
- **Throws**: An assertion exception if the actual logs do not match the expected logs exactly.

## Usage

### Verifying Order of Logs
```csharp
[Fact]
public void LogService_ShouldReturnLogsInDescendingOrder()
{
    var tempLogPath = LogAggregationServiceTestsExtensions.CreateTempLogFileWithMultipleEntries();
    var logs = _service.ReadLogs(tempLogPath);

    LogAggregationServiceTestsExtensions.ShouldBeInDescendingChronologicalOrder(logs);
}
```

### Asserting Exact Log Content
```csharp
[Fact]
public void LogService_ShouldWriteSpecificEntries()
{
    var tempLogPath = LogAggregationServiceTestsExtensions.CreateTempLogFile();
    _service.WriteEntry(tempLogPath, "Event 1");
    _service.WriteEntry(tempLogPath, "Event 2");

    var logs = _service.ReadLogs(tempLogPath);
    var expected = new[] { "Event 2", "Event 1" };

    LogAggregationServiceTestsExtensions.ShouldContainExactly(logs, expected);
}
```

## Notes

- **Edge Cases**: These extensions assume standard log record structures. If the log file is malformed or unreadable, the `CreateTempLogFile` methods may succeed, but subsequent read or assertion operations are likely to fail.
- **Thread Safety**: These methods are not thread-safe. They rely on temporary file system operations that are susceptible to race conditions if multiple tests attempt to modify the same temporary files concurrently. It is recommended to use unique temporary file paths per test execution to avoid contention.
