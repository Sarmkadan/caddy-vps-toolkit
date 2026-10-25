# ConfigurationFinding

`ConfigurationFinding` is a single result produced by configuration validation. It pairs a severity level with a human-readable message describing the issue. Validation routines such as `CaddyConfigurationService.ValidateConfiguration` return a list of these objects, which callers can then display, log, or filter by severity.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `Severity` | `string` | Severity level of the finding, such as `"error"`, `"warning"`, or `"info"`. Defaults to `"error"`. | — | The severity string as assigned. | None. |
| `Message` | `string` | Human-readable description of the issue. Defaults to `string.Empty`. | — | The message text. | None. |
| `ToString` | `string` | Produces a compact, log-friendly representation of the finding. | — | A string formatted as `"[SEVERITY] Message"`, where `SEVERITY` is `Severity` converted to upper case (e.g., `"[WARNING] Non-localhost domain ..."`). | `NullReferenceException` if `Severity` has been set to `null`. |

## Usage

### Example 1: Creating a finding

```csharp
using CaddyVpsToolkit.Domain.Models;

var finding = new ConfigurationFinding
{
    Severity = "error",
    Message = "Duplicate site address detected: example.com"
};

Console.WriteLine(finding.Severity); // error
Console.WriteLine(finding.Message);  // Duplicate site address detected: example.com
```

Because both properties have defaults, a finding created without an explicit `Severity` is an `"error"`:

```csharp
var defaulted = new ConfigurationFinding { Message = "Upstream URL is empty" };

Console.WriteLine(defaulted.Severity); // error
```

### Example 2: Printing findings returned by validation

```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;

List<ConfigurationFinding> findings = configurationService.ValidateConfiguration(globalConfig, routes);

foreach (var finding in findings)
{
    Console.WriteLine(finding); // e.g. [ERROR] Route 'api.example.com' has an empty upstream URL
}
```

### Example 3: Filtering findings by severity

```csharp
using System.Collections.Generic;
using System.Linq;
using CaddyVpsToolkit.Domain.Models;

IEnumerable<ConfigurationFinding> blocking = findings
    .Where(f => f.Severity == "error");

if (blocking.Any())
{
    Console.Error.WriteLine($"Configuration has {blocking.Count()} blocking issue(s).");
}
```

Severity values are plain strings, so comparisons are case-sensitive. The validation code in `CaddyConfigurationService` uses lower-case values (`"error"` and `"warning"`), so match those exactly or compare with `StringComparison.OrdinalIgnoreCase`.

## Notes

- `Severity` is not restricted to a fixed set of values. The type does not validate it, so callers that need a closed set of levels should check the value themselves.
- `ToString` converts `Severity` with `ToUpper()`, which uses the current culture. If `Severity` is `null`, `ToString` throws `NullReferenceException`. `Severity` defaults to `"error"` and is non-nullable in the declared type, so this only happens when a caller explicitly assigns `null`.
- `Message` is rendered as-is. A `null` message is rendered as an empty string by string interpolation, so `ToString` does not throw in that case.
- The type is a mutable POCO with no `Validate` method. Instances are not thread-safe for concurrent writes.
