# ArgumentParserTests

The `ArgumentParserTests` class provides a comprehensive test suite for the command-line argument parsing logic within the `caddy-vps-toolkit`. It validates that input commands, including those with mixed-case formatting, are correctly normalized, and that flag values are accurately extracted across different syntax variations such as space-separated and equals-sign-separated formats. Furthermore, the class includes test scenarios for service repository interactions to ensure correct data retrieval operations through mocked dependencies.

## API

### GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand
Verifies that the command-line parser correctly normalizes mixed-case command input to lowercase.
*   **Parameters:** None.
*   **Returns:** `void`.
*   **Throws:** None.

### GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly
Validates the parser's capability to correctly extract values from flags provided in the `--flag=value` format.
*   **Parameters:** None.
*   **Returns:** `void`.
*   **Throws:** None.

### GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly
Validates the parser's capability to correctly extract values from flags provided in the `--flag value` format.
*   **Parameters:** None.
*   **Returns:** `void`.
*   **Throws:** None.

### ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService
Verifies the asynchronous retrieval of a service entity by its unique identifier, ensuring correct interaction with the `ServiceRepository` when utilizing mocked dependencies.
*   **Parameters:** None.
*   **Returns:** `Task`.
*   **Throws:** Throws assertion exceptions if the retrieved service does not match the expected result or if the repository interaction fails.

## Usage

```csharp
// Example 1: Executing the argument parser flag tests using an xUnit runner
var testSuite = new ArgumentParserTests();
testSuite.GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly();
testSuite.GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly();

// Example 2: Running the asynchronous repository test
var testSuite = new ArgumentParserTests();
await testSuite.ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService();
```

## Notes

*   **Edge Cases:** The parser tests assume standard valid input formats. Behavior with malformed arguments, such as missing values or unsupported flag structures, is not explicitly covered by these members and should be validated in additional test cases.
*   **Thread Safety:** The test methods within this class are generally designed to be executed sequentially by test runners (e.g., NUnit, xUnit). They are not inherently thread-safe if state is shared across test instances, and parallel execution of these tests should be avoided if they modify shared mock repositories or static parser state.
*   **Dependencies:** The `ServiceRepository_GetByIdAsync` test depends on an appropriate mocking framework being configured to intercept repository calls and return the expected service object.
