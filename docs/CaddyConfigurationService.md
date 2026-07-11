# CaddyConfigurationService

The `CaddyConfigurationService` class provides methods for generating, reading, writing, and validating Caddyfile configurations, as well as constructing route blocks and JSON representations. It is designed to encapsulate the logic required to produce Caddy configuration artifacts from service definitions within the `caddy-vps-toolkit` project.

## API

### `CaddyConfigurationService()`
Initializes a new instance of the `CaddyConfigurationService`. No parameters are required.

### `async Task<string> GenerateCaddyfileAsync()`
Generates a complete Caddyfile configuration as a string.  
**Returns:** A `Task<string>` representing the generated Caddyfile content.  
**Throws:** `InvalidOperationException` if the service has not been properly configured with required data before calling this method.

### `async Task<bool> WriteCaddyfileAsync()`
Writes the currently generated Caddyfile to the default output path.  
**Returns:** `true` if the file was written successfully; otherwise `false`.  
**Throws:** `IOException` if a file system error occurs during writing.  
**Throws:** `InvalidOperationException` if no Caddyfile has been generated yet.

### `async Task<string> ReadCaddyfileAsync()`
Reads the Caddyfile from the default file location and returns its content as a string.  
**Returns:** A `Task<string>` containing the file content.  
**Throws:** `FileNotFoundException` if the Caddyfile does not exist.  
**Throws:** `IOException` if a read error occurs.

### `string GenerateRouteBlock()`
Generates a single route block configuration as a string.  
**Returns:** A string representing the route block in Caddyfile syntax.  
**Throws:** `InvalidOperationException` if the required route data has not been provided.

### `async Task<bool> ValidateCaddyfileAsync()`
Validates the currently generated Caddyfile using the Caddy validation tool.  
**Returns:** `true` if the Caddyfile is valid; otherwise `false`.  
**Throws:** `InvalidOperationException` if no Caddyfile has been generated.  
**Throws:** `ExternalToolException` if the validation process fails to execute.

### `CaddyRoute GenerateRouteForService()`
Creates a `CaddyRoute` object representing the routing configuration for a service.  
**Returns:** A `CaddyRoute` instance populated with the current service configuration.  
**Throws:** `InvalidOperationException` if the service definition is incomplete or missing.

### `string GenerateCaddyJsonAsync()`
Generates a JSON representation of the Caddy configuration.  
**Returns:** A string containing the JSON configuration.  
**Throws:** `InvalidOperationException` if the configuration data is insufficient.

## Usage

### Example 1: Generate and Write a Caddyfile

```csharp
var configService = new CaddyConfigurationService();

// Assume the service has been configured with the necessary data
string caddyfileContent = await configService.GenerateCaddyfileAsync();
bool written = await configService.WriteCaddyfileAsync();

if (written)
{
    Console.WriteLine("Caddyfile written successfully.");
}
else
{
    Console.WriteLine("Failed to write Caddyfile.");
}
```

### Example 2: Validate and Read Back the Configuration

```csharp
var configService = new CaddyConfigurationService();

// Generate and write the Caddyfile
await configService.GenerateCaddyfileAsync();
await configService.WriteCaddyfileAsync();

// Validate the written file
bool isValid = await configService.ValidateCaddyfileAsync();
if (isValid)
{
    string content = await configService.ReadCaddyfileAsync();
    Console.WriteLine("Valid Caddyfile content:");
    Console.WriteLine(content);
}
else
{
    Console.WriteLine("Caddyfile validation failed.");
}
```

## Notes

- The service is **not thread-safe**. Concurrent calls to any of its methods from multiple threads may lead to inconsistent state or exceptions. External synchronization is required when using the same instance across threads.
- Methods that depend on generated state (e.g., `WriteCaddyfileAsync`, `ValidateCaddyfileAsync`) will throw an `InvalidOperationException` if `GenerateCaddyfileAsync` has not been called first.
- The default file path used by `WriteCaddyfileAsync` and `ReadCaddyfileAsync` is determined by internal configuration and may vary based on the environment. Ensure the service is initialized with the correct path if a custom location is needed.
- `GenerateCaddyJsonAsync` is a synchronous method despite its name; it does not perform any asynchronous I/O.
- `GenerateRouteBlock` and `GenerateRouteForService` rely on internal service definitions that must be set prior to invocation. Failure to do so will result in an `InvalidOperationException`.
