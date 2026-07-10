# ArgumentParserEdgeCaseTests

Unit tests for the `ArgumentParser` class, focusing on edge cases and boundary conditions in command-line argument parsing. These tests validate behavior when inputs are null, empty, malformed, or contain unexpected combinations of flags and positional arguments.

## API

### `Constructor_NullArgs_DoesNotThrow`
Ensures the `ArgumentParser` constructor does not throw exceptions when initialized with null arguments. This test verifies defensive programming practices in the parser's initialization logic.

### `GetCommand_EmptyArgs_ReturnsEmptyString`
Confirms that when no arguments are provided, the `GetCommand` method returns an empty string rather than throwing or returning null.

### `GetCommand_SingleArg_ReturnsLowercasedCommand`
Validates that a single argument is treated as the command and is returned in lowercase, ensuring consistent command normalization.

### `GetPositional_OutOfBounds_ReturnsNull`
Checks that accessing a positional argument with an index outside the valid range returns `null` instead of throwing an exception, supporting safe access patterns.

### `GetPositional_ValidIndex_ReturnsArgument`
Ensures that accessing a positional argument with a valid index returns the expected argument value without modification.

### `GetFlagValue_NullFlagName_ReturnsNull`
Verifies that requesting the value of a flag with a `null` name returns `null`, avoiding null reference exceptions in client code.

### `HasFlag_NullFlagName_ReturnsFalse`
Confirms that checking for the presence of a flag with a `null` name returns `false`, maintaining consistent behavior with invalid inputs.

### `GetFlagValue_BooleanFlag_ReturnsEmptyStringWhenPresent`
Tests that a boolean flag (e.g., `--verbose`) with no value returns an empty string when present, adhering to common CLI conventions.

### `GetFlagValue_BooleanFlag_ReturnsNullWhenAbsent`
Ensures that querying the value of a boolean flag that is not present returns `null`, allowing callers to distinguish absence from presence with no value.

### `GetFlagValue_EqualsFormat_ParsesCorrectly`
Validates correct parsing of flags in the format `--flag=value`, ensuring the parser correctly extracts the value from the equals-separated format.

### `GetFlagValue_SpaceFormat_ParsesCorrectly`
Confirms correct parsing of flags in the format `--flag value`, ensuring the parser correctly associates the value with the flag when separated by whitespace.

### `GetFlagValue_ValueFlag_LastArgWithoutValue_ReturnsEmptyString`
Tests that when a value-taking flag is the last argument and lacks a value, the parser returns an empty string rather than throwing or returning `null`.

### `HasFlag_PresentFlag_ReturnsTrue`
Ensures that `HasFlag` returns `true` when the specified flag is present in the argument list, regardless of whether it has a value.

### `HasFlag_AbsentFlag_ReturnsFalse`
Confirms that `HasFlag` returns `false` when the specified flag is not present in the argument list.

### `GetFlagValue_ValueFollowedByAnotherFlag_ReturnsEmptyString`
Validates that when a value-taking flag is immediately followed by another flag (e.g., `--path /tmp --verbose`), the value is correctly parsed and the second flag is treated as a separate entity.

### `HasFlag_CaseInsensitiveBooleanFlags`
Ensures that boolean flag detection is case-insensitive, allowing variations like `--Verbose`, `--VERBOSE`, or `--verbose` to be treated equivalently.

## Usage
