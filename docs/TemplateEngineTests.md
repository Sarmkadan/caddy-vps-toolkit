# TemplateEngineTests

`TemplateEngineTests` is the unit test suite for the `TemplateEngine` class in the `caddy-vps-toolkit` project. It validates template rendering behaviour, variable substitution logic, error handling for invalid inputs, and the behaviour of the static and instance-based APIs. The tests cover both the dictionary-backed constructor and the imperative `Set`/`Get` methods.

## API

All members are public test methods returning `void`. They follow the NUnit/xUnit-style convention of describing the scenario and expected outcome in the method name. No parameters are accepted, and no values are returned. Assertions within each method verify correctness; failures surface as test runner exceptions.

### `Render_SingleVariable_SubstitutesCorrectly`
Verifies that when a template contains exactly one placeholder and the corresponding variable is defined, the rendered output replaces the placeholder with the variable’s value.

### `Render_MultipleVariables_SubstitutesAllPlaceholders`
Confirms that a template with several distinct placeholders has every placeholder replaced by its respective value, with no leftover markers and no cross-substitution.

### `Render_UnknownVariable_LeavesPlaceholderIntact`
Ensures that a placeholder referencing a variable not present in the engine’s state is left unchanged in the output, rather than being removed or replaced with an empty string.

### `Render_NullTemplate_ReturnsNull`
Validates that passing a `null` template string to `Render` returns `null` immediately, without throwing an exception.

### `Render_EmptyTemplate_ReturnsEmptyString`
Checks that rendering a zero-length string produces a zero-length result, preserving the invariant that an empty input yields an empty output.

### `Render_NoPlaceholders_ReturnsTemplateUnchanged`
Demonstrates that a template string containing no placeholder syntax is returned verbatim, with no modification or whitespace alteration.

### `Render_StaticOverload_SubstitutesFromDictionary`
Exercises the static `Render` method that accepts a template string and a `Dictionary<string, string>`. It confirms that placeholders are resolved using the supplied dictionary without requiring an engine instance.

### `Set_EmptyKey_ThrowsArgumentException`
Ensures that calling `Set` with an empty string key throws an `ArgumentException`, preventing empty placeholder names from entering the variable store.

### `Set_NullKey_ThrowsArgumentException`
Ensures that calling `Set` with a `null` key throws an `ArgumentException`, guarding against null-reference placeholder identifiers.

### `Get_ExistingKey_ReturnsValue`
Verifies that `Get` called with a key previously added via `Set` returns the exact value that was stored.

### `Get_MissingKey_ReturnsNull`
Confirms that `Get` for a key never added returns `null`, distinguishing missing variables from variables explicitly set to an empty string.

### `Render_VariableWithNullValue_SubstitutesEmptyString`
Tests the edge case where a variable’s value is explicitly `null`. The placeholder is replaced with an empty string (`""`) rather than the literal text “null” or throwing an exception.

### `Constructor_WithDictionary_UsesProvidedVariables`
Validates that constructing a `TemplateEngine` with a `Dictionary<string, string>` seeds the engine with those variables, so that a subsequent `Render` call resolves placeholders from the supplied dictionary.

## Usage

```csharp
// Example 1: Instance-based rendering with explicit variable management
var engine = new TemplateEngine();
engine.Set("hostname", "web01");
engine.Set("domain", "example.com");

string template = "server { listen 80; server_name {{hostname}}.{{domain}}; }";
string result = engine.Render(template);
// result: "server { listen 80; server_name web01.example.com; }"

// Verify behaviour when a variable is missing
string unknown = engine.Render("Welcome to {{sitename}}");
// unknown: "Welcome to {{sitename}}"
```

```csharp
// Example 2: Static rendering from a pre-built dictionary
var variables = new Dictionary<string, string>
{
    ["port"] = "443",
    ["tls"] = "on"
};

string template = "listen {{port}} ssl {{tls}};";
string result = TemplateEngine.Render(template, variables);
// result: "listen 443 ssl on;"

// Null-value substitution
variables["tls"] = null;
string nullResult = TemplateEngine.Render(template, variables);
// nullResult: "listen 443 ssl ;"
```

## Notes

- **Placeholder syntax**: The tests imply a placeholder format such as `{{variableName}}`. Placeholders that do not match any known key are preserved literally in the output.
- **Null vs. missing**: A variable set to `null` produces an empty string upon rendering. A variable never set leaves the placeholder intact. These are intentionally distinct behaviours.
- **Empty keys**: Both `null` and empty-string keys are rejected by `Set` with `ArgumentException`. Consumers should validate placeholder names before storage.
- **Static method**: The static `Render` overload operates on a dictionary snapshot; it does not mutate any shared state and is safe for concurrent callers.
- **Thread safety**: The instance methods (`Set`, `Get`, `Render`) are tested in isolation. Unless the implementation explicitly synchronises access, concurrent reads and writes to the same engine instance should be externally serialised.
- **Constructor seeding**: When a dictionary is passed to the constructor, the engine takes a copy or retains the reference depending on implementation. Tests confirm that values provided at construction are immediately available for rendering.
