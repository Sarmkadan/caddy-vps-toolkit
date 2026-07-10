# ArgumentParserEdgeCaseTestsExtensions

Provides extension methods for testing edge cases in argument parsing scenarios, particularly focusing on command, positional argument, and flag handling in argument parser implementations.

## API

### `GetCommand_ShouldReturnExpected`

Ensures that the argument parser correctly identifies and returns the expected command from the input arguments. This test verifies that the parser does not misinterpret positional arguments or flags as commands and that it handles empty or malformed command inputs appropriately.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions are thrown under normal test conditions.

### `GetPositional_ShouldHandleNegativeIndices`

Validates that the argument parser correctly processes positional arguments when accessed via negative indices (e.g., `-1` for the last argument). This test ensures that the parser does not throw exceptions or return incorrect values when negative indices are used, and that it behaves consistently with positive indices.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions are thrown under normal test conditions.

### `GetFlagValue_ShouldWorkWithHasFlag`

Confirms that the argument parser correctly retrieves flag values and integrates with the `HasFlag` method. This test ensures that flag values are parsed and stored correctly, and that `HasFlag` returns accurate results based on the parsed input.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions are thrown under normal test conditions.

### `HasFlag_ShouldHandleMultipleFlags`

Tests that the argument parser correctly identifies and handles multiple flags in the input arguments. This method ensures that the parser does not confuse flags with other argument types and that it accurately reports the presence of multiple flags.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions are thrown under normal test conditions.

## Usage
