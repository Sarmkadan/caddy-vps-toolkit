# CaddyConfigBenchmarksExtensions

The `CaddyConfigBenchmarksExtensions` class provides a suite of static utility methods designed to streamline the creation, management, and validation of Caddy server configurations within the `caddy-vps-toolkit`. These tools are specifically tailored for generating production-ready Caddyfile definitions and ensuring route configurations adhere to expected standards, facilitating consistent deployment and benchmarking processes.

## API

### CreateProductionConfig
Creates a `CaddyConfig` instance pre-configured with default settings suitable for a production environment.

*   **Parameters**: 
    *   `string host`: The primary hostname for the configuration.
    *   `int port`: The port on which the Caddy server should listen.
*   **Returns**: A `CaddyConfig` object initialized for production usage.
*   **Throws**: `ArgumentException` if the `host` is null or empty, or if the `port` is invalid.

### CreateApiRoute
Generates a `CaddyRoute` object configured specifically for API traffic, including default middleware settings for common API handling.

*   **Parameters**: 
    *   `string path`: The URL path pattern for the route.
    *   `string destinationUrl`: The target upstream URL for reverse proxying.
*   **Returns**: A configured `CaddyRoute` object.

### GenerateCaddyfile
Serializes a provided `CaddyConfig` object into its valid Caddyfile string representation.

*   **Parameters**: 
    *   `CaddyConfig config`: The configuration object to serialize.
*   **Returns**: A string containing the formatted Caddyfile content.

### ValidateRoutes
Performs a validation check on a collection of `CaddyRoute` objects to ensure structural integrity and logical consistency.

*   **Parameters**: 
    *   `IEnumerable<CaddyRoute> routes`: The routes to validate.
*   **Returns**: `void`
*   **Throws**: `InvalidConfigurationException` if any route fails validation checks, such as conflicting paths or malformed upstream definitions.

### GetPathMatchers
Retrieves a dictionary mapping standard path patterns to their corresponding matching logic used by the Caddy configuration engine.

*   **Returns**: A `Dictionary<string, string>` where keys represent match patterns and values describe the matching behavior.

## Usage

### Generating a Production Configuration
```csharp
using CaddyVpsToolkit;

var config = CaddyConfigBenchmarksExtensions.CreateProductionConfig("example.com", 443);
string caddyfile = CaddyConfigBenchmarksExtensions.GenerateCaddyfile(config);

Console.WriteLine(caddyfile);
```

### Creating and Validating API Routes
```csharp
using CaddyVpsToolkit;

var apiRoute = CaddyConfigBenchmarksExtensions.CreateApiRoute("/api/v1/*", "http://localhost:5000");
var routes = new List<CaddyRoute> { apiRoute };

try 
{
    CaddyConfigBenchmarksExtensions.ValidateRoutes(routes);
    // Proceed with further configuration
}
catch (InvalidConfigurationException ex)
{
    Console.WriteLine($"Route validation failed: {ex.Message}");
}
```

## Notes

*   **Thread Safety**: All methods within `CaddyConfigBenchmarksExtensions` are implemented as static stateless operations. They are safe to be called concurrently from multiple threads, provided that the objects passed as arguments are not simultaneously modified by other threads.
*   **Validation**: The `ValidateRoutes` method checks for logical conflicts within the route set (e.g., overlapping path definitions). It does not perform network-level connectivity checks to ensure the `destinationUrl` is reachable.
*   **Serialization**: `GenerateCaddyfile` assumes the `CaddyConfig` object passed to it is in a valid state. While it handles the translation to the Caddyfile format, it does not re-validate the entire configuration before serialization; calling `ValidateRoutes` prior to generation is recommended.
