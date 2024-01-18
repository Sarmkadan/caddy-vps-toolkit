# Architecture Deep Dive

Comprehensive technical overview of caddy-vps-toolkit's design and implementation.

## System Architecture

```
┌────────────────────────────────────────────────────────┐
│                      CLI Layer                         │
│  (ArgumentParser, CommandRegistry, CliCommandHandler) │
└─────────────────────┬────────────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
  ┌──────────┐  ┌──────────┐  ┌──────────┐
  │ Service  │  │ Caddy    │  │ Systemd  │
  │ Commands │  │ Commands │  │ Commands │
  └────┬─────┘  └────┬─────┘  └────┬─────┘
       │             │             │
       └─────────────┼─────────────┘
                     │
       ┌─────────────┼─────────────┐
       │             │             │
       ▼             ▼             ▼
  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
  │ Service      │  │ Caddy        │  │ Systemd Unit │
  │ Management   │  │ Configuration│  │ Service      │
  │ Service      │  │ Service      │  └──────────────┘
  └──────┬───────┘  └──────┬───────┘
         │                 │
         └─────────┬───────┘
                   │
         ┌─────────┼──────────┐
         │         │          │
         ▼         ▼          ▼
    ┌────────────────────────────────┐
    │  Repository Layer (Data Access)│
    │  - ServiceRepository           │
    │  - HealthCheckRepository       │
    │  - ConfigurationRepository     │
    └────────────────┬───────────────┘
                     │
         ┌───────────┴────────────┐
         │                        │
         ▼                        ▼
    ┌─────────────┐         ┌──────────────┐
    │  SQLite DB  │         │  Config File │
    │ services.db │         │ appsettings  │
    └─────────────┘         └──────────────┘
```

## Core Components

### 1. Domain Models

Located in `Domain/Models/`, these represent the core entities:

#### ManagedService

Core service representation with all configuration:

```csharp
public class ManagedService
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Port { get; set; }
    public string Domain { get; set; }
    public string? HealthCheckUrl { get; set; }
    public TimeSpan HealthCheckInterval { get; set; }
    public TimeSpan HealthCheckTimeout { get; set; }
    public int HealthCheckRetries { get; set; }
    public List<ServicePort> Ports { get; set; }
    public Dictionary<string, string> Environment { get; set; }
    public List<string> Dependencies { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### HealthCheckResult

Records health check execution outcomes:

```csharp
public class HealthCheckResult
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public bool IsHealthy { get; set; }
    public int? HttpStatusCode { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; }
}
```

#### CaddyConfig & CaddyRoute

Configuration generation models:

```csharp
public class CaddyRoute
{
    public string Domain { get; set; }
    public string Upstream { get; set; }
    public string? TlsIssuer { get; set; }
    public List<string> Middlewares { get; set; }
}

public class CaddyConfig
{
    public List<CaddyRoute> Routes { get; set; }
    public bool AutoHttps { get; set; }
    public string? GlobalSettings { get; set; }
}
```

#### SystemdUnitConfig

Systemd unit file representation:

```csharp
public class SystemdUnitConfig
{
    public string ServiceName { get; set; }
    public string ExecStart { get; set; }
    public string WorkingDirectory { get; set; }
    public string RestartPolicy { get; set; }
    public TimeSpan RestartDelay { get; set; }
    public List<string> Dependencies { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; }
}
```

### 2. Service Layer

Core business logic implementations:

#### ServiceManagementService

Handles CRUD operations and service lifecycle:

- `AddServiceAsync()`: Creates new service with validation
- `UpdateServiceAsync()`: Modifies service configuration
- `RemoveServiceAsync()`: Safely removes services
- `GetServiceAsync()`: Retrieves service by name
- `GetAllServicesAsync()`: Lists all services
- Validates service names, ports, and health check endpoints

#### CaddyConfigurationService

Generates and manages Caddy configuration:

- `GenerateCaddyfileAsync()`: Creates Caddyfile from services
- `ValidateConfigAsync()`: Syntax validation
- `ReloadCaddyAsync()`: Reloads Caddy without downtime
- `PreviewAsync()`: Shows configuration before applying
- Uses template engine for flexible output

#### HealthMonitoringService

Continuous health check execution:

- `CheckServiceHealthAsync()`: Single health check
- `StartMonitoringAsync()`: Background monitoring loop
- `StopMonitoringAsync()`: Graceful shutdown
- `GetHealthHistoryAsync()`: Historical data access
- Implements retry logic and configurable timeouts

#### SystemdUnitService

Systemd unit file generation:

- `GenerateUnitFileAsync()`: Creates unit file content
- `ValidateUnitAsync()`: Validates systemd syntax
- `GetUnitStatusAsync()`: Queries systemd state
- Handles dependencies and restart policies
- Supports multiple unit types (service, target, etc.)

### 3. Data Access Layer

Repository pattern implementation for database operations:

#### ServiceRepository

Manages ManagedService persistence:

```csharp
public interface IServiceRepository
{
    Task<ManagedService> GetByNameAsync(string name);
    Task<IEnumerable<ManagedService>> GetAllAsync();
    Task AddAsync(ManagedService service);
    Task UpdateAsync(ManagedService service);
    Task DeleteAsync(string name);
}
```

#### HealthCheckRepository

Persists health check results:

- Stores individual check results with timestamps
- Provides query methods for historical analysis
- Implements data retention policies
- Supports aggregated statistics

#### ConfigurationRepository

Manages application configuration:

- Stores key-value configuration pairs
- Supports versioning and rollback
- Provides secure storage for sensitive values
- Tracks configuration changes in audit log

### 4. CLI Layer

Command-line interface implementation:

#### ArgumentParser

Parses command-line arguments into structured format:

- Supports both long (`--option`) and short (`-o`) formats
- Handles positional arguments and flags
- Validates argument types and ranges
- Generates help text automatically

#### CommandRegistry

Centralized command registration system:

```csharp
var registry = new CommandRegistry();
registry.Register("service", new ServiceCommands());
registry.Register("caddy", new CaddyCommands());
registry.Register("health", new HealthCommands());
```

Each command module handles related operations (add, list, update, remove).

### 5. Supporting Systems

#### Middleware Pipeline

Processes all requests:

- **ErrorHandlingMiddleware**: Catches and formats exceptions
- **LoggingMiddleware**: Records all operations
- **RateLimitingMiddleware**: Prevents abuse

#### Event Bus

Decoupled event handling:

```csharp
// Service health changed
EventBus.Publish(new ServiceHealthChanged { 
    ServiceName = "api", 
    IsHealthy = false 
});

// Subscribers receive event
```

#### Background Workers

Long-running processes:

- **HealthCheckWorker**: Periodic health monitoring
- **MaintenanceWorker**: Database cleanup and optimization
- Coordinated by WorkerCoordinator

#### Metrics Collection

Real-time performance monitoring:

- Response times
- Health check success rates
- Service restart counts
- System resource usage

## Data Flow

### Service Creation Flow

```
User Input → ArgumentParser
   ↓
ArgumentValidator
   ↓
ServiceManagementService.AddServiceAsync()
   ↓
Validation Checks
   ↓
ServiceRepository.AddAsync() → SQLite
   ↓
EventBus.Publish(ServiceAdded)
   ↓
AuditLog Entry
   ↓
User Feedback
```

### Health Check Flow

```
HealthCheckWorker (Scheduled)
   ↓
HealthMonitoringService.CheckServiceHealthAsync()
   ↓
HttpClientWrapper.GetAsync(HealthCheckUrl)
   ↓
HealthCheckResult Created
   ↓
HealthCheckRepository.SaveAsync()
   ↓
EventBus.Publish(HealthCheckCompleted)
   ↓
Webhook Notification (if configured)
   ↓
Audit Log Entry
```

### Configuration Generation Flow

```
User Requests Caddyfile
   ↓
CaddyConfigurationService.GenerateCaddyfileAsync()
   ↓
ServiceRepository.GetAllAsync()
   ↓
TemplateEngine.Render(template, services)
   ↓
CaddyConfig Object
   ↓
String Output (Caddyfile)
   ↓
Optional: Validation & Preview
   ↓
Output to stdout or file
```

## Database Schema

### services Table

```sql
CREATE TABLE services (
    id INTEGER PRIMARY KEY,
    name TEXT UNIQUE NOT NULL,
    port INTEGER NOT NULL,
    domain TEXT NOT NULL,
    health_check_url TEXT,
    health_check_interval TEXT,
    health_check_timeout TEXT,
    health_check_retries INTEGER,
    environment_vars TEXT, -- JSON
    dependencies TEXT, -- JSON array
    enabled BOOLEAN DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### health_check_results Table

```sql
CREATE TABLE health_check_results (
    id INTEGER PRIMARY KEY,
    service_id INTEGER NOT NULL,
    is_healthy BOOLEAN NOT NULL,
    http_status_code INTEGER,
    response_time_ms INTEGER,
    error_message TEXT,
    checked_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (service_id) REFERENCES services(id)
);
```

### audit_log Table

```sql
CREATE TABLE audit_log (
    id INTEGER PRIMARY KEY,
    service_name TEXT,
    action TEXT NOT NULL,
    details TEXT, -- JSON
    user TEXT,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    ip_address TEXT
);
```

## Extension Points

### Custom Health Check Providers

Implement `IHealthCheckProvider`:

```csharp
public interface IHealthCheckProvider
{
    Task<HealthCheckResult> CheckAsync(string endpoint, TimeSpan timeout);
}
```

### Custom Output Formatters

Implement `IOutputFormatter`:

```csharp
public interface IOutputFormatter
{
    string Format(IEnumerable<ManagedService> services);
}
```

### Custom Middleware

Register in middleware pipeline:

```csharp
pipeline.Add(new CustomValidationMiddleware());
```

## Performance Considerations

### Database Optimization

- Indexes on frequently queried columns (name, service_id)
- Connection pooling for concurrent operations
- Prepared statements to prevent SQL injection
- Batch operations for bulk updates

### Health Check Optimization

- Parallel health checks (configurable concurrency)
- Caching of health status (configurable TTL)
- Exponential backoff for failing checks
- Separate thread pool for health monitoring

### Configuration Generation

- Template caching for repeated generation
- Incremental generation (only changed services)
- Lazy loading of service dependencies

## Security Architecture

### Input Validation

All user input validated at entry points:
- Service names: alphanumeric + hyphens
- Ports: 1-65535
- URLs: RFC 3986 compliant
- Environment variables: no shell metacharacters

### Access Control

- File permissions restricted (700 for config directory)
- Database encryption for sensitive data
- Audit logging for all modifications
- Optional password protection for sensitive operations

### Network Security

- HTTPS validation for health check URLs
- Certificate pinning support
- Webhook request signing (HMAC)
- Timeout protection against hanging requests

## Future Enhancements

### Planned Features

1. **Kubernetes Integration**: Export to Helm charts
2. **Service Mesh Support**: Istio/Linkerd configuration
3. **Advanced Scheduling**: Cron-based service operations
4. **Distributed Mode**: Multi-node VPS management
5. **Web UI**: Management dashboard
6. **Plugin System**: Third-party extensions
7. **Event Streaming**: Real-time event export

### Architectural Improvements

- CQRS pattern for scalability
- Event sourcing for audit trail
- GraphQL API alongside CLI
- Distributed tracing support
