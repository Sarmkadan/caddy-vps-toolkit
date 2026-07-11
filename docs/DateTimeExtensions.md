# DateTimeExtensions

The `DateTimeExtensions` class provides a collection of static extension methods and utility functions designed to simplify common date and time operations within the `caddy-vps-toolkit` project. It focuses on enhancing readability for time-based logic, standardizing string representations, and performing calendar-based calculations such as determining range boundaries, relative time status, and working day intervals.

## API

### ToRelativeTime
Generates a human-readable string representing the time elapsed between the source `DateTime` and the current system time (e.g., "2 hours ago", "in 5 minutes").
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `string` containing the relative time description.
*   **Throws**: None.

### ToIso8601
Converts the source `DateTime` into a standardized ISO 8601 string format.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `string` formatted according to ISO 8601 standards.
*   **Throws**: None.

### ToReadableString
Converts the source `DateTime` into a user-friendly string representation suitable for display in logs or UIs, typically including date and time components.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `string` containing the formatted date and time.
*   **Throws**: None.

### StartOfDay
Returns a new `DateTime` representing the very beginning (00:00:00) of the day for the source date.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `DateTime` set to midnight of the same day.
*   **Throws**: None.

### EndOfDay
Returns a new `DateTime` representing the very end (23:59:59.999...) of the day for the source date.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `DateTime` set to the last tick of the same day.
*   **Throws**: None.

### StartOfWeek
Returns a new `DateTime` representing the beginning of the week containing the source date. The start of the week is determined by the current culture's calendar settings (typically Monday or Sunday).
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `DateTime` set to midnight of the first day of the week.
*   **Throws**: None.

### StartOfMonth
Returns a new `DateTime` representing the first day of the month for the source date.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `DateTime` set to midnight on the 1st of the month.
*   **Throws**: None.

### IsPast
Determines whether the source `DateTime` occurred before the current system time.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `true` if the date is in the past; otherwise, `false`.
*   **Throws**: None.

### IsFuture
Determines whether the source `DateTime` will occur after the current system time.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `true` if the date is in the future; otherwise, `false`.
*   **Throws**: None.

### IsToday
Determines whether the source `DateTime` falls within the current system day.
*   **Parameters**: `this DateTime dateTime`
*   **Returns**: `true` if the date matches today's date; otherwise, `false`.
*   **Throws**: None.

### WorkingDaysBetween
Calculates the number of working days (typically excluding weekends) between the source `DateTime` and a specified end date.
*   **Parameters**: `this DateTime startDate`, `DateTime endDate`
*   **Returns**: `int` representing the count of working days. Returns 0 if the start and end dates are identical or if the range is invalid depending on implementation logic.
*   **Throws**: None.

### ToDurationString
Converts a `TimeSpan` (implied via extension on DateTime difference or direct TimeSpan usage depending on overload) or calculates the duration between two points into a formatted string (e.g., "1d 4h 30m"). *Note: Based on signature context, this likely extends `TimeSpan` or accepts a second date parameter to calculate duration.*
*   **Parameters**: Context dependent (likely `this TimeSpan timeSpan` or `this DateTime startDate, DateTime endDate`).
*   **Returns**: `string` representing the formatted duration.
*   **Throws**: None.

## Usage

### Example 1: Logging and Status Checks
This example demonstrates how to check if a scheduled task is due and format the timestamp for a log entry.

```csharp
using System;
using CaddyVpsToolkit.Extensions; // Namespace assumption

public class TaskScheduler
{
    public void EvaluateTask(DateTime scheduledTime)
    {
        if (scheduledTime.IsPast() && !scheduledTime.IsToday())
        {
            Console.WriteLine($"Missed task scheduled for: {scheduledTime.ToReadableString()}");
            Console.WriteLine($"Time elapsed: {scheduledTime.ToRelativeTime()}");
        }
        else if (scheduledTime.IsFuture())
        {
            Console.WriteLine($"Upcoming task at: {scheduledTime.ToIso8601()}");
        }
    }
}
```

### Example 2: Date Range Calculations
This example illustrates calculating business metrics by determining the start of the reporting period and counting working days.

```csharp
using System;
using CaddyVpsToolkit.Extensions;

public class ReportGenerator
{
    public void GenerateWeeklyReport(DateTime reportDate)
    {
        var weekStart = reportDate.StartOfWeek();
        var weekEnd = reportDate.EndOfDay(); // Or specific end of week logic
        
        // Calculate working days from start of week to now
        int daysElapsed = weekStart.WorkingDaysBetween(DateTime.Now);
        
        Console.WriteLine($"Report Period: {weekStart.ToReadableString()} to Present");
        Console.WriteLine($"Working days elapsed: {daysElapsed}");
        Console.WriteLine($"Duration string: {DateTime.Now.Subtract(weekStart).ToDurationString()}");
    }
}
```

## Notes

*   **Thread Safety**: As this class consists entirely of static methods that operate on immutable `DateTime` and `TimeSpan` structs without maintaining internal state, all methods are inherently thread-safe.
*   **Culture Sensitivity**: Methods such as `StartOfWeek` and `ToReadableString` rely on the current thread's culture settings (`CultureInfo.CurrentCulture`). Results may vary if the application runs under different cultural contexts (e.g., week start day differences between US and EU cultures).
*   **Time Zone Awareness**: These methods operate on the `DateTime` value provided. If the input `DateTime` has `Kind` set to `Unspecified`, calculations like `IsPast` or `ToRelativeTime` will compare against the local system time without automatic time zone conversion, which may lead to inaccuracies if the input represents UTC or a different time zone.
*   **Edge Cases**: 
    *   `WorkingDaysBetween` returns 0 if the start and end dates are the same. If the end date precedes the start date, the return value behavior (negative vs. zero) should be verified against specific implementation details.
    *   `EndOfDay` sets the time to the maximum possible tick for that day; care should be taken when comparing this result with other `DateTime` values that might have higher precision or different kinds.
