# IServiceDiscoveryClient

`IServiceDiscoveryClient` defines the contract for service registration and discovery within the `caddy-vps-toolkit` infrastructure. Implementations of this interface allow services to announce their presence, locate peers, and manage lifecycle events through a consistent API that combines identity metadata with network location details.

## API

### Properties

#### `Id`
`string` — A unique identifier for this service instance. Typically assigned by the discovery backend upon registration or supplied by the client before registration.

#### `ServiceName`
`string` — The logical name of the service this instance represents. Multiple instances sharing the same `ServiceName` form a discoverable cluster.

#### `Host`
`string` — The network hostname or IP address where this service instance is reachable.

#### `Port`
`int` — The network port on which this service instance listens.

#### `Metadata`
`Dictionary<string, string>` — Arbitrary key-value pairs carrying additional information about the instance (version, region, tags, health endpoints, etc.). May be empty but is never null on a properly initialized instance.

#### `GetUrl`
`string` — Returns a fully formed URL string constructed from `Host` and `Port` (typically `http://{Host}:{Port}` or a scheme derived from metadata). This is a computed property, not backed by a settable field.

### Methods

#### `DiscoverAsync`
```csharp
public async Task<ServiceInstance> DiscoverAsync(string serviceName, CancellationToken cancellationToken = default)
```
Resolves a single instance of the named service. The selection strategy (round-robin, random, least-loaded) depends on the implementation. Returns a `ServiceInstance` representing the chosen peer. Throws `ServiceNotFoundException` when no instances of `serviceName` are currently registered. Throws `OperationCanceledException` if the token is cancelled. Throws implementation-specific exceptions on transport or backend failures.

#### `DiscoverAllAsync`
```csharp
public async Task<List<ServiceInstance>> DiscoverAllAsync(string serviceName, CancellationToken cancellationToken = default)
```
Returns all currently registered instances of the named service. The returned list is a snapshot; it may be empty if no instances are registered, but never null. Throws `OperationCanceledException` if the token is cancelled. Throws implementation-specific exceptions on transport or backend failures.

#### `RegisterAsync`
```csharp
public async Task RegisterAsync(CancellationToken cancellationToken = default)
```
Registers this client’s instance (using its `Id`, `ServiceName`, `Host`, `Port`, and `Metadata`) with the discovery backend. If `Id` is null or empty at call time, the implementation may auto-generate one and update the property. Throws `InvalidOperationException` when required fields (`ServiceName`, `Host`) are null or empty, or `Port` is out of range. Throws `DuplicateInstanceException` when an instance with the same `Id` is already registered and the backend does not allow overwrite. Throws `OperationCanceledException` if the token is cancelled.

#### `DeregisterAsync`
```csharp
public async Task DeregisterAsync(CancellationToken cancellationToken = default)
```
Removes this instance from the discovery backend. After successful deregistration, the instance is no longer returned by `DiscoverAsync` or `DiscoverAllAsync`. Safe to call on an instance that was never registered; implementations should treat this as a no-op rather than throwing. Throws `OperationCanceledException` if the token is cancelled.

#### `Equals`
```csharp
public override bool Equals(object obj)
```
Two `IServiceDiscoveryClient` instances are considered equal if they share the same `Id`. If `Id` is null on either instance, reference equality is used.

#### `GetHashCode`
```csharp
public override int GetHashCode()
```
Hash code is derived from `Id`. Consistent with `Equals`.

## Usage

### Example 1: Register, Discover a Peer, and Deregister

```csharp
var client = new ConsulServiceDiscoveryClient
{
    ServiceName = "caddy-reverse-proxy",
    Host = "10.0.1.15",
    Port = 8080,
    Metadata = new Dictionary<string, string>
    {
        ["version"] = "2.1.0",
        ["region"] = "us-east"
    }
};

await client.RegisterAsync();

// Locate a peer to share configuration state
ServiceInstance peer = await client.DiscoverAsync("caddy-reverse-proxy");
Console.WriteLine($"Discovered peer at {peer.GetUrl}");

// Shutdown sequence
await client.DeregisterAsync();
```

### Example 2: Enumerate All Instances and Build a Peer Map

```csharp
var client = new ConsulServiceDiscoveryClient
{
    ServiceName = "vps-manager",
    Host = "10.0.2.9",
    Port = 9001
};

await client.RegisterAsync();

List<ServiceInstance> allPeers = await client.DiscoverAllAsync("vps-manager");

var peerUrls = allPeers
    .Where(p => p.Id != client.Id)
    .Select(p => p.GetUrl)
    .ToList();

Console.WriteLine($"Active peers (excluding self): {peerUrls.Count}");
foreach (var url in peerUrls)
{
    Console.WriteLine($"  {url}");
}
```

## Notes

- **Thread safety:** Implementations are not required to be thread-safe for concurrent modification of properties (`Host`, `Port`, `Metadata`) while registration or discovery operations are in flight. Callers should avoid mutating instance properties after `RegisterAsync` is invoked unless the specific implementation documents support for live updates.
- **Empty `Id` at registration:** If `Id` is null or empty when `RegisterAsync` is called, the backend may assign one. Callers should inspect `Id` after registration completes if they rely on it for equality checks or subsequent deregistration.
- **Deregistration of unregistered instances:** Calling `DeregisterAsync` on an instance that was never successfully registered should not throw. This supports cleanup paths in `finally` blocks without requiring state tracking.
- **`GetUrl` scheme:** The URL scheme returned by `GetUrl` is implementation-defined. Callers that require a specific scheme (e.g., `https`) should verify the returned value or configure the implementation accordingly, typically through `Metadata` entries that the implementation respects.
- **`Equals` and collections:** Because equality depends solely on `Id`, storing instances in a `HashSet` or using them as dictionary keys is safe only when `Id` is guaranteed non-null and stable. Avoid relying on hash-based collections before `RegisterAsync` completes if `Id` is auto-generated.
- **Cancellation:** All async methods accept a `CancellationToken`. Implementations may not roll back side effects (e.g., a registration that completed before cancellation was observed); callers should handle this ambiguity when cancelling mid-operation.
