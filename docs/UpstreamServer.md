# UpstreamServer

Represents a backend server in a load-balancing configuration. This type tracks connection details, health status, and performance metrics for an upstream server that Caddy may route traffic to. It is used to maintain real-time availability data and inform load-balancing decisions.

## API

### `public string Id`
Unique identifier for the upstream server. Assigned during creation and immutable thereafter.

### `public required string Address`
The hostname or IP address of the upstream server. Must be non-empty and valid for DNS resolution or direct IP usage.

### `public int Port`
The port number on which the upstream server listens. Must be between 1 and 65535.

### `public int Weight`
Relative weight of this server in load-balancing decisions. Higher values indicate a preference for routing traffic to this server. Must be ≥ 0.

### `public UpstreamServerStatus Status`
Current operational status of the server. Possible values include `Healthy`, `Unhealthy`, `Draining`, and `Disabled`. Updated via health probes or administrative actions.

### `public bool IsHealthy`
Convenience property indicating whether the server is considered healthy (`Status == UpstreamServerStatus.Healthy`). Does not perform health checks; reflects the last recorded `Status`.

### `public DateTime? LastCheckedAt`
Timestamp of the most recent health probe. `null` if no probe has been performed since creation.

### `public int ConsecutiveFailures`
Number of consecutive failed health probes. Resets to 0 on success. Used to determine `Status` transitions.

### `public int ConsecutiveSuccesses`
Number of consecutive successful health probes. Resets to 0 on failure. Used to determine `Status` transitions.

### `public int AverageResponseTimeMs`
Rolling average response time of successful health probes, in milliseconds. Updated via `RecordHealthProbeResult`.

### `public int ActiveConnections`
Number of currently active connections to this upstream server. Updated externally by the load balancer.

### `public string? Tags`
Optional comma-separated list of tags for grouping or filtering upstream servers. May be `null`.

### `public string? Notes`
Optional administrative notes about the server. May be `null`.

### `public DateTime CreatedAt`
Timestamp of server creation. Immutable after initialization.

### `public DateTime UpdatedAt`
Timestamp of the last modification to any property. Updated automatically on changes.

### `public string GetUpstreamAddress()`
Returns the full address in the format `Address:Port`. Useful for constructing connection strings or logging.

### `public bool IsAvailable()`
Returns `true` if the server is available for routing (`Status == UpstreamServerStatus.Healthy` and `ActiveConnections` below configured limits). Does not perform health checks; reflects current state.

### `public void RecordHealthProbeResult(bool success, int responseTimeMs)`
Records the result of a health probe.

**Parameters:**
- `success`: `true` if the probe succeeded, `false` otherwise.
- `responseTimeMs`: Response time of the probe in milliseconds. Must be ≥ 0.

**Throws:**
- `ArgumentOutOfRangeException`: If `responseTimeMs` is negative.

Updates `ConsecutiveFailures`, `ConsecutiveSuccesses`, `AverageResponseTimeMs`, `LastCheckedAt`, and `Status` based on the probe result.

### `public void Validate()`
Validates the server configuration.

**Throws:**
- `ArgumentException`: If `Address` is empty, `Port` is outside 1-65535, or `Weight` is negative.
- `InvalidOperationException`: If required fields are not initialized (e.g., `Address`).

## Usage

### Example 1: Creating and Validating an Upstream Server
