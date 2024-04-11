# CaddyVpsException

A base exception type for the `caddy-vps-toolkit` project, providing structured error handling with an `ErrorCode` and `Details` payload. This exception serves as the parent class for domain-specific exceptions related to Caddy operations, service management, and system interactions, enabling consistent error reporting and recovery strategies.

## API

### `public string ErrorCode`
A machine-readable identifier for the error, typically following a convention like `CADDY_<MODULE>_<TYPE>`. This field allows callers to programmatically handle specific error conditions without string parsing.

### `public object Details`
An optional payload containing additional context about the error. The structure and content depend on the specific exception subclass but may include raw API responses, configuration objects, or validation results.

### `public CaddyVpsException()`
Constructs a new instance with default values. The `ErrorCode` will be `null`, and `Details` will be `null`.

### `public CaddyVpsException(string message)`
Constructs a new instance with the specified error message. The `ErrorCode` will be `null`, and `Details` will be `null`.

### `public ServiceNotFoundException()`
Constructs a new `ServiceNotFoundException` with a default message indicating the requested service could not be located. The `ErrorCode` is set to `CADDY_SERVICE_NOT_FOUND`.

### `public ServiceConfigurationException()`
Constructs a new `ServiceConfigurationException` with a default message indicating an invalid or missing service configuration. The `ErrorCode` is set to `CADDY_SERVICE_CONFIG_INVALID`.

### `public SystemdOperationException()`
Constructs a new `SystemdOperationException` with a default message indicating a failure during a systemd operation (e.g., start, stop, enable). The `ErrorCode` is set to `CADDY_SYSTEMD_OP_FAILED`.

### `public CaddyOperationException()`
Constructs a new `CaddyOperationException` with a default message indicating a failure during a Caddy-specific operation (e.g., configuration reload, certificate issuance). The `ErrorCode` is set to `CADDY_OPERATION_FAILED`.

### `public HealthCheckException()`
Constructs a new `HealthCheckException` with a default message indicating a health check failure (e.g., endpoint unreachable, unhealthy status). The `ErrorCode` is set to `CADDY_HEALTH_CHECK_FAILED`.

### `public DatabaseException()`
Constructs a new `DatabaseException` with a default message indicating a database-related failure (e.g., connection error, query failure). The `ErrorCode` is set to `CADDY_DATABASE_ERROR`.

### `public ValidationException()`
Constructs a new `ValidationException` with a default message indicating input or state validation failure. The `ErrorCode` is set to `CADDY_VALIDATION_FAILED`.

### `public NotSupportedException()`
Constructs a new `NotSupportedException` with a default message indicating an unsupported operation or feature. The `ErrorCode` is set to `CADDY_NOT_SUPPORTED`.

## Usage

### Example 1: Handling a service configuration error
