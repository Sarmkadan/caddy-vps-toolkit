# AuditLogEntry

Represents a single entry in an audit log, capturing details about actions performed within the system, including the actor, target, timestamp, and outcome.

## API

### `Id`
A unique identifier for the audit log entry. Read-only.

### `Timestamp`
The date and time when the action occurred. Read-only.

### `Action`
The type of action performed (e.g., "Create", "Delete", "Update"). Read-only.

### `Actor`
The identifier of the entity (user, service, or system) that performed the action. Read-only.

### `Target`
The resource or object affected by the action (e.g., file path, endpoint, or entity ID). Read-only.

### `Result`
The outcome of the action (e.g., "Success", "Failure", "Partial"). Read-only.

### `Details`
A dictionary of additional contextual data related to the action. Read-only. May include details such as IP addresses, request IDs, or error messages.

### `FileAuditLog`
The file path to the audit log where this entry is stored. Read-only.

### `LogAsync()`
Asynchronously writes the current `AuditLogEntry` to the audit log file.

**Returns:**
`Task` – A task representing the asynchronous write operation.

**Throws:**
`IOException` – If the log file cannot be written to (e.g., due to permissions or disk issues).
`UnauthorizedAccessException` – If the caller lacks write permissions to the log file.

### `GetEntriesAsync()`
Asynchronously retrieves all audit log entries from the log file.

**Returns:**
`Task<List<AuditLogEntry>>` – A list of all audit log entries, ordered by timestamp (newest first).

**Throws:**
`FileNotFoundException` – If the audit log file does not exist.
`IOException` – If the log file cannot be read.
`UnauthorizedAccessException` – If the caller lacks read permissions.

### `GetActionSummary()`
Aggregates the count of each action type across all log entries.

**Returns:**
`Dictionary<string, int>` – A dictionary where keys are action types (e.g., "Create", "Delete") and values are the number of occurrences.

**Throws:**
`FileNotFoundException` – If the audit log file does not exist.
`IOException` – If the log file cannot be read.
`UnauthorizedAccessException` – If the caller lacks read permissions.

### `GetEntriesByActor(string actor)`
Retrieves all audit log entries associated with a specific actor.

**Parameters:**
- `actor` (string) – The identifier of the actor to filter by.

**Returns:**
`List<AuditLogEntry>` – A list of entries where the `Actor` matches the provided value, ordered by timestamp (newest first).

**Throws:**
`FileNotFoundException` – If the audit log file does not exist.
`IOException` – If the log file cannot be read.
`ArgumentNullException` – If `actor` is `null`.

## Usage

### Writing an audit log entry
