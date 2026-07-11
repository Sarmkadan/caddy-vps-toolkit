# NotificationExtensions

Provides extension methods for the `Notification` type to simplify common metadata operations such as adding, removing, and retrieving metadata values, as well as generating human-readable summaries of notifications.

## API

### `AddMetadata(Notification notification, string key, string value)`

Adds or updates a metadata entry on the specified `Notification`.

- **Parameters**
  - `notification`: The `Notification` instance to modify.
  - `key`: The metadata key to add or update.
  - `value`: The metadata value to associate with the key.
- **Return value**: The modified `Notification` instance (for method chaining).
- **Exceptions**: Throws `ArgumentNullException` if `notification` or `key` is `null`.

### `RemoveMetadata(Notification notification, string key)`

Removes a metadata entry from the specified `Notification`.

- **Parameters**
  - `notification`: The `Notification` instance to modify.
  - `key`: The metadata key to remove.
- **Return value**: The modified `Notification` instance (for method chaining).
- **Exceptions**: Throws `ArgumentNullException` if `notification` or `key` is `null`.

### `GetMetadataValue(Notification notification, string key)`

Retrieves the metadata value associated with the specified key from the `Notification`.

- **Parameters**
  - `notification`: The `Notification` instance to query.
  - `key`: The metadata key whose value should be retrieved.
- **Return value**: The metadata value as a string, or `null` if the key does not exist.
- **Exceptions**: Throws `ArgumentNullException` if `notification` or `key` is `null`.

### `ToSummaryString(Notification notification)`

Generates a concise, human-readable summary of the `Notification` including its metadata.

- **Parameters**
  - `notification`: The `Notification` instance to summarize.
- **Return value**: A string containing a brief description of the notification and its metadata, or `null` if `notification` is `null`.

## Usage

```csharp
// Example 1: Adding and retrieving metadata
var notification = new Notification("Validation failed");
notification = notification.AddMetadata("EntityType", "User")
                       .AddMetadata("Field", "Email");

string entityType = NotificationExtensions.GetMetadataValue(notification, "EntityType");
Console.WriteLine($"Entity type: {entityType}"); // Output: Entity type: User

// Example 2: Removing metadata and generating a summary
notification = notification.RemoveMetadata("Field");
string summary = NotificationExtensions.ToSummaryString(notification);
Console.WriteLine(summary); // Output: Validation failed (EntityType: User)
```

## Notes

- Metadata operations (`AddMetadata`, `RemoveMetadata`, `GetMetadataValue`) are not thread-safe. Callers must ensure external synchronization when modifying or reading metadata concurrently.
- `GetMetadataValue` returns `null` for missing keys rather than throwing, allowing callers to handle absence gracefully without exception handling.
- `ToSummaryString` includes all metadata entries in the output; for large metadata sets, consider filtering or truncating the summary to avoid excessive string allocation.
