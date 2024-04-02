# DateTimeExtensionsTests

Unit tests for `DateTimeExtensions` helper methods in the `caddy-vps-toolkit` project. These tests validate the behavior of relative time formatting, ISO 8601 conversion, day boundary calculations, past-date detection, and working-day arithmetic.

## API

### `ToRelativeTime_ShouldReturnJustNow_WhenLessThanMinute`
Ensures that `DateTimeExtensions.ToRelativeTime` returns the string `"just now"` when the time difference between the current UTC time and the provided `DateTime` is less than one minute.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the result equals `"just now"`.
- **Throws**: No exceptions under normal test conditions.

### `ToRelativeTime_ShouldReturnMinutesAgo_WhenLessThanHour`
Validates that `DateTimeExtensions.ToRelativeTime` returns a string in the format `"X minutes ago"` when the time difference is between one minute and one hour.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the result matches the expected minute-based format.
- **Throws**: No exceptions under normal test conditions.

### `ToRelativeTime_ShouldReturnHoursAgo_WhenLessThanDay`
Confirms that `DateTimeExtensions.ToRelativeTime` returns a string in the format `"X hours ago"` when the time difference is between one hour and one day.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the result matches the expected hour-based format.
- **Throws**: No exceptions under normal test conditions.

### `ToIso8601_ShouldReturnCorrectFormat`
Ensures that `DateTimeExtensions.ToIso8601` formats a `DateTime` as an ISO 8601 string in UTC with the pattern `"yyyy-MM-ddTHH:mm:ssZ"`.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the result matches the expected ISO 8601 format.
- **Throws**: No exceptions under normal test conditions.

### `StartOfDay_ShouldReturnCorrectTime`
Validates that `DateTimeExtensions.StartOfDay` truncates the time portion of a `DateTime` to midnight (00:00:00) in the same time zone.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the time component is `00:00:00`.
- **Throws**: No exceptions under normal test conditions.

### `IsPast_ShouldReturnTrue_WhenDateIsInPast`
Confirms that `DateTimeExtensions.IsPast` returns `true` when the provided `DateTime` is earlier than the current UTC time.

- **Parameters**: None (implicitly tests the current system time).
- **Return value**: Assertion that the result is `true`.
- **Throws**: No exceptions under normal test conditions.

### `WorkingDaysBetween_ShouldReturnCorrectCount`
Ensures that `DateTimeExtensions.WorkingDaysBetween` returns the correct number of weekdays between two dates, excluding weekends.

- **Parameters**: None (implicitly tests date ranges).
- **Return value**: Assertion that the result matches the expected count of working days.
- **Throws**: No exceptions under normal test conditions.

## Usage
