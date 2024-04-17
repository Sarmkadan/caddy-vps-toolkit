# CaddyRoute

A configuration container for defining HTTP routing rules in Caddy, used to manage reverse proxy routes, TLS settings, rate limiting, and authentication for services exposed through Caddy.

## API

### `public string Id`
A unique identifier for the route. Used internally to reference the route in the system. Must be non-null and non-empty.

### `public string ServiceId`
An identifier linking the route to a specific service or backend. Used to group related routes and manage service-specific configurations.

### `public string Domain`
The domain name under which the route will be served (e.g., `api.example.com`). Must be a valid domain string. Used to match incoming requests and configure TLS certificates.

### `public string Path`
The base path under which the route will be served (e.g., `/v1`). Must be a valid URI path. Combined with `Domain` to form the full request match condition.

### `public string UpstreamUrl`
The target URL to which requests matching the route will be proxied (e.g., `http://localhost:8080`). Must be a valid absolute URL. Supports HTTP and HTTPS schemes.

### `public bool StripPath`
If `true`, strips the matched `Path` segment from the request before forwarding to the upstream. Useful when the upstream expects requests at its root.

### `public bool PreserveHostHeader`
If `true`, forwards the original `Host` header to the upstream. If `false`, replaces it with the upstream host. Affects how the backend identifies the source of the request.

### `public int TimeoutSeconds`
The maximum duration (in seconds) to wait for a response from the upstream before timing out. Must be a non-negative integer. A value of `0` indicates no timeout.

### `public bool EnableHttps`
If `true`, enables automatic HTTPS handling via Caddy’s built-in ACME support for the `Domain`. If `false`, disables HTTPS and serves HTTP only.

### `public bool AutoRedirectHttp`
If `true`, automatically redirects HTTP requests to HTTPS for the `Domain`. Only effective when `EnableHttps` is `true`.

### `public Dictionary<string, string> CustomHeaders`
A collection of custom HTTP headers to add to requests forwarded to the upstream. Keys are header names, values are header values. Headers are appended to the request before proxying.

### `public string RateLimitRule`
A Caddy-compatible rate limiting rule string (e.g., `100, 5m`) to apply to requests matching this route. If `null` or empty, no rate limiting is applied.

### `public bool BasicAuthEnabled`
If `true`, enables HTTP Basic Authentication for this route. Requires `BasicAuthUsername` and `BasicAuthPasswordHash` to be set.

### `public string BasicAuthUsername`
The username required for Basic Authentication. Must be non-null and non-empty when `BasicAuthEnabled` is `true`.

### `public string BasicAuthPasswordHash`
The bcrypt-hashed password required for Basic Authentication. Must be a valid bcrypt hash when `BasicAuthEnabled` is `true`.

### `public string TlsDnsProvider`
The DNS provider name to use for DNS-01 challenge when obtaining TLS certificates (e.g., `cloudflare`). Must match a supported provider in Caddy. If `null` or empty, defaults to HTTP challenge.

### `public DateTime CreatedAt`
The timestamp when the route was created. Set automatically and immutable after creation.

### `public DateTime UpdatedAt`
The timestamp when the route was last updated. Updated automatically on modification.

### `public bool IsActive`
Indicates whether the route is currently active and serving traffic. Can be toggled to enable or disable routing without deletion.

### `public void Validate()`
Validates the route configuration. Throws `ArgumentException` if any required field is invalid or missing. Validates:
- `Id`, `ServiceId`, `Domain`, `Path`, `UpstreamUrl`, `BasicAuthUsername`, and `BasicAuthPasswordHash` are non-null and non-empty when required.
- `TimeoutSeconds` is non-negative.
- If `BasicAuthEnabled` is `true`, ensures `BasicAuthUsername` and `BasicAuthPasswordHash` are set.
- If `RateLimitRule` is non-empty, assumes it is syntactically valid (no deep parsing).
- `TlsDnsProvider`, if set, is assumed to be a supported provider name.

## Usage

### Example 1: Basic Reverse Proxy Route
