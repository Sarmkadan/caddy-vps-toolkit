# UpstreamManagerService

Central service for managing upstream pools and servers in the Caddy VPS Toolkit. Handles registration, health monitoring, selection, and configuration generation for upstream pools used by load-balanced services. Maintains state for pools and their associated servers, including health status and metrics.

## API

### `UpstreamManagerService`

Constructor for the service. Initializes the upstream pool registry and health tracking infrastructure. No parameters are required as dependencies are resolved via dependency injection.

### `async Task<string> RegisterPoolAsync(string poolName, List<string> serverAddresses, string healthCheckPath = "/health", int healthCheckIntervalSeconds = 30)`

Registers a new upstream pool with the specified servers and health check configuration.

- **Parameters**
  - `poolName`: Unique identifier for the pool.
  - `serverAddresses`: List of server addresses (e.g., `["192.168.1.10:8080", "192.168.1.11:8080"]`).
  - `healthCheckPath`: Optional health check endpoint path. Defaults to `"/health"`.
  - `healthCheckIntervalSeconds`: Optional interval in seconds between health checks. Defaults to `30`.
- **Return value**: A unique pool identifier string.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null, empty, or already registered. Throws `ArgumentException` if `serverAddresses` is null or empty. Throws `InvalidOperationException` if the service is disposed.

### `Task<UpstreamPool?> GetPoolAsync(string poolName)`

Retrieves a registered upstream pool by name.

- **Parameters**
  - `poolName`: Name of the pool to retrieve.
- **Return value**: The `UpstreamPool` instance if found; otherwise `null`.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null or empty.

### `Task<List<UpstreamPool>> GetPoolsForServiceAsync(string serviceName)`

Retrieves all upstream pools associated with a given service name.

- **Parameters**
  - `serviceName`: Name of the service to filter pools by.
- **Return value**: List of `UpstreamPool` instances matching the service name. Returns empty list if none found.
- **Exceptions**: Throws `ArgumentException` if `serviceName` is null or empty.

### `Task<List<UpstreamPool>> GetAllPoolsAsync()`

Retrieves all registered upstream pools.

- **Return value**: List of all `UpstreamPool` instances. Returns empty list if none registered.
- **Exceptions**: None.

### `Task<bool> RemovePoolAsync(string poolName)`

Removes a registered upstream pool and all its associated servers.

- **Parameters**
  - `poolName`: Name of the pool to remove.
- **Return value**: `true` if the pool was found and removed; otherwise `false`.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null or empty.

### `Task<UpstreamServer?> SelectUpstreamAsync(string poolName)`

Selects a healthy upstream server from the specified pool using round-robin load balancing.

- **Parameters**
  - `poolName`: Name of the pool to select from.
- **Return value**: The selected `UpstreamServer` instance if available; otherwise `null`.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null or empty. Throws `InvalidOperationException` if the pool has no healthy servers.

### `Task RecordUpstreamResultAsync(string poolName, string serverAddress, bool success, int responseTimeMs)`

Records the result of a request to an upstream server, updating health metrics and status.

- **Parameters**
  - `poolName`: Name of the pool containing the server.
  - `serverAddress`: Address of the server that handled the request.
  - `success`: Whether the request succeeded.
  - `responseTimeMs`: Response time in milliseconds.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentException` if `poolName` or `serverAddress` is null or empty. Throws `InvalidOperationException` if the pool or server does not exist.

### `async Task<List<UpstreamHealthSnapshot>> ProbeAllUpstreamsAsync()`

Performs a full health probe of all upstream servers across all pools.

- **Return value**: List of `UpstreamHealthSnapshot` instances representing the health status of each server.
- **Exceptions**: None.

### `async Task<List<UpstreamPoolHealthReport>> ProbeAllPoolsAsync()`

Performs a full health probe of all upstream pools and their servers.

- **Return value**: List of `UpstreamPoolHealthReport` instances, one per pool, summarizing health and metrics.
- **Exceptions**: None.

### `async Task DrainUpstreamAsync(string poolName, string serverAddress)`

Marks a specific upstream server as drained, preventing new traffic from being routed to it.

- **Parameters**
  - `poolName`: Name of the pool containing the server.
  - `serverAddress`: Address of the server to drain.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentException` if `poolName` or `serverAddress` is null or empty. Throws `InvalidOperationException` if the pool or server does not exist.

### `Task ReactivateUpstreamAsync(string poolName, string serverAddress)`

Reactivates a previously drained upstream server, allowing it to receive traffic again.

- **Parameters**
  - `poolName`: Name of the pool containing the server.
  - `serverAddress`: Address of the server to reactivate.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentException` if `poolName` or `serverAddress` is null or empty. Throws `InvalidOperationException` if the pool or server does not exist.

### `Task<string> GenerateCaddyConfigForPoolAsync(string poolName)`

Generates a Caddy reverse-proxy configuration snippet for a specific upstream pool.

- **Parameters**
  - `poolName`: Name of the pool to generate config for.
- **Return value**: Caddy configuration string for the pool.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null or empty. Throws `InvalidOperationException` if the pool does not exist.

### `async Task<string> GenerateCaddyConfigForServiceAsync(string serviceName)`

Generates a Caddy reverse-proxy configuration snippet for all upstream pools associated with a given service name.

- **Parameters**
  - `serviceName`: Name of the service to generate config for.
- **Return value**: Caddy configuration string combining all relevant pools.
- **Exceptions**: Throws `ArgumentException` if `serviceName` is null or empty.

### `Task<UpstreamPoolHealthReport> GetHealthReportAsync(string poolName)`

Retrieves a detailed health report for a specific upstream pool.

- **Parameters**
  - `poolName`: Name of the pool to report on.
- **Return value**: `UpstreamPoolHealthReport` instance with current health and metrics.
- **Exceptions**: Throws `ArgumentException` if `poolName` is null or empty. Throws `InvalidOperationException` if the pool does not exist.

### `Task<List<UpstreamPoolHealthReport>> GetAllHealthReportsAsync()`

Retrieves health reports for all registered upstream pools.

- **Return value**: List of `UpstreamPoolHealthReport` instances, one per pool.
- **Exceptions**: None.

## Usage

### Example 1: Registering a pool and generating Caddy config
