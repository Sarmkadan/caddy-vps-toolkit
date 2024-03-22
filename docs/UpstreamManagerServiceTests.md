# UpstreamManagerServiceTests

This test class validates the behavior of `UpstreamManagerService` for pool registration and retrieval operations. It ensures correct exception handling for invalid inputs and verifies successful pool creation and lookup.

## API

### `public UpstreamManagerServiceTests()`
Initializes a new instance of the test class.  
- **Parameters**: None.  
- **Return value**: None.  
- **Throws**: None.

### `public async Task RegisterPoolAsync_NullPool_ThrowsArgumentNullException()`
Tests that `RegisterPoolAsync` throws an `ArgumentNullException` when a null pool is provided.  
- **Parameters**: None (test method).  
- **Return value**: `Task`.  
- **Throws**: `ArgumentNullException` (expected by test).

### `public async Task RegisterPoolAsync_InvalidPool_ThrowsServiceConfigurationException()`
Tests that `RegisterPoolAsync` throws a `ServiceConfigurationException` when the pool configuration is invalid (e.g., missing required fields).  
- **Parameters**: None.  
- **Return value**: `Task`.  
- **Throws**: `ServiceConfigurationException` (expected by test).

### `public async Task RegisterPoolAsync_ServiceNotFound_ThrowsServiceNotFoundException()`
Tests that `RegisterPoolAsync` throws a `ServiceNotFoundException` when the referenced service does not exist.  
- **Parameters**: None.  
- **Return value**: `Task`.  
- **Throws**: `ServiceNotFoundException` (expected by test).

### `public async Task RegisterPoolAsync_ValidPool_ReturnsPoolId()`
Tests that `RegisterPoolAsync` returns a valid pool identifier when a correctly configured pool is provided.  
- **Parameters**: None.  
- **Return value**: `Task`.  
- **Throws**: None (test expects success).

### `public async Task GetPoolAsync_ExistingPool_ReturnsPool()`
Tests that `GetPoolAsync` returns the expected pool object when queried with an existing pool identifier.  
- **Parameters**: None.  
- **Return value**: `Task`.  
- **Throws**: None (test expects success).

### `public async Task GetPoolAsync_NonexistentPool_ReturnsNull()`
Tests that `GetPoolAsync` returns `null` when queried with a pool identifier that has not been registered.  
- **Parameters**: None.  
- **Return value**: `Task`.  
- **Throws**: None (test expects null return).

## Usage

The following examples demonstrate typical usage of `UpstreamManagerServiceTests` within an xUnit test runner.

**Example 1: Running all pool registration tests**
```csharp
public class UpstreamManagerServiceTestsRunner
{
    [Fact]
    public async Task RunAllRegistrationTests()
    {
        var tests = new UpstreamManagerServiceTests();
        
        await tests.RegisterPoolAsync_NullPool_ThrowsArgumentNullException();
        await tests.RegisterPoolAsync_InvalidPool_ThrowsServiceConfigurationException();
        await tests.RegisterPoolAsync_ServiceNotFound_ThrowsServiceNotFoundException();
        await tests.RegisterPoolAsync_ValidPool_ReturnsPoolId();
        
        // All tests passed
    }
}
```

**Example 2: Testing retrieval after registration**
```csharp
[Fact]
public async Task RegisterAndRetrievePool()
{
    var tests = new UpstreamManagerServiceTests();
    
    // Register a valid pool
    await tests.RegisterPoolAsync_ValidPool_ReturnsPoolId();
    
    // Verify retrieval of the same pool
    await tests.GetPoolAsync_ExistingPool_ReturnsPool();
    
    // Verify that a non-existent pool returns null
    await tests.GetPoolAsync_NonexistentPool_ReturnsNull();
}
```

## Notes

- **Edge cases**: The test methods do not accept parameters; they rely on internal setup (e.g., mock services, predefined invalid pools). Ensure that the test infrastructure (e.g., `ITestOutputHelper`, mock repositories) is correctly initialized in the constructor or via test fixtures.
- **Thread safety**: Each test method is independent and can be run in parallel with other tests in the same class, provided the underlying `UpstreamManagerService` implementation is stateless or properly isolated per test. The class itself does not maintain shared state between tests.
- **Exception expectations**: Tests that verify exceptions use `Assert.ThrowsAsync` or equivalent. The exact exception types (`ArgumentNullException`, `ServiceConfigurationException`, `ServiceNotFoundException`) must be defined in the project's exception hierarchy.
- **Asynchronous behavior**: All test methods are `async Task` and should be awaited. The test runner must support asynchronous test execution (e.g., xUnit, NUnit 3+).
