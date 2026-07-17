# DateTimeExtensionsTestsExtensions

Extension methods for `DateTimeExtensions` that provide additional testing utilities for date and time calculations. These methods extend the standard `DateTime` type to enable common date manipulation and comparison scenarios used in unit testing scenarios.

## API

### `ToUnixTimestamp`

Converts a `DateTime` to a Unix timestamp (seconds since Unix epoch, 1970-01-01T00:00:00Z).

- **Parameters:**
  - `dateTime` - The date to convert.
- **Return Value:**
  - `long` - The Unix timestamp in seconds.
- **Exceptions:**
  - Throws `ArgumentException` if the date is before the Unix epoch (1970-01-01).


### `FirstDayOfMonth`

Returns a new `DateTime` representing the first day of the month for the given date.

- **Parameters:**
  - `dateTime` - The date to get the first day from.
- **Return Value:**
  - `DateTime` - A new DateTime with day set to 1, preserving hour, minute, second, millisecond, and DateTimeKind.
- **Exceptions:**
  - None.

### `LastDayOfMonth`

Returns a new `DateTime` representing the last day of the month for the given date.

- **Parameters:**
  - `dateTime` - The date to get the last day from.
- **Return Value:**
  - `DateTime` - A new DateTime representing the last day of the month.
- **Exceptions:**
  - None.

### `IsWeekend`

Determines whether the given date falls on a weekend day (Saturday or Sunday).

- **Parameters:**
  - `dateTime` - The date to check.
- **Return Value:**
  - `bool` - `true` if the date is a weekend day; otherwise, `false`.
- **Exceptions:**
  - None.

### `GetDatesBetween`

Returns an enumerable sequence of all dates between two dates (inclusive).

- **Parameters:**
  - `startDate` - The start date (inclusive).
  - `endDate` - The end date (inclusive).
- **Return Value:**
  - `IEnumerable<DateTime>` - An enumerable of dates from startDate to endDate, one per day.
- **Exceptions:**
  - Throws `ArgumentException` if `startDate` is after `endDate`.

### `BusinessDaysBetween`

Calculates the number of business days (Monday through Friday) between two dates (inclusive).

- **Parameters:**
  - `startDate` - The start date.
  - `endDate` - The end date.
- **Return Value:**
  - `int` - The count of business days between the dates.
- **Exceptions:**
  - Throws `ArgumentException` if `startDate` is after `endDate`.


## Usage

### Example 1: Calculating Unix timestamp

```csharp
using System;
using CaddyVpsToolkit.Tests.Utilities;

var now = DateTime.UtcNow;
long timestamp = now.ToUnixTimestamp();
Console.WriteLine($"Unix timestamp: {timestamp}");
```

### Example 2: Counting business days between dates

```csharp
using System;
using CaddyVpsToolkit.Tests.Utilities;

var start = new DateTime(2024, 7, 1); // Monday
var end = new DateTime(2024, 7, 15);  // Monday two weeks later
int businessDays = start.BusinessDaysBetween(end);
Console.WriteLine($"Business days between {start:yyyy-MM-dd} and {end:yyyy-MM-dd}: {businessDays}"); // Output: 10
```

## Notes

- **Thread Safety:** All methods are thread-safe as they do not mutate shared state and only operate on input parameters.
- **DateTimeKind Preservation:** `FirstDayOfMonth` and `LastDayOfMonth` preserve the `DateTime.Kind` of the input date.
- **Edge Cases:**
  - `ToUnixTimestamp` will throw for dates before 1970-01-01.
  - `GetDatesBetween` and `BusinessDaysBetween` will throw if `startDate` > `endDate`.
  - `BusinessDaysBetween` counts both start and end dates if they are weekdays.
  - The sequence returned by `GetDatesBetween` is lazily evaluated using `yield return`.
- **Time Components:** When working with `FirstDayOfMonth` and `LastDayOfMonth`, the time portion (hour, minute, second, millisecond) is preserved from the original date.