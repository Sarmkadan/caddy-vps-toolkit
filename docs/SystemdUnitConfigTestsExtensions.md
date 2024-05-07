# SystemdUnitConfigTestsExtensions

The `SystemdUnitConfigTestsExtensions` static class provides a set of fluent assertion extension methods designed to simplify and standardize unit testing for `SystemdUnitConfig` objects within the `caddy-vps-toolkit`. By encapsulating common validation and content generation checks, these extensions reduce boilerplate code in test suites and ensure consistent verification of systemd unit configuration states.

## API

### Validate_ShouldNotThrow
Verifies that the `Validate()` method of the `SystemdUnitConfig` instance executes without throwing an exception.

*   **Parameters:** `this SystemdUnitConfig config`
*   **Returns:** `void`
*   **Throws:** Throws an `AssertException` if validation fails.

### Validate_ShouldThrowWithMessage
Verifies that the `Validate()` method of the `SystemdUnitConfig` instance throws an exception with the expected error message.

*   **Parameters:** `this SystemdUnitConfig config`, `string expectedMessage`
*   **Returns:** `void`
*   **Throws:** Throws an `AssertException` if validation succeeds or if the exception message does not match.

### GenerateSystemdContent_ShouldContainDirectives
Verifies that the `GenerateSystemdContent()` method of the `SystemdUnitConfig` instance produces output containing the specified systemd directives.

*   **Parameters:** `this SystemdUnitConfig config`, `params string[] directives`
*   **Returns:** `void`
*   **Throws:** Throws an `AssertException` if one or more specified directives are missing from the generated content.

### GenerateSystemdContent_ShouldHaveCorrectStructure
Verifies that the `GenerateSystemdContent()` method of the `SystemdUnitConfig` instance produces output that adheres to the expected systemd unit file structural requirements (e.g., valid section headers and key-value pairings).

*   **Parameters:** `this SystemdUnitConfig config`
*   **Returns:** `void`
*   **Throws:** Throws an `AssertException` if the structural integrity of the generated content is invalid.

## Usage

```csharp
[Fact]
public void Should_HaveValidConfiguration()
{
    var config = new SystemdUnitConfig { ServiceName = "caddy" };
    
    // Verifies no validation errors
    config.Validate_ShouldNotThrow();
}

[Fact]
public void Should_ContainRequiredDirectives()
{
    var config = new SystemdUnitConfig { ServiceName = "caddy" };
    
    // Verifies generated content structure and specific directives
    config.GenerateSystemdContent_ShouldHaveCorrectStructure();
    config.GenerateSystemdContent_ShouldContainDirectives("ExecStart=", "Restart=");
}
```

## Notes

*   **Edge Cases:** These extension methods rely on the internal `Validate()` and `GenerateSystemdContent()` implementations of the `SystemdUnitConfig` class. If the underlying configuration object is in an uninitialized or corrupt state, these methods may throw unexpected exceptions outside of the expected assertion failures.
*   **Thread Safety:** The extension methods themselves are stateless and thread-safe. However, they operate on the `SystemdUnitConfig` instance provided. If the `SystemdUnitConfig` instance is being modified by multiple threads simultaneously during testing, the results of these assertions may be non-deterministic. It is recommended to perform these tests on isolated, thread-local configuration instances.
