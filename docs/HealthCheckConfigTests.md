# HealthCheckConfigTests

The `HealthCheckConfigTests` class serves as the unit test suite for validating the logic and constraints within the `HealthCheckConfig` domain model. It ensures that configuration instances adhere to specific business rules regarding health check intervals, timeouts, and endpoint requirements based on the protocol type (HTTP vs. TCP), while also verifying the correct construction of health check URLs.

## API

### `Validate_WithValidData_ShouldNotThrow`
Verifies that a `HealthCheckConfig` instance populated with compliant data passes validation without raising any exceptions. This method confirms the baseline happy path where all constraints (interval, timeout, endpoint) are satisfied.
*   **Parameters**: None (uses internally constructed valid data).
*   **Return Value**: `void`.
*   **Throws**: No exceptions are expected; the test fails if any exception is thrown.

### `Validate_WithIntervalLessThan5_ShouldThrowValidationException`
Ensures that the validation logic rejects configurations where the health check interval is set to a value less than 5 seconds.
*   **Parameters**: None (uses internally constructed data with an invalid interval).
*   **Return Value**: `void`.
*   **Throws**: The test expects a `ValidationException` (or equivalent assertion failure if the exception is not thrown).

### `Validate_WithTimeoutGreaterThanInterval_ShouldThrowValidationException`
Validates the constraint that the health check timeout duration cannot exceed the configured interval. This prevents logical conflicts where a check would still be running when the next one is scheduled.
*   **Parameters**: None (uses internally constructed data where timeout > interval).
*   **Return Value**: `void`.
*   **Throws**: The test expects a `ValidationException` (or equivalent assertion failure if the exception is not thrown).

### `Validate_WithMissingEndpointForHttp_ShouldThrowValidationException`
Confirms that an HTTP-type health check configuration requires a specific endpoint path. Validation must fail if the type is HTTP and the endpoint is null or empty.
*   **Parameters**: None (uses internally constructed HTTP data with a missing endpoint).
*   **Return Value**: `void`.
*   **Throws**: The test expects a `ValidationException` (or equivalent assertion failure if the exception is not thrown).

### `GetHealthCheckUrl_WithHttpType_ShouldConstructCorrectUrl`
Tests the URL generation logic for HTTP health checks. It verifies that the method correctly combines the host, port, and endpoint path into a valid absolute URI string.
*   **Parameters**: None (uses internally constructed valid HTTP data).
*   **Return Value**: `void` (asserts the resulting string matches the expected format).
*   **Throws**: Fails the test if the constructed URL does not match the expected pattern.

### `GetHealthCheckUrl_WithTcpType_ShouldReturnNull`
Verifies that URL generation is bypassed for TCP health checks, as TCP connectivity does not utilize an HTTP-style path endpoint.
*   **Parameters**: None (uses internally constructed valid TCP data).
*   **Return Value**: `void` (asserts the result is null).
*   **Throws**: Fails the test if the method returns a non-null value.

## Usage

The following examples demonstrate how the test cases validate specific configuration scenarios within the `caddy-vps-toolkit` project.

**Example 1: Validating Constraint Enforcement**
This scenario illustrates the test ensuring that a configuration with a timeout exceeding the interval is correctly rejected.

```csharp
[Test]
public void Validate_WithTimeoutGreaterThanInterval_ShouldThrowValidationException()
{
    // Arrange
    var config = new HealthCheckConfig
    {
        Type = HealthCheckType.Http,
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(15), // Invalid: Timeout > Interval
        Endpoint = "/health",
        Host = "localhost",
        Port = 8080
    };

    // Act & Assert
    Assert.Throws<ValidationException>(() => config.Validate());
}
```

**Example 2: Verifying URL Construction Logic**
This scenario demonstrates the test verifying that a valid HTTP configuration produces the correct absolute URL string.

```csharp
[Test]
public void GetHealthCheckUrl_WithHttpType_ShouldConstructCorrectUrl()
{
    // Arrange
    var config = new HealthCheckConfig
    {
        Type = HealthCheckType.Http,
        Host = "example.com",
        Port = 443,
        Endpoint = "/api/status"
    };

    // Act
    var url = config.GetHealthCheckUrl();

    // Assert
    Assert.AreEqual("https://example.com:443/api/status", url);
}
```

## Notes

*   **Edge Cases**: The validation logic strictly enforces a minimum interval of 5 seconds; values such as 4999 milliseconds will trigger a validation failure. Additionally, the distinction between HTTP and TCP types is critical: providing an endpoint for TCP is likely ignored or invalid, while omitting an endpoint for HTTP is a hard failure.
*   **Thread Safety**: As this class consists entirely of unit test methods instantiated per test run, it is inherently thread-safe within the context of a test runner. The underlying `HealthCheckConfig` objects created within these tests should be treated as transient data carriers; if the domain model itself is intended for concurrent access, that safety must be verified in separate concurrency-specific tests, as these methods do not simulate multi-threaded access patterns.
*   **Null Handling**: The `GetHealthCheckUrl` method explicitly returns `null` for TCP types rather than an empty string or a `tcp://` scheme URI. Consumers of the `HealthCheckConfig` must handle this null return value to avoid `NullReferenceException`s when processing health check targets generically.
