# ValidationHelper

`ValidationHelper` provides a fluent, composable API for performing multiple validation checks and aggregating their results. It exposes static factory methods that produce `ValidationResult` instances, each representing either success or a list of error messages. These results can be combined to evaluate complex validation scenarios in a single pass, with the final outcome accessible through instance members on the combined result.

## API

### Static Methods

#### `ValidatePort`
Validates that a string represents a valid network port number (1–65535). Returns `Success` if the input can be parsed as an integer within the allowed range; otherwise returns `Failure` with an appropriate error message.

**Parameters:**
- `string input` — The value to validate.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateDomain`
Validates that a string conforms to domain name syntax rules (length limits, allowed characters, label format). Returns `Success` for a well-formed domain; otherwise `Failure` with a descriptive error.

**Parameters:**
- `string domain` — The domain name to validate.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateFilePath`
Validates that a string represents a well-formed file path and that the path meets application-specific constraints (e.g., no invalid characters, acceptable root). Returns `Success` if the path is valid; otherwise `Failure`.

**Parameters:**
- `string path` — The file path to validate.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateServiceName`
Validates that a string is acceptable as a system service name (typically alphanumeric with hyphens, within length limits). Returns `Success` if the name is valid; otherwise `Failure`.

**Parameters:**
- `string serviceName` — The service name to validate.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateRange`
Validates that an integer falls within a specified inclusive range.

**Parameters:**
- `int value` — The value to check.
- `int min` — The inclusive lower bound.
- `int max` — The inclusive upper bound.
- `string fieldName` — A label for the field, used in error messages.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateNotNull<T>`
Validates that a reference is not `null`.

**Parameters:**
- `T value` — The object to check.
- `string fieldName` — A label for the field, used in error messages.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `ValidateNotEmpty`
Validates that a string is not `null`, empty, or whitespace.

**Parameters:**
- `string value` — The string to check.
- `string fieldName` — A label for the field, used in error messages.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `Combine`
Aggregates multiple `ValidationResult` instances into a single result. The combined result is successful only if all constituent results are successful; otherwise it contains the concatenated error messages from every failed result.

**Parameters:**
- `params ValidationResult[] results` — One or more validation results to combine.

**Returns:** `ValidationResult`

**Throws:** Does not throw.

---

#### `Success`
A static property that returns a pre-built successful `ValidationResult` with no errors.

**Returns:** `ValidationResult`

---

#### `Failure`
A static factory that creates a failed `ValidationResult` with the given error message.

**Parameters:**
- `string error` — The error message.

**Returns:** `ValidationResult`

---

### Instance Members (on `ValidationResult`)

#### `IsValid`
Indicates whether the result represents successful validation. `true` if no errors are present; `false` otherwise.

**Type:** `bool` (read-only)

---

#### `Errors`
The list of error messages accumulated during validation. Empty when `IsValid` is `true`.

**Type:** `List<string>` (read-only)

---

#### `GetErrorMessage`
Returns all error messages joined into a single string, typically separated by newlines or a delimiter suitable for display.

**Returns:** `string`

**Throws:** Does not throw.

---

## Usage

### Example 1: Validating Service Configuration Input

```csharp
public ValidationResult ValidateServiceConfig(string port, string domain, string serviceName)
{
    return ValidationHelper.Combine(
        ValidationHelper.ValidatePort(port),
        ValidationHelper.ValidateDomain(domain),
        ValidationHelper.ValidateServiceName(serviceName)
    );
}

// Consuming the result
var result = ValidateServiceConfig("443", "example.com", "my-service");
if (!result.IsValid)
{
    Console.WriteLine("Configuration errors:");
    Console.WriteLine(result.GetErrorMessage());
}
```

### Example 2: Multi-Field Form Validation with Range and NotEmpty

```csharp
public ValidationResult ValidateFormInput(string username, int age, string filePath)
{
    var results = new[]
    {
        ValidationHelper.ValidateNotEmpty(username, "Username"),
        ValidationHelper.ValidateNotNull(username, "Username"),
        ValidationHelper.ValidateRange(age, 18, 120, "Age"),
        ValidationHelper.ValidateFilePath(filePath)
    };

    return ValidationHelper.Combine(results);
}

// Early-exit pattern
var formResult = ValidateFormInput("", 17, "/invalid/path");
if (!formResult.IsValid)
{
    foreach (var error in formResult.Errors)
    {
        Log.Error(error);
    }
    throw new ValidationException(formResult.GetErrorMessage());
}
```

## Notes

- **Immutability:** Each `ValidationResult` is immutable once created. `Combine` produces a new instance; it does not modify the originals.
- **Thread Safety:** All static methods and the `Success` property are thread-safe. Individual `ValidationResult` instances are safe for concurrent reads. No shared mutable state is used.
- **Error Accumulation:** `Combine` preserves all error messages from failed inputs. Ordering of messages follows the order of arguments passed to `Combine`.
- **Null Handling in Combine:** Passing `null` instead of a `ValidationResult` to `Combine` will typically cause a `NullReferenceException` at the call site. Defensive callers should ensure all arguments are non-null.
- **Empty Collections:** Calling `Combine` with zero arguments produces a successful result (vacuously true).
- **Field Names:** Methods accepting a `fieldName` parameter use it solely for constructing human-readable error messages. They do not perform reflection or metadata lookup.
- **Customisation:** The `Failure` static method allows callers to inject domain-specific error messages into the validation pipeline, enabling custom checks to participate in `Combine` seamlessly.
