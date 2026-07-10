# ArgumentValidatorTests

The `ArgumentValidatorTests` class contains unit tests for the `ArgumentValidator` component of the `caddy-vps-toolkit` project. Each test method exercises a specific validation scenario, verifying that the `Validate` method returns the expected `ValidationResult` and that error messages are correctly aggregated by `ValidationResult.GetErrorMessage`. The tests ensure that argument parsing handles null descriptors, missing required positional arguments, unknown flags, and fully valid input.

## API

### `public ArgumentValidatorTests()`

Initializes a new instance of the test class. No parameters or return value. Does not throw.

### `public void Validate_WithNullDescriptor_ShouldReturnInvalid()`

Tests that `ArgumentValidator.Validate` returns an invalid result when the descriptor argument is `null`.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw directly; test assertions may throw on failure.

### `public void Validate_WithMissingRequiredPositionalArgs_ShouldReturnInvalid()`

Tests that validation fails when one or more required positional arguments are not provided in the input arguments.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw directly; test assertions may throw on failure.

### `public void Validate_WithUnknownFlag_ShouldReturnInvalid()`

Tests that validation fails when an unrecognized flag (e.g., `--unknown`) is present in the arguments.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw directly; test assertions may throw on failure.

### `public void Validate_WithValidArguments_ShouldReturnValid()`

Tests that validation succeeds when all arguments conform to the descriptor, including required positional arguments and recognized flags.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw directly; test assertions may throw on failure.

### `public void ValidationResult_GetErrorMessage_ShouldJoinErrors()`

Tests that the `GetErrorMessage` method on a `ValidationResult` correctly concatenates multiple error messages into a single string.  
**Parameters:** None.  
**Return value:** None (void).  
**Throws:** Does not throw directly; test assertions may throw on failure.

## Usage

The following examples demonstrate how to run the tests programmatically or integrate them into a test suite.

**Example 1: Running a single test method in a console harness**

```csharp
using caddy_vps_toolkit.Tests;

var tests = new ArgumentValidatorTests();
try
{
    tests.Validate_WithNullDescriptor_ShouldReturnInvalid();
    Console.WriteLine("Test passed: Validate_WithNullDescriptor_ShouldReturnInvalid");
}
catch (Exception ex)
{
    Console.WriteLine($"Test failed: {ex.Message}");
}
```

**Example 2: Executing all test methods and collecting results**

```csharp
using caddy_vps_toolkit.Tests;

var tests = new ArgumentValidatorTests();
var testMethods = typeof(ArgumentValidatorTests).GetMethods()
    .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0);

foreach (var method in testMethods)
{
    try
    {
        method.Invoke(tests, null);
        Console.WriteLine($"PASS: {method.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL: {method.Name} - {ex.InnerException?.Message ?? ex.Message}");
    }
}
```

## Notes

- The test methods are stateless and do not rely on shared instance fields; they can be executed in any order or in parallel without side effects.
- Edge cases covered include `null` descriptor, missing required positional arguments, and unknown flags. Tests for duplicate flags, empty argument arrays, or mixed valid/invalid input are not present in this class.
- The `ValidationResult_GetErrorMessage_ShouldJoinErrors` test verifies that multiple errors are joined with a separator (typically a newline or comma), but the exact separator is an implementation detail of `ValidationResult`.
- Thread safety is not a concern for these tests because they do not modify any shared state. Running them concurrently in a test runner is safe.
