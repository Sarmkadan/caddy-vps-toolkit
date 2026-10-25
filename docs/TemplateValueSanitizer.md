# TemplateValueSanitizer

`Utilities/TemplateValueSanitizer.cs` provides a static helper in the `CaddyVpsToolkit.Utilities` namespace that validates values before they are written into a Caddyfile template. It guards against template injection: a value containing braces or line breaks could close a Caddy block, open a new directive, or otherwise change the generated configuration.

The class is used by [TemplateEngine](TemplateEngine.md) in `SetCaddyValue`, which sanitizes a value before storing it. Callers that build Caddyfile fragments by hand can call the same method directly.

## API

### `TemplateValueSanitizer`

```csharp
public static class TemplateValueSanitizer
```

Static class with no state and no constructor. It cannot be instantiated.

### `SanitizeCaddyValue`

```csharp
public static string SanitizeCaddyValue(string value)
```

Validates a string for use in a Caddyfile and returns it unchanged when it is safe.

The method rejects any value that contains at least one of the following characters:

- Any control character, as defined by `char.IsControl` (this includes `\n`, `\r`, and tab).
- `{` or `}`.
- `\n` or `\r`, checked explicitly in addition to the control-character test.

The value is never modified. The method only accepts or rejects it.

**Parameters**
- `value` — the string to validate.

**Returns**
The same `value` instance when it contains no forbidden characters. A `null` or empty string is returned as-is without validation.

**Throws**
- `ArgumentException` when `value` contains a forbidden character. The exception message names the first offending character and includes the full value.

> **Note:** The exception message includes the complete value. Avoid logging it if the value may contain secrets such as passwords or tokens.

---

## Usage Examples

### Accepting a safe value

```csharp
using CaddyVpsToolkit.Utilities;

string host = TemplateValueSanitizer.SanitizeCaddyValue("api.example.com");
// host == "api.example.com"

string upstream = TemplateValueSanitizer.SanitizeCaddyValue("localhost:8080");
// upstream == "localhost:8080"
```

### Null and empty input

```csharp
string? missing = TemplateValueSanitizer.SanitizeCaddyValue(null!);
// missing == null

string empty = TemplateValueSanitizer.SanitizeCaddyValue(string.Empty);
// empty == string.Empty
```

Null and empty values pass through unchanged. Callers that require a non-empty value must check for that separately.

### Rejecting an injection attempt

```csharp
using System;
using CaddyVpsToolkit.Utilities;

try
{
    // A closing brace followed by a new directive would break out of the site block.
    TemplateValueSanitizer.SanitizeCaddyValue("example.com } respond \"pwned\" {");
}
catch (ArgumentException ex)
{
    // ex.Message: "Invalid character '}' in Caddyfile template value: example.com } respond ..."
    Console.Error.WriteLine(ex.Message);
}
```

### Rejecting a multi-line value

```csharp
try
{
    TemplateValueSanitizer.SanitizeCaddyValue("example.com\nreverse_proxy evil:9000");
}
catch (ArgumentException)
{
    // Newlines are rejected, so a value cannot start a new Caddyfile line.
}
```

### Validating before a template is rendered

```csharp
public static string BuildSiteBlock(string domain, string upstream)
{
    string safeDomain = TemplateValueSanitizer.SanitizeCaddyValue(domain);
    string safeUpstream = TemplateValueSanitizer.SanitizeCaddyValue(upstream);

    return $"{safeDomain} {{\n    reverse_proxy {safeUpstream}\n}}";
}
```

Sanitize every interpolated value before it reaches the template string. If any value is invalid, the method throws and no partial output is produced.

---

## Behaviour Notes

- **Validation only.** The method does not escape or rewrite characters. A value that contains a forbidden character is rejected entirely, so there is no silent truncation or substitution.
- **Stateless and thread-safe.** The method reads only its argument and can be called concurrently.
- **Scope.** The check targets Caddyfile syntax. It does not validate that a value is a well-formed domain name, URL, or port.
- **Character set.** Characters outside the control and brace set, such as spaces, quotes, and `#`, are accepted. Callers that place values inside quoted Caddy arguments may need stricter checks of their own.
