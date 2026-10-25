# FormatterServiceCollectionExtensions

`FormatterServiceCollectionExtensions` provides one extension method on `IServiceCollection` that registers the built-in output formatters from the `CaddyVpsToolkit.Formatters` namespace. The class lives in the `CaddyVpsToolkit.DependencyInjection` namespace and is `public static`.

The formatters implement `IOutputFormatter`, which renders a list or a single item as a `string`. See [IOutputFormatter](IOutputFormatter.md) for the interface contract.

## API

### `AddFormatters`
```csharp
public static IServiceCollection AddFormatters(this IServiceCollection services)
```

Registers the four built-in formatter classes and the default `IOutputFormatter` mapping. All registrations use the transient lifetime, so each resolution creates a new instance.

Registered services:
- `TableFormatter`, `CsvFormatter`, `JsonFormatter`, `TextFormatter`, each as itself.
- `IOutputFormatter`, mapped to `TableFormatter`.

**Parameters:**
- `services` (`IServiceCollection`): The service collection that receives the registrations.

**Return value:**
- Returns the same `IServiceCollection` instance, so calls can be chained.

**Exceptions:**
- Throws `ArgumentNullException` if `services` is `null`.

**Known limitation:**
- `TableFormatter` has a constructor `TableFormatter(params string[] headers)`. The container cannot supply a `string[]`, so resolving `TableFormatter` or `IOutputFormatter` throws `InvalidOperationException` ("Unable to resolve service for type 'System.String[]'"). Resolving `CsvFormatter`, `JsonFormatter`, or `TextFormatter` works.
- Because the registration is part of the collection, `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })` fails for the whole collection, not only for the table formatter. Build the provider without validation when you call `AddFormatters`, or see the Notes below for the workaround.

---

## Usage

### Example 1: Registering the formatters at startup
```csharp
using CaddyVpsToolkit.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFormatters();

using var provider = services.BuildServiceProvider();

// Resolves without error
var json = provider.GetRequiredService<CaddyVpsToolkit.Formatters.JsonFormatter>();
string output = json.Format(new List<string> { "example.com", "api.example.com" });
```

### Example 2: Using TableFormatter without the container
The constructor takes the column headers, so construct it directly. This avoids the resolution failure described above. Each row is built from the item's public properties, in the order reflection returns them, so the headers should match those properties.
```csharp
using CaddyVpsToolkit.Formatters;

// Declared at namespace level; its properties match the headers
public sealed record ServiceRow(string Name, string Status);

var table = new TableFormatter("Name", "Status");
string text = table.Format(new List<ServiceRow>
{
    new("web", "running"),
    new("api", "stopped")
});
```

### Example 3: Resolving a formatter by its concrete type
```csharp
using CaddyVpsToolkit.Formatters;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFormatters();

using var provider = services.BuildServiceProvider();

// Each call returns a new instance (transient lifetime)
var csv = provider.GetRequiredService<CsvFormatter>();
var text = provider.GetRequiredService<TextFormatter>();
```

---

## Notes

- **Transient lifetime**: Every registration is transient. Formatters are stateless apart from `TableFormatter`'s header widths, which are computed in its constructor, so a fresh instance per resolution is safe.
- **Default formatter**: `IOutputFormatter` resolves to `TableFormatter` by design. Because `TableFormatter` cannot be constructed by the container, `IOutputFormatter` is also unresolvable. Do not rely on it until the constructor is changed to a form the container can satisfy.
- **Repeated calls**: Each call adds a new set of descriptors to the collection. Calling `AddFormatters` twice does not throw, but it duplicates the registrations.
- **Validation**: Calling `BuildServiceProvider` with `ValidateOnBuild = true` after `AddFormatters` throws, because the `IOutputFormatter` descriptor cannot be validated. Build the provider without validation, or resolve the formatters you need directly.
- **Scope**: The method only adds registrations. It does not start any service or modify configuration.
