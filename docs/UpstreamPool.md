# UpstreamPool

Represents a pool of upstream servers used for load balancing within the Caddy VPS Toolkit. It encapsulates server health monitoring, connection tracking, and configuration for distributing traffic across multiple backend services. The type provides methods to generate Caddy-compatible upstream blocks and evaluate server availability based on health checks and thresholds.

## API

### `public string Id`
A unique identifier for the upstream pool. Used internally for reference and tracking changes.

### `public required string Name`
The human-readable name of the upstream pool. Required during construction and used for logging and identification.

### `public required string ServiceId`
The identifier of the associated service this pool belongs to. Required during construction and used to scope health checks and metrics.

### `public LoadBalancingStrategy Strategy`
Determines how traffic is distributed among available servers. Supported strategies include round-robin, least-connections, and random. Defaults to round-robin if not specified.

### `public List<UpstreamServer> Servers`
The collection of upstream servers managed by this pool. Includes both active and inactive servers based on health status. Modifying this list after construction may lead to inconsistent state; prefer using health status APIs.

### `public bool PassiveHealthEnabled`
Enables passive health monitoring, where server health is inferred from connection failures and timeouts. When enabled, servers exceeding failure thresholds are temporarily marked unhealthy.

### `public bool ActiveHealthEnabled`
Enables active health checks via periodic probes to the `HealthProbePath`. When enabled, servers failing consecutive probes are marked unhealthy until recovery.

### `public int HealthCheckIntervalSeconds`
The interval, in seconds, between active health checks when `ActiveHealthEnabled` is true. Must be a positive integer. Lower values increase probe frequency but may impact performance.

### `public int UnhealthyThreshold`
The number of consecutive failures (active or passive) required to mark a server unhealthy. Must be a positive integer. Affects both passive and active health evaluation.

### `public int HealthyThreshold`
The number of consecutive successes required to restore a server to healthy status after being marked unhealthy. Must be a positive integer. Prevents flapping during transient issues.

### `public int MaxRetries`
The maximum number of retry attempts for failed requests before considering the upstream permanently unavailable. Must be a non-negative integer. Used in conjunction with `RetryDurationSeconds`.

### `public int RetryDurationSeconds`
The duration, in seconds, to wait before retrying a failed request. Must be a non-negative integer. Limits aggressive retry behavior and allows downstream systems to recover.

### `public string? StickyCookieName`
The name of the cookie used to enable session affinity (sticky sessions). When set, clients will be routed to the same upstream server based on a cookie value. Optional; omit to disable session persistence.

### `public string HealthProbePath`
The HTTP path used for active health checks when `ActiveHealthEnabled` is true. Must be a valid absolute or relative path. Probes are sent to each server in the pool at the specified interval.

### `public bool IsEnabled`
Indicates whether the upstream pool is currently active and accepting traffic. When false, all traffic is bypassed regardless of server health.

### `public DateTime CreatedAt`
The timestamp when the upstream pool was created. Immutable after construction.

### `public DateTime UpdatedAt`
The timestamp of the last modification to the pool or its servers. Updated automatically on structural changes such as server list updates or health status transitions.

### `public List<UpstreamServer> GetAvailableServers()`
Returns a filtered list of servers currently considered healthy and eligible to receive traffic.

- **Returns**: A new `List<UpstreamServer>` containing only servers where `IsHealthy` is true and `IsEnabled` is true.
- **Throws**: None. Returns empty list if no servers are available.

### `public int GetTotalActiveConnections()`
Returns the total number of active connections across all servers in the pool.

- **Returns**: An integer representing the sum of active connections on all servers.
- **Throws**: None.

### `public string GenerateCaddyUpstreamBlock()`
Generates a Caddy-compatible upstream block configuration string based on the current state of the pool.

- **Returns**: A string containing a valid Caddy upstream directive, including server addresses, load balancing strategy, health checks, and retry policies.
- **Throws**: None. May return empty string if no servers are configured.

## Usage
