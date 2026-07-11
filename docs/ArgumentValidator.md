# ArgumentValidator

The `ArgumentValidator` class provides a lightweight, fluent-style mechanism for validating method arguments and collecting error messages. It is designed to be used within a single validation scope, where the caller invokes `Validate` to perform checks, then inspects `IsValid` and `Errors` to determine the outcome. A formatted summary of all errors can be retrieved via `GetErrorMessage`.

## API

### `public ValidationResult Validate`

Performs the validation logic defined by the concrete implementation. The exact checks are determined by the subclass or by the delegate passed at construction (depending on the internal design). This method must be called before inspecting `IsValid` or `Errors` to ensure the validation state is populated.

- **Parameters**: None.
- **Returns**: A `ValidationResult` object that encapsulates the outcome of the validation. The result can be used for further programmatic inspection or chaining.
- **Throws**: `InvalidOperationException` if `Validate` is called more than once on the same instance, or if the validator has already been finalized.

### `public bool IsValid`

Indicates whether the validation succeeded (i.e., no errors were recorded). The value is only meaningful after `Validate` has been called.

- **Parameters**: None.
- **Returns**: `true` if no errors were found; otherwise `false`.
- **Throws**: `InvalidOperationException` if accessed before `Validate` is called.

### `public List<string> Errors`

Contains the list of error messages accumulated during validation. Each entry corresponds to a single validation failure. The list is read-only after `Validate` completes; modifying it externally may lead to undefined behavior.

- **Parameters**: None.
- **Returns**: A `List<string>` of error messages. The list is empty if validation succeeded.
- **Throws**: `InvalidOperationException` if accessed before `Validate` is called.

### `public string GetErrorMessage`

Returns a single, human-readable string that concatenates all validation errors, typically separated by newlines or a configurable delimiter. This is a convenience method for producing a summary suitable for logging or exception messages.

- **Parameters**: None.
- **Returns**: A `string` containing all error messages. Returns an empty string if `IsValid` is `true`.
- **Throws**: `InvalidOperationException` if accessed before `Validate` is called.

## Usage

### Example 1: Validating a required string argument

```csharp
public void ProcessName(string name)
{
    var validator = new ArgumentValidator();
    validator.Validate(() =>
    {
        if (string.IsNullOrWhiteSpace(name))
            validator.Errors.Add("Name must not be null or whitespace.");
    });

    if (!validator.IsValid)
        throw new ArgumentException(validator.GetErrorMessage());

    // Continue processing...
}
```

### Example 2: Validating multiple arguments with custom rules

```csharp
public void Configure(int port, string host, bool useSsl)
{
    var validator = new ArgumentValidator();
    validator.Validate(() =>
    {
        if (port < 1 || port > 65535)
            validator.Errors.Add($"Port {port} is out of valid range (1-65535).");
        if (string.IsNullOrEmpty(host))
            validator.Errors.Add("Host must be provided.");
        if (useSsl && port == 80)
            validator.Errors.Add("Cannot use SSL on port 80.");
    });

    if (!validator.IsValid)
    {
        // Log errors or throw
        throw new InvalidOperationException(validator.GetErrorMessage());
    }

    // Apply configuration...
}
```

## Notes

- **Validation must be triggered**: Accessing `IsValid`, `Errors`, or `GetErrorMessage` before calling `Validate` will throw an `InvalidOperationException`. Always call `Validate` first.
- **Single-use**: The `Validate` method is intended to be called exactly once per instance. Repeated calls may throw or produce undefined behavior.
- **Mutable state**: The `Errors` list is populated during validation and is not thread-safe. If the same `ArgumentValidator` instance is accessed from multiple threads concurrently, external synchronization is required.
- **Empty errors**: When `IsValid` is `true`, `Errors` is an empty list and `GetErrorMessage` returns an empty string. No special null handling is needed.
- **Error message format**: The exact formatting of `GetErrorMessage` is implementation-defined; it may use newlines, semicolons, or other delimiters. Do not rely on a specific delimiter for programmatic parsing.
