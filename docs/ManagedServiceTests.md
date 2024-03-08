# ManagedServiceTests

This class contains unit tests for the `ManagedService` type, covering validation logic, status updates, and systemd unit name generation. Each test method verifies a specific behavior of the corresponding production method under various input conditions.

## API

### `Validate_WithValidData_ShouldNotThrow`
Tests that `ManagedService.Validate()` completes without throwing an exception when the service instance is populated with valid data (e.g., a non-empty name and a port within the allowed range).  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Does not throw; the test passes if no exception is raised by the production code.

### `Validate_WithMissingName_ShouldThrowValidationException`
Tests that `ManagedService.Validate()` throws a `ValidationException` when the service’s `Name` property is null or empty.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** The test itself may throw if the expected exception is not thrown; the production code is expected to throw `ValidationException`.

### `Validate_WithInvalidPort_ShouldThrowValidationException`
Tests that `ManagedService.Validate()` throws a `ValidationException` when the service’s `Port` property is outside the valid range (e.g., less than 1 or greater than 65535).  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** The test itself may throw if the expected exception is not thrown; the production code is expected to throw `ValidationException`.

### `UpdateStatus_ShouldChangeStatusAndUpdatedAt`
Tests that `ManagedService.UpdateStatus()` correctly updates the `Status` property to the new value and sets the `UpdatedAt` timestamp to the current UTC time.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Does not throw; the test passes if both properties are updated as expected.

### `GetSystemdUnitName_WithExplicitName_ShouldReturnExplicitName`
Tests that `ManagedService.GetSystemdUnitName()` returns the value of the `ExplicitSystemdUnitName` property when it is set, regardless of the service’s `Name`.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Does not throw; the test passes if the returned string matches the explicit name.

### `GetSystemdUnitName_WithNoExplicitName_ShouldGenerateFromName`
Tests that `ManagedService.GetSystemdUnitName()` generates a systemd unit name based on the service’s `Name` when `ExplicitSystemdUnitName` is null or empty.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Does not throw; the test passes if the generated name follows the expected convention (e.g., lowercased, spaces replaced, `.service` suffix).

## Usage

The following examples demonstrate how the production code tested by this class behaves. These snippets are typical of how `ManagedService` is used in application logic.

**Example 1: Valid service validation**
```csharp
var service = new ManagedService
{
    Name = "my-web-app",
    Port = 8080
};

// Should complete without exception
service.Validate();
```

**Example 2: Invalid service (missing name)**
```csharp
var service = new ManagedService
{
    Name = "",
    Port = 80
};

try
{
    service.Validate();
    // Test would fail – expected exception not thrown
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## Notes

- **Edge cases:** The validation tests cover boundary conditions such as an empty or null `Name`, and ports at the extremes (0, 1, 65535, 65536). The `GetSystemdUnitName` tests handle cases where the explicit name is null, empty, or whitespace.
- **Thread safety:** The test methods themselves are not thread-safe and should be executed sequentially within a single test runner session. The `ManagedService` type under test is assumed to be used in a single-threaded context or with external synchronization; no thread-safety guarantees are implied by these tests.
- **Time-sensitive assertions:** `UpdateStatus_ShouldChangeStatusAndUpdatedAt` relies on `DateTime.UtcNow` and may be sensitive to system clock resolution. In practice, a tolerance of a few milliseconds is acceptable.
