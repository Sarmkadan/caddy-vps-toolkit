# HealthCheckRepository

The `HealthCheckRepository` class provides data access operations for health check results stored in the application's database. It encapsulates queries for retrieving, adding, and deleting health check records, as well as computing aggregate statistics. The repository is designed to be used with dependency injection and relies on an underlying `DbContext` (typically Entity Framework Core) for persistence.

## API

### `public HealthCheckRepository(DbContext context)`

Constructs a new repository instance with the specified database context.

- **Parameters**  
  `context` – The `DbContext` instance used for all database operations. Must not be null.

- **Throws**  
  `ArgumentNullException` if `context` is null.

---

### `public async Task<HealthCheckResult> GetLatestAsync()`

Retrieves the most recent health check result across all services.

- **Returns**  
  A `HealthCheckResult` representing the latest record, or `null` if no health checks exist.

- **Throws**  
  `InvalidOperationException` if the underlying data store is unavailable or the query fails.

---

### `public async Task<List<HealthCheckResult>> GetRecentAsync(int count)`

Retrieves the most recent health check results, up to the specified count.

- **Parameters**  
  `count` – The maximum number of results to return. Must be greater than zero.

- **Returns**  
  A list of `HealthCheckResult` objects, ordered from newest to oldest. The list may be empty if no records exist.

- **Throws**  
  `ArgumentOutOfRangeException` if `count` is less than or equal to zero.

---

### `public async Task<List<HealthCheckResult>> GetByServiceIdAsync(string serviceId)`

Retrieves all health check results for a specific service, ordered by timestamp descending.

- **Parameters**  
  `serviceId` – The unique identifier of the service. Must not be null or empty.

- **Returns**  
  A list of `HealthCheckResult` objects for the given service. Returns an empty list if no records are found.

- **Throws**  
  `ArgumentException` if `serviceId` is null or empty.

---

### `public async Task<string> AddAsync(HealthCheckResult result)`

Persists a new health check result and returns its generated identifier.

- **Parameters**  
  `result` – The `HealthCheckResult` to add. Must not be null and must have a valid `ServiceId`.

- **Returns**  
  A string containing the unique identifier (e.g., GUID) assigned to the new record.

- **Throws**  
  `ArgumentNullException` if `result` is null.  
  `InvalidOperationException` if the service identifier is missing or the database operation fails.

---

### `public async Task<bool> DeleteOlderThanAsync(DateTime threshold)`

Deletes all health check results older than the specified date and time.

- **Parameters**  
  `threshold` – The cutoff date. Records with a timestamp strictly older than this value are removed.

- **Returns**  
  `true` if at least one record was deleted; otherwise `false`.

- **Throws**  
  `InvalidOperationException` if the database operation fails.

---

### `public async Task<HealthCheckStatistics> GetStatisticsAsync()`

Computes aggregate statistics across all health check results.

- **Returns**  
  A `HealthCheckStatistics` object containing metrics such as total checks, success/failure counts, and average response time. Returns a zeroed statistics object if no records exist.

- **Throws**  
  `InvalidOperationException` if the underlying query fails.

## Usage

### Example 1: Adding a health check and retrieving the latest result

```csharp
public async Task RecordAndVerifyHealthCheck(HealthCheckRepository repo)
{
    var result = new HealthCheckResult
    {
        ServiceId = "web-api",
        Status = HealthStatus.Healthy,
        ResponseTimeMs = 120,
        Timestamp = DateTime.UtcNow
    };

    string id = await repo.AddAsync(result);
    Console.WriteLine($"Inserted health check with ID: {id}");

    var latest = await repo.GetLatestAsync();
    Console.WriteLine($"Latest check: {latest?.Status} at {latest?.Timestamp}");
}
```

### Example 2: Cleaning up old records and showing statistics

```csharp
public async Task CleanupAndShowStats(HealthCheckRepository repo)
{
    // Delete records older than 30 days
    bool deleted = await repo.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-30));
    Console.WriteLine(deleted ? "Old records removed." : "No old records found.");

    // Display aggregate statistics
    var stats = await repo.GetStatisticsAsync();
    Console.WriteLine($"Total checks: {stats.TotalCount}");
    Console.WriteLine($"Success rate: {stats.SuccessRate:P}");
}
```

## Notes

- **Thread safety**: This repository is not inherently thread-safe. Each instance is intended to be used within a single scope (e.g., a web request). Concurrent calls on the same `DbContext` instance may lead to unexpected behavior or exceptions. Use separate scoped instances or synchronize access if needed.
- **Null and empty parameters**: Methods that accept `string` parameters (`serviceId`) throw `ArgumentException` if the value is null or empty. Always validate input before calling.
- **Empty results**: `GetLatestAsync` returns `null` when no records exist. `GetRecentAsync` and `GetByServiceIdAsync` return an empty list. Callers should handle these cases gracefully.
- **Database failures**: All asynchronous methods may throw `InvalidOperationException` or derived exceptions (e.g., `DbUpdateException`) if the database is unreachable or a constraint is violated. Production code should include appropriate error handling and retry logic.
- **Performance**: `GetRecentAsync` with a large `count` may cause memory pressure. Consider pagination or limiting the count to a reasonable value (e.g., 100). `DeleteOlderThanAsync` performs a bulk delete; ensure the threshold is not too recent to avoid accidental data loss.
