# Result

A generic result type used to encapsulate the outcome of operations, supporting both success and failure states with optional data payloads. Designed for explicit error handling without exceptions, it provides a uniform way to return operation results or errors.

## API

### Properties

#### `public bool IsSuccess`
Indicates whether the result represents a successful operation (`true`) or a failure (`false`). Always `true` for results created via `Success` and `false` for those created via `Failure`.

#### `public T Data`
The payload associated with a successful result. Only valid when `IsSuccess` is `true`; accessing it otherwise yields implementation-defined behavior. Not present in non-generic `Result`.

#### `public string ErrorMessage`
A human-readable description of the error. Only meaningful when `IsSuccess` is `false`. May be `null` or empty if no message was provided.

#### `public string ErrorCode`
A machine-readable code classifying the error. Only meaningful when `IsSuccess` is `false`. May be `null` or empty if no code was provided.

#### `public System.Collections.Generic.List<T> Items`
A collection of items, typically used for paginated results. Only valid when the result represents a successful paginated query.

#### `public int Page`
The current page number in a paginated result set. Only valid when the result represents a successful paginated query.

#### `public int PageSize`
The number of items per page in a paginated result set. Only valid when the result represents a successful paginated query.

#### `public int TotalCount`
The total number of items available across all pages in a paginated result set. Only valid when the result represents a successful paginated query.

### Methods

#### `public static Result<T> Success(T data)`
Creates a successful result containing the provided data payload.

- **Parameters**
  - `data`: The value to encapsulate on success.
- **Return Value**
  Returns a `Result<T>` instance where `IsSuccess` is `true`, `Data` equals `data`, and error fields are unset.
- **Exceptions**
  Throws `System.ArgumentNullException` if `data` is `null`.

#### `public static Result<T> Failure(string errorMessage, string errorCode = null)`
Creates a failed result with the specified error details.

- **Parameters**
  - `errorMessage`: A non-null description of the error.
  - `errorCode`: An optional machine-readable error code.
- **Return Value**
  Returns a `Result<T>` instance where `IsSuccess` is `false`, `ErrorMessage` equals `errorMessage`, and `ErrorCode` equals `errorCode`.
- **Exceptions**
  Throws `System.ArgumentNullException` if `errorMessage` is `null`.

#### `public static Result Success()`
Creates a successful result with no payload.

- **Return Value**
  Returns a non-generic `Result` instance where `IsSuccess` is `true` and error fields are unset.

#### `public static Result Failure(string errorMessage, string errorCode = null)`
Creates a failed result with the specified error details.

- **Parameters**
  - `errorMessage`: A non-null description of the error.
  - `errorCode`: An optional machine-readable error code.
- **Return Value**
  Returns a non-generic `Result` instance where `IsSuccess` is `false`, `ErrorMessage` equals `errorMessage`, and `ErrorCode` equals `errorCode`.
- **Exceptions**
  Throws `System.ArgumentNullException` if `errorMessage` is `null`.

## Usage

### Example 1: Basic success and failure
