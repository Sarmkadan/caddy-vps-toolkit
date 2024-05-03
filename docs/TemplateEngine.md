# TemplateEngine

`TemplateEngine` provides a lightweight, string-based templating mechanism that stores named values and renders template strings by substituting placeholders with their corresponding stored values. It supports both instance-based and static rendering scenarios, allowing callers to either build up a set of substitutions over time or perform one-shot template expansion.

## API

### `TemplateEngine()` (parameterless constructor)
Creates a new, empty `TemplateEngine` instance with no predefined substitutions.

### `TemplateEngine(object values)`
Creates a new `TemplateEngine` instance and populates it with the properties of the provided object. Each public property name becomes a key, and its value becomes the associated substitution value.

**Parameters:**
- `values` — An object whose public properties will be read as key/value pairs for template substitution.

**Throws:**
- `ArgumentNullException` — if `values` is `null`.

### `void Set(string key, object value)`
Adds or updates a substitution entry in the engine. If the key already exists, its value is overwritten.

**Parameters:**
- `key` — The placeholder name to set (case-sensitive).
- `value` — The value to associate with the key. `null` is permitted and will be converted to an empty string during rendering.

**Throws:**
- `ArgumentNullException` — if `key` is `null`.

### `object Get(string key)`
Retrieves the raw value stored for a given key.

**Parameters:**
- `key` — The placeholder name whose value is to be retrieved (case-sensitive).

**Returns:**
- The stored `object` value, or `null` if the key has not been set.

**Throws:**
- `ArgumentNullException` — if `key` is `null`.

### `string Render(string template)`
Renders a template string by replacing all occurrences of `{{key}}` placeholders with the corresponding values stored in this instance. Placeholders that do not match any stored key are left unchanged in the output.

**Parameters:**
- `template` — The template string containing zero or more `{{key}}` placeholders.

**Returns:**
- The rendered string with all matching placeholders substituted.

**Throws:**
- `ArgumentNullException` — if `template` is `null`.

### `static string Render(string template, object values)`
A static convenience method that creates a temporary `TemplateEngine` from the given object and renders the template in a single call. Equivalent to `new TemplateEngine(values).Render(template)`.

**Parameters:**
- `template` — The template string containing zero or more `{{key}}` placeholders.
- `values` — An object whose public properties supply the substitution values.

**Returns:**
- The rendered string with all matching placeholders substituted.

**Throws:**
- `ArgumentNullException` — if `template` or `values` is `null`.

## Usage

### Example 1: Instance-based rendering with incremental setup
```csharp
var engine = new TemplateEngine();
engine.Set("name", "Alice");
engine.Set("role", "Administrator");

string template = "User {{name}} has the {{role}} role.";
string result = engine.Render(template);

Console.WriteLine(result);
// Output: User Alice has the Administrator role.
```

### Example 2: One-shot static rendering from an anonymous object
```csharp
string template = "Server {{host}} is listening on port {{port}}.";
string result = TemplateEngine.Render(template, new
{
    host = "mail.example.com",
    port = 587
});

Console.WriteLine(result);
// Output: Server mail.example.com is listening on port 587.
```

## Notes

- Placeholder syntax is strictly `{{key}}` with no whitespace between the braces and the key name. Keys are case-sensitive; `{{Name}}` and `{{name}}` are distinct.
- If a placeholder references a key that has not been set via `Set` or does not exist as a property on the object passed to the constructor or static `Render`, the placeholder is preserved verbatim in the output.
- Setting a value to `null` causes the placeholder to be replaced with an empty string during rendering. Retrieving it via `Get` returns `null`.
- The parameterless constructor and the `Set` method allow mutation of the engine's state over time. This state is not thread-safe; concurrent calls to `Set` and `Render` on the same instance must be externally synchronized.
- The static `Render` method is thread-safe, as it creates a new engine instance per call and does not share state.
- The object-based constructor and the static `Render` method rely on reflection to enumerate public properties at the time of construction. Subsequent changes to the source object are not reflected in the engine's internal state.
