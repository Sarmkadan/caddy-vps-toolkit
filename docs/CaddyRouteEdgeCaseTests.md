# CaddyRouteEdgeCaseTests

The `CaddyRouteEdgeCaseTests` class serves as a comprehensive validation suite for the `CaddyRoute` configuration logic within the `caddy-vps-toolkit` project. It rigorously verifies that route definitions adhere to strict constraints regarding domain validity, upstream URL formatting, timeout values, and basic authentication requirements before being applied to a Caddy server configuration. By isolating edge cases such as null inputs, malformed URLs, and invalid numeric ranges, this component ensures that only well-formed routing rules are generated, preventing runtime configuration errors in the reverse proxy setup.

## API

### Validation Methods

#### `Validate_NullDomain_ThrowsValidationException`
Verifies that the route validation logic correctly rejects a configuration where the domain property is null.
*   **Parameters**: None (operates on internal test fixtures).
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the domain is null.

#### `Validate_EmptyDomain_ThrowsValidationException`
Ensures that an empty string provided as the domain triggers a validation failure.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the domain is empty.

#### `Validate_NullUpstreamUrl_ThrowsValidationException`
Confirms that a null value for the upstream target URL is not permitted.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the upstream URL is null.

#### `Validate_InvalidUpstreamUrl_ThrowsValidationException`
Validates that malformed or syntactically incorrect upstream URLs result in a rejection.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the upstream URL format is invalid.

#### `Validate_ZeroTimeout_ThrowsValidationException`
Checks that a timeout value of zero is considered invalid for route processing.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the timeout is zero.

#### `Validate_NegativeTimeout_ThrowsValidationException`
Ensures that negative timeout values are rejected by the validation logic.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if the timeout is negative.

#### `Validate_BasicAuthEnabledWithoutUsername_ThrowsValidationException`
Verifies that enabling basic authentication without specifying a username results in a configuration error.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Expects a `ValidationException` to be thrown if basic auth is enabled but the username is missing.

#### `Validate_ValidRoute_DoesNotThrow`
Confirms that a fully compliant route configuration (valid domain, URL, timeout, and auth settings) passes validation without errors.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: None expected; the test fails if any exception is thrown.

### Path Matching and Generation Methods

#### `GetCaddyPathMatcher_NullPath_ReturnsEmpty`
Tests the path matcher generator when the input path is null, ensuring it returns an empty matcher string.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts the result is an empty string).
*   **Exceptions**: None.

#### `GetCaddyPathMatcher_SlashPath_ReturnsEmpty`
Validates that a root path (`/`) is treated equivalently to a null or empty path, returning an empty matcher.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts the result is an empty string).
*   **Exceptions**: None.

#### `GetCaddyPathMatcher_CustomPath_ReturnsPath`
Ensures that a specific custom path (e.g., `/api`) is correctly returned as the path matcher.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts the result matches the custom path).
*   **Exceptions**: None.

#### `GenerateRoutePath_WithCustomPath_ConcatenatesDomainAndPath`
Verifies the full route path generation logic when a custom path is provided, ensuring the domain and path are correctly concatenated.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts the concatenated string format).
*   **Exceptions**: None.

#### `GenerateRoutePath_WithSlashPath_ReturnsDomainOnly`
Confirms that when the path is effectively root (`/`), the generated route path consists solely of the domain.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts the result is the domain only).
*   **Exceptions**: None.

## Usage

The following examples demonstrate how the validation logic covered by this test class applies to real-world configuration scenarios.

### Example 1: Validating a Secure Microservice Route
This example illustrates a scenario where a route is configured with a custom path, a valid upstream, and basic authentication. The validation ensures that all constraints are met before deployment.

```csharp
var route = new CaddyRoute
{
    Domain = "api.example.com",
    UpstreamUrl = "http://localhost:5000",
    Path = "/v1/users",
    TimeoutSeconds = 30,
    BasicAuthEnabled = true,
    BasicAuthUsername = "admin"
};

try 
{
    // Internally triggers logic equivalent to Validate_ValidRoute_DoesNotThrow
    RouteValidator.Validate(route); 
    Console.WriteLine("Route configuration is valid.");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

### Example 2: Handling Invalid Timeout and Missing Auth
This example demonstrates the failure cases where a zero timeout is set and basic authentication is enabled without a username, triggering the specific exceptions verified by the test suite.

```csharp
var invalidRoute = new CaddyRoute
{
    Domain = "service.example.com",
    UpstreamUrl = "http://localhost:8080",
    Path = "/",
    TimeoutSeconds = 0, // Triggers Validate_ZeroTimeout_ThrowsValidationException
    BasicAuthEnabled = true 
    // Username is missing, triggering Validate_BasicAuthEnabledWithoutUsername_ThrowsValidationException
};

try 
{
    RouteValidator.Validate(invalidRoute);
}
catch (ValidationException ex)
{
    // Expected behavior: Execution stops here due to validation failure
    Console.WriteLine($"Rejected invalid route: {ex.Message}");
}
```

## Notes

*   **Edge Case Sensitivity**: The validation logic distinguishes strictly between `null`, empty strings, and whitespace for domains and URLs. A path consisting solely of a forward slash (`/`) is normalized to an empty path matcher, which affects how the final Caddyfile directive is generated.
*   **Timeout Constraints**: Timeout values must be strictly positive integers. Both zero and negative values are treated as critical configuration errors.
*   **Authentication Dependency**: Enabling the `BasicAuthEnabled` flag creates a hard dependency on the presence of a `BasicAuthUsername`. This flag does not implicitly allow anonymous access if the username is omitted; it strictly requires credentials.
*   **Thread Safety**: As this class represents a suite of stateless validation tests and the underlying validation logic typically operates on immutable data transfer objects (DTOs), the methods are inherently thread-safe. Multiple threads can simultaneously validate different route configurations without risk of race conditions.
*   **Exception Consistency**: All validation failures consistently throw `ValidationException`, allowing calling code to implement uniform error handling strategies regardless of the specific constraint violated.
