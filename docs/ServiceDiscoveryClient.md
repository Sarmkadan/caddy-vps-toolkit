# ServiceDiscoveryClient

`Integration/ServiceDiscoveryClient.cs` contains the service discovery abstraction used by the `caddy-vps-toolkit` infrastructure, together with the one implementation shipped in this file. All types live in the `CaddyVpsToolkit.Integration` namespace.

- `IServiceDiscoveryClient` - the contract for registering, deregistering, and looking up service instances. Consumers should depend on this interface.
- `InMemoryServiceDiscoveryClient` - a `sealed` in-process implementation intended for local development and tests. It is the implementation registered by `AddServiceDiscovery`.
- `ServiceInstance` - a `sealed` model describing one endpoint of a named service.

No Consul, Eureka, or other remote backend is included in this file. Such a backend would implement `IServiceDiscoveryClient` in its own type.

## API

### IServiceDiscoveryClient

```csharp
public interface IServiceDiscoveryClient
{
    Task<ServiceInstance> DiscoverAsync(string serviceName);
    Task<List<ServiceInstance>> DiscoverAllAsync(string serviceName);
    Task RegisterAsync(ServiceInstance instance);
    Task DeregisterAsync(string serviceId);
}
```

The interface has no `CancellationToken` parameters. Its members are described below as implemented by `InMemoryServiceDiscoveryClient`; other implementations may differ.

#### `DiscoverAsync`
- **Signature:** `Task<ServiceInstance> DiscoverAsync(string serviceName)`
- **Purpose:** Returns one registered instance of `serviceName`.
- **Parameters:** `serviceName` - the logical name the instances were registered under (`ServiceInstance.ServiceName`).
- **Returns:** The first instance in registration order, or `null` when no instance of `serviceName` is registered. The selection is not randomized and not round-robin.
- **Throws:** Nothing for an unknown name; an unregistered name yields `null`.

#### `DiscoverAllAsync`
- **Signature:** `Task<List<ServiceInstance>> DiscoverAllAsync(string serviceName)`
- **Purpose:** Returns every registered instance of `serviceName`.
- **Parameters:** `serviceName` - the logical service name.
- **Returns:** A new `List<ServiceInstance>` holding a snapshot of the registry. It is empty, never `null`, when nothing is registered. Changing the returned list does not change the registry.
- **Throws:** Nothing.

#### `RegisterAsync`
- **Signature:** `Task RegisterAsync(ServiceInstance instance)`
- **Purpose:** Adds `instance` to the registry under its `ServiceName`.
- **Parameters:** `instance` - the instance to add. Its `ServiceName` selects the bucket it is stored in.
- **Returns:** A completed `Task`.
- **Throws:** `ArgumentNullException` when `instance` is `null`. The exception is delivered through the returned `Task`, because the method is declared `async`.
- **Remarks:** No other validation happens. `Id`, `Host`, and `Port` are stored as given, and `Id` is not generated. Registering the same instance twice adds it twice.

#### `DeregisterAsync`
- **Signature:** `Task DeregisterAsync(string serviceId)`
- **Purpose:** Removes every instance whose `Id` equals `serviceId`, across all service names.
- **Parameters:** `serviceId` - the `Id` of the instance to remove. This is the instance `Id`, not the service name.
- **Returns:** A completed `Task`. Removing an identifier that is not registered does nothing.
- **Throws:** `ArgumentException` when `serviceId` is `null` or empty. The exception is delivered through the returned `Task`.

### InMemoryServiceDiscoveryClient

```csharp
public sealed class InMemoryServiceDiscoveryClient : IServiceDiscoveryClient
```

The registry is a `Dictionary<string, List<ServiceInstance>>` keyed by service name. Every read and write takes a private lock, so the client can be shared across threads. Registrations are lost when the process exits.

The class has an implicit parameterless constructor and no public members beyond the four interface methods.

### ServiceInstance

```csharp
public sealed class ServiceInstance
```

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `string` | Identifier of this instance. Used by `DeregisterAsync` and by equality. Set by the caller; not generated. |
| `ServiceName` | `string` | Logical service name. The registry groups instances by this value. |
| `Host` | `string` | Hostname or IP address where the instance is reachable. |
| `Port` | `int` | Port the instance listens on. |
| `Metadata` | `Dictionary<string, string>` | Free-form key-value data (version, region, and so on). Initialized to an empty dictionary. |

#### `GetUrl`
- **Signature:** `public string GetUrl()`
- **Purpose:** Builds the base URL of the instance.
- **Returns:** `http://{Host}:{Port}`. The scheme is always `http`; to use another scheme, the caller must build the URL itself.
- **Note:** This is a method, so it is called as `GetUrl()`.

#### `Equals`
- **Signature:** `public override bool Equals(object obj)`
- **Purpose:** Two instances are equal when `obj` is a `ServiceInstance` with the same `Id`. `ServiceName`, `Host`, `Port`, and `Metadata` are not compared.

#### `GetHashCode`
- **Signature:** `public override int GetHashCode()`
- **Purpose:** Returns the hash code of `Id`, or `0` when `Id` is `null`. Consistent with `Equals`.

## Usage

### Example 1: Register an instance, discover it, and deregister

```csharp
IServiceDiscoveryClient discovery = new InMemoryServiceDiscoveryClient();

var instance = new ServiceInstance
{
    Id = "caddy-node-1",
    ServiceName = "caddy-reverse-proxy",
    Host = "10.0.1.15",
    Port = 8080,
    Metadata = new Dictionary<string, string>
    {
        ["version"] = "2.1.0",
        ["region"] = "us-east"
    }
};

await discovery.RegisterAsync(instance);

ServiceInstance found = await discovery.DiscoverAsync("caddy-reverse-proxy");
if (found is not null)
{
    Console.WriteLine($"Discovered instance at {found.GetUrl()}");
}

// Shutdown sequence
await discovery.DeregisterAsync(instance.Id);
```

### Example 2: List every instance of a service

```csharp
List<ServiceInstance> peers = await discovery.DiscoverAllAsync("vps-manager");

var peerUrls = peers
    .Where(p => p.Id != "vps-manager-2")
    .Select(p => p.GetUrl())
    .ToList();

Console.WriteLine($"Active peers (excluding self): {peerUrls.Count}");
foreach (var url in peerUrls)
{
    Console.WriteLine($"  {url}");
}
```

`DiscoverAllAsync` returns an empty list for an unknown service, so the loop above needs no null check.

### Example 3: Handling an unknown service

`DiscoverAsync` returns `null` instead of throwing when nothing is registered. Its declared return type is not nullable, so check for `null` explicitly:

```csharp
ServiceInstance backend = await discovery.DiscoverAsync("metrics-collector");

if (backend is null)
{
    Console.WriteLine("No metrics-collector instance is registered; skipping export.");
    return;
}

Console.WriteLine($"Exporting to {backend.GetUrl()}");
```

### Example 4: Registering through dependency injection

`AddServiceDiscovery` registers `InMemoryServiceDiscoveryClient` as a singleton `IServiceDiscoveryClient`:

```csharp
services.AddServiceDiscovery();

// Later, in a consumer:
public sealed class PeerRefresher(IServiceDiscoveryClient discovery)
{
    public Task<List<ServiceInstance>> ListBackendsAsync() =>
        discovery.DiscoverAllAsync("caddy-reverse-proxy");
}
```

## Notes

- **`null` versus empty results.** `DiscoverAsync` returns `null` when no instance matches, while `DiscoverAllAsync` returns an empty list. Check for `null` after `DiscoverAsync` and check `Count` after `DiscoverAllAsync`.
- **Argument validation is deferred to the task.** Because `RegisterAsync` and `DeregisterAsync` are `async`, their argument exceptions are stored in the returned `Task` and surface when it is awaited. Calling them without `await` can hide the failure.
- **Deregistration uses `Id`.** `DeregisterAsync` takes an instance `Id`. Passing a service name removes nothing. If one `Id` was registered more than once, all matching entries are removed.
- **Ids are not generated.** Set `Id` before calling `RegisterAsync`. Registering an instance with a `null` or duplicate `Id` is accepted.
- **Equality ignores everything but `Id`.** Two instances with a `null` `Id` compare equal to each other, because `null == null` is `true` in the implementation. Give every instance a unique `Id` before storing instances in a `HashSet` or using them as dictionary keys.
- **The registry stores references.** `RegisterAsync` keeps the object it is given, not a copy. Changing `ServiceName` after registration does not move the instance to another bucket. Changing `Id` after registration means `DeregisterAsync` must be called with the new value, because the stored entry is matched on its current `Id`.
- **Registry lifetime.** The registry lives only in memory. Instances are not persisted or shared between processes. The DI registration is a singleton, so all consumers in one process share one registry.
- **Thread safety.** Registry operations are synchronized, so concurrent `RegisterAsync`, `DiscoverAsync`, `DiscoverAllAsync`, and `DeregisterAsync` calls are safe. `ServiceInstance` itself has no locking. Avoid changing its properties while other threads may be registering or reading it.
- **Scheme.** `GetUrl()` always returns an `http://` URL. Use `Host` and `Port` directly if an `https://` URL is required.
