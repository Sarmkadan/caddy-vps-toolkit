# CaddyConfigurationServiceTests

`CaddyConfigurationServiceTests` is the unit test suite for the `CaddyConfigurationService` class within the `caddy-vps-toolkit` project. It verifies the correctness of Caddyfile generation and validation logic, ensuring that methods handle valid inputs properly and throw expected exceptions for null arguments, empty content, and other edge cases.

## API

### CaddyConfigurationServiceTests
The default constructor for the test class. It is parameterless and initializes the test fixture, typically setting up any shared mock objects or the system under test instance required by the individual test methods.

### GenerateCaddyfileAsync_WithNullGlobalConfig_ShouldThrowArgumentNullException
```csharp
public async Task GenerateCaddyfileAsync_WithNullGlobalConfig_ShouldThrowArgumentNullException()
```
**Purpose:** Verifies that the asynchronous Caddyfile generation method throws an `ArgumentNullException` when a null global configuration is supplied.  
**Parameters:** None (test method).  
**Returns:** A `Task` representing the asynchronous test operation.  
**Throws:** The test itself asserts that the target method throws `ArgumentNullException`; the test method does not throw under normal execution.

### GenerateCaddyfileAsync_WithValidInputs_ShouldReturnString
```csharp
public async Task GenerateCaddyfileAsync_WithValidInputs_ShouldReturnString()
```
**Purpose:** Confirms that providing valid, non-null inputs to the asynchronous Caddyfile generation method produces a non-null, non-empty string result representing the generated Caddyfile content.  
**Parameters:** None (test method).  
**Returns:** A `Task` representing the asynchronous test operation.  
**Throws:** The test fails if the target method returns null, an empty string, or throws an unexpected exception.

### GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException
```csharp
public void GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException()
```
**Purpose:** Ensures that the route block generation method immediately throws an `ArgumentNullException` when a null route object is passed.  
**Parameters:** None (test method).  
**Returns:** `void`.  
**Throws:** The test asserts that `ArgumentNullException` is thrown by the method under test; the test method itself does not throw.

### GenerateRouteForService_WithValidService_ShouldCreateRoute
```csharp
public void GenerateRouteForService_WithValidService_ShouldCreateRoute()
```
**Purpose:** Validates that a properly populated service object results in a correctly structured route configuration block. The test checks that the returned route is not null and contains expected directives.  
**Parameters:** None (test method).  
**Returns:** `void`.  
**Throws:** The test fails if the generated route is null or does not match expected output.

### GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException
```csharp
public void GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException()
```
**Purpose:** Verifies that passing a null service object to the route generation method causes an `ArgumentNullException` to be thrown.  
**Parameters:** None (test method).  
**Returns:** `void`.  
**Throws:** The test asserts that `ArgumentNullException` is thrown; the test method itself does not throw.

### ValidateCaddyfileAsync_WithEmptyContent_ShouldThrowArgumentException
```csharp
public async Task ValidateCaddyfileAsync_WithEmptyContent_ShouldThrowArgumentException()
```
**Purpose:** Ensures that the asynchronous Caddyfile validation method throws an `ArgumentException` when provided with an empty or whitespace-only content string.  
**Parameters:** None (test method).  
**Returns:** A `Task` representing the asynchronous test operation.  
**Throws:** The test asserts that `ArgumentException` is thrown by the method under test; the test method does not throw under normal execution.

## Usage

### Example 1: Running All Tests in the Suite
```csharp
using Xunit;

public class TestRunner
{
    private readonly CaddyConfigurationServiceTests _tests;

    public TestRunner()
    {
        _tests = new CaddyConfigurationServiceTests();
    }

    [Fact]
    public async Task RunAllConfigurationTests()
    {
        // Null-guard tests
        await _tests.GenerateCaddyfileAsync_WithNullGlobalConfig_ShouldThrowArgumentNullException();
        _tests.GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException();
        _tests.GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException();
        await _tests.ValidateCaddyfileAsync_WithEmptyContent_ShouldThrowArgumentException();

        // Positive-path tests
        var result = await _tests.GenerateCaddyfileAsync_WithValidInputs_ShouldReturnString();
        _tests.GenerateRouteForService_WithValidService_ShouldCreateRoute();
    }
}
```

### Example 2: Selective Execution During Refactoring
```csharp
using Xunit;

public class TargetedTestExecution
{
    [Fact]
    public async Task VerifyRouteGenerationAfterRefactor()
    {
        var tests = new CaddyConfigurationServiceTests();

        // Focus only on route-related tests to confirm refactoring didn't break them
        tests.GenerateRouteForService_WithValidService_ShouldCreateRoute();
        tests.GenerateRouteForService_WithNullService_ShouldThrowArgumentNullException();
        tests.GenerateRouteBlock_WithNullRoute_ShouldThrowArgumentNullException();
    }
}
```

## Notes

- **Edge Cases:** The `ValidateCaddyfileAsync_WithEmptyContent_ShouldThrowArgumentException` test specifically targets empty or whitespace-only strings; content consisting solely of newline characters or spaces should also trigger the exception. Null content is expected to be handled separately (likely by an `ArgumentNullException`), though no dedicated test for that scenario is listed among the public members.
- **Thread Safety:** All asynchronous test methods return `Task` and are designed to be awaited. The test class itself does not manage shared mutable state across tests; each test method operates independently. When executed via a test runner such as xUnit, the runner may parallelize test execution, but the individual test methods do not share instance fields that would introduce race conditions.
- **Test Isolation:** The constructor likely initializes a fresh instance of the system under test for each test method invocation, ensuring no residual state from one test affects another. This is standard practice in xUnit test classes where the test runner creates a new instance of the test class for each `[Fact]` or `[Theory]` method.
