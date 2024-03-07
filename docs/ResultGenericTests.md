# ResultGenericTests

Unit test class for verifying the behavior of generic `Result<T>` type implementations, focusing on success/failure state management, error handling, and pagination support.

## API

### `Success_WithData_SetsIsSuccessTrueAndData`
Verifies that a successful result with provided data correctly sets `IsSuccess` to true and assigns the data value.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Success_WithoutArgument_SetsIsSuccessTrueAndDefaultData`
Ensures that a successful result without explicit data initializes with default(T) and sets `IsSuccess` to true.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Failure_WithMessage_SetsIsSuccessFalseAndMessage`
Confirms that a failure result with a message sets `IsSuccess` to false and stores the provided error message.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Failure_WithMessageAndCode_SetsErrorCode`
Validates that a failure result with both a message and error code correctly assigns the error code.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Failure_WithoutCode_DefaultsToUnknownError`
Checks that a failure result created without an error code defaults the error code to "UnknownError".
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Success_SetsIsSuccessTrue`
Basic test confirming that a successful result sets `IsSuccess` to true.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `Failure_SetsIsSuccessFalseAndMessage`
Ensures that a failure result sets `IsSuccess` to false and stores the error message.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `TotalPages_CalculatesCorrectlyForEvenDivision`
Verifies that `TotalPages` returns the correct value when total item count divides evenly by page size.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `TotalPages_RoundsUpForRemainder`
Confirms that `TotalPages` rounds up to the next integer when total item count leaves a remainder when divided by page size.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `HasNextPage_WhenNotOnLastPage_ReturnsTrue`
Checks that `HasNextPage` returns true when the current page is not the last page.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `HasNextPage_WhenOnLastPage_ReturnsFalse`
Validates that `HasNextPage` returns false when the current page is the last page.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `HasPreviousPage_WhenOnFirstPage_ReturnsFalse`
Ensures that `HasPreviousPage` returns false when on the first page.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `HasPreviousPage_WhenPastFirstPage_ReturnsTrue`
Confirms that `HasPreviousPage` returns true when not on the first page.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

### `TotalPages_WhenTotalCountIsZero_ReturnsZero`
Verifies that `TotalPages` returns zero when total item count is zero.
- **Parameters**: None
- **Return value**: void
- **Throws**: No exceptions expected under normal test conditions

## Usage
