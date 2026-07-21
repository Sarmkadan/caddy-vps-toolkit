# MaintenanceWindow

Represents a recurring maintenance window defined by start time, end time, and days of the week, used to control when automated maintenance operations may execute on a VPS.

## API

### StartTime
**Type:** `string`  
Gets or sets the start time of the maintenance window in 24-hour format (HH:mm).  
**Throws:** `FormatException` during `Validate()` if the value is not a valid HH:mm time string.

### EndTime
**Type:** `string`  
Gets or sets the end time of the maintenance window in 24-hour format (HH:mm).  
**Throws:** `FormatException` during `Validate()` if the value is not a valid HH:mm time string.

### DaysOfWeek
**Type:** `string`  
Gets or sets a comma-separated list of day names (e.g., "Monday,Wednesday,Friday") or day abbreviations (e.g., "Mon,Wed,Fri") on which the window is active. Case-insensitive.  
**Throws:** `ArgumentException` during `Validate()` if any day token is unrecognized.

### IsEnabled
**Type:** `bool`  
Gets or sets a value indicating whether the maintenance window is active. When `false`, `IsInWindow` always returns `false`.

### Description
**Type:** `string`  
Gets or sets an optional human-readable description of the maintenance window's purpose.

### IsInWindow
**Type:** `bool`  
Gets a value indicating whether the current system time (local) falls within the defined window, considering `DaysOfWeek`, `StartTime`, `EndTime`, and `IsEnabled`. Returns `false` if the window is disabled or validation fails.

### Validate()
**Returns:** `void`  
Validates the configuration: ensures `StartTime` and `EndTime` are valid HH:mm formats, `DaysOfWeek` contains only recognized day names, and `StartTime` is not equal to `EndTime`.  
**Throws:** `FormatException` for invalid time formats. `ArgumentException` for unrecognized days or when `StartTime` equals `EndTime`.

## Usage

```csharp
var window = new MaintenanceWindow
{
    StartTime = "02:00",
    EndTime = "04:00",
    DaysOfWeek = "Mon,Wed,Fri",
    IsEnabled = true,
    Description = "Bi-weekly security patching window"
};

window.Validate();

if (window.IsInWindow)
{
    await RunMaintenanceTasksAsync();
}
```

```csharp
var weekendWindow = new MaintenanceWindow
{
    StartTime = "23:00",
    EndTime = "03:00",
    DaysOfWeek = "Saturday,Sunday",
    IsEnabled = true
};

try
{
    weekendWindow.Validate();
}
catch (ArgumentException ex)
{
    logger.LogError(ex, "Invalid maintenance window configuration");
    return;
}

var nextRun = CalculateNextExecution(weekendWindow);
```

## Notes

- `IsInWindow` evaluates against the local system clock; ensure the host time zone is correctly configured for predictable behavior.
- The window supports overnight ranges (e.g., 23:00–03:00) by treating `EndTime` earlier than `StartTime` as crossing midnight.
- `DaysOfWeek` accepts full names ("Monday") or three-letter abbreviations ("Mon"); mixing formats is permitted.
- `Validate()` does not modify the instance; it only throws on invalid state. Call it after construction or deserialization before relying on `IsInWindow`.
- This type is not thread-safe. If shared across threads, synchronize access to property setters and `Validate()` calls.
