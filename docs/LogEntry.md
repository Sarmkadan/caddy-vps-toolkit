# LogEntry

The `LogEntry` type serves as a data transfer object within the `caddy-vps-toolkit` project, encapsulating structured information regarding system events, errors, and operational metrics. It aggregates temporal data, severity classification, contextual messaging, and source identification to facilitate consistent logging, filtering, and analysis across Caddy server management tasks. This class is primarily used to serialize log data for storage or transmission and to define query parameters for retrieving specific log subsets based on time ranges, service identifiers, or severity thresholds.

## API

The following public members define the structure and behavior of the `LogEntry` type:

### `Timestamp`
*   **Type:** `DateTime`
*   **Purpose:** Represents the exact date and time when the log event occurred.
*   **Remarks:** This property is mandatory and must contain a valid `DateTime` value. It is typically used for sorting chronologically or filtering by time ranges.

### `Level`
*   **Type:** `string`
*   **Purpose:** Indicates the severity or classification of the log entry (e.g., "INFO", "WARN", "ERROR").
*   **Remarks:** This property is mandatory. The specific string values depend on the logging configuration of the underlying Caddy instance.

### `Message`
*   **Type:** `string`
*   **Purpose:** Contains the human-readable description or payload of the log event.
*   **Remarks:** This property is mandatory and provides the core context of the logged occurrence.

### `Source`
*   **Type:** `string`
*   **Purpose:** Identifies the component, module, or file within the system that generated the log entry.
*   **Remarks:** This property is mandatory, allowing users to trace the origin of specific events within the toolkit or the Caddy server.

### `ServiceId`
*   **Type:** `string?`
*   **Purpose:** Optionally associates the log entry with a specific service identifier managed by the toolkit.
*   **Remarks:** This property is nullable. It is populated when the log event is scoped to a particular VPS service or container; otherwise, it remains `null`.

### `Lines`
*   **Type:** `int`
*   **Purpose:** Represents a numeric count associated with the entry, typically indicating the number of log lines aggregated in this record or a specific line number reference.
*   **Remarks:** This property is mandatory. When used as a query parameter, it may define a limit on the number of lines to retrieve.

### `MinLevel`
*   **Type:** `string?`
*   **Purpose:** When used as a filter criterion, specifies the minimum severity level required for inclusion in a result set.
*   **Remarks:** This property is nullable. It is generally utilized in request objects to filter logs, ignoring entries below the specified severity.

### `Since`
*   **Type:** `DateTime?`
*   **Purpose:** Defines the starting point for a time-based query, retrieving only logs created after this timestamp.
*   **Remarks:** This property is nullable. If `null`, no lower time bound is applied to the query.

### `ServiceId` (Duplicate Member)
*   **Type:** `string?`
*   **Purpose:** Functionally identical to the previously listed `ServiceId` member, serving as an optional identifier for scoping logs to a specific service.
*   **Remarks:** In the context of this definition, this member reinforces the capability to filter or tag entries by service identity.

## Usage

The following examples demonstrate how to instantiate and utilize the `LogEntry` type for recording events and constructing query filters.

### Example 1: Creating a Log Entry for Error Reporting
This example illustrates populating a `LogEntry` instance to capture a critical failure within a specific service context.

```csharp
using System;
using CaddyVpsToolkit.Models;

public class ErrorHandler
{
    public void CaptureFailure(string serviceName, Exception ex)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = "ERROR",
            Message = $"Failed to restart service: {ex.Message}",
            Source = "ServiceManager",
            ServiceId = serviceName,
            Lines = 1
        };

        // Proceed to serialize or store 'entry'
        Console.WriteLine($"[{entry.Timestamp}] {entry.Level}: {entry.Message}");
    }
}
```

### Example 2: Constructing a Log Query Filter
This example demonstrates configuring a `LogEntry` object to act as a filter specification for retrieving recent warning-level logs for a specific service.

```csharp
using System;
using CaddyVpsToolkit.Models;

public class LogViewer
{
    public LogEntry BuildQuery(string targetServiceId)
    {
        return new LogEntry
        {
            // Properties used as filters
            ServiceId = targetServiceId,
            MinLevel = "WARN",
            Since = DateTime.UtcNow.AddHours(-24),
            Lines = 100,
            
            // These properties are ignored by the filter logic but required by constructor/initializer
            Timestamp = DateTime.MinValue,
            Level = string.Empty,
            Message = string.Empty,
            Source = string.Empty
        };
    }
}
```

## Notes

*   **Nullable Reference Types:** The `ServiceId`, `MinLevel`, and `Since` properties are nullable. Consumers of this type must handle `null` values appropriately, particularly when these properties are used as optional filter criteria where `null` implies "no restriction."
*   **Data Integrity:** The `Timestamp`, `Level`, `Message`, `Source`, and `Lines` properties are non-nullable. Instantiating this type requires valid values for these members; assigning `null` to any of these will result in a compilation error in projects with nullable reference types enabled or a `NullReferenceException` at runtime if bypassed.
*   **Thread Safety:** As `LogEntry` is a simple data container (POCO) with mutable public properties, it is not inherently thread-safe for modification. While reading properties is generally safe once initialized, concurrent writes to the same instance from multiple threads should be avoided. It is recommended to treat instances as immutable after creation or to ensure proper synchronization if modification is required in a multi-threaded context.
*   **Duplicate Property Definition:** The type definition includes two declarations of `ServiceId`. In a compiled assembly, this typically resolves to a single property. If this represents a data contract for serialization, ensure that the serialization strategy handles the member correctly to avoid ambiguity or data loss during JSON/XML transformation.
