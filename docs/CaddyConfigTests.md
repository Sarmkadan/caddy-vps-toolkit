# CaddyConfigTests

The `CaddyConfigTests` class contains unit tests for the `CaddyConfig` type, which is part of the `caddy-vps-toolkit` project. Each test method validates a specific behavior of `CaddyConfig`, such as configuration validation, default value assignment, and Caddyfile globals generation. The tests are designed to be run with a standard test framework (e.g., xUnit or NUnit) and assert that the correct exceptions are thrown or that no exceptions occur under given conditions.

## API

### `public void Validate_WithValidData_ShouldNotThrow`

- **Purpose**: Verifies that the `Validate` method of `CaddyConfig` does not throw any exception when provided with a fully valid configuration.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw; the test passes if no exception is thrown.

### `public void Validate_WithInvalidAdminPort_ShouldThrowValidationException`

- **Purpose**: Ensures that `Validate` throws a `ValidationException` when the admin port is set to an invalid value (e.g., out of range, negative, or zero).
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: The test itself does not throw; it expects the underlying `Validate` call to throw a `ValidationException`.

### `public void Validate_WithNegativeTimeout_ShouldThrowValidationException`

- **Purpose**: Confirms that `Validate` throws a `ValidationException` when a timeout value is negative.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: The test expects a `ValidationException` from the `Validate` call.

### `public void SetDefaultValues_WhenEmailsAreNull_ShouldSetDefaults`

- **Purpose**: Tests that the `SetDefaultValues` method assigns default email addresses when the email property is `null`.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw; the test asserts that default values are correctly populated.

### `public void GenerateCaddyfileGlobals_ShouldContainMetricsWhenEnabled`

- **Purpose**: Validates that the generated Caddyfile globals section includes the metrics directive when metrics are enabled in the configuration.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw; the test asserts that the output string contains the expected metrics directive.

### `public void GenerateCaddyfileGlobals_WhenAutoHttpsDisabled_ShouldContainDirective`

- **Purpose**: Verifies that when automatic HTTPS is disabled, the generated Caddyfile globals include the appropriate directive to disable it.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw; the test asserts that the output string contains the expected directive.

## Usage

The following examples demonstrate how to use the `CaddyConfigTests` class within a test project. The first example shows a validation test, and the second shows a default value test.

```csharp
using Xunit;

public class CaddyConfigTestsIntegration
{
    [Fact]
    public void Validate_WithValidData_ShouldNotThrow_Example()
    {
        var test = new CaddyConfigTests();
        // This test method internally creates a valid CaddyConfig and calls Validate.
        // It will pass if no exception is thrown.
        test.Validate_WithValidData_ShouldNotThrow();
    }

    [Fact]
    public void SetDefaultValues_WhenEmailsAreNull_ShouldSetDefaults_Example()
    {
        var test = new CaddyConfigTests();
        // The test method sets up a CaddyConfig with null emails,
        // calls SetDefaultValues, and then asserts that defaults are applied.
        test.SetDefaultValues_WhenEmailsAreNull_ShouldSetDefaults();
    }
}
```

## Notes

- **Edge cases**: The validation tests cover boundary conditions such as negative timeouts and invalid port numbers (e.g., 0, 65536, or negative values). The default value test handles the case where email addresses are `null`; it does not cover empty strings or whitespace-only strings unless explicitly tested elsewhere.
- **Thread safety**: Each test method is designed to be run in isolation and does not rely on shared mutable state. The `CaddyConfigTests` class itself is not thread-safe, but this is irrelevant because test frameworks typically execute each test method on its own thread or sequentially. Concurrent execution of multiple test methods from the same instance is not supported and may lead to unpredictable results.
- **Dependencies**: The tests assume that `CaddyConfig` and its dependencies (e.g., `ValidationException`) are correctly implemented and available in the test assembly. No external resources (files, network) are required.
