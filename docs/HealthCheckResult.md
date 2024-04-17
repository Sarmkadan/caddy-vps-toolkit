# HealthCheckResult

`HealthCheckResult` is a data structure used to encapsulate the outcome of a health check operation, including status indicators, timing metrics, and diagnostic information for services monitored by the `caddy-vps-toolkit`.

## API

### `public string Id`
A unique identifier for the health check instance. Used to track and reference the result in logs or monitoring systems.

### `public string ServiceId`
The identifier of the service being monitored by this health check. Helps correlate results with specific services.

### `public bool IsHealthy`
Indicates whether the health check passed (`true`) or failed (`false`). Derived from the `Status` field but provided for convenience.

### `public HealthCheckStatus Status`
The enumerated status of the health check (`Healthy`, `Unhealthy`, `Degraded`). Represents the overall outcome of the check.

### `public int ResponseTimeMs`
The time taken for the health check request to complete, measured in milliseconds. Useful for detecting slow responses.

### `public int HttpStatusCode`
The HTTP status code returned by the endpoint during the health check. `0` if the check did not involve an HTTP request.

### `public string ErrorMessage`
A descriptive error message if the health check failed. `null` or empty if the check succeeded.

### `public string ResponseBody`
The raw response body received from the endpoint during the health check. May be truncated or omitted in some implementations.

### `public DateTime CheckedAt`
The timestamp when the health check was executed. Useful for tracking when the result was generated.

### `public int ConsecutiveFailures`
The number of consecutive times this health check has failed. Resets to `0` on success.

### `public int ConsecutiveSuccesses`
The number of consecutive times this health check has succeeded. Resets to `0` on failure.

### `public string CheckType`
The type of health check performed (e.g., `Http`, `Tcp`, `Dns`). Defines the protocol or method used.

### `public string Endpoint`
The endpoint or address targeted by the health check (e.g., `https://example.com/health`, `tcp://192.168.1.1:8080`). May include protocol and port.

### `public static HealthCheckResult CreateSuccess(string serviceId, string endpoint, int responseTimeMs, int httpStatusCode, string responseBody)`
Creates a successful health check result.

- **Parameters**:
  - `serviceId`: Identifier of the service being checked.
  - `endpoint`: Target endpoint of the check.
  - `responseTimeMs`: Time taken for the request to complete.
  - `httpStatusCode`: HTTP status code returned.
  - `responseBody`: Raw response body from the endpoint.
- **Returns**: A `HealthCheckResult` instance with `IsHealthy = true`, `Status = Healthy`, and default values for failure-related fields.

### `public static HealthCheckResult CreateFailure(string serviceId, string endpoint, string errorMessage, int responseTimeMs, int httpStatusCode, string responseBody)`
Creates a failed health check result.

- **Parameters**:
  - `serviceId`: Identifier of the service being checked.
  - `endpoint`: Target endpoint of the check.
  - `errorMessage`: Description of the failure.
  - `responseTimeMs`: Time taken for the request to complete.
  - `httpStatusCode`: HTTP status code returned (if applicable).
  - `responseBody`: Raw response body from the endpoint (if available).
- **Returns**: A `HealthCheckResult` instance with `IsHealthy = false`, `Status = Unhealthy` or `Degraded`, and populated error fields.

### `public bool IsSlowResponse(int thresholdMs)`
Determines whether the response time exceeds a given threshold.

- **Parameters**:
  - `thresholdMs`: The maximum acceptable response time in milliseconds.
- **Returns**: `true` if `ResponseTimeMs > thresholdMs`; otherwise, `false`.

## Usage

### Example 1: Performing a Health Check
