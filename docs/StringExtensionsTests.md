# StringExtensionsTests

`StringExtensionsTests` provides a comprehensive suite of unit tests designed to verify the reliability and correctness of the `StringExtensions` static helper class within the `caddy-vps-toolkit` project. These tests ensure that core string manipulation operations—including kebab-case conversion, length truncation, email validation, and shell-safe character escaping—adhere to expected functional specifications and handle edge cases consistently across the application.

## API

### ToKebabCase_CamelCaseString_ReturnsLowercaseWithHyphens
Validates the conversion logic that transforms camelCase or PascalCase strings into lowercase, hyphen-separated (kebab-case) strings.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: Does not throw under normal execution; assertions fail if the transformation logic produces unexpected output.

### Truncate_StringLongerThanMaxLength_TruncatesWithDefaultSuffix
Verifies that strings exceeding a defined maximum length are truncated and appended with the default truncation suffix, maintaining compliance with length constraints.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: Does not throw under normal execution; assertions fail if the resulting string exceeds the allowed length or lacks the expected suffix.

### IsValidEmail_ValidEmailFormat_ReturnsTrue
Ensures that the email validation logic correctly identifies well-formed email addresses and returns a positive result, preventing false negatives for valid input.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: Does not throw under normal execution; assertions fail if a syntactically valid email is rejected.

### EscapeShell_StringWithSingleQuote_ProducesShellSafeOutput
Confirms that the shell escaping mechanism correctly sanitizes strings containing single quotes, ensuring they are rendered safe for inclusion in shell commands and preventing injection vulnerabilities.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: Does not throw under normal execution; assertions fail if single quotes remain unescaped in the output.

## Usage

### Running the Test Suite
The tests in this class are executed by the project's standard test runner. They can be invoked via the CLI to validate the integrity of string manipulation logic:

```bash
# Execute all tests within the project
dotnet test
```

### Extending the Test Class
When adding new test cases to this class, adhere to the established `Method_Scenario_ExpectedResult` naming convention to maintain consistency and clarity:

```csharp
[Fact]
public void ToKebabCase_NullString_ReturnsEmptyString()
{
    // Arrange
    string input = null;

    // Act
    string result = input.ToKebabCase();

    // Assert
    Assert.Equal(string.Empty, result);
}
```

## Notes

*   **Edge Cases**: The test suite covers standard operational scenarios; however, ensure additional test cases are added for null inputs, empty strings, and culture-specific string comparisons where applicable to the underlying `StringExtensions` implementation.
*   **Thread Safety**: These tests are designed to be run in isolation by a test runner. While the tests themselves do not share state, ensure that the `StringExtensions` implementation under test does not rely on mutable static state to remain thread-safe during concurrent test execution.
