# ValidationHelperTests

`ValidationHelperTests` provides comprehensive unit testing coverage for the core validation and utility logic within the `caddy-vps-toolkit`. These tests verify that various business rules—including network configuration constraints, domain and service naming conventions, and health check state management—are enforced consistently across different execution scenarios.

## API

*   **`ValidatePort_PortZero_ReturnsInvalidResult`**: Validates the port validation logic correctly identifies and rejects a port number of zero as invalid.
*   **`ValidateDomain_WellFormedDomain_ReturnsValidResult`**: Validates the domain name parsing logic correctly accepts standard, well-formed domain names.
*   **`ValidateServiceName_LessThanThreeChars_ReturnsError`**: Validates that the service naming constraints correctly enforce a minimum length of three characters, returning an error for shorter inputs.
*   **`Combine_TwoFailureResults_MergesAllErrorMessages`**: Validates that the `Combine` utility method effectively aggregates and merges error messages from multiple failure results into a single collection.
*   **`HealthCheckResult_CreateSuccess_SetsHealthyProperties`**: Validates that instances of `HealthCheckResult` initialized as successful correctly set the internal health properties.
*   **`HealthCheckResult_IsSlowResponse_ReturnsTrueOnlyAboveThreshold`**: Validates the threshold logic in `HealthCheckResult`, ensuring that slow response flags are only triggered when response times exceed the defined threshold.
*   **`ManagedService_GetSystemdUnitName_WithSpacesInName_FormatsCorrectly`**: Validates that the service-to-unit-name mapping correctly sanitizes service names by handling spaces according to the required systemd formatting rules.

## Usage

```csharp
// Example of running the port validation test within an xUnit context
[Fact]
public void TestPortValidation()
{
    var tests = new ValidationHelperTests();
    tests.ValidatePort_PortZero_ReturnsInvalidResult();
}
```

```csharp
// Example of verifying service name formatting behavior
[Fact]
public void TestManagedServiceNaming()
{
    var tests = new ValidationHelperTests();
    tests.ManagedService_GetSystemdUnitName_WithSpacesInName_FormatsCorrectly();
}
```

## Notes

*   **Execution Environment**: These methods are designed to be executed by standard test runners (e.g., xUnit, NUnit). They do not maintain internal state between tests and are safe for parallel execution by configured test runners.
*   **Edge Case Coverage**: The tests specifically target boundary conditions, such as minimum length requirements for service names and zero-value constraints for ports, to ensure robust input handling.
*   **Dependency Assumptions**: These tests assume that the underlying domain and service validation logic under test does not rely on external I/O or network calls, ensuring fast and deterministic execution.
