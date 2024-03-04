# CaddyConfigBenchmarks

The `CaddyConfigBenchmarks` class provides utility methods designed for performance testing and structural validation of Caddy configuration components within the `caddy-vps-toolkit`. It enables the generation of various path matcher strings, Caddyfile global directives, and provides mechanisms to validate configuration objects and route strings against established schema requirements. This class is primarily intended for use in benchmarking suites or automated testing environments that require consistent generation of Caddyfile fragments.

## API

### GenerateRoutePath_Simple
Generates a basic route path string based on the provided route identifier.
*   **Parameters:** `string route`
*   **Return:** A string representing the simple route path.
*   **Throws:** `ArgumentNullException` if `route` is null or empty.

### GenerateRoutePath_WithPath
Generates a route path string combined with a specific path component.
*   **Parameters:** `string route`, `string path`
*   **Return:** A string representing the combined route path.
*   **Throws:** `ArgumentNullException` if `route` or `path` is null or empty.

### GenerateCaddyfileGlobals
Generates the global configuration directive block required for a Caddyfile.
*   **Parameters:** None.
*   **Return:** A string containing the formatted global directives.

### ValidateConfig
Validates a configuration object against schema requirements.
*   **Parameters:** `object config`
*   **Return:** `void`
*   **Throws:** `InvalidOperationException` if the configuration is invalid. `ArgumentNullException` if `config` is null.

### ValidateRoute
Validates a route string to ensure it conforms to expected formatting rules.
*   **Parameters:** `string route`
*   **Return:** `void`
*   **Throws:** `ArgumentException` if the `route` is invalid.

### GetCaddyPathMatcher_Root
Retrieves the standard matcher string used for the root path (`/`).
*   **Parameters:** None.
*   **Return:** A string representing the root path matcher.

### GetCaddyPathMatcher_Prefixed
Retrieves the matcher string used for a specific prefixed path.
*   **Parameters:** `string prefix`
*   **Return:** A string representing the prefixed path matcher.
*   **Throws:** `ArgumentNullException` if `prefix` is null or empty.

## Usage

### Example 1: Basic Route Validation
```csharp
var benchmarks = new CaddyConfigBenchmarks();
string route = "/api/v1";

// Validate the route before proceeding
benchmarks.ValidateRoute(route);

// Generate the simple route path
string path = benchmarks.GenerateRoutePath_Simple(route);
Console.WriteLine($"Generated Path: {path}");
```

### Example 2: Generating Global Configs and Matchers
```csharp
var benchmarks = new CaddyConfigBenchmarks();

// Generate global directives
string globals = benchmarks.GenerateCaddyfileGlobals();
Console.WriteLine($"Global Config: {globals}");

// Generate a specific path matcher
string matcher = benchmarks.GetCaddyPathMatcher_Prefixed("/static");
Console.WriteLine($"Matcher: {matcher}");
```

## Notes

*   **Edge Cases:** Passing `null` or empty strings to methods requiring path or route identifiers will typically result in `ArgumentNullException` or `ArgumentException` depending on the specific implementation.
*   **Thread Safety:** The `CaddyConfigBenchmarks` class is not explicitly thread-safe. If multiple threads require access to these methods, they should either operate on separate instances of `CaddyConfigBenchmarks` or be protected by external synchronization primitives.
*   **Validation:** The `ValidateConfig` method performs a structural check. It does not verify the actual connectivity or functionality of the resulting Caddy configuration, only that it meets the structural requirements defined by the `caddy-vps-toolkit`.
