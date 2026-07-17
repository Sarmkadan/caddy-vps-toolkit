# ResultExtensions

Utility class providing extension-style helper methods for working with `Result<T>` and `Result` types, enabling functional-style composition of operations that may fail. These methods allow chaining, transforming, and inspecting results without explicit null checks or try-catch blocks, while preserving error context.

## API

### `Map<T, TResult>`
Transforms a successful `Result<T>` into a `Result<TResult>` by applying a synchronous selector function to the contained value. If the input is a failure, the error is propagated unchanged.

- **Parameters**
  - `source`: The `Result<T>` to transform.
  - `selector`: Function to apply to the value if successful.

- **Returns**
  - `Result<TResult>`: Success with the transformed value, or failure with the original error.

- **Throws**
  - `ArgumentNullException`: If `selector` is `null`.

---

### `Bind<T, TResult>`
Applies a function that returns a `Result<TResult>` to the contained value of a successful `Result<T>`, enabling chaining of dependent operations. If the input is a failure, the error is propagated unchanged.

- **Parameters**
  - `source`: The `Result<T>` to bind.
  - `selector`: Function returning a `Result<TResult>` if successful.

- **Returns**
  - `Result<TResult>`: Result of the bound operation, or failure with the original error.

- **Throws**
  - `ArgumentNullException`: If `selector` is `null`.

---

### `ToBoolean<T>`
Converts a `Result<T>` into a boolean indicating success or failure. The contained value is discarded.

- **Parameters**
  - `source`: The `Result<T>` to evaluate.

- **Returns**
  - `bool`: `true` if the result is successful; `false` otherwise.

---

### `ToBoolean`
Converts a non-generic `Result` into a boolean indicating success or failure.

- **Parameters**
  - `source`: The `Result` to evaluate.

- **Returns**
  - `bool`: `true` if the result is successful; `false` otherwise.

---

### `GetErrorOrNull<T>`
Extracts the error message from a `Result<T>` if it represents a failure; otherwise returns `null`.

- **Parameters**
  - `source`: The `Result<T>` to inspect.

- **Returns**
  - `string?`: The error message if the result is a failure; otherwise `null`.

---

### `GetErrorOrNull`
Extracts the error message from a non-generic `Result` if it represents a failure; otherwise returns `null`.

- **Parameters**
  - `source`: The `Result` to inspect.

- **Returns**
  - `string?`: The error message if the result is a failure; otherwise `null`.

---
### `OnSuccess<T>`
Invokes an action if the `Result<T>` represents a success. The action receives the contained value. If the result is a failure, the action is not executed.

- **Parameters**
  - `source`: The `Result<T>` to inspect.
  - `action`: Action to invoke on success.

- **Throws**
  - `ArgumentNullException`: If `action` is `null`.

---
### `OnSuccess`
Invokes an action if the non-generic `Result` represents a success. If the result is a failure, the action is not executed.

- **Parameters**
  - `source`: The `Result` to inspect.
  - `action`: Action to invoke on success.

- **Throws**
  - `ArgumentNullException`: If `action` is `null`.

---
### `OnFailure<T>`
Invokes an action if the `Result<T>` represents a failure. The action receives the error message. If the result is a success, the action is not executed.

- **Parameters**
  - `source`: The `Result<T>` to inspect.
  - `action`: Action to invoke on failure.

- **Throws**
  - `ArgumentNullException`: If `action` is `null`.

---
### `OnFailure`
Invokes an action if the non-generic `Result` represents a failure. If the result is a success, the action is not executed.

- **Parameters**
  - `source`: The `Result` to inspect.
  - `action`: Action to invoke on failure.

- **Throws**
  - `ArgumentNullException`: If `action` is `null`.

---
### `ToReadOnlyList<T>`
Converts a `Result<T>` into a read-only list containing the value if successful, or an empty list if the result is a failure.

- **Parameters**
  - `source`: The `Result<T>` to convert.

- **Returns**
  - `IReadOnlyList<T>`: A read-only list with the value if successful; otherwise empty.

---
### `ToTuple<T>`
Converts a `Result<T>` into a tuple indicating success status and the contained value if successful.

- **Parameters**
  - `source`: The `Result<T>` to convert.

- **Returns**
  - `(bool IsSuccess, T Data)`: Tuple where `IsSuccess` is `true` if the result is successful, and `Data` contains the value if successful; otherwise `Data` is the default value of `T`.

---
### `ToTuple`
Converts a non-generic `Result` into a tuple indicating success status and the error message if failed.

- **Parameters**
  - `source`: The `Result` to convert.

- **Returns**
  - `(bool Success, string ErrorMessage)`: Tuple where `Success` is `true` if the result is successful, and `ErrorMessage` contains the error if failed; otherwise `ErrorMessage` is `null`.

## Usage
