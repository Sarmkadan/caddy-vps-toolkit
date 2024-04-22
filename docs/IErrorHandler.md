# IErrorHandler

A contract for components that centralize and standardize error handling within the Caddy VPS Toolkit. Implementations of this interface are responsible for capturing, enriching, and normalizing errors so that callers can uniformly decide how to respond or terminate the process.

## API

### `ErrorHandlingPipeline`

- **Type**: `public`
- **Purpose**: Provides the sequence of registered error handlers that will be invoked when an error occurs.
- **Remarks**: This property is read-only; the pipeline is built during initialization and should not be modified at runtime.

### `public void AddHandler(IErrorHandler handler)`

- **Purpose**: Registers an additional error handler to be invoked in sequence when an error is processed.
- **Parameters**:
  - `handler` – The `IErrorHandler` instance to append to the pipeline.
- **Exceptions**:
  - `ArgumentNullException` – Thrown if `handler` is `null`.

### `public async Task<ErrorResponse> HandleAsync(Exception error, CancellationToken cancellationToken = default)`

- **Purpose**: Executes the registered pipeline of handlers to transform the raw exception into a structured `ErrorResponse`.
- **Parameters**:
  - `error` – The exception that triggered the error-handling flow.
  - `cancellationToken` – A token to monitor for cancellation requests.
- **Return value**: A `Task<ErrorResponse>` that resolves to the final, enriched error response.
- **Exceptions**:
  - `ArgumentNullException` – Thrown if `error` is `null`.
  - `OperationCanceledException` – Thrown if `cancellationToken` is canceled before completion.
  - Any exception thrown by a handler in the pipeline that is not handled internally.

### `public int ExitCode`

- **Type**: `public` read-only property
- **Purpose**: Returns the process exit code that should be used when the error is unhandled and the application terminates.
- **Remarks**: The value is typically derived from the error category or severity; it defaults to a non-zero value to indicate failure.

### `public string Code`

- **Type**: `public` read-only property
- **Purpose**: A short, machine-readable identifier for the error category (e.g., `DNS_TIMEOUT`, `AUTH_FAILED`).
- **Remarks**: Must be stable across versions to allow programmatic matching.

### `public string Message`

- **Type**: `public` read-only property
- **Purpose**: A concise, human-readable description of the error intended for logs and user interfaces.
- **Remarks**: Should not contain sensitive data or stack traces.

### `public string Details`

- **Type**: `public` read-only property
- **Purpose**: An optional, detailed explanation that may include stack traces, inner exceptions, or diagnostic context.
- **Remarks**: May be empty; intended for debugging and support, not for end-user display.

## Usage
