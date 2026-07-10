# HealthCheckConfigEdgeCaseTests

This class contains a suite of unit tests that validate the edge‑case behavior of the `HealthCheckConfig` class. Each test method exercises a specific boundary condition – such as minimum intervals, zero thresholds, or missing endpoints – and asserts that the configuration’s validation logic either throws a `ValidationException` or completes without error. The tests also cover the `GetHealthCheckUrl` method for HTTP and non‑HTTP health‑check types.

## API

All methods are parameterless, return `void`, and are intended to be executed by a test framework (e.g., xUnit, NUnit). A test passes if no assertion fails; a test fails if an expected exception is not thrown or an unexpected exception is thrown.

| Method | Purpose |
|--------|---------|
| `Validate_IntervalBelowMinimum_ThrowsValidationException` | Verifies that setting the health‑check interval below the allowed minimum causes `Validate()` to throw a `ValidationException`. |
| `Validate_IntervalExactlyMinimum_DoesNotThrow` | Verifies that setting the interval exactly to the minimum allowed value passes validation without throwing. |
| `Validate_TimeoutGreaterThanInterval_ThrowsValidationException` | Verifies that a timeout value larger than the interval causes `Validate()` to throw a `ValidationException`. |
| `Validate_TimeoutEqualsInterval_DoesNotThrow` | Verifies that a timeout equal to the interval passes validation without throwing. |
| `Validate_ZeroTimeout_ThrowsValidationException` | Verifies that a timeout of zero causes `Validate()` to throw a `ValidationException`. |
| `Validate_ZeroUnhealthyThreshold_ThrowsValidationException` | Verifies that an unhealthy threshold of zero causes `Validate()` to throw a `ValidationException`. |
| `Validate_ZeroHealthyThreshold_ThrowsValidationException` | Verifies that a healthy threshold of zero causes `Validate()` to throw a `ValidationException`. |
| `Validate_HttpTypeWithoutEndpoint_ThrowsValidationException` | Verifies that a health‑check type of `Http` with no endpoint configured causes `Validate()` to throw a `ValidationException`. |
| `GetHealthCheckUrl_HttpType_ReturnsFormattedUrl` | Verifies that `GetHealthCheckUrl()` returns a correctly formatted URL string when the health‑check type is `Http`. |
| `GetHealthCheckUrl_NonHttpType_ReturnsNull` | Verifies that `GetHealthCheckUrl()` returns `null` when the health‑check type is not `Http` (e.g., `Tcp`, `Exec`). |

## Usage

The following examples demonstrate how the test class is used within a test project and how the underlying `HealthCheckConfig` validation is invoked.

### Example 1: Running the edge‑case tests

```csharp
using Xunit;

public class HealthCheckConfigEdgeCaseTests
{
    [Fact]
    public void Validate_IntervalBelowMinimum_ThrowsValidationException()
    {
        var config = new HealthCheckConfig
        {
            Interval = TimeSpan.FromMilliseconds(1) // below minimum
        };
        Assert.Throws<ValidationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_IntervalExactlyMinimum_DoesNotThrow()
    {
        var config = new HealthCheckConfig
        {
            Interval = TimeSpan.FromSeconds(1) // minimum allowed
        };
        config.Validate(); // should not throw
    }

    // ... other test methods follow the same pattern
}
```

### Example 2: Using the validation logic in application code

```csharp
public class HealthCheckService
{
    public void ConfigureHealthCheck(HealthCheckConfig config)
    {
        try
        {
            config.Validate();
            // Configuration is valid – proceed with setup
        }
        catch (ValidationException ex)
        {
            // Log or handle invalid configuration
            Console.WriteLine($"Health check configuration error: {ex.Message}");
        }
    }
}
```

## Notes

- **Edge cases covered**: The tests explicitly verify boundary conditions that are most likely to cause runtime failures: intervals below the minimum, timeouts exceeding intervals, zero values for thresholds, and missing endpoints for HTTP checks. These conditions are often overlooked in standard validation tests.
- **Thread safety**: The `HealthCheckConfig` class itself is assumed to be thread‑safe for read operations after construction, but the validation tests are not designed to be run concurrently. Each test method modifies a separate instance of `HealthCheckConfig` and should be executed sequentially to avoid shared state interference.
- **Test isolation**: Each test creates a fresh `HealthCheckConfig` instance, ensuring that no side effects from one test affect another. This is a standard practice for unit tests and is followed here.
- **Dependency on `ValidationException`**: The tests rely on the existence of a `ValidationException` class (typically defined in the same project or a shared library). If the exception type changes, these tests will need to be updated accordingly.
