# HealthMonitoringServiceTests

Unit test class for verifying the behavior of the `HealthMonitoringService` implementation. It exercises the service's health monitoring functionality including health checks, status retrieval, history queries, and data cleanup operations. The tests validate both success and failure paths, ensuring proper exception handling and argument validation.

## API

### `HealthMonitoringServiceTests`

Public test class containing unit tests for `HealthMonitoringService`. This class is not intended for direct instantiation or use outside of test contexts.

### `CheckServiceHealthAsync_WhenServiceHasNoHealthCheck_ShouldThrowHealthCheckException`

Verifies that calling `CheckServiceHealthAsync` on a service without a configured health check throws a `HealthCheckException`.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: `HealthCheckException` when the service lacks a health check endpoint or mechanism.

### `GetLatestHealthStatusAsync_ShouldReturnFromRepository`

Ensures that `GetLatestHealthStatusAsync` retrieves the most recent health status from the repository.

- **Parameters**: None
- **Return value**: `Task` completing when the operation succeeds
- **Throws**: May throw exceptions from underlying repository or service dependencies.

### `GetHealthHistoryAsync_WithValidHours_ShouldReturnList`

Tests that `GetHealthHistoryAsync` returns a non-empty list of health records when queried with a valid time window.

- **Parameters**: None (uses default valid hours)
- **Return value**: `Task<IEnumerable<HealthRecord>>` containing health history entries
- **Throws**: May throw `ArgumentException` if the hours parameter is invalid (not covered by this test)

### `GetHealthHistoryAsync_WithInvalidHours_ShouldThrowArgumentException`

Validates that `GetHealthHistoryAsync` throws an `ArgumentException` when provided with an invalid hours value (e.g., negative or zero).

- **Parameters**: None (uses invalid hours internally)
- **Return value**: `Task`
- **Throws**: `ArgumentException` with descriptive message

### `CleanupOldRecordsAsync_WithValidDays_ShouldReturnTrue`

Confirms that `CleanupOldRecordsAsync` successfully removes old health records and returns `true` when given a valid retention period in days.

- **Parameters**: None (uses valid days internally)
- **Return value**: `Task<bool>` returning `true` on successful cleanup
- **Throws**: May throw `ArgumentException` if days parameter is invalid (not covered by this test)

### `CleanupOldRecordsAsync_WithInvalidDays_ShouldThrowArgumentException`

Ensures that `CleanupOldRecordsAsync` throws an `ArgumentException` when provided with an invalid days value (e.g., negative).

- **Parameters**: None (uses invalid days internally)
- **Return value**: `Task`
- **Throws**: `ArgumentException` with descriptive message

## Usage
