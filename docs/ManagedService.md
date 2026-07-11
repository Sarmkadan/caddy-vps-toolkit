# ManagedService

Represents a managed service within the caddy-vps-toolkit ecosystem. This class encapsulates the configuration, runtime status, and health-check settings for a service that can be deployed and supervised on a VPS. It is used to define how a service should be started, monitored, and exposed to the network, and provides a `Validate` method to ensure the configuration is consistent before use.

## API

### `public string Id`
Gets or sets a unique identifier for the service. This value is typically assigned by the system and should not be modified after creation.

### `public string Name`
Gets or sets the human-readable name of the service. Used for display and logging purposes.

### `public string Description`
Gets or sets a brief description of the service’s purpose.

### `public ServiceType Type`
Gets or sets the type of the service (e.g., `Systemd`, `Docker`, `Executable`). The `ServiceType` enum defines the supported kinds.

### `public string ExecutablePath`
Gets or sets the absolute or relative path to the executable or entry point of the service. Required for executable-type services.

### `public string WorkingDirectory`
Gets or sets the working directory in which the service process will run. If `null` or empty, the system default is used.

### `public string Arguments`
Gets or sets the command-line arguments passed to the service executable. May be `null` or empty.

### `public ServiceStatus Status`
Gets or sets the current runtime status of the service (e.g., `Running`, `Stopped`, `Failed`). This property is typically updated by the supervisor component.

### `public int Port`
Gets or sets the primary port the service listens on. A value of `0` indicates no port is configured.

### `public string HostBinding`
Gets or sets the host IP address or interface to which the service binds (e.g., `"0.0.0.0"`, `"127.0.0.1"`). May be `null` or empty.

### `public bool AutoStart`
Gets or sets whether the service should be automatically started when the system boots or when the supervisor initializes.

### `public int Priority`
Gets or sets the startup priority of the service. Lower numbers indicate higher priority. Used to order service startup sequences.

### `public string EnvironmentVariables`
Gets or sets environment variables for the service, typically formatted as key=value pairs separated by newlines or semicolons. May be `null` or empty.

### `public DateTime CreatedAt`
Gets or sets the timestamp when the service record was created. Typically set by the system.

### `public DateTime UpdatedAt`
Gets or sets the timestamp when the service record was last modified. Updated automatically by the system.

### `public string SystemdUnitName`
Gets or sets the name of the systemd unit file associated with the service (e.g., `"myapp.service"`). Only relevant for systemd-type services.

### `public List<ServicePort> ExposedPorts`
Gets or sets a list of additional ports that the service exposes beyond the primary `Port`. Each `ServicePort` object contains details such as protocol and container mapping. May be `null`.

### `public HealthCheckConfig HealthCheck`
Gets or sets the health-check configuration for the service. Contains settings like probe interval, timeout, and expected status codes. May be `null` if no health check is defined.

### `public bool IsEnabled`
Gets or sets whether the service is enabled for management by the toolkit. Disabled services are not started, stopped, or monitored.

### `public void Validate()`
Validates the current configuration of the service. Throws an `InvalidOperationException` if any required fields are missing or inconsistent (e.g., missing `ExecutablePath` for executable-type services, invalid port range, or conflicting settings). Does not return a value.

## Usage

### Example 1: Creating and validating a service configuration

```csharp
var service = new ManagedService
{
    Id = "web-app-01",
    Name = "Web Application",
    Description = "Main web server",
    Type = ServiceType.Executable,
    ExecutablePath = "/usr/local/bin/webapp",
    WorkingDirectory = "/opt/webapp",
    Arguments = "--port 8080",
    Port = 8080,
    HostBinding = "0.0.0.0",
    AutoStart = true,
    Priority = 10,
    EnvironmentVariables = "LOG_LEVEL=info\nNODE_ENV=production",
    ExposedPorts = new List<ServicePort>
    {
        new ServicePort { Port = 9090, Protocol = "tcp" }
    },
    HealthCheck = new HealthCheckConfig
    {
        Endpoint = "/health",
        IntervalSeconds = 30
    },
    IsEnabled = true
};

try
{
    service.Validate();
    Console.WriteLine("Service configuration is valid.");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

### Example 2: Updating service status and reading properties

```csharp
var service = new ManagedService();
service.Id = "db-service";
service.Name = "PostgreSQL";
service.Type = ServiceType.Systemd;
service.SystemdUnitName = "postgresql.service";
service.Port = 5432;
service.IsEnabled = true;
service.CreatedAt = DateTime.UtcNow;
service.UpdatedAt = DateTime.UtcNow;

// Simulate supervisor updating status
service.Status = ServiceStatus.Running;

Console.WriteLine($"Service '{service.Name}' is {service.Status}");
Console.WriteLine($"Systemd unit: {service.SystemdUnitName}");
Console.WriteLine($"Port: {service.Port}");
```

## Notes

- **Validation**: The `Validate()` method checks for required fields based on the `Type` property. For `ServiceType.Executable`, `ExecutablePath` must not be null or empty. For `ServiceType.Systemd`, `SystemdUnitName` must be provided. Port values must be between 0 and 65535. If `ExposedPorts` is non-null, each port in the list is also validated.
- **Null and empty values**: Properties such as `Description`, `Arguments`, `HostBinding`, `EnvironmentVariables`, `ExposedPorts`, and `HealthCheck` may be `null` or empty. The `Validate()` method does not require them unless the service type or other settings imply a dependency.
- **Thread safety**: This class is not thread-safe. Concurrent reads and writes to its properties from multiple threads may result in inconsistent state. External synchronization (e.g., a lock) is required if the instance is shared across threads.
- **Timestamp behavior**: `CreatedAt` and `UpdatedAt` are not automatically set by the class; they must be assigned by the caller or by the persistence layer. The system typically sets these when the service is first saved or updated.
- **`IsEnabled` vs `AutoStart`**: `IsEnabled` controls whether the toolkit manages the service at all. `AutoStart` controls whether the service starts automatically during system boot or supervisor initialization. A disabled service will not be started even if `AutoStart` is `true`.
