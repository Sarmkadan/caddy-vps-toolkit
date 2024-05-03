# StringExtensions

A utility class providing common string manipulation and validation extension methods for C# applications, particularly in web and configuration contexts.

## API

### `IsNullOrWhiteSpace`
Determines whether a string is null, empty, or consists only of white-space characters.

- **Parameters**: `string value` – the string to check.
- **Return value**: `bool` – `true` if the string is null, empty, or whitespace; otherwise, `false`.
- **Exceptions**: None.

### `ToTitleCase`
Converts the specified string to title case (e.g., "hello world" → "Hello World").

- **Parameters**: `string input` – the string to convert.
- **Return value**: `string` – the title-cased string, or `null` if the input is `null`.
- **Exceptions**: None.

### `ToKebabCase`
Converts the specified string to kebab-case (e.g., "HelloWorld" → "hello-world").

- **Parameters**: `string input` – the string to convert.
- **Return value**: `string` – the kebab-cased string, or `null` if the input is `null`.
- **Exceptions**: None.

### `ToCamelCase`
Converts the specified string to camelCase (e.g., "hello world" → "helloWorld").

- **Parameters**: `string input` – the string to convert.
- **Return value**: `string` – the camel-cased string, or `null` if the input is `null`.
- **Exceptions**: None.

### `Truncate`
Truncates a string to a specified maximum length and appends an ellipsis if truncated.

- **Parameters**:
  - `string input` – the string to truncate.
  - `int maxLength` – the maximum length of the resulting string (must be ≥ 0).
- **Return value**: `string` – the truncated string with ellipsis if needed, or `null` if the input is `null`.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `maxLength` is negative.

### `IsValidEmail`
Validates whether a string is a syntactically valid email address.

- **Parameters**: `string email` – the string to validate.
- **Return value**: `bool` – `true` if the string is a valid email; otherwise, `false`.
- **Exceptions**: None.

### `IsValidUrl`
Validates whether a string is a syntactically valid URL.

- **Parameters**: `string url` – the string to validate.
- **Return value**: `bool` – `true` if the string is a valid URL; otherwise, `false`.
- **Exceptions**: None.

### `IsNumeric`
Determines whether a string represents a numeric value (integer or decimal).

- **Parameters**: `string input` – the string to check.
- **Return value**: `bool` – `true` if the string is numeric; otherwise, `false`.
- **Exceptions**: None.

### `Repeat`
Repeats a string a specified number of times.

- **Parameters**:
  - `string input` – the string to repeat.
  - `int count` – the number of times to repeat the string (must be ≥ 0).
- **Return value**: `string` – the repeated string, or `null` if the input is `null`.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `count` is negative.

### `EscapeShell`
Escapes a string for safe use in shell commands by surrounding it with single quotes and escaping embedded single quotes.

- **Parameters**: `string input` – the string to escape.
- **Return value**: `string` – the escaped string, or `null` if the input is `null`.
- **Exceptions**: None.

### `SafeSubstring`
Returns a substring of the specified string, handling out-of-range indices gracefully.

- **Parameters**:
  - `string input` – the string to extract from.
  - `int startIndex` – the zero-based starting character position.
  - `int length` – the number of characters to return.
- **Return value**: `string` – the substring, or `null` if the input is `null`.
- **Exceptions**: None.

### `StartsWithAny`
Determines whether a string starts with any of the provided prefixes (case-sensitive).

- **Parameters**:
  - `string input` – the string to check.
  - `params string[] prefixes` – the prefixes to test against.
- **Return value**: `bool` – `true` if the string starts with any prefix; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `prefixes` is `null`.

## Usage
