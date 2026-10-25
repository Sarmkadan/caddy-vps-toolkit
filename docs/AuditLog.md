# AuditLog

Audit logging for important operations. The file defines the `AuditLogEntry` class, the `IAuditLog` contract, and `FileAuditLog`, the file-backed implementation. Each entry is appended to a JSON Lines file as it is logged, and also kept in memory so it can be queried.

Namespace: `CaddyVpsToolkit.Auditing`

## Types

### `AuditLogEntry`
A single audit record. `sealed` class with settable properties (all default to empty or generated values).

| Property | Type | Default | Description |
|---|---|---|---|
| `Id` | `string` | `Guid.NewGuid().ToString()` | Unique identifier of the entry. |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` | When the entry was created. Stored as UTC. |
| `Action` | `string` | `string.Empty` | The operation performed (e.g., `"Create"`, `"Delete"`). |
| `Actor` | `string` | `string.Empty` | The user, service, or system that performed the operation. |
| `Target` | `string` | `string.Empty` | The resource affected (e.g., a domain, file path, or service name). |
| `Result` | `string` | `string.Empty` | The outcome (e.g., `"Success"`, `"Failure"`). |
| `Details` | `Dictionary<string, object>` | `new()` | Additional context. Serialized to JSON with `System.Text.Json`. |

### `IAuditLog`
Public interface for recording and querying audit entries.

#### `LogAsync(string action, string actor, string target, string result, Dictionary<string, object> details = null)`
Creates an `AuditLogEntry` from the arguments and records it.

**Parameters:**
- `action` (string) – The operation performed.
- `actor` (string) – The entity that performed the operation.
- `target` (string) – The resource affected.
- `result` (string) – The outcome of the operation.
- `details` (Dictionary<string, object>, optional) – Additional context. `null` is stored as an empty dictionary.

**Returns:**
`Task` – Completes after the entry has been recorded.

#### `GetEntriesAsync(DateTime? from = null, DateTime? to = null)`
Returns the entries whose `Timestamp` falls within the given range.

**Parameters:**
- `from` (DateTime?, optional) – Lower bound, inclusive. `null` means no lower bound.
- `to` (DateTime?, optional) – Upper bound, inclusive. `null` means no upper bound.

**Returns:**
`Task<List<AuditLogEntry>>` – Matching entries in the order they were logged (oldest first).

#### `QueryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string actor = null, string action = null)`
Returns all entries matching the filters.

**Parameters:**
- `fromUtc` (DateTime?, optional) – Start of the time range, inclusive.
- `toUtc` (DateTime?, optional) – End of the time range, inclusive.
- `actor` (string, optional) – Exact, case-sensitive actor name. Ignored if `null` or empty.
- `action` (string, optional) – Exact, case-sensitive action name. Ignored if `null` or empty.

**Returns:**
`Task<List<AuditLogEntry>>` – Matching entries in the order they were logged (oldest first).

#### `QueryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string actor = null, string action = null, int page = 1, int pageSize = 10)`
Paginated version of the query above.

**Parameters:**
- `fromUtc`, `toUtc`, `actor`, `action` – Same as the non-paginated overload.
- `page` (int, optional) – Page number, 1-based. Values below 1 are treated as 1.
- `pageSize` (int, optional) – Items per page. Values below 1 are treated as 10.

**Returns:**
`Task<PaginatedResult<AuditLogEntry>>` – The requested page of items, together with `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasNextPage`, and `HasPreviousPage`. See `PaginatedResult<T>` in `Results/Result.cs`.

### `FileAuditLog`
`sealed` implementation of `IAuditLog`. Thread-safe: all reads and writes are guarded by a single internal lock.

#### `FileAuditLog(string logPath)`
Creates a log that writes to `logPath`.

**Parameters:**
- `logPath` (string) – Path to the log file. The parent directory is created if it does not exist.

**Throws:**
`ArgumentNullException` – If `logPath` is `null`.
`IOException` – If the parent directory cannot be created.
`UnauthorizedAccessException` – If the parent directory cannot be created due to permissions.

The constructor does not read an existing file. Only entries logged through this instance are held in memory and returned by queries.

#### `LogAsync`, `GetEntriesAsync`, `QueryAsync`
Implement the `IAuditLog` members described above.

Notes on behavior:
- `LogAsync` adds the entry to memory first, then appends it to the file as one JSON line. Write errors are caught and reported with `Console.WriteLine`; they are not thrown. An entry whose write failed stays in memory but is missing from the file.
- The method returns after the synchronous work completes. It does not perform asynchronous I/O.

#### `GetActionSummary()`
Counts the logged entries for each action.

**Returns:**
`Dictionary<string, int>` – Keys are action names; values are the number of entries with that action. Counts only the entries held in memory.

#### `GetEntriesByActor(string actor)`
Returns the entries logged by a specific actor.

**Parameters:**
- `actor` (string) – Exact, case-sensitive actor name.

**Returns:**
`List<AuditLogEntry>` – Entries whose `Actor` equals `actor`, in the order they were logged (oldest first). Returns an empty list if none match.

`GetActionSummary` and `GetEntriesByActor` are defined on `FileAuditLog` only. They are not part of `IAuditLog`.

## Usage

### Writing an audit entry

```csharp
IAuditLog auditLog = new FileAuditLog("/var/lib/caddy-vps-toolkit/audit.log");

await auditLog.LogAsync(
    action: "Create",
    actor: "admin",
    target: "api.example.com",
    result: "Success",
    details: new Dictionary<string, object> { ["port"] = 8080 });
```

### Querying a time range

```csharp
var since = DateTime.UtcNow.AddDays(-1);

List<AuditLogEntry> recent = await auditLog.QueryAsync(fromUtc: since, actor: "admin");
```

### Paging through results

```csharp
PaginatedResult<AuditLogEntry> page = await auditLog.QueryAsync(page: 2, pageSize: 25);

foreach (var entry in page.Items)
{
    Console.WriteLine($"{entry.Timestamp:u} {entry.Action} {entry.Target} {entry.Result}");
}
```

Pass UTC values for `fromUtc` and `toUtc`. Entry timestamps are UTC, so local times will give the wrong range.
