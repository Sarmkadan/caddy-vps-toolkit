# CaddyConfig

The `CaddyConfig` class serves as a comprehensive data transfer object representing the runtime configuration state for a Caddy server instance within the `caddy-vps-toolkit` ecosystem. It encapsulates administrative settings, network binding parameters, TLS policies, logging configurations, and timeout values required to instantiate or modify a Caddy process. This type acts as the primary serialization target for generating Caddyfile equivalents or JSON configurations, ensuring that all critical server behaviors are defined programmatically before deployment.

## API

The following members constitute the public interface of the `CaddyConfig` type. As this is a data container, members are exposed as properties without parameters, return values, or exceptional conditions under normal access.

### `Id`
*   **Type**: `string`
*   **Purpose**: Uniquely identifies the specific Caddy configuration instance within the toolkit's management scope. This identifier is typically used for tracking configuration history, mapping to specific VPS instances, or referencing the config in orchestration logs.

### `AdminEmail`
*   **Type**: `string`
*   **Purpose**: Specifies the email address used for ACME registration and certificate expiration notifications. This is mandatory for automated Let's Encrypt certificate issuance unless explicitly disabled by other policies.

### `AdminPort`
*   **Type**: `int`
*   **Purpose**: Defines the TCP port on which the Caddy admin API listens. This interface allows for dynamic configuration updates and metrics retrieval without restarting the server.

### `AdminHost`
*   **Type**: `string`
*   **Purpose**: Sets the network interface binding for the admin API. Common values include `localhost` for local-only access or `0.0.0.0` for remote administration (subject to firewall rules).

### `EnableMetrics`
*   **Type**: `bool`
*   **Purpose**: Toggles the exposure of Prometheus-compatible metrics. When `true`, the server exposes operational data such as request counts, TLS handshake statistics, and resource usage.

### `EnableLogOutput`
*   **Type**: `bool`
*   **Purpose**: Controls whether access and error logs are written to the standard output (stdout/stderr). Disabling this is common in environments where logs are shipped directly to a centralized aggregator via sidecars.

### `LogLevel`
*   **Type**: `string`
*   **Purpose**: Determines the verbosity of the logging system. Valid values typically align with standard logging levels (e.g., `DEBUG`, `INFO`, `WARN`, `ERROR`), filtering out messages below the specified threshold.

### `LogFormat`
*   **Type**: `string`
*   **Purpose**: Specifies the structural format of the log entries. Common options include `json` for machine parsing or `console` for human-readable text output.

### `HttpPort`
*   **Type**: `int`
*   **Purpose**: Sets the listening port for unencrypted HTTP traffic. This is often used for redirecting traffic to HTTPS or serving non-TLS content.

### `HttpsPort`
*   **Type**: `int`
*   **Purpose**: Sets the listening port for encrypted HTTPS traffic. The standard value is 443, though this allows for customization in containerized or non-privileged environments.

### `AutoHttpsDisabled`
*   **Type**: `bool`
*   **Purpose**: When `true`, disables Caddy's automatic HTTPS redirection and certificate management features. This is required when using external load balancers for TLS termination or when running in purely internal HTTP modes.

### `TlsPolicy`
*   **Type**: `string`
*   **Purpose**: Defines the strategy for TLS enforcement. Values may dictate strict requirements for client certificates, specific protocol versions, or cipher suites allowed during handshakes.

### `IdleTimeout`
*   **Type**: `int`
*   **Purpose**: Specifies the maximum duration (in seconds) a keep-alive connection remains open while idle. Closing idle connections frees up server resources.

### `ReadTimeout`
*   **Type**: `int`
*   **Purpose**: Sets the maximum duration (in seconds) allowed for reading the entire request, including the body. This protects against slowloris attacks and stalled clients.

### `WriteTimeout`
*   **Type**: `int`
*   **Purpose**: Sets the maximum duration (in seconds) allowed for writing the response. This ensures that slow clients do not hold server threads indefinitely during large file transfers.

### `OnDemandTls`
*   **Type**: `List<string>`
*   **Purpose**: Contains a list of domain names authorized for On-Demand TLS. When enabled, Caddy will only obtain certificates for domains present in this list during the handshake, preventing abuse of the ACME CA.

### `CertificateEmail`
*   **Type**: `string`
*   **Purpose**: An alternative or specific email address used solely for certificate management contexts, distinct from the general `AdminEmail` if separation of concerns is required.

### `StrictSniRequired`
*   **Type**: `bool`
*   **Purpose**: Enforces the requirement of the Server Name Indication (SNI) extension in the TLS handshake. If `true`, connections without SNI are rejected, which is necessary for hosting multiple secure sites on a single IP.

### `CustomGlobals`
*   **Type**: `Dictionary<string, string>`
*   **Purpose**: Provides a key-value map for injecting custom global options into the Caddy configuration. This allows for extensibility without modifying the core `CaddyConfig` class structure.

### `CreatedAt`
*   **Type**: `DateTime`
*   **Purpose**: Records the UTC timestamp when this configuration object was initially instantiated or persisted. Useful for auditing, versioning, and determining configuration age.

## Usage

### Example 1: Basic Web Server Configuration
This example demonstrates initializing a standard public-facing web server with automatic HTTPS, JSON logging, and default timeouts.

```csharp
var config = new CaddyConfig
{
    Id = "prod-web-01",
    AdminEmail = "ops@example.com",
    AdminPort = 2019,
    AdminHost = "localhost",
    EnableMetrics = true,
    EnableLogOutput = true,
    LogLevel = "INFO",
    LogFormat = "json",
    HttpPort = 80,
    HttpsPort = 443,
    AutoHttpsDisabled = false,
    TlsPolicy = "default",
    IdleTimeout = 30,
    ReadTimeout = 10,
    WriteTimeout = 15,
    OnDemandTls = new List<string>(),
    CertificateEmail = "certs@example.com",
    StrictSniRequired = true,
    CustomGlobals = new Dictionary<string, string>(),
    CreatedAt = DateTime.UtcNow
};

// Serialize 'config' to JSON or pass to the deployment service
```

### Example 2: Internal Service with Custom Globals
This example configures an internal microservice where HTTPS is terminated externally (`AutoHttpsDisabled`), metrics are disabled for security, and custom global directives are injected.

```csharp
var internalConfig = new CaddyConfig
{
    Id = "internal-api-gateway",
    AdminEmail = "devops@internal.local",
    AdminPort = 2019,
    AdminHost = "127.0.0.1",
    EnableMetrics = false,
    EnableLogOutput = false,
    LogLevel = "ERROR",
    LogFormat = "console",
    HttpPort = 8080,
    HttpsPort = 0, // Not used
    AutoHttpsDisabled = true,
    TlsPolicy = "disabled",
    IdleTimeout = 120,
    ReadTimeout = 5,
    WriteTimeout = 60,
    OnDemandTls = new List<string>(),
    CertificateEmail = "",
    StrictSniRequired = false,
    CustomGlobals = new Dictionary<string, string>
    {
        { "order", "encode before basicauth" },
        { "debug", "false" }
    },
    CreatedAt = DateTime.UtcNow
};
```

## Notes

*   **Thread Safety**: The `CaddyConfig` class is a Plain Old CLR Object (POCO) and is not inherently thread-safe. While reading individual primitive properties is generally atomic on modern architectures, modifying the `CustomGlobals` dictionary or the `OnDemandTls` list from multiple threads simultaneously requires external synchronization (e.g., using a `lock` statement). It is recommended to treat instances as immutable after initialization or to clone the object before making modifications in concurrent scenarios.
*   **Validation Constraints**: While the type system enforces data types, logical validity is not enforced by the class itself. For instance, setting `AutoHttpsDisabled` to `true` while providing a non-empty `OnDemandTls` list may result in a runtime error during Caddy server initialization if the resulting configuration file is semantically invalid. Callers must ensure logical consistency between TLS policies and port assignments.
*   **Timeout Units**: The `IdleTimeout`, `ReadTimeout`, and `WriteTimeout` properties are defined as integers representing seconds. Passing negative values or excessively large integers may cause undefined behavior depending on the underlying Caddy version's parsing logic.
*   **Dictionary Mutability**: The `CustomGlobals` property exposes a mutable `Dictionary`. If this instance is shared across different components of the toolkit, changes to this dictionary will reflect immediately for all consumers. Defensive copying is advised if the configuration object is passed across trust boundaries.
