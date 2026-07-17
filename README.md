![Build](https://github.com/sarmkadan/caddy-vps-toolkit/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/caddy-vps-toolkit)

# caddy-vps-toolkit

A .NET CLI tool for managing multiple services on a single VPS with integrated Caddy configuration generation, systemd unit management, and real-time health monitoring.

## Table of Contents

- [Overview](#overview)
- [Why caddy-vps-toolkit?](#why-caddy-vps-toolkit)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [CLI Reference](#cli-reference)
- [Configuration Reference](#configuration-reference)
- [API Reference](#api-reference)
- [Troubleshooting](#troubleshooting)
- [Advanced Topics](#advanced-topics)
- [Testing](#testing)
- [Performance](#performance)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Overview

**caddy-vps-toolkit** is a production-grade CLI tool designed for DevOps engineers and system administrators who manage multiple services on Linux VPS instances. It automates the most tedious aspects of VPS service management:

- **Caddy Configuration Generation**: Automatically generate Caddyfile entries for your services
- **Systemd Unit Management**: Create and maintain systemd unit files for automated service management
- **Health Monitoring**: Continuous health checks with configurable thresholds and actions
- **Service Orchestration**: Manage service lifecycle, dependencies, and startup order
- **Configuration Management**: Centralized configuration repository with validation and versioning
- **Backup & Restore**: Full JSON backups of all service configurations and the Caddyfile
- **Log Aggregation**: Unified log viewer that merges and filters entries across all service logs
- **SSL Certificate Checker**: Real-time TLS certificate status with expiry warnings
- **Audit Logging**: Complete audit trail of all service changes and health events
- **Webhook Integration**: External system integration and event notifications
- **Performance Monitoring**: Real-time metrics collection and reporting

## Why caddy-vps-toolkit?

### The Problem

Managing multiple services on a single VPS typically requires:

1. **Manual Caddy Configuration**: Writing reverse proxy rules by hand, prone to syntax errors
2. **Service Duplication**: Creating similar systemd units for each service
3. **Health Check Scripts**: Writing custom monitoring scripts for each service
4. **Configuration Sprawl**: Configs scattered across `/etc`, home directories, and deployment scripts
5. **Lack of Visibility**: No centralized view of service health and status
6. **Operational Risk**: Manual changes lead to configuration drift and downtime

### The Solution

caddy-vps-toolkit consolidates all service management into a single configuration source, generating:
- Validated Caddy reverse proxy configurations
- Consistent systemd unit files with proper dependencies
- Health monitoring with automatic remediation
- Audit logs for compliance and troubleshooting
- Webhook notifications for critical events

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   caddy-vps-toolkit CLI                     │
└────────────────────────┬────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
        ▼                ▼                ▼
    ┌────────────┐  ┌────────────┐  ┌────────────┐
    │  Service   │  │ Caddy      │  │ Systemd    │
    │ Management │  │ Generation │  │ Management │
    └─────┬──────┘  └─────┬──────┘  └─────┬──────┘
          │                │               │
          └────────────────┼───────────────┘
                           │
                ┌──────────┴──────────┐
                │                     │
                ▼                     ▼
           ┌─────────────┐      ┌──────────────┐
           │ Config Repo │      │ Health Check │
           │ (SQLite)    │      │ Service      │
           └─────────────┘      └──────────────┘
                │                     │
                │                     ▼
                │              ┌──────────────┐
                │              │ Metrics &    │
                │              │ Audit Logs   │
                │              └──────────────┘
                │
                ▼
         ┌────────────────┐
         │ Systemd/Caddy/ │
         │ Linux System   │
         └────────────────┘
```

## Installation

### Prerequisites

- Linux kernel 4.15+ (Ubuntu 18.04+, Debian 9+, CentOS 7+)
- .NET 10 runtime
- Caddy 2.7+
- systemd
- SQLite 3.8+

### From Source (Recommended for Development)

```bash
git clone https://github.com/Sarmkadan/caddy-vps-toolkit.git
cd caddy-vps-toolkit
dotnet build -c Release
dotnet publish -c Release -o ./publish
sudo ./publish/caddy-vps-toolkit config init
```

### From GitHub Releases

```bash
# Download the latest release
wget https://github.com/Sarmkadan/caddy-vps-toolkit/releases/download/v2.0.0/caddy-vps-toolkit-linux-x64.tar.gz
tar -xzf caddy-vps-toolkit-linux-x64.tar.gz
sudo ./install.sh
```

### Docker Installation

```bash
docker pull sarmkadan/caddy-vps-toolkit:latest
docker run -it sarmkadan/caddy-vps-toolkit:latest /app/caddy-vps-toolkit --help
```

### Docker Development Usage

To run the toolkit within the provided development environment:

1. **Start all services**:
   ```bash
   docker-compose up -d
   ```

2. **Access the toolkit container**:
   ```bash
   docker-compose exec caddy-vps-toolkit /app/caddy-vps-toolkit --help
   ```

3. **Stop all services**:
   ```bash
   docker-compose down
   ```

### From Package Manager (Ubuntu/Debian)

```bash
curl -fsSL https://apt.sarmkadan.com/pubkey.gpg | sudo apt-key add -
echo "deb https://apt.sarmkadan.com focal main" | sudo tee /etc/apt/sources.list.d/sarmkadan.list
sudo apt update
sudo apt install caddy-vps-toolkit
```

### Post-Installation

```bash
# Initialize the configuration database
caddy-vps-toolkit config init

# Verify installation
caddy-vps-toolkit --version

# Enable background health monitoring
sudo systemctl enable caddy-vps-toolkit-monitor
sudo systemctl start caddy-vps-toolkit-monitor
```

## Quick Start

### 1. Create Your First Service Configuration

```bash
caddy-vps-toolkit service add \
  --name my-web-app \
  --port 3000 \
  --domain app.example.com \
  --health-check-url http://localhost:3000/health
```

### 2. Generate Caddy Configuration

```bash
caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

### 3. Create Systemd Unit

```bash
caddy-vps-toolkit systemd generate my-web-app > /etc/systemd/system/my-web-app.service
sudo systemctl daemon-reload
sudo systemctl enable my-web-app
sudo systemctl start my-web-app
```

### 4. Start Health Monitoring

```bash
caddy-vps-toolkit monitor start --interval 30s
```

## Usage Examples

### Programming Examples

For programmatic usage, see the [examples](./examples) directory:

- [BasicUsage.cs](./examples/BasicUsage.cs): Minimal setup and service listing.
- [AdvancedUsage.cs](./examples/AdvancedUsage.cs): Creating and updating services with error handling.
- [IntegrationExample.cs](./examples/IntegrationExample.cs): Wiring toolkit services into an ASP.NET Core DI container.

### Example 1: Simple Web Application

```bash
# Add service
caddy-vps-toolkit service add \
  --name api \
  --port 8080 \
  --domain api.example.com

# Generate and apply Caddy config
caddy-vps-toolkit caddy generate | sudo tee /etc/caddy/Caddyfile
sudo systemctl reload caddy

# Verify health
caddy-vps-toolkit health check api
```

### Example 2: Multiple Microservices

```bash
# Service 1
caddy-vps-toolkit service add --name auth --port 5000 --domain auth.example.com

# Service 2
caddy-vps-toolkit service add --name database --port 5432 --domain db.example.com

# Service 3
caddy-vps-toolkit service add --name cache --port 6379 --domain cache.example.com

# View all services
caddy-vps-toolkit service list

# Generate complete Caddyfile
caddy-vps-toolkit caddy generate --format full
```

### Example 3: Health Check with Custom Interval

```bash
caddy-vps-toolkit service add \
  --name critical-app \
  --port 9000 \
  --domain critical.example.com \
  --health-check-interval 10s \
  --health-check-timeout 5s \
  --health-check-retries 3
```

### Example 4: Service with Environment Variables

```bash
caddy-vps-toolkit service add \
  --name node-app \
  --port 3000 \
  --domain app.example.com \
  --env NODE_ENV=production \
  --env LOG_LEVEL=info
```

### Example 5: Generate Systemd Unit with Dependencies

```bash
caddy-vps-toolkit systemd generate \
  --service web-app \
  --depends-on database \
  --depends-on cache \
  --restart-policy on-failure
```

### Example 6: Enable Webhook Notifications

```bash
caddy-vps-toolkit notification add-webhook \
  --name slack \
  --url https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
  --on-health-failure
```

### Example 7: Export Configuration for Backup

```bash
caddy-vps-toolkit config export --format json > backup.json
caddy-vps-toolkit config export --format yaml > backup.yaml
```

### Example 8: Monitor Service Metrics

```bash
caddy-vps-toolkit metrics collect \
  --service api \
  --duration 5m \
  --output prometheus
```

### Example 9: Batch Import Services

```bash
cat << 'EOF' | caddy-vps-toolkit service import --format json
[
  {
    "name": "api",
    "port": 8080,
    "domain": "api.example.com",
    "healthCheckUrl": "http://localhost:8080/health"
  },
  {
    "name": "web",
    "port": 3000,
    "domain": "web.example.com",
    "healthCheckUrl": "http://localhost:3000/health"
  }
]
EOF
```

### Example 10: View Audit Log

```bash
caddy-vps-toolkit audit log --limit 50 --service api
caddy-vps-toolkit audit log --start-time "2024-01-01" --end-time "2024-01-31"
```

### Example 11: Configure Rate Limiting

```bash
caddy-vps-toolkit service update api \
  --rate-limit "100/10s" \
  --rate-limit-status 429
```

### Example 12: Health Check Recovery Script

```bash
caddy-vps-toolkit service update api \
  --recovery-script /opt/scripts/restart-api.sh \
  --recovery-script-timeout 30s
```

## PerformanceMonitor

The `PerformanceMonitor` class is used to measure the performance of operations, providing detailed timing metrics for profiling and identifying bottlenecks. It allows you to mark milestones during an operation, measure the total elapsed time, and generate a structured report.

### Example Usage

```csharp
using CaddyVpsToolkit.Utilities;

// Initialize and start timing
using (var monitor = new PerformanceMonitor("DataProcessing"))
{
    // Perform operations...
    monitor.MarkMilestone("Step1Complete");
    
    // Simulate some work
    await Task.Delay(100);
    monitor.MarkMilestone("Step2Complete");

    // Get total elapsed time
    long elapsedMs = monitor.GetElapsedMs();
    Console.WriteLine($"Total time: {elapsedMs}ms");

    // Generate a detailed report
    string report = monitor.GetReport();
    Console.WriteLine(report);
}

// Measure async operations using static helper methods
var (result, elapsed) = await PerformanceMonitor.TimeAsync(async () =>
{
    await Task.Delay(50);
    return "OperationResult";
});

Console.WriteLine($"Result: {result}, Elapsed: {elapsed}ms");
```

## AuditLogEntry

The `AuditLogEntry` type represents a single entry in the audit log, tracking important operations performed within the caddy-vps-toolkit system. It captures metadata about actions including who performed them, when they occurred, what was affected, and the outcome. This type is essential for compliance, troubleshooting, and maintaining an operational history of all system changes.

Example usage:

```csharp
// Create a new audit log entry for a service creation
var entry = new AuditLogEntry
{
    Action = "ServiceCreated",
    Actor = "admin@sarmkadan.com",
    Target = "web-app-01",
    Result = "Success",
    Details = new Dictionary<string, object>
    {
        ["ServiceName"] = "web-app-01",
        ["Port"] = 3000,
        ["Domain"] = "web.example.com",
        ["UserId"] = "vlad-123",
        ["IpAddress"] = "192.168.1.100"
    }
};

// Log the entry using the audit log service
var auditLog = new FileAuditLog("/var/log/caddy-vps-toolkit/audit.log");
await auditLog.LogAsync(
    action: "ServiceCreated",
    actor: "admin@sarmkadan.com",
    target: "web-app-01",
    result: "Success",
    details: new Dictionary<string, object>
    {
        ["ServiceName"] = "web-app-01",
        ["Port"] = 3000,
        ["Domain"] = "web.example.com"
    }
);

// Retrieve audit entries within a specific time range
var entries = await auditLog.GetEntriesAsync(
    from: DateTime.UtcNow.AddDays(-7),
    to: DateTime.UtcNow
);

Console.WriteLine($"Found {entries.Count} audit entries in the last 7 days");

// Get summary of actions performed
var actionSummary = auditLog.GetActionSummary();
foreach (var kvp in actionSummary)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value} occurrences");
}

// Get all entries by a specific actor
var userEntries = auditLog.GetEntriesByActor("admin@sarmkadan.com");
Console.WriteLine($"User has {userEntries.Count} entries in the audit log");
```

## CLI Reference

### Global Options

```
--config FILE              Path to configuration file (default: ~/.caddy-vps-toolkit/config.json)
--verbose, -v             Enable verbose output
--quiet, -q               Suppress non-error output
--format FORMAT           Output format: json, yaml, text, csv (default: text)
--output FILE, -o FILE    Write output to file instead of stdout
```

### Commands

#### service

Service management commands.

**Add a Service**
```bash
caddy-vps-toolkit service add \
  --name NAME \
  --port PORT \
  --domain DOMAIN \
  [--health-check-url URL] \
  [--health-check-interval DURATION] \
  [--env KEY=VALUE]...
```

**List Services**
```bash
caddy-vps-toolkit service list [--format json|yaml|table]
```

**Show Service Details**
```bash
caddy-vps-toolkit service show NAME
```

**Update Service**
```bash
caddy-vps-toolkit service update NAME [--port PORT] [--domain DOMAIN]
```

**Remove Service**
```bash
caddy-vps-toolkit service remove NAME [--force]
```

#### caddy

Caddy configuration generation.

**Generate Caddyfile**
```bash
caddy-vps-toolkit caddy generate \
  [--output FILE] \
  [--format full|minimal] \
  [--include-comments]
```

**Validate Configuration**
```bash
caddy-vps-toolkit caddy validate [--file FILE]
```

**Preview Configuration**
```bash
caddy-vps-toolkit caddy preview SERVICE_NAME
```

#### systemd

Systemd unit management.

**Generate Unit File**
```bash
caddy-vps-toolkit systemd generate SERVICE_NAME \
  [--output FILE] \
  [--restart-policy on-failure|always|no] \
  [--depends-on SERVICE]...
```

**List Units**
```bash
caddy-vps-toolkit systemd list
```

**Status**
```bash
caddy-vps-toolkit systemd status SERVICE_NAME
```

#### health

Health monitoring commands.

**Check Service Health**
```bash
caddy-vps-toolkit health check SERVICE_NAME
```

**Monitor Continuous**
```bash
caddy-vps-toolkit health monitor \
  --interval DURATION \
  [--service SERVICE] \
  [--webhook-url URL]
```

**View Health History**
```bash
caddy-vps-toolkit health history SERVICE_NAME --limit 100
```

#### config

Configuration management.

**Initialize**
```bash
caddy-vps-toolkit config init [--path PATH]
```

**Export**
```bash
caddy-vps-toolkit config export [--format json|yaml] [--output FILE]
```

**Import**
```bash
caddy-vps-toolkit config import --file FILE [--format json|yaml]
```

**Validate**
```bash
caddy-vps-toolkit config validate [--file FILE]
```

#### audit

Audit log management.

**View Logs**
```bash
caddy-vps-toolkit audit log \
  [--limit N] \
  [--service NAME] \
  [--start-time TIME] \
  [--end-time TIME]
```

**Export Logs**
```bash
caddy-vps-toolkit audit export --format json --output FILE
```

#### backup

Create and restore full configuration backups.

**Create a Backup**
```bash
caddy-vps-toolkit backup-create [--output <path>] [--description "weekly snapshot"]
```
Creates a JSON backup containing all managed service configurations, application
settings, and the current Caddyfile. If `--output` is omitted the file is written
to the config directory with an auto-generated timestamped name.

**Restore a Backup**
```bash
caddy-vps-toolkit backup-restore /path/to/backup-20250115_120000.backup.json
```
Re-creates any services and configuration keys missing from the current database
and writes the Caddyfile back to disk. Existing records are updated in place.

**List Backups**
```bash
caddy-vps-toolkit backup-list [--dir /path/to/backups]
```
Prints all `*.backup.json` files found in the configured (or specified) directory.

## BackupManifest

The `BackupManifest` type represents a backup manifest that tracks all configuration components included in a backup.

It contains metadata about the backup itself (ID, creation timestamp, application version) and references to all managed services, their configurations, and the Caddyfile content.

Example usage:
```csharp
// Create a backup manifest for a configuration backup
var backupManifest = new BackupManifest
{
    BackupId = Guid.NewGuid().ToString(),
    CreatedAt = DateTime.UtcNow,
    AppVersion = "2.0.0",
    Services = new List<ManagedService>
    {
        new ManagedService
        {
            Name = "web-app",
            Port = 3000,
            Domain = "app.example.com",
            HealthCheck = new HealthCheckConfig
            {
                Url = "http://localhost:3000/health",
                Interval = TimeSpan.FromSeconds(30)
            }
        },
        new ManagedService
        {
            Name = "api-service",
            Port = 8080,
            Domain = "api.example.com"
        }
    },
    Configuration = new Dictionary<string, string>
    {
        ["Caddy.AdminPort"] = "2019",
        ["HealthCheck.DefaultInterval"] = "30s",
        ["Systemd.EnableOnCreation"] = "true"
    },
    CaddyfileContent = "example.com {\n    reverse_proxy localhost:3000\n}",
    Description = "Weekly backup including web-app and api-service"
};

// Validate the backup manifest
backupManifest.Validate();

// Serialize to JSON for backup storage
string json = JsonSerializer.Serialize(backupManifest, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText($"backup-{backupManifest.BackupId}.json", json);
```

#### logs-view

Aggregate and filter application logs from all service log files.

```bash
caddy-vps-toolkit logs-view \
  [--lines N]         # Number of entries to show (default: 100)
  [--level LEVEL]     # Minimum level: Debug, Info, Warning, Error
  [--service ID]      # Filter to a specific service ID
  [--since DATETIME]  # Only show entries on or after this timestamp
```

Entries are parsed from the structured `[timestamp] [level] message` format
written by the application logger and sorted newest-first.

**Examples**
```bash
# Last 50 warning-or-above entries
caddy-vps-toolkit logs-view --lines 50 --level Warning

# Errors from a specific service since yesterday
caddy-vps-toolkit logs-view --level Error --service my-api --since 2025-05-25
```

#### ssl-check

Check the SSL/TLS certificate status for one domain or all services.

**Single domain**
```bash
caddy-vps-toolkit ssl-check example.com
```

**All registered services**
```bash
caddy-vps-toolkit ssl-check
```

Output includes expiry date, days remaining, and a status classification:
- `Valid` — certificate is healthy
- `ExpiringSoon` — expires within 30 days
- `Critical` — expires within 7 days
- `Expired` — certificate has already expired
- `Error` — could not connect or retrieve the certificate

## TemplateEngine

The `TemplateEngine` class provides simple string template substitution using `{{variable}}` syntax. It's ideal for generating configuration files, service definitions, and other text-based content where you need to substitute placeholders with actual values. The engine supports both instance-based variable management and static rendering with inline dictionaries.

### Example Usage

```csharp
// Create a template engine with predefined variables
var engine = new TemplateEngine();

// Set template variables
engine.Set("serviceName", "api-service");
engine.Set("port", 8080);
engine.Set("domain", "api.example.com");
engine.Set("timeout", 30000);

// Render a template with variable substitution
string template = @"
service {{serviceName}} {
    listen {{port}};
    domain {{domain}};
    timeout {{timeout}};
}
";

string rendered = engine.Render(template);
Console.WriteLine(rendered);

// Output:
// service api-service {
//     listen 8080;
//     domain api.example.com;
//     timeout 30000;
// }

// Create template engine with initial variables
var variables = new Dictionary<string, object>
{
    ["appName"] = "web-app",
    ["port"] = 3000,
    ["environment"] = "production"
};
var engineWithVars = new TemplateEngine(variables);

// Render template
string configTemplate = "Application {{appName}} running on port {{port}} in {{environment}} mode";
string appConfig = engineWithVars.Render(configTemplate);
Console.WriteLine(appConfig);
// Output: Application web-app running on port 3000 in production mode

// Static rendering with inline dictionary
string staticRender = TemplateEngine.Render(
    "Service {{name}} listening on {{port}}",
    new Dictionary<string, object> { ["name"] = "cache", ["port"] = 6379 }
);
Console.WriteLine(staticRender);
// Output: Service cache listening on 6379

// Access and modify variables
var engine = new TemplateEngine();
engine.Set("version", "1.0.0");
Console.WriteLine(engine.Get("version")); // Output: 1.0.0
engine.Set("version", "2.0.0");
Console.WriteLine(engine.Get("version")); // Output: 2.0.0
```

## IServiceRepository

The `IServiceRepository` interface provides data access methods for managing service configurations in the SQLite database. It serves as the primary contract for CRUD operations on managed services, enabling the application to persist service definitions, retrieve service information, and maintain system state across application restarts.

This repository interface abstracts database operations for service management, providing methods to create, read, update, and delete service configurations while supporting additional operations like searching, filtering by service type, and retrieving enabled services only.

### Example Usage

```csharp
// Create a service repository with database context
var repository = new ServiceRepository(dbContext);

// Add a new managed service
var newService = new ManagedService
{
    Name = "api-service",
    Type = ServiceType.WebApplication,
    Port = 8080,
    Domain = "api.example.com",
    IsEnabled = true,
    HostBinding = "localhost"
};

string serviceId = await repository.AddAsync(newService);
Console.WriteLine($"Created service with ID: {serviceId}");

// Retrieve a service by ID
var retrievedService = await repository.GetByIdAsync(serviceId);
Console.WriteLine($"Retrieved service: {retrievedService.Name} on port {retrievedService.Port}");

// Update service properties
retrievedService.Port = 9000;
bool updateSuccess = await repository.UpdateAsync(retrievedService);
Console.WriteLine($"Update successful: {updateSuccess}");

// Get all enabled services
var enabledServices = await repository.GetEnabledServicesAsync();
Console.WriteLine($"Found {enabledServices.Count} enabled services");

// Check if service exists
bool exists = await repository.ExistsAsync(serviceId);
Console.WriteLine($"Service exists: {exists}");

// Get total service count
int totalServices = await repository.GetCountAsync();
Console.WriteLine($"Total services in database: {totalServices}");

// Search for services by name
var searchResults = await repository.SearchAsync("api");
Console.WriteLine($"Found {searchResults.Count} services matching 'api'");

// Delete a service
bool deleteSuccess = await repository.DeleteAsync(serviceId);
Console.WriteLine($"Service deleted: {deleteSuccess}");
```

## ServiceRepository

The `ServiceRepository` class provides a concrete implementation of the `IServiceRepository` interface for managing service configurations in a SQLite database. It handles all CRUD operations for `ManagedService` entities, including retrieval by ID, name, service type, and enabled status, as well as searching and counting services.

This repository is responsible for persisting service definitions to the SQLite database, enabling the application to maintain system state across application restarts and providing efficient data access methods for service management operations.

## ICacheService

The `ICacheService` interface provides an abstraction for caching operations with support for asynchronous operations and expiration. It's designed for in-memory caching scenarios and can be easily replaced with distributed cache implementations like Redis for production environments requiring multiple servers.

This interface supports common caching patterns including get/set operations, existence checks, cache clearing, and a convenience method for getting or setting values with automatic fallback to a factory function.

### Example Usage

```csharp
// Create cache service instance (typically injected via DI)
var cache = new MemoryCache();

// Basic set and get operations
await cache.SetAsync("user:123", new User { Id = 123, Name = "Alice", Email = "alice@example.com" }, TimeSpan.FromMinutes(30));

var user = await cache.GetAsync<User>("user:123");
if (user != null)
{
    Console.WriteLine($"Retrieved user: {user.Name}");
}

// Check if key exists
bool exists = await cache.ExistsAsync("user:123");
Console.WriteLine($"Key exists: {exists}");

// Remove a specific key
await cache.RemoveAsync("user:123");

// Clear entire cache
await cache.ClearAsync();

// Get or set with fallback (cache-aside pattern)
var product = await cache.GetOrSetAsync(
    "product:456",
    async () => await FetchProductFromDatabaseAsync(456),
    TimeSpan.FromHours(1)
);

// Create composite cache keys
string cacheKey = CacheExtensions.MakeCacheKey("service", "health", "api-service-01");
await cache.SetAsync(cacheKey, healthStatus, TimeSpan.FromSeconds(15));

// Get cache statistics
int cacheSize = cache.GetCacheSize();
Console.WriteLine($"Current cache size: {cacheSize} entries");

// Clean expired entries (optional maintenance)
// cache.CleanExpiredEntries();

// Generic type usage with expiration
await cache.SetAsync("config:timeout", 30000, TimeSpan.FromDays(1));
int timeout = await cache.GetAsync<int>("config:timeout");
```

### Example Usage

```csharp
// Create a service repository instance
var repository = new ServiceRepository();

// Add a new managed service
var newService = new ManagedService
{
    Name = "api-service",
    Type = ServiceType.WebApplication,
    Port = 8080,
    Domain = "api.example.com",
    IsEnabled = true,
    HostBinding = "localhost",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/api-service",
    Description = "Production API service",
    Status = ServiceStatus.Stopped,
    AutoStart = true,
    Priority = 100,
    EnvironmentVariables = "ASPNETCORE_ENVIRONMENT=Production"
};

string serviceId = await repository.AddAsync(newService);
Console.WriteLine($"Created service with ID: {serviceId}");

// Retrieve a service by ID
var retrievedService = await repository.GetByIdAsync(serviceId);
Console.WriteLine($"Retrieved service: {retrievedService.Name} on port {retrievedService.Port}");

// Retrieve a service by name
var serviceByName = await repository.GetByNameAsync("api-service");
Console.WriteLine($"Found service by name: {serviceByName.Name}");

// Update service properties
retrievedService.Port = 9090;
bool updateSuccess = await repository.UpdateAsync(retrievedService);
Console.WriteLine($"Update successful: {updateSuccess}");

// Get all enabled services
var enabledServices = await repository.GetEnabledServicesAsync();
Console.WriteLine($"Found {enabledServices.Count} enabled services");

// Get services by type
var webServices = await repository.GetByTypeAsync(ServiceType.WebApplication);
Console.WriteLine($"Found {webServices.Count} web application services");

// Check if service exists
bool exists = await repository.ExistsAsync(serviceId);
Console.WriteLine($"Service exists: {exists}");

// Get total service count
int totalServices = await repository.GetCountAsync();
Console.WriteLine($"Total services in database: {totalServices}");

// Search for services by name
var searchResults = await repository.SearchAsync("api");
Console.WriteLine($"Found {searchResults.Count} services matching 'api'");

// Delete a service
bool deleteSuccess = await repository.DeleteAsync(serviceId);
Console.WriteLine($"Service deleted: {deleteSuccess}");
```

## ServiceManagementService

The `ServiceManagementService` is the primary interface for managing the lifecycle of VPS services, including creating, updating, removing, and querying service status. It acts as a wrapper around the `IServiceRepository` to ensure all operations are validated and follow consistent configuration rules.

### Example Usage

```csharp
// Assuming you have an IServiceRepository implementation
var repository = new ServiceRepository(dbContext);
var serviceManagement = new ServiceManagementService(repository);

// Create a new managed service
var newService = new ManagedService
{
    Name = "my-api",
    Type = ServiceType.WebApplication,
    Port = 8080,
    Domain = "api.example.com",
    IsEnabled = true
};
string serviceId = await serviceManagement.CreateServiceAsync(newService);

// Update service status to Running
await serviceManagement.UpdateServiceStatusAsync(serviceId, ServiceStatus.Running);

// Check if service exists
bool exists = await serviceManagement.ServiceExistsAsync(serviceId);

// Get all enabled services
var enabledServices = await serviceManagement.GetEnabledServicesAsync();

// Update priority
await serviceManagement.UpdateServicePriorityAsync(serviceId, 50);

// Delete the service if it's not running
await serviceManagement.UpdateServiceStatusAsync(serviceId, ServiceStatus.Stopped);
await serviceManagement.DeleteServiceAsync(serviceId);
```

## MetricsCollector

The `MetricsCollector` class provides real-time metrics collection and reporting capabilities for monitoring application performance and system health. It supports three metric types: counters for tracking occurrences, gauges for measuring current values, and histograms for analyzing distributions of measurements.

### Example Usage

```csharp
// Create metrics collector instance
var metrics = new MetricsCollector();

// Increment a counter for tracking API requests
metrics.IncrementCounter("api.requests.total");
metrics.IncrementCounter("api.requests.total");
metrics.IncrementCounter("api.requests.total");

// Increment counter with custom value
metrics.IncrementCounter("api.errors", 5); // Record 5 errors

// Set gauge values for current system state
metrics.SetGauge("system.memory.usage", 75.5); // 75.5% memory used
metrics.SetGauge("system.cpu.load", 2.3); // Current CPU load

// Record histogram values for response times
metrics.RecordHistogram("api.response.time.ms", 45.2);
metrics.RecordHistogram("api.response.time.ms", 89.1);
metrics.RecordHistogram("api.response.time.ms", 12.7);
metrics.RecordHistogram("api.response.time.ms", 203.4);
metrics.RecordHistogram("api.response.time.ms", 67.8);

// Retrieve metric values
long totalRequests = metrics.GetCounter("api.requests.total");
double memoryUsage = metrics.GetGauge("system.memory.usage");
var responseStats = metrics.GetHistogramStats("api.response.time.ms");

Console.WriteLine($"Total API requests: {totalRequests}");
Console.WriteLine($"Memory usage: {memoryUsage}%");
Console.WriteLine($"Response time stats: count={responseStats.Count}, min={responseStats.Min:F2}ms, max={responseStats.Max:F2}ms, avg={responseStats.Average:F2}ms, median={responseStats.Median:F2}ms");

// Generate comprehensive report
string report = metrics.GenerateReport();
Console.WriteLine(report);
```

## CaddyConfig

The `CaddyConfig` type represents the global configuration for Caddy reverse proxy. It controls core settings like ports, timeouts, logging behavior, and TLS configuration that apply across all services managed by the toolkit.



### Example Usage

```csharp
// Create a Caddy configuration with custom settings
var caddyConfig = new CaddyConfig
{
    AdminEmail = "admin@example.com",
    AdminPort = 2019,
    AdminHost = "127.0.0.1",
    HttpPort = 80,
    HttpsPort = 443,
    LogLevel = "info",
    LogFormat = "{common_log}",
    EnableMetrics = true,
    EnableLogOutput = true,
    IdleTimeout = 120,
    ReadTimeout = 30,
    WriteTimeout = 30,
    AutoHttpsDisabled = false,
    TlsPolicy = "clients",
    StrictSniRequired = false,
    OnDemandTls = new List<string> { "example.com", "*.example.com" },
    CertificateEmail = "admin@example.com",
    CustomGlobals = new Dictionary<string, string>
    {
        ["debug"] = "true"
    },
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the configuration
caddyConfig.Validate();

// Set default values for any missing required fields
caddyConfig.SetDefaultValues();

// Generate Caddyfile global configuration block
string caddyfileGlobals = caddyConfig.GenerateCaddyfileGlobals();
Console.WriteLine(caddyfileGlobals);
```



## CaddyRoute


The `CaddyRoute` type represents an individual route configuration for Caddy reverse proxy. It defines how incoming HTTP/HTTPS requests are routed to backend services based on domain, path, and other routing rules. Each route can be customized with various options like path stripping, host header preservation, timeouts, rate limiting, and authentication.



### Example Usage


```csharp
// Create a route for a web application service
var route = new CaddyRoute
{

Id = Guid.NewGuid().ToString(),

ServiceId = "web-app-01",

Domain = "app.example.com",

Path = "/api/*",

UpstreamUrl = "http://localhost:8080",

StripPath = true,

PreserveHostHeader = true,

TimeoutSeconds = 30,

EnableHttps = true,

AutoRedirectHttp = true,

CustomHeaders = new Dictionary<string, string>
{
["X-Content-Type-Options"] = "nosniff",
["X-Frame-Options"] = "DENY"
},

RateLimitRule = "100/10s",

BasicAuthEnabled = false,

IsActive = true,

CreatedAt = DateTime.UtcNow,

UpdatedAt = DateTime.UtcNow
};


// Validate the route configuration
route.Validate();

// Generate Caddy configuration block for this route
string caddyRouteBlock = route.GenerateCaddyRouteBlock();
Console.WriteLine(caddyRouteBlock);
```

**Key Properties:**

- **Id**: Unique identifier for the configuration
- **AdminEmail**: Email address for administrative notifications and certificate registration
- **AdminPort**: Port for the Caddy admin interface (default: 2019)
- **AdminHost**: Host address for the admin interface (default: "localhost")
- **EnableMetrics**: Enable Caddy metrics endpoint (default: true)
- **EnableLogOutput**: Enable structured log output (default: true)
- **LogLevel**: Logging verbosity level (default: "info")
- **LogFormat**: Format string for log entries (default: "{common_log}")
- **HttpPort**: HTTP port for incoming traffic (default: 80)
- **HttpsPort**: HTTPS port for incoming traffic (default: 443)
- **AutoHttpsDisabled**: Disable automatic HTTPS certificate provisioning
- **TlsPolicy**: TLS policy for client connections (default: "clients")
- **IdleTimeout**: Idle connection timeout in seconds (default: 120)
- **ReadTimeout**: Read timeout in seconds (default: 30)
- **WriteTimeout**: Write timeout in seconds (default: 30)


## SslCertificateExtensions

The `SslCertificateExtensions` class provides extension methods for registering SSL certificate monitoring services into the dependency injection container and utility methods for checking SSL certificate status. It simplifies SSL certificate monitoring integration and provides clear status indicators for certificate health.

### Example Usage

```csharp
// Register SSL certificate monitoring in your application startup
var services = new ServiceCollection();
services.AddSslCertificateMonitoring();

// For a specific managed service with public host binding
var webService = new ManagedService
{
    Id = "web-app-01",
    Name = "Web Application",
    HostBinding = "example.com",
    Port = 443,
    Protocol = "https"
};

services.AddSslCertificateMonitoring(webService);

// Check certificate status
var certificate = new SslCertificateInfo
{
    Issuer = "Let's Encrypt R3",
    Subject = "CN=example.com",
    ExpiresAt = DateTime.UtcNow.AddDays(15),
    DaysUntilExpiry = 15,
    SerialNumber = "1234567890ABCDEF",
    Thumbprint = "A1B2C3D4E5F6"
};

// Get certificate status
var status = certificate.GetCertificateStatus();
Console.WriteLine($"Certificate status: {status}");

// Check if renewal is required
if (certificate.RequiresRenewal())
{
    Console.WriteLine("Certificate requires immediate renewal!");
}

// Get human-readable status message
string statusMessage = certificate.GetStatusMessage();
Console.WriteLine(statusMessage);
```

## HealthCheckConfig

The `HealthCheckConfig` type represents configuration for service health monitoring. It defines parameters for checking service availability and responsiveness, including check intervals, timeout settings, failure thresholds, and expected response conditions. Health checks are essential for automatic service recovery and maintaining system reliability.

### Example Usage

```csharp
// Create a basic HTTP health check configuration for a web service
var healthCheck = new HealthCheckConfig
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "web-app-01",
    Type = HealthCheckType.Http,
    Endpoint = "/health",
    IntervalSeconds = 30,
    TimeoutSeconds = 5,
    UnhealthyThreshold = 3,
    HealthyThreshold = 2,
    Enabled = true,
    HttpMethod = "GET",
    ExpectedHttpStatus = 200,
    ExpectedResponse = "OK",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the health check configuration
healthCheck.Validate();

// Get the health check URL for a specific service
string healthUrl = healthCheck.GetHealthCheckUrl("localhost", 3000);
Console.WriteLine($"Health check URL: {healthUrl}");

// Create a TCP health check for database connectivity
var tcpHealthCheck = new HealthCheckConfig
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "database-01",
    Type = HealthCheckType.Tcp,
    IntervalSeconds = 60,
    TimeoutSeconds = 10,
    UnhealthyThreshold = 5,
    HealthyThreshold = 3,
    Enabled = true
};

tcpHealthCheck.Validate();
```

## UpstreamManagementOptions

The `UpstreamManagementOptions` type provides configuration for the v2 adaptive upstream management and health-aware load-balancing subsystem. It controls how upstream servers are scored, penalized, and selected for traffic routing based on latency, error rates, and connection pressure.

This type is typically bound from `appsettings.json` under the `"UpstreamManagement"` key or supplied programmatically via dependency injection.

### Example Usage

```csharp
// Configure upstream management options in appsettings.json
var upstreamManagementConfig = new UpstreamManagementOptions
{
    // Composite score weights (sum doesn't need to equal 1.0 - they're normalized internally)
    LatencyWeight = 0.45,      // Prefer low-latency backends
    ErrorRateWeight = 0.40,     // Penalize error-prone upstreams
    ConnectionWeight = 0.15,    // Consider connection pressure
    
    // Latency target and normalization
    TargetLatencyMs = 150.0,    // Target p99 latency in milliseconds
    MaxExpectedConnections = 200, // Expected max concurrent connections
    
    // Adaptive weight adjustment
    WeightAdaptationAlpha = 0.20, // Faster adaptation to changing conditions
    
    // Failure penalty configuration
    PenaltyMultiplier = 0.25,   // Reduce score to 25% after failure
    PenaltyDecaySeconds = 45.0,  // Penalty decays over 45 seconds
    
    // Metrics and windowing
    MetricsWindowSize = 250,     // Track 250 request samples per upstream
    
    // Periodic recalibration
    AutoRecalibrationEnabled = true,      // Enable automatic recalibration
    RecalibrationIntervalSeconds = 600     // Recalibrate every 10 minutes
};

// Register with dependency injection (typically in Program.cs or Startup.cs)
builder.Services.AddUpstreamManagement(upstreamManagementConfig);

// Or use default configuration
builder.Services.AddUpstreamManagement();
```

## LoadBalancingOptionsExtensions

The `LoadBalancingOptionsExtensions` class provides extension methods for the `LoadBalancingOptions` type that simplify common configuration patterns and enable fluent-style configuration of load balancing behavior. These extensions allow you to configure round-robin and least-connections strategies with health checks, set up circuit breakers, enable sticky sessions, and retrieve configuration values for monitoring and reporting purposes.

### Example Usage

```csharp
using CaddyVpsToolkit.LoadBalancing;

// Create load balancing options with round-robin strategy and health checks
var options = new LoadBalancingOptions()
    .UseRoundRobinWithHealthChecks(
        healthCheckIntervalSeconds: 30,
        healthProbeTimeoutMs: 5000,
        unhealthyThreshold: 3,
        healthyThreshold: 2
    )
    .ConfigureCircuitBreaker(
        healthThreshold: 0.5,  // Circuit opens when less than 50% of upstreams are healthy
        recoverySeconds: 60
    )
    .EnableStickySessions(
        cookieName: "X-Session-Id",
        drainTimeoutSeconds: 30
    );

// Get configuration values for monitoring
int healthCheckIntervalMs = options.GetHealthCheckIntervalMs();
double healthProbeTimeoutSeconds = options.GetHealthProbeTimeoutSeconds();
bool isStrictCircuitBreaker = options.IsStrictCircuitBreaker();

// Retrieve retry configuration
var retryConfig = options.GetRetryConfiguration();
foreach (var kvp in retryConfig)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Alternative: Use least-connections strategy
var leastConnectionsOptions = new LoadBalancingOptions()
    .UseLeastConnectionsWithHealthChecks(
        healthCheckIntervalSeconds: 20,
        healthProbeTimeoutMs: 3000,
        unhealthyThreshold: 2,
        healthyThreshold: 1
    );
```

## UpstreamPoolRepository

The `UpstreamPoolRepository` provides an interface to manage `UpstreamPool` configurations in the SQLite database. It handles the persistence of upstream pool definitions, including health check settings, load balancing strategies, and server lists.

### Example Usage

```csharp
// Instantiate the repository (ensures database and table exist)
var repository = new UpstreamPoolRepository();

// Create a new upstream pool
var newPool = new UpstreamPool
{
    Name = "api-pool",
    ServiceId = "api-service",
    Strategy = LoadBalancingStrategy.RoundRobin,
    Servers = new List<UpstreamServer> { new UpstreamServer("localhost", 8080) },
    IsEnabled = true
};

// Add to database
string poolId = await repository.AddAsync(newPool);

// Check if it exists
if (await repository.ExistsAsync(poolId))
{
    // Update the pool
    var pool = await repository.GetByIdAsync(poolId);
    if (pool != null)
    {
        pool.PassiveHealthEnabled = true;
        await repository.UpdateAsync(pool);
    }
}

// Retrieve all pools for a specific service
var servicePools = await repository.GetByServiceIdAsync("api-service");

// Delete a pool
await repository.DeleteAsync(poolId);
```

## ConfigurationRepository

The `ConfigurationRepository` is a SQLite-based repository for managing application-level configuration values. It provides asynchronous methods to store, retrieve, delete, and list configuration entries, ensuring persistent storage for application settings.

### Example Usage

```csharp
// Instantiate the repository
var repo = new ConfigurationRepository();

// Set a configuration value
await repo.SetValueAsync("app:theme", "dark");

// Retrieve a specific value
string theme = await repo.GetValueAsync("app:theme");
Console.WriteLine($"Current theme: {theme}");

// Retrieve all configuration values
var allConfig = await repo.GetAllAsync();
Console.WriteLine($"Total settings: {allConfig.Count}");

// Delete a setting
await repo.DeleteAsync("app:theme");
```

## Configuration Reference

### appsettings.json

```json
{
  "CaddyVpsToolkit": {
    "Database": {
      "ConnectionString": "Data Source=services.db",
      "EnableMigrations": true
    },
    "Caddy": {
      "ConfigPath": "/etc/caddy/Caddyfile",
      "BinaryPath": "/usr/bin/caddy",
      "ReloadCommandTimeout": "10s"
    },
    "Systemd": {
      "UnitDirectory": "/etc/systemd/system",
      "EnableOnCreation": true,
      "RestartPolicy": "on-failure"
    },
    "HealthCheck": {
      "DefaultInterval": "30s",
      "DefaultTimeout": "5s",
      "DefaultRetries": 3,
      "EnableAutoRecovery": true
    },
    "Logging": {
      "LogLevel": "Information",
      "EnableFileLogging": true,
      "LogFilePath": "/var/log/caddy-vps-toolkit"
    },
    "Monitoring": {
      "EnableMetrics": true,
      "MetricsPort": 9090,
      "EnableAuditLog": true
    }
  }
}
```

### Service Configuration Schema

```json
{
  "name": "string",
  "port": "number",
  "domain": "string",
  "healthCheckUrl": "string",
  "healthCheckInterval": "duration",
  "healthCheckTimeout": "duration",
  "healthCheckRetries": "number",
  "environment": {
    "KEY": "value"
  },
  "dependencies": ["service1", "service2"],
  "restartPolicy": "on-failure|always|no",
  "maxRestarts": "number",
  "rateLimit": "string",
  "enabled": "boolean"
}
```

## API Reference

### Core Services

#### BatchProcessor

`BatchProcessor<T>` processes items in configurable batch sizes for efficient bulk operations. It's ideal for optimizing database operations, API calls, and other scenarios where processing items individually would be inefficient.

```csharp
// Example: Processing a list of service configurations in batches
var services = new List<ManagedService> {
    new ManagedService { Name = "api", Port = 8080, Domain = "api.example.com" },
    new ManagedService { Name = "web", Port = 3000, Domain = "web.example.com" },
    new ManagedService { Name = "cache", Port = 6379, Domain = "cache.example.com" }
};

var batchProcessor = new BatchProcessor<ManagedService>(
    batchSize: 50,
    processFunction: async batch => {
        // Process each batch of services
        foreach (var service in batch) {
            Console.WriteLine($"Processing service: {service.Name}");
            // Your batch processing logic here
        }
    }
);

await batchProcessor.ProcessAsync(services);
```

#### ServiceManagementService

```csharp
public class ServiceManagementService
{
    public Task<Result<ManagedService>> AddServiceAsync(ManagedService service);
    public Task<Result<ManagedService>> UpdateServiceAsync(string name, ManagedService updates);
    public Task<Result> RemoveServiceAsync(string name);
    public Task<IEnumerable<ManagedService>> GetAllServicesAsync();
    public Task<ManagedService> GetServiceAsync(string name);
}
```

#### CaddyConfigurationService

```csharp
public class CaddyConfigurationService
{
    public Task<string> GenerateCaddyfileAsync(CaddyGenerationOptions options);
    public Task<Result> ReloadCaddyAsync();
    public Task<Result> ValidateConfigAsync(string caddyfileContent);
}
```

The `CaddyConfigurationService` provides functionality for generating, validating, and managing Caddy reverse proxy configurations. It handles the creation of Caddyfile configurations from service definitions, writes configurations to disk, reads existing configurations, and validates Caddyfile syntax. The service supports both traditional Caddyfile format and JSON-based configuration generation.





**Key Features:**
- Generate complete Caddyfile configurations from service definitions
- Write configurations to disk with optional dry-run mode for safety
- Read and validate existing Caddyfile configurations
- Generate route blocks for individual services
- Validate Caddyfile syntax for structural correctness
- Support both Caddyfile and JSON configuration formats




### Example Usage

```csharp
// Create service manager and Caddy configuration service
var serviceManager = new ServiceManagementService(databaseService);
var caddyConfigService = new CaddyConfigurationService(serviceManager);

// Create global Caddy configuration
var globalConfig = new CaddyConfig
{
AdminEmail = "admin@example.com",
AdminPort = 2019,
AdminHost = "127.0.0.1",
HttpPort = 80,
HttpsPort = 443,
LogLevel = "info",
EnableMetrics = true
};

// Create routes for services
var routes = new List<CaddyRoute>
{
new CaddyRoute
{
Id = Guid.NewGuid().ToString(),
ServiceId = "web-app-01",
Domain = "app.example.com",
UpstreamUrl = "http://localhost:3000",
Path = "/api/*",
StripPath = true,
PreserveHostHeader = true,
TimeoutSeconds = 30,
EnableHttps = true,
AutoRedirectHttp = true,
CustomHeaders = new Dictionary<string, string>
{
["X-Content-Type-Options"] = "nosniff",
["X-Frame-Options"] = "DENY"
},
RateLimitRule = "100/10s",
IsActive = true,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow
},
new CaddyRoute
{
Id = Guid.NewGuid().ToString(),
ServiceId = "api-service-01",
Domain = "api.example.com",
UpstreamUrl = "http://localhost:8080",
Path = "/",
PreserveHostHeader = true,
TimeoutSeconds = 30,
EnableHttps = true,
AutoRedirectHttp = true,
IsActive = true,
CreatedAt = DateTime.UtcNow,
UpdatedAt = DateTime.UtcNow
}
};

// Generate Caddyfile content
string caddyfileContent = await caddyConfigService.GenerateCaddyfileAsync(globalConfig, routes);
Console.WriteLine("Generated Caddyfile:");
Console.WriteLine(caddyfileContent);

// Validate the generated Caddyfile
bool isValid = await caddyConfigService.ValidateCaddyfileAsync(caddyfileContent);
Console.WriteLine($"Validation result: {isValid}");

// Write to disk (dry-run mode first for safety)
await caddyConfigService.WriteCaddyfileAsync(caddyfileContent, dryRun: true);

// Write actual file
await caddyConfigService.WriteCaddyfileAsync(caddyfileContent, filePath: "/etc/caddy/Caddyfile");

// Read existing Caddyfile
string existingContent = await caddyConfigService.ReadCaddyfileAsync("/etc/caddy/Caddyfile");
Console.WriteLine($"Read {existingContent.Length} characters from Caddyfile");
```

## UpstreamManagerService

The `UpstreamManagerService` is the central engine for v2 dynamic upstream management. It coordinates upstream pool registration, health-aware request routing, active health probing, graceful connection draining, and Caddy reverse-proxy configuration generation. The service integrates with the existing `HealthMonitoringService` and `CaddyConfigurationService` to slot into the v1.x infrastructure without requiring changes to those services.






**Key Features:**
- Register and manage upstream pools with health monitoring and load balancing
- Perform active health probes against all upstream servers
- Implement health-aware request routing with multiple load balancing strategies (Round Robin, Least Connections, Random, Weighted Random, IP Hash)
- Support connection draining for graceful service shutdowns
- Generate Caddy reverse proxy configurations for upstream pools
- Provide comprehensive health reporting and monitoring
- Apply global defaults and thresholds for consistent behavior
- Support circuit breaking when no upstreams are available





### Example Usage

```csharp
// Create required services (typically injected via DI)
var serviceManager = new ServiceManagementService(databaseService);
var healthMonitor = new HealthMonitoringService(healthCheckRepository, serviceManager);
var caddyConfig = new CaddyConfigurationService(serviceManager);
var options = new LoadBalancingOptions
{
HealthCheckIntervalSeconds = 30,
UnhealthyThreshold = 3,
HealthyThreshold = 2,
MaxRetries = 2,
RetryDurationSeconds = 30,
CircuitBreakerEnabled = true,
ConnectionDrainTimeoutSeconds = 30,
HealthProbeTimeoutMs = 2000
};

// Create global Caddy configuration
var globalConfig = new CaddyConfig
{
    AdminEmail = "admin@example.com",
    AdminPort = 2019,
    AdminHost = "127.0.0.1",
    HttpPort = 80,
    HttpsPort = 443,
    LogLevel = "info",
    EnableMetrics = true
};

// Create routes for services
var routes = new List<CaddyRoute>
{
    new CaddyRoute
    {
        Id = Guid.NewGuid().ToString(),
        ServiceId = "web-app-01",
        Domain = "app.example.com",
        UpstreamUrl = "http://localhost:3000",
        Path = "/api/*",
        StripPath = true,
        PreserveHostHeader = true,
        TimeoutSeconds = 30,
        EnableHttps = true,
        AutoRedirectHttp = true,
        CustomHeaders = new Dictionary<string, string>
        {
            ["X-Content-Type-Options"] = "nosniff",
            ["X-Frame-Options"] = "DENY"
        },
        RateLimitRule = "100/10s",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    },
    new CaddyRoute
    {
        Id = Guid.NewGuid().ToString(),
        ServiceId = "api-service-01",
        Domain = "api.example.com",
        UpstreamUrl = "http://localhost:8080",
        Path = "/",
        PreserveHostHeader = true,
        TimeoutSeconds = 30,
        EnableHttps = true,
        AutoRedirectHttp = true,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    }
};

// Generate Caddyfile content
string caddyfileContent = await caddyConfigService.GenerateCaddyfileAsync(globalConfig, routes);
Console.WriteLine("Generated Caddyfile:");
Console.WriteLine(caddyfileContent);

// Validate the generated Caddyfile
bool isValid = await caddyConfigService.ValidateCaddyfileAsync(caddyfileContent);
Console.WriteLine($"Validation result: {isValid}");

// Write to disk (dry-run mode first for safety)
await caddyConfigService.WriteCaddyfileAsync(caddyfileContent, dryRun: true);

// Write actual file
await caddyConfigService.WriteCaddyfileAsync(caddyfileContent, filePath: "/etc/caddy/Caddyfile");

// Read existing Caddyfile
string existingContent = await caddyConfigService.ReadCaddyfileAsync("/etc/caddy/Caddyfile");
Console.WriteLine($"Read {existingContent.Length} characters from Caddyfile");

// Generate route block for a specific service
var serviceRoute = caddyConfigService.GenerateRouteForService(
    new ManagedService
    {
        Id = "web-app-01",
        Name = "web-app",
        HostBinding = "localhost",
        Port = 3000
    },
    "web.example.com"
);

string routeBlock = caddyConfigService.GenerateRouteBlock(serviceRoute);
Console.WriteLine("Generated route block:");
Console.WriteLine(routeBlock);

// Generate JSON configuration (alternative format)
string jsonConfig = caddyConfigService.GenerateCaddyJsonAsync(globalConfig, routes);
Console.WriteLine("Generated JSON configuration:");
Console.WriteLine(jsonConfig);
```

#### HealthMonitoringService

```csharp
public class HealthMonitoringService
{
    public Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName);
    public Task StartMonitoringAsync(TimeSpan interval);
    public Task StopMonitoringAsync();
    public Task<IEnumerable<HealthCheckResult>> GetHealthHistoryAsync(string serviceName, int limit);
}
```

The `HealthMonitoringService` provides comprehensive health monitoring capabilities for all managed services. It performs regular health checks, tracks historical health data, and provides statistics and summaries to help maintain system reliability. The service supports both HTTP and TCP health checks with configurable intervals, timeouts, and failure thresholds.












**Key Features:**
- Perform individual service health checks with automatic result logging
- Retrieve latest health status for any service
- Access detailed health history with configurable time ranges
- Get aggregated health statistics and trends
- Monitor all services at once with comprehensive results
- Generate health summaries showing overall system status
- Clean up old health check records to maintain database performance
- Support for HTTP and TCP health check protocols









### Example Usage

```csharp
// Create required dependencies (typically injected via DI)
var healthCheckRepository = new HealthCheckRepository(databaseService);
var serviceManager = new ServiceManagementService(databaseService);
var healthMonitoringService = new HealthMonitoringService(healthCheckRepository, serviceManager);

// Check health of a specific service
var healthResult = await healthMonitoringService.CheckServiceHealthAsync("api-service-01");

if (healthResult.IsHealthy)
{
    Console.WriteLine($"Service is healthy! Response time: {healthResult.ResponseTimeMs}ms");
}
else
{
    Console.WriteLine($"Service is unhealthy: {healthResult.ErrorMessage}");
}

// Get the latest health status for a service
var latestStatus = await healthMonitoringService.GetLatestHealthStatusAsync("api-service-01");
Console.WriteLine($"Latest status: {latestStatus.Status} at {latestStatus.CheckedAt}");

// Get health history for the last 24 hours
var history = await healthMonitoringService.GetHealthHistoryAsync("api-service-01", 24);
Console.WriteLine($"Found {history.Count} health checks in the last 24 hours");

// Get health statistics for trend analysis
var stats = await healthMonitoringService.GetHealthStatisticsAsync(
    "api-service-01",
    DateTime.UtcNow.AddDays(-7),
    DateTime.UtcNow
);
Console.WriteLine($"Healthy checks: {stats.HealthyCount}, Failed: {stats.FailedCount}");
Console.WriteLine($"Average response time: {stats.AverageResponseTimeMs}ms");

// Check health of all services
var allHealthResults = await healthMonitoringService.CheckAllServicesHealthAsync();
Console.WriteLine($"Checked {allHealthResults.Count} services");

// Get overall health summary
var summary = await healthMonitoringService.GetHealthSummaryAsync();
Console.WriteLine($"System Health: {summary.HealthPercentage:F1}%");
Console.WriteLine($"Total: {summary.TotalServices}, Healthy: {summary.HealthyServices}, Unhealthy: {summary.UnhealthyServices}");
Console.WriteLine($"Unchecked: {summary.UncheckedServices}, Disabled: {summary.DisabledServices}");

// Clean up old health records (keeps 30 days of history by default)
bool cleanupSuccess = await healthMonitoringService.CleanupOldRecordsAsync(daysToKeep: 30);
Console.WriteLine($"Cleanup successful: {cleanupSuccess}");
```

### ServiceCreatedEventHandlerExtensions

Provides extension methods for `ServiceCreatedEventHandler` that enable enhanced event handling capabilities including validation, custom logging levels, and access to internal components.

```csharp
// Example: Creating a handler with custom logger and webhook handler
var logger = new ConsoleLogger();
var webhookHandler = new SlackWebhookHandler("https://hooks.slack.com/services/...");
var handler = logger.WithLogger(webhookHandler);

// Example: Handling an event with validation
var serviceEvent = new ServiceCreatedEvent(
    serviceName: "api-service",
    port: 8080,
    serviceType: "web-api",
    executablePath: "/usr/local/bin/api-server"
);

await handler.HandleWithValidationAsync(serviceEvent);

// Example: Handling with custom log level
await handler.HandleWithLogLevelAsync(serviceEvent, LogLevel.Info);

// Example: Accessing internal components
var eventLogger = handler.GetLogger();
var webhook = handler.GetWebhookHandler();
```

## ArgumentParserExtensions

The `ArgumentParserExtensions` class provides extension methods for the `ArgumentParser` type that simplify command-line argument parsing. These extensions offer convenient methods for extracting and converting positional arguments and flags to common data types like integers and booleans, along with utility methods for checking flag presence and counting arguments. This class is particularly useful for CLI tools that need robust argument handling with proper error checking.

### Example Usage

```csharp
using CaddyVpsToolkit.Cli;

// Create argument parser with sample command-line arguments
var args = new[] { "service", "add", "--port", "8080", "--enabled", "true", "debug", "production" };
var parser = new ArgumentParser(args);

// Get command name as span for zero-allocation scenarios
ReadOnlySpan<char> commandSpan = parser.GetCommandSpan();
Console.WriteLine($"Command: {commandSpan.ToString()}"); // Output: "Command: service"

// Get positional arguments and parse them
int? port = parser.GetPositionalAsInt(0);
Console.WriteLine($"Port: {port}"); // Output: "Port: 8080"

bool? enabled = parser.GetFlagValueAsBoolean("enabled");
Console.WriteLine($"Enabled: {enabled}"); // Output: "Enabled: True"

string environment = parser.GetPositional(1);
Console.WriteLine($"Environment: {environment}"); // Output: "Environment: production"

// Check if flag exists and has a value
bool hasDebugFlag = parser.HasFlagWithValue("debug");
Console.WriteLine($"Has debug flag: {hasDebugFlag}"); // Output: "Has debug flag: True"

// Get all positional arguments as read-only list
var allPositional = parser.GetAllPositionalReadOnly();
Console.WriteLine($"All positional count: {allPositional.Count}"); // Output: "All positional count: 2"

// Get all flags as read-only list
var allFlags = parser.GetAllFlagsReadOnly();
Console.WriteLine($"All flags count: {allFlags.Count}"); // Output: "All flags count: 2"

// Get total positional count
int positionalCount = parser.GetPositionalCount();
Console.WriteLine($"Positional count: {positionalCount}"); // Output: "Positional count: 2"

// Check if any of multiple flags are present
bool hasAnyConfigFlag = parser.HasAnyFlag("debug", "verbose", "quiet");
Console.WriteLine($"Has any config flag: {hasAnyConfigFlag}"); // Output: "Has any config flag: True"
```

## AppConfigurationBuilder

The `AppConfigurationBuilder` class provides a fluent interface for building application configuration from multiple sources including JSON files, environment variables, and programmatic settings. It supports chaining configuration sources and building a type-safe `AppConfiguration` container for accessing configuration values with automatic type conversion.

### Example Usage

```csharp
// Build configuration from JSON file and environment variables
var config = new AppConfigurationBuilder()
    .WithJsonFile("/etc/caddy-vps-toolkit/config.json")
    .WithEnvironmentVariables("CADDY_")
    .WithSetting("app:timeout", 30000)
    .WithDefaults(new Dictionary<string, object>
    {
        ["logging:level"] = "info",
        ["healthcheck:enabled"] = true,
        ["systemd:restart-policy"] = "on-failure"
    })
    .Build();

// Access configuration values with type safety
string adminPort = config.GetString("caddy:admin-port", "2019");
int timeout = config.GetInt("app:timeout", 5000);
bool healthCheckEnabled = config.GetBool("healthcheck:enabled", true);

// Check if a configuration key exists
bool hasCustomConfig = config.Exists("custom:setting");

// Get all configuration as a dictionary
var allConfig = config.GetAll();
foreach (var kvp in allConfig)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Get complex configuration objects
var databaseConfig = config.GetObject<Dictionary<string, string>>("database:connection");
```

## DateTimeExtensions

The `DateTimeExtensions` class provides extension methods for `DateTime` operations, offering human-readable time formatting and common datetime calculations. These utilities are particularly useful for logging, audit trails, health monitoring, and any scenario requiring user-friendly date/time representations or date arithmetic.

## ValidationHelper

The `ValidationHelper` class provides centralized validation for common inputs, ensuring data integrity across the system. It offers static methods to validate ports, domains, file paths, service names, and ranges, returning detailed `ValidationResult` objects.

### Example Usage

```csharp
using CaddyVpsToolkit.Utilities;

// Validate a port
var portResult = ValidationHelper.ValidatePort(8080);
if (!portResult.IsValid)
{
    Console.WriteLine($"Invalid port: {portResult.GetErrorMessage()}");
}

// Validate a domain and service name
var domainResult = ValidationHelper.ValidateDomain("example.com");
var nameResult = ValidationHelper.ValidateServiceName("my-app");

// Combine validation results
var combinedResult = ValidationHelper.Combine(domainResult, nameResult);
if (combinedResult.IsValid)
{
    Console.WriteLine("All inputs are valid.");
}
else
{
    Console.WriteLine($"Validation failed: {combinedResult.GetErrorMessage()}");
}

// Using manual ValidationResult
var result = ValidationResult.Success();
if (someCondition)
{
    result = ValidationResult.Failure("Something went wrong");
}
```

## EnvironmentHelper

The `EnvironmentHelper` class provides a set of static methods for accessing environment variables, application paths, and system-level information. It ensures consistent and safe access to environment-specific data across different operating systems.

### Example Usage

```csharp
using CaddyVpsToolkit.Utilities;

// Check environment
if (EnvironmentHelper.IsDevelopment())
{
    Console.WriteLine("Development mode active.");
}
else if (EnvironmentHelper.IsProduction())
{
    Console.WriteLine("Production mode active.");
}

// Get application paths and info
Console.WriteLine($"Root: {EnvironmentHelper.GetApplicationRoot()}");
Console.WriteLine($"Home: {EnvironmentHelper.GetHomeDirectory()}");
Console.WriteLine($"Temp: {EnvironmentHelper.GetTempDirectory()}");
Console.WriteLine($"Current: {EnvironmentHelper.GetCurrentDirectory()}");
Console.WriteLine($"Version: {EnvironmentHelper.GetApplicationVersion()}");
Console.WriteLine($"Processors: {EnvironmentHelper.GetProcessorCount()}");

// Environment variables
EnvironmentHelper.SetEnvironmentVariable("APP_MODE", "debug");
string appMode = EnvironmentHelper.GetEnvironmentVariable("APP_MODE", "prod");

// OS detection
if (EnvironmentHelper.IsWindows()) Console.WriteLine("Running on Windows");
if (EnvironmentHelper.IsUnix()) Console.WriteLine("Running on Unix/Linux/Mac");
```

## PathUtilities

The `PathUtilities` class provides utility methods for file system path operations, including path manipulation, size calculations, and file system safety checks. It handles cross-platform path handling with security considerations for path traversal prevention and provides human-readable formatting for file sizes.

### Example Usage

```csharp
// Get relative path between two directories
string relativePath = PathUtilities.GetRelativePath(
    @"/var/www/services",
    @"/var/www/services/api/bin/Debug/net8.0"
);
Console.WriteLine($"Relative path: {relativePath}");
// Output: Relative path: api/bin/Debug/net8.0

// Safely combine paths with security checks
string configPath = PathUtilities.SafeCombine(
    @"/etc/caddy-vps-toolkit",
    "configs", "api-service.json"
);
Console.WriteLine($"Config path: {configPath}");
// Output: Config path: /etc/caddy-vps-toolkit/configs/api-service.json

// Normalize path separators for current OS
string normalizedPath = PathUtilities.NormalizePath(
    @"C:\Program Files\caddy-vps-toolkit\configs\api"
);
Console.WriteLine($"Normalized: {normalizedPath}");
// Output: Normalized: C:\Program Files\caddy-vps-toolkit\configs\api

// Get directory size in human-readable format
long directorySize = PathUtilities.GetDirectorySize(@"/var/www/services/api");
string formattedSize = PathUtilities.FormatFileSize(directorySize);
Console.WriteLine($"Directory size: {formattedSize}");

// Ensure directory exists before writing files
PathUtilities.EnsureDirectoryExists(@"/var/log/caddy-vps-toolkit");

// Get unique filename if file exists
string uniquePath = PathUtilities.GetUniqueFilePath(@"/tmp/backup.json");
Console.WriteLine($"Unique path: {uniquePath}");

// Sanitize filename for cross-platform compatibility
string sanitized = PathUtilities.SanitizeFileName("my:file*name?.txt");
Console.WriteLine($"Sanitized: {sanitized}");
// Output: Sanitized: myfilename.txt
```

## ProcessUtilities

The `ProcessUtilities` class provides robust tools for executing external system commands, managing process lifecycle, and capturing output asynchronously. It includes functionality for running commands with timeouts, verifying process execution status, and safely terminating processes to ensure system stability.

## IOutputFormatter

The `IOutputFormatter` interface provides a standardized contract for converting structured data into various output formats such as JSON, CSV, and formatted tables for console display. By implementing this interface, different formatters can be seamlessly swapped to control how service data or monitoring results are presented to the user.

### Example Usage

```csharp
using CaddyVpsToolkit.Formatters;
using System.Collections.Generic;

// Data to format
var services = new List<ManagedService>
{
    new ManagedService { Name = "api-service", Port = 8080 },
    new ManagedService { Name = "web-app", Port = 3000 }
};

// Use TableFormatter to generate a console-friendly table
IOutputFormatter tableFormatter = new TableFormatter("Name", "Port");
string tableOutput = tableFormatter.Format(services);
Console.WriteLine(tableOutput);

// Use JsonFormatter for structured API-like output
IOutputFormatter jsonFormatter = new JsonFormatter();
string jsonOutput = jsonFormatter.Format(services);
Console.WriteLine(jsonOutput);
```

### Example Usage

```csharp
using CaddyVpsToolkit.Utilities;

// Execute a command asynchronously with a timeout
var result = await ProcessUtilities.ExecuteAsync("systemctl", "status caddy", timeoutMs: 5000);

if (result.IsSuccess)
{
    Console.WriteLine($"Command Output: {result.Output}");
}
else
{
    Console.WriteLine($"Error: {result.GetOutput()} (Exit Code: {result.ExitCode})");
}

// Manage processes by name
if (ProcessUtilities.IsProcessRunning("caddy"))
{
    int count = ProcessUtilities.GetProcessCount("caddy");
    Console.WriteLine($"Caddy is running with {count} instances.");
    
    // Terminate processes if needed
    if (count > 1)
    {
        ProcessUtilities.KillProcess("caddy");
    }
}
```


### Public Members

- `ToRelativeTime()` - Get human-readable time difference (e.g., "2 hours ago")
- `ToIso8601()` - Format datetime as ISO 8601 string
- `ToReadableString()` - Format datetime as readable date string (e.g., "2026-05-03 14:30:45")
- `StartOfDay()` - Get the start of the current day
- `EndOfDay()` - Get the end of the current day
- `StartOfWeek()` - Get the start of the week (Monday)
- `StartOfMonth()` - Get the start of the month
- `IsPast()` - Check if datetime is in the past
- `IsFuture()` - Check if datetime is in the future
- `IsToday()` - Check if datetime is today
- `WorkingDaysBetween()` - Get the number of working days between two dates
- `ToDurationString()` - Get human-readable duration between two times (e.g., "2h 30m")

### Example Usage

```csharp
// Create a sample datetime for demonstration
var now = DateTime.UtcNow;
var pastDate = DateTime.UtcNow.AddDays(-2);
var futureDate = DateTime.UtcNow.AddDays(5);
var startOfWeek = DateTime.UtcNow.StartOfWeek();

// Format as human-readable relative time
string relativeTime = now.ToRelativeTime();
Console.WriteLine($"Current time: {relativeTime}");
// Output: "just now" or "X minutes/hours/days ago"

// Format as ISO 8601 string
string iso8601 = now.ToIso8601();
Console.WriteLine($"ISO 8601: {iso8601}");
// Output: "2026-07-18T14:30:45.1234567Z"

// Format as readable date string
string readable = now.ToReadableString();
Console.WriteLine($"Readable: {readable}");
// Output: "2026-07-18 14:30:45"

// Get start/end of day
DateTime startOfDay = now.StartOfDay();
DateTime endOfDay = now.EndOfDay();
Console.WriteLine($"Day starts: {startOfDay:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Day ends: {endOfDay:yyyy-MM-dd HH:mm:ss}");

// Get start of week (Monday)
DateTime monday = now.StartOfWeek();
Console.WriteLine($"Week starts: {monday:yyyy-MM-dd}");

// Get start of month
DateTime firstOfMonth = now.StartOfMonth();
Console.WriteLine($"Month starts: {firstOfMonth:yyyy-MM-dd}");

// Check if date is in past/future/today
bool isPast = pastDate.IsPast();
bool isFuture = futureDate.IsFuture();
bool isToday = now.IsToday();
Console.WriteLine($"Past: {isPast}, Future: {isFuture}, Today: {isToday}");

// Calculate working days between two dates
int workingDays = pastDate.WorkingDaysBetween(now);
Console.WriteLine($"Working days between: {workingDays}");

// Format time span as duration string
TimeSpan duration = TimeSpan.FromHours(2.5);
string durationString = duration.ToDurationString();
Console.WriteLine($"Duration: {durationString}");
// Output: "2h 30m"
```

## CollectionExtensions

The `CollectionExtensions` class provides extension methods for working with collections (List, IEnumerable, Dictionary, etc.). It offers utility functions for safe collection access, batching, partitioning, and other common operations that simplify collection manipulation throughout the codebase.

### Example Usage

```csharp
// Create a list of services
var services = new List<ManagedService>
{
    new ManagedService { Name = "api-service", Port = 8080, Domain = "api.example.com" },
    new ManagedService { Name = "web-app", Port = 3000, Domain = "web.example.com" },
    new ManagedService { Name = "cache", Port = 6379, Domain = "cache.example.com" },
    new ManagedService { Name = "database", Port = 5432, Domain = "db.example.com" }
};

// SafeGet - safely access list elements with default value
var firstService = services.SafeGet(0, new ManagedService { Name = "default" });
Console.WriteLine($"First service: {firstService.Name}"); // Outputs: First service: api-service

var outOfRangeService = services.SafeGet(10, new ManagedService { Name = "default" });
Console.WriteLine($"Out of range: {outOfRangeService.Name}"); // Outputs: Out of range: default

// IsNullOrEmpty - check if collection is null or empty
bool isEmpty = services.IsNullOrEmpty();
Console.WriteLine($"Is empty: {isEmpty}"); // Outputs: Is empty: False

bool isNullEmpty = ((List<ManagedService>)null).IsNullOrEmpty();
Console.WriteLine($"Is null or empty: {isNullEmpty}"); // Outputs: Is null or empty: True

// FirstOrDefault - get first element with null safety
var firstOrDefault = services.FirstOrDefault();
Console.WriteLine($"First or default: {firstOrDefault?.Name}"); // Outputs: First or default: api-service

// Batch - split collection into batches of specified size
var batches = services.Batch(2);
Console.WriteLine($"Number of batches: {batches.Count}"); // Outputs: Number of batches: 2
foreach (var batch in batches)
{
    Console.WriteLine($"Batch has {batch.Count} items");
}

## IObserver

The `IObserver` interface provides a lightweight, flexible implementation of the observer pattern, enabling decoupled event-driven communication between system components. It supports both classic observer-based attachment/detachment and subscription-based notification mechanisms, allowing for versatile event handling throughout the toolkit.

### Example Usage

```csharp
using CaddyVpsToolkit.Utilities;

// Create an instance (assuming an implementation exists)
var observer = new Observer<string>();

// Use subscriber-based mechanism
observer.Subscribe(msg => Console.WriteLine($"Received: {msg}"));
observer.Publish("Event occurred");

// Use observer-based mechanism
var myObserver = new MyObserverImplementation();
observer.Attach(myObserver);
observer.NotifyObservers();

// Manage state and inspect
observer.SetState("Idle");
var state = observer.GetState();
Console.WriteLine($"Current state: {state}");
Console.WriteLine($"Subscriber count: {observer.GetSubscriberCount()}");
```

## DateTimeExtensionsTestsExtensions

The `DateTimeExtensionsTestsExtensions` class provides extension methods for `DateTime` operations specifically designed for testing scenarios. These utilities offer common datetime calculations and conversions that are useful when writing tests involving date/time operations.

### Example Usage

```csharp
using CaddyVpsToolkit.Tests.Utilities;

// Convert a date to Unix timestamp
var testDate = new DateTime(2025, 7, 19, 14, 30, 0, DateTimeKind.Utc);
long unixTimestamp = testDate.ToUnixTimestamp();
Console.WriteLine($"Unix timestamp: {unixTimestamp}"); // Output: Unix timestamp: 1752954600

// Get first day of month
var currentDate = DateTime.Now;
DateTime firstDayOfMonth = currentDate.FirstDayOfMonth();
Console.WriteLine($"First day of {currentDate:yyyy-MM}: {firstDayOfMonth:yyyy-MM-dd}");
// Output: First day of 2025-07: 2025-07-01

// Get last day of month
DateTime lastDayOfMonth = currentDate.LastDayOfMonth();
Console.WriteLine($"Last day of {currentDate:yyyy-MM}: {lastDayOfMonth:yyyy-MM-dd}");
// Output: Last day of 2025-07: 2025-07-31

// Check if date is a weekend
bool isWeekend = new DateTime(2025, 7, 19).IsWeekend(); // Saturday
bool isWeekday = new DateTime(2025, 7, 18).IsWeekend(); // Friday
Console.WriteLine($"Is weekend: {isWeekend}, Is weekday: {isWeekday}");
// Output: Is weekend: True, Is weekday: False

// Get all dates between two dates
var startDate = new DateTime(2025, 7, 18);
var endDate = new DateTime(2025, 7, 20);
var dates = startDate.GetDatesBetween(endDate);
Console.WriteLine("Dates between 2025-07-18 and 2025-07-20:");
foreach (var date in dates)
{
    Console.WriteLine($"  {date:yyyy-MM-dd}");
}
// Output: Dates between 2025-07-18 and 2025-07-20:
//   2025-07-18
//   2025-07-19
//   2025-07-20

// Calculate business days between two dates
int businessDays = new DateTime(2025, 7, 18).BusinessDaysBetween(new DateTime(2025, 7, 25));
Console.WriteLine($"Business days between 2025-07-18 and 2025-07-25: {businessDays}");
// Output: Business days between 2025-07-18 and 2025-07-25: 5
```

## AppConfigurationBuilderTestsExtensions

The `AppConfigurationBuilderTestsExtensions` class provides extension methods for `AppConfigurationBuilder` that simplify building test configurations. It includes methods for adding test-specific settings, web server configurations, Caddy configurations, and validation helpers to ensure configurations meet test requirements.

### Example Usage

```csharp
using CaddyVpsToolkit.Tests.Configuration;
using Microsoft.Extensions.Configuration;

// Create a test configuration with default test settings
var config = new AppConfigurationBuilder()
    .WithTestDefaults()
    .Build();

// Verify that required test settings are present
config.ShouldContainSettings("Test:ApiUrl", "https://api.test.local");
config.ShouldContainSettings("Test:DatabaseConnection", "Server=test-db;Database=test");

// Check specific configuration values
config.ShouldHaveValue<string>("Test:Timeout", "30000");

// Create configuration with custom test settings
var customConfig = new AppConfigurationBuilder()
    .WithTestSettings(
        timeout: 60000,
        apiUrl: "https://custom-api.test.local",
        databaseConnection: "Server=custom-db;Database=custom"
    )
    .Build();

// Create configuration with web server settings
var webConfig = new AppConfigurationBuilder()
    .WithWebServerSettings(
        port: 8080,
        environment: "Test",
        enableHealthChecks: true
    )
    .Build();

// Create configuration with Caddy settings
var caddyConfig = new AppConfigurationBuilder()
    .WithCaddySettings(
        adminPort: 2019,
        httpPort: 80,
        httpsPort: 443,
        enableMetrics: true
    )
    .Build();
```

## MemoryCacheTestsExtensions

The `MemoryCacheTestsExtensions` class provides extension methods for testing the `MemoryCache` implementation. It offers helper methods to verify cache operations including setting values with immediate verification, getting values with existence checks, testing expiration behavior, and validating cache clearing functionality. These extensions are designed to simplify unit testing of cache-related code by providing fluent assertion patterns.

### Example Usage

```csharp
using CaddyVpsToolkit.Caching;
using CaddyVpsToolkit.Tests.Caching;
using Xunit;

public class CacheTests : MemoryCacheTests
{
    [Fact]
    public async Task Cache_Operations_Work_Correctly()
    {
        // Set a value and immediately verify it was stored correctly
        await this.SetAndVerifyAsync("user:123", new User { Id = 123, Name = "Alice" });
        
        // Verify a value exists and retrieve it
        var user = await this.GetAndVerifyAsync<User>("user:123");
        Assert.Equal("Alice", user.Name);
        
        // Verify multiple keys exist
        var keys = new[] { "user:123", "user:456" };
        var values = await this.GetMultipleAsync<User>(keys);
        Assert.Single(values);
        
        // Create a cache key using the same logic as the cache implementation
        string cacheKey = this.CreateCacheKey("service", "health", "api-service-01");
        await this.SetAndVerifyAsync(cacheKey, "healthy");
        
        // Verify expiration behavior
        await this.VerifyExpirationAsync("temp:data", 100, 500);
        
        // Verify cache clearing
        await this.VerifyClearAsync(5);
    }
}
```

// Partition - split collection based on predicate
var (matching, notMatching) = services.Partition(s => s.Port > 5000);
Console.WriteLine($"Matching (>5000): {matching.Count} items"); // Outputs: Matching (>5000): 2 items
Console.WriteLine($"Not matching: {notMatching.Count} items"); // Outputs: Not matching: 2 items

// ToTupleList - convert dictionary to list of tuples
var serviceDict = new Dictionary<string, int>
{
    ["api"] = 8080,
    ["web"] = 3000,
    ["cache"] = 6379
};
var tupleList = serviceDict.ToTupleList();
foreach (var (key, value) in tupleList)
{
    Console.WriteLine($"{key}: {value}");
}

// IntersectAll - get intersection of multiple collections
var list1 = new List<int> { 1, 2, 3, 4, 5 };
var list2 = new List<int> { 2, 4, 6, 8 };
var list3 = new List<int> { 4, 5, 6, 7 };
var commonElements = new[] { list1, list2, list3 }.IntersectAll();
Console.WriteLine($"Common elements: {string.Join(", ", commonElements)}"); // Outputs: Common elements: 4

// RemoveWhere - remove items matching predicate
services.RemoveWhere(s => s.Name.StartsWith("a"));
Console.WriteLine($"After remove where: {services.Count} items"); // Outputs: After remove where: 3 items

// AddRangeIfNotExists - add items if they don't exist
var newServices = new List<ManagedService>
{
    new ManagedService { Name = "api-service", Port = 8080 },
    new ManagedService { Name = "metrics", Port = 9090 }
};
services.AddRangeIfNotExists(newServices);
Console.WriteLine($"After add range if not exists: {services.Count} items"); // Outputs: After add range if not exists: 4 items

// Shuffle - randomize collection order
var shuffled = services.Shuffle();
Console.WriteLine($"Original first: {services[0].Name}");
Console.WriteLine($"Shuffled first: {shuffled[0].Name}");
```

## ConfigurationService

The `ConfigurationService` provides centralized configuration management for the caddy-vps-toolkit application. It handles reading, writing, and persisting configuration values with support for typed values, caching, and file-based persistence. The service manages both application-level settings and Caddy-specific configurations like admin port, logging levels, and health check settings.

### Example Usage

```csharp
// Create configuration service with repository dependency
var configurationRepository = new ConfigurationRepository();
var configService = new ConfigurationService(configurationRepository);

// Set application-level configuration
await configService.SetCaddyAdminPortAsync(2019);
await configService.SetLoggingLevelAsync("debug");
await configService.SetHealthCheckEnabledAsync(true);

// Get configuration values
int adminPort = await configService.GetCaddyAdminPortAsync();
Console.WriteLine($"Caddy admin port: {adminPort}"); // Outputs: Caddy admin port: 2019

string logLevel = await configService.GetLoggingLevelAsync();
Console.WriteLine($"Current log level: {logLevel}"); // Outputs: Current log level: debug

bool healthCheckEnabled = await configService.IsHealthCheckEnabledAsync();
Console.WriteLine($"Health checks enabled: {healthCheckEnabled}"); // Outputs: Health checks enabled: True

// Set and get typed configuration values
await configService.SetValueAsync("service:timeout", "30000");
int timeout = await configService.GetValueAsync<int>("service:timeout", 5000);
Console.WriteLine($"Service timeout: {timeout}ms"); // Outputs: Service timeout: 30000ms

// Load and save configuration from/to file
await configService.LoadFromFileAsync("/etc/caddy-vps-toolkit/config.json");
await configService.SaveToFileAsync("/etc/caddy-vps-toolkit/config.json");

// Get all configuration as dictionary
var allConfig = await configService.GetAllAsync();
foreach (var kvp in allConfig)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Delete configuration key
bool deleted = await configService.DeleteAsync("temp:setting");
Console.WriteLine($"Key deleted: {deleted}");
```

## SystemdUnitService

The `SystemdUnitService` provides comprehensive management of systemd unit files and service lifecycle operations for managed services. It enables tasks such as creating, deleting, starting, stopping, restarting, and enabling/disabling systemd services, as well as querying detailed service status and unit configurations.

### Example Usage

```csharp
// Instantiate the systemd unit service
var unitService = new SystemdUnitService();

// List all registered service units
var units = await unitService.ListUnitsAsync();

// Check if a specific unit file exists
string unitName = "my-web-app.service";
if (unitService.UnitFileExists(unitName))
{
    // Retrieve detailed status for the service
    SystemdUnitStatus status = await unitService.GetDetailedStatusAsync(unitName);
    
    Console.WriteLine($"Service: {status.UnitName}");
    Console.WriteLine($"Status: {status.ActiveState}");
    Console.WriteLine($"Type: {status.Type}");
    Console.WriteLine($"PID: {status.MainPid}");
    Console.WriteLine($"Timeout: {status.TimeoutStartUsec}us");

    // Manage service lifecycle
    await unitService.RestartServiceAsync(unitName);
}
```

## Troubleshooting

### Service Won't Start

**Symptom**: `systemctl start my-service` fails

**Solutions**:
1. Check systemd unit file syntax: `systemd-analyze verify /etc/systemd/system/my-service.service`
2. Verify port is available: `sudo lsof -i :PORT`
3. Check service logs: `journalctl -u my-service -n 50`
4. Validate environment variables are set correctly

### Caddy Configuration Error

**Symptom**: `caddy reload` fails with syntax error

**Solutions**:
1. Validate current configuration: `caddy validate`
2. Regenerate from toolkit: `caddy-vps-toolkit caddy generate`
3. Check domain DNS resolution: `nslookup domain.example.com`
4. Verify Caddy binary is up to date: `caddy --version`

### Health Checks Always Fail

**Symptom**: Service reports unhealthy even though it's running

**Solutions**:
1. Manually test endpoint: `curl -v http://localhost:PORT/health`
2. Check timeout setting: increase `--health-check-timeout`
3. Verify network connectivity: `ping 127.0.0.1`
4. Check service logs for errors: `journalctl -u service-name`
5. Verify health check URL is correct: `caddy-vps-toolkit service show name`

### Database Locked Error

**Symptom**: "database is locked" error in logs

**Solutions**:
1. Stop all toolkit processes: `systemctl stop caddy-vps-toolkit*`
2. Wait 30 seconds for file locks to release
3. Restart: `systemctl start caddy-vps-toolkit-monitor`
4. Consider moving database to faster disk

### Configuration Not Applying

**Symptom**: Changes don't take effect after running commands

**Solutions**:
1. Check if running with sufficient permissions (use `sudo`)
2. Verify configuration was saved: `caddy-vps-toolkit config validate`
3. Force reload: `caddy-vps-toolkit caddy generate | sudo systemctl reload caddy`
4. Check audit log for errors: `caddy-vps-toolkit audit log`

## Advanced Topics

### Custom Health Check Scripts

Create a recovery script at `/opt/scripts/recover-api.sh`:

```bash
#!/bin/bash
set -e

SERVICE_NAME="api"
SERVICE_PORT=8080

# Kill existing process
pkill -f "dotnet.*api.dll" || true
sleep 2

# Start service
systemctl start $SERVICE_NAME

# Wait for startup
sleep 5

# Verify
curl -f http://localhost:$SERVICE_PORT/health || exit 1

echo "Service recovered successfully"
```

Configure in toolkit:
```bash
caddy-vps-toolkit service update api \
  --recovery-script /opt/scripts/recover-api.sh \
  --recovery-script-timeout 30s
```

### Webhook Integration

Send notifications to external systems on health events:

```bash
caddy-vps-toolkit notification add-webhook \
  --name pagerduty \
  --url https://events.pagerduty.com/v2/enqueue \
  --auth-header "Authorization: Token YOUR_TOKEN" \
  --on-health-failure \
  --payload-template /opt/templates/pagerduty.json
```

### Metrics Export

Export metrics in Prometheus format:

```bash
curl http://localhost:9090/metrics

# Output example:
# service_health{name="api",status="healthy"} 1
# service_restarts_total{name="api"} 2
# health_check_duration_seconds{name="api"} 0.125
```

## CommandRegistry

The `CommandRegistry` class provides centralized registration and discovery of CLI commands with metadata and validation rules. It serves as the command catalog for the caddy-vps-toolkit CLI tool, enabling dynamic command registration, lookup, and help text generation. The registry maintains a collection of `CommandDescriptor` objects that describe each command's name, description, usage patterns, required arguments, and optional flags.

This design allows for extensible command architecture where new commands can be registered at runtime without modifying core CLI parsing logic, making it ideal for CLI tools that need to support plugins or modular features.

**Key Features:**
- Command registration with metadata (name, description, usage patterns)
- Command lookup by name (case-insensitive)
- Help text generation for all registered commands
- Validation support through RequiredArguments and OptionalFlags
- Extensible architecture for dynamic command discovery

**Example Usage:**

```csharp
// Create command registry
var registry = new CommandRegistry();

// Register a new command with basic metadata
registry.Register(new CommandDescriptor("service", "Service management commands")
    .WithUsage("service <action> [serviceName] [--flags]")
    .RequireArgument("action")
    .AllowFlag("verbose")
    .AllowFlag("force"));

// Register additional commands
registry.Register(new CommandDescriptor("caddy", "Caddy configuration generation")
    .WithUsage("caddy <action> [--flags]"));

registry.Register(new CommandDescriptor("health", "Health monitoring commands")
    .WithUsage("health <action> [serviceName] [--flags]"));

// Check if a command exists
bool hasServiceCommand = registry.Exists("service"); // Returns true
bool hasUnknownCommand = registry.Exists("unknown"); // Returns false

// Get a command descriptor for detailed inspection
var serviceCommand = registry.Get("service");
if (serviceCommand != null)
{
    Console.WriteLine($"Command: {serviceCommand.Name}");
    Console.WriteLine($"Description: {serviceCommand.Description}");
    Console.WriteLine($"Usage: {serviceCommand.Usage}");
    Console.WriteLine($"Required Arguments: {string.Join(", ", serviceCommand.RequiredArguments)}");
    Console.WriteLine($"Optional Flags: {string.Join(", ", serviceCommand.OptionalFlags)}");
}

// Generate comprehensive help text for all registered commands
string helpText = registry.GenerateHelpText();
Console.WriteLine(helpText);

// Get all registered commands
var allCommands = registry.GetAll();
Console.WriteLine($"Total registered commands: {allCommands.Count}");
```

## IRetryPolicy

The `IRetryPolicy` interface defines a contract for implementing retry strategies when handling transient failures in asynchronous operations. It provides a standardized way to wrap operations with retry logic using different backoff strategies, making it ideal for network operations, database calls, and other operations that may fail temporarily.

This interface is implemented by three concrete policies:
- `ExponentialBackoffRetryPolicy` - Implements exponential backoff with jitter to prevent thundering herd scenarios
- `LinearBackoffRetryPolicy` - Uses a fixed delay increment between retry attempts
- `NoRetryPolicy` - Executes operations exactly once without retry

### Example Usage

```csharp
// Create an exponential backoff retry policy with default settings
var retryPolicy = new ExponentialBackoffRetryPolicy(
    maxRetries: 5,
    initialDelayMs: 200,
    backoffMultiplier: 2.0,
    maxDelayMs: 10000
);

// Execute an async operation with retry logic
try
{
    var result = await retryPolicy.ExecuteAsync(async () =>
    {
        // Your transient operation here
        return await httpClient.GetAsync("https://api.example.com/data");
    });
    
    Console.WriteLine($"Success after retry attempts! Status: {result.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Operation failed after retries: {ex.Message}");
}

// Execute a void async operation with retry
var fileLogger = new FileLogger("/var/log/app.log");
await retryPolicy.ExecuteAsync(async () =>
{
    await fileLogger.LogInfoAsync("Processing data...");
    await ProcessDataAsync();
});

// Use linear backoff for predictable delays
var linearRetry = new LinearBackoffRetryPolicy(
    maxRetries: 3,
    delayIncrementMs: 1000
);

// Use no retry policy when failures should not be retried
var noRetry = new NoRetryPolicy();
var immediateResult = await noRetry.ExecuteAsync(async () =>
{
    return await GetImmediateResultAsync();
});
```

## StringExtensions

The `StringExtensions` class provides a collection of extension methods for common string manipulation and validation operations. These methods improve code readability by adding domain-specific string operations that handle null safety, whitespace, and edge cases consistently across the codebase.

### Example Usage

```csharp
// Create service configuration with kebab-case naming convention
var serviceName = "MyWebApplication".ToKebabCase(); // "my-web-application"

// Validate service configuration values
if ("admin@example.com".IsValidEmail())
{
    Console.WriteLine("Email is valid");
}

if ("https://api.example.com".IsValidUrl())
{
    Console.WriteLine("URL is valid");
}

// Generate human-readable service names
var displayName = "myWebService".ToTitleCase(); // "Myweb service"

// Create configuration keys in consistent format
var configKey = $"service:{serviceName}:timeout".ToCamelCase(); // "service:my-web-application:timeout"

// Format log messages with truncated content
var logMessage = $"Service {serviceName} started successfully on port 8080".Truncate(50); // "Service my-web-application started successfully on..."

// Check if service name starts with common prefixes
var isCoreService = serviceName.StartsWithAny("core-", "api-", "web-");

// Repeat configuration patterns
var separatorLine = "=".Repeat(80); // "================================================================================"

// Validate numeric service ports
if ("8080".IsNumeric())
{
    Console.WriteLine("Port is numeric");
}

// Escape service names for shell commands
var escapedName = serviceName.EscapeShell(); // Wraps in single quotes and escapes internal quotes

// Safe substring operations for parsing configuration strings
var serviceType = "web-service-api-production".SafeSubstring(4, 8); // "service-a"
```

## ILogger

The `ILogger` interface provides structured logging capabilities with support for multiple log levels and asynchronous operations. It serves as the primary logging abstraction for the application, enabling consistent log formatting and output to different destinations (file, console, or in-memory for testing).

The interface is implemented by `FileLogger` for persistent logging to disk and `MemoryLogger` for testing scenarios where log inspection is needed without writing to disk.

**Public Members:**
- `Task LogInfoAsync(string message)` - Logs an informational message
- `Task LogWarningAsync(string message)` - Logs a warning message
- `Task LogErrorAsync(string message)` - Logs an error message
- `Task LogDebugAsync(string message)` - Logs a debug message

**Example Usage:**

```csharp
// Create a file-based logger with minimum log level of Info
var fileLogger = new FileLogger(
    logPath: "/var/log/caddy-vps-toolkit/app.log",
    minLevel: LogLevel.Info,
    consoleOutput: true
);

// Log messages at different levels
await fileLogger.LogInfoAsync("Application started successfully");
await fileLogger.LogWarningAsync("High memory usage detected - 85%");
await fileLogger.LogErrorAsync("Failed to connect to database: connection timeout");
await fileLogger.LogDebugAsync("Processing request: GET /api/users/123");

// Create an in-memory logger for testing scenarios
var memoryLogger = new MemoryLogger(minLevel: LogLevel.Debug);

// Log messages during testing
await memoryLogger.LogInfoAsync("Test case started: ServiceHealthCheck");
await memoryLogger.LogDebugAsync("Mock service response: 200 OK");

// Retrieve and verify logged messages
var logs = memoryLogger.GetLogs();
Console.WriteLine($"Total log entries: {logs.Count}");

foreach (var logEntry in logs)
{
    Console.WriteLine(logEntry);
}

// Clear logs between test cases
memoryLogger.Clear();
```

## IRateLimiter

The `IRateLimiter` interface provides a mechanism for rate limiting API operations to prevent abuse and ensure fair usage. It's implemented by two rate limiting algorithms: `TokenBucketRateLimiter` for burst-capable rate limiting and `FixedWindowRateLimiter` for simple window-based limiting.



Rate limiting is essential for protecting API endpoints from being overwhelmed by excessive requests, whether malicious or accidental, ensuring system stability and predictable performance.



**Public Members:**
- `Task<bool> AllowAsync(string key)` - Checks if a request should be allowed based on the rate limit rules

**Implementations:**
- `TokenBucketRateLimiter` - Token bucket algorithm that allows burst traffic up to bucket capacity, then enforces per-second rate limits
- `FixedWindowRateLimiter` - Simple fixed-window counter that resets after a specified time period

**Example Usage:**

```csharp
// Create a token bucket rate limiter with capacity of 100 requests and refill rate of 10 requests per second
var tokenBucketLimiter = new TokenBucketRateLimiter(capacity: 100, refillRatePerSecond: 10);

// Check if a request is allowed for a specific API key
bool isAllowed = await tokenBucketLimiter.AllowAsync("api-key-123");

if (isAllowed)
{
    Console.WriteLine("Request allowed - rate limit not exceeded");
    // Process the request
}
else
{
    Console.WriteLine("Request denied - rate limit exceeded");
    // Return 429 Too Many Requests
}

// Create a fixed window rate limiter with 50 requests per 60-second window
var fixedWindowLimiter = new FixedWindowRateLimiter(maxRequests: 50, windowSeconds: 60);

// Check if a request is allowed for a user session
bool isAllowed2 = await fixedWindowLimiter.AllowAsync("user-session-456");

if (isAllowed2)
{
    Console.WriteLine("User request allowed within current window");
}
else
{
    Console.WriteLine("User rate limit exceeded for this window");
}
```

## IWebhookHandler

The `IWebhookHandler` interface provides a mechanism for external system integration through webhook notifications. It allows registration of webhook URLs for specific event types and triggers notifications when those events occur. The interface supports registering and unregistering webhook endpoints, triggering events with payload data, and retrieving current registrations.

The `WebhookHandler` implementation manages webhook registrations in memory and delivers notifications via HTTP POST requests. It handles event types such as service creation, deletion, status changes, health check failures, and configuration updates.

**Example Usage:**

```csharp
// Create webhook handler with HTTP client
var httpClient = new HttpClient();
var webhookHandler = new WebhookHandler(httpClient);

// Register webhook URL for service creation events
webhookHandler.Register(
    url: "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
    eventType: WebhookEventType.ServiceCreated
);

// Register additional webhook for health check failures
webhookHandler.Register(
    url: "https://your-monitoring-system.example.com/webhook",
    eventType: WebhookEventType.HealthCheckFailed
);

// Trigger a service creation event
var serviceCreatedPayload = new
{
    ServiceName = "api-service",
    Port = 8080,
    Domain = "api.example.com",
    Status = "created"
};

bool success = await webhookHandler.TriggerAsync(
    WebhookEventType.ServiceCreated,
    serviceCreatedPayload
);

Console.WriteLine($"Webhook triggered successfully: {success}");

// Get all registered URLs for a specific event type
var healthCheckUrls = webhookHandler.GetRegistrations(WebhookEventType.HealthCheckFailed);
foreach (var url in healthCheckUrls)
{
    Console.WriteLine($"Health check webhook registered at: {url}");
}

// Unregister a webhook URL
webhookHandler.Unregister(
    url: "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
    eventType: WebhookEventType.ServiceCreated
);
```

## Result

The `Result` type is a simple, type-safe wrapper for operation results that provides a consistent way to handle both success and failure states without throwing exceptions. It's commonly used throughout the application to return operation outcomes where failures are expected and should be handled gracefully. The generic `Result<T>` variant carries data for successful operations, while the non-generic `Result` is used for operations that don't need to return values.

## HealthCheckResult

The `HealthCheckResult` type represents the outcome of a health check for a service endpoint. It captures detailed information about the check including response times, HTTP status codes, error messages, and historical tracking of consecutive successes and failures. This type is used by the health monitoring system to evaluate service health and trigger recovery actions when thresholds are exceeded.

```csharp
// Create a successful health check result for a healthy service
var successResult = HealthCheckResult.CreateSuccess(
    serviceId: "api-service-01",
    responseTimeMs: 125,
    httpStatus: 200
);

Console.WriteLine($"Healthy: {successResult.IsHealthy}");
Console.WriteLine($"Status: {successResult.Status}");
Console.WriteLine($"Response time: {successResult.ResponseTimeMs}ms");
Console.WriteLine($"HTTP status: {successResult.HttpStatusCode}");
Console.WriteLine($"Checked at: {successResult.CheckedAt}");

// Create a failed health check result for an unhealthy service
var failureResult = HealthCheckResult.CreateFailure(
    serviceId: "database-service-02",
    errorMessage: "Connection timeout after 5000ms",
    responseTimeMs: 5200
);

Console.WriteLine($"Healthy: {failureResult.IsHealthy}");
Console.WriteLine($"Status: {failureResult.Status}");
Console.WriteLine($"Error: {failureResult.ErrorMessage}");
Console.WriteLine($"Consecutive failures: {failureResult.ConsecutiveFailures}");

// Check if response is slow (threshold defaults to 5000ms)
bool isSlow = successResult.IsSlowResponse();
Console.WriteLine($"Is slow response: {isSlow}");

// Check with custom threshold
bool isVerySlow = failureResult.IsSlowResponse(thresholdMs: 5000);
Console.WriteLine($"Is very slow: {isVerySlow}");
```

## SslCertificateInfo

The `SslCertificateInfo` class represents metadata for an SSL/TLS certificate retrieved from a remote domain. It provides information about certificate validity, subject, issuer, and expiry dates. This type is used by the SSL certificate checker functionality to monitor certificate health and alert users before certificates expire.

```csharp
// Example: Checking SSL certificate status and creating a monitoring alert
var certificateInfo = new SslCertificateInfo
{
    Domain = "example.com",
    Subject = "CN=example.com, O=Example Inc, L=San Francisco, ST=California, C=US",
    Issuer = "CN=Let's Encrypt Authority X3, O=Let's Encrypt, C=US",
    IssuedAt = DateTime.UtcNow.AddDays(-90),
    ExpiresAt = DateTime.UtcNow.AddDays(30)
};

// Check if certificate is valid
bool isValid = certificateInfo.IsValid; // true
int daysRemaining = certificateInfo.DaysUntilExpiry; // 30

// Create a certificate check result based on status
SslCertificateCheckResult result;
if (certificateInfo.DaysUntilExpiry <= 7)
{
    result = SslCertificateCheckResult.CreateExpiringSoon(
        certificateInfo.Domain, 
        certificateInfo, 
        certificateInfo.DaysUntilExpiry <= 7
    );
}
else if (certificateInfo.DaysUntilExpiry <= 0)
{
    result = SslCertificateCheckResult.CreateExpired(certificateInfo.Domain, certificateInfo);
}
else
{
    result = SslCertificateCheckResult.CreateValid(certificateInfo.Domain, certificateInfo);
}

Console.WriteLine($"Certificate status: {result.Status}");
Console.WriteLine($"Message: {result.Message}");
Console.WriteLine($"Checked at: {result.CheckedAt}");
```

### Public Members

The `Result` type provides the following public members:

- **Non-generic Result:**
  - `bool IsSuccess` - Indicates whether the operation succeeded
  - `string ErrorMessage` - Error message if the operation failed
  - `string ErrorCode` - Optional error code (e.g., "BAD_REQUEST")
  - `static Result Success()` - Creates a successful result
  - `static Result Failure(string errorMessage)` - Creates a failed result
  - `static Result Failure(string errorMessage, string errorCode)` - Creates a failed result with error code

- **Generic Result<T>:**
  - `bool IsSuccess` - Indicates whether the operation succeeded
  - `T Data` - The returned data if successful
  - `string ErrorMessage` - Error message if the operation failed
  - `string ErrorCode` - Optional error code (e.g., "NOT_FOUND")
  - `static Result<T> Success(T data)` - Creates a successful result with data
  - `static Result<T> Success()` - Creates a successful result with default data
  - `static Result<T> Failure(string errorMessage)` - Creates a failed result
  - `static Result<T> Failure(string errorMessage, string errorCode)` - Creates a failed result with error code

- **Pagination Result (Result<List<T>>):**
  - `List<T> Items` - The collection of items
  - `int Page` - Current page number
  - `int PageSize` - Number of items per page
  - `int TotalCount` - Total number of items across all pages

### Usage Examples

**Successful result with data:**

```csharp
var userResult = Result<User>.Success(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });

if (userResult.IsSuccess)
{
    User user = userResult.Data;
    Console.WriteLine($"Retrieved user: {user.Name}");
    Console.WriteLine($"Email: {userResult.Data.Email}");
}
```

**Successful result with default data:**

```csharp
var countResult = Result<int>.Success();
Console.WriteLine($"Default count: {countResult.Data}"); // Outputs: Default count: 0
```

**Failed result with error message:**

```csharp
var errorResult = Result<string>.Failure("User not found");

if (!errorResult.IsSuccess)
{
    Console.WriteLine($"Error: {errorResult.ErrorMessage}"); // Outputs: Error: User not found
}
```

**Failed result with error code:**

```csharp
var apiError = Result<ApiResponse>.Failure("Invalid request", "BAD_REQUEST");
Console.WriteLine($"Error code: {apiError.ErrorCode}"); // Outputs: Error code: BAD_REQUEST
```

**Non-generic result for operations without return values:**

```csharp
var operationResult = Result.Success();

if (operationResult.IsSuccess)
{
    Console.WriteLine("Operation completed successfully");
}
else
{
    Console.WriteLine($"Operation failed: {operationResult.ErrorMessage}");
}
```

**Pagination result with collection data:**

```csharp
var servicesResult = Result<List<ManagedService>>.Success(services);

if (servicesResult.IsSuccess)
{
    Console.WriteLine($"Total services: {servicesResult.Data.Count}");
    Console.WriteLine($"Page: {servicesResult.Items.Page}");
    Console.WriteLine($"Page size: {servicesResult.Items.PageSize}");
    Console.WriteLine($"Total count: {servicesResult.Items.TotalCount}");
    
    foreach (var service in servicesResult.Data)
    {
        Console.WriteLine($" - {service.Name} on port {service.Port}");
    }
}
```

## ServiceCollectionExtensions

The `ServiceCollectionExtensions` class provides extension methods for `IServiceCollection` that simplify dependency injection registration for infrastructure services. It offers a fluent API for adding caching, HTTP clients, webhooks, logging, event bus, rate limiting, and service discovery services to your ASP.NET Core application's service collection.

These extension methods follow the common .NET dependency injection pattern and return the `IServiceCollection` to enable method chaining. Each method validates its parameters and throws appropriate exceptions for invalid inputs.

### Example Usage

```csharp
// Configure services in your ASP.NET Core application's Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add infrastructure services with default configuration
builder.Services
    .AddCachingServices()
    .AddHttpClientServices()
    .AddWebhookServices()
    .AddLoggingServices()
    .AddEventBus()
    .AddRateLimiting()
    .AddServiceDiscovery();

// Or configure all services at once with custom options
builder.Services.AddInfrastructureServices(options =>
{
    options.HttpTimeoutMs = 15000;      // 15 second timeout
    options.MaxRetries = 5;             // Retry up to 5 times
    options.LogPath = "/var/log/my-app.log";
    options.MinLogLevel = LogLevel.Debug;  // Log debug messages
    options.RateLimitCapacity = 200;       // Allow 200 requests
    options.RateLimitRefillRate = 20;    // 20 tokens per second
});

var app = builder.Build();
app.Run();
```

### WorkerCoordinator

The `WorkerCoordinator` type coordinates background workers within the caddy-vps-toolkit system. It manages worker registration, lifecycle, and monitoring, providing centralized control over multiple background services.

Usage example:

```csharp
// Create a worker coordinator
var coordinator = new WorkerCoordinator();

// Register workers
coordinator.Register("health-monitor", async () => await healthMonitor.StartAsync());
coordinator.Register("metrics-collector", async () => await metricsService.StartAsync());

// Start all registered workers
await coordinator.StartAllAsync();

// Check overall status
string status = coordinator.GetStatus();
Console.WriteLine($"System status: {status}");

// Get list of registered workers
List<string> workerNames = coordinator.GetWorkerNames();
foreach (string name in workerNames)
{
    Console.WriteLine($"Worker: {name}");
}

// Check if a specific worker is running
bool isRunning = coordinator.IsWorkerRunning("health-monitor");
Console.WriteLine($"Health monitor running: {isRunning}");

// Stop all workers when shutting down
await coordinator.StopAllAsync();
```

### Available Extension Methods

- **AddCachingServices()** - Registers in-memory caching services
- **AddHttpClientServices(timeoutMs, maxRetries)** - Configures HTTP client with retry policy
- **AddWebhookServices()** - Sets up webhook notification handler
- **AddLoggingServices(logPath, minLevel)** - Configures file-based logging
- **AddEventBus()** - Registers event bus for publish-subscribe messaging
- **AddRateLimiting(capacity, refillRate)** - Configures rate limiting with token bucket algorithm
- **AddServiceDiscovery()** - Registers service discovery client
- **AddInfrastructureServices(configure)** - Convenience method to add all infrastructure services at once

## HealthCheckRepository

The `HealthCheckRepository` class provides data access methods for managing health check results in the SQLite database. It serves as the primary storage mechanism for health monitoring data, enabling the application to persist health check outcomes, retrieve historical health data, and maintain system reliability metrics across application restarts.

This repository handles all CRUD operations for `HealthCheckResult` entities, including retrieval of latest results, recent history, results by service ID, and aggregated statistics. It also provides methods for cleanup operations to maintain database performance by removing old health check records.

### Example Usage

```csharp
// Create a health check repository instance
var repository = new HealthCheckRepository(dbContext);

// Add a new health check result
var healthResult = HealthCheckResult.CreateSuccess(
    serviceId: "api-service-01",
    responseTimeMs: 125,
    httpStatus: 200
);

string resultId = await repository.AddAsync(healthResult);
Console.WriteLine($"Added health check result with ID: {resultId}");

// Retrieve the latest health check for a service
var latestResult = await repository.GetLatestAsync("api-service-01");
if (latestResult != null)
{
    Console.WriteLine($"Latest status: {latestResult.Status}");
    Console.WriteLine($"Response time: {latestResult.ResponseTimeMs}ms");
    Console.WriteLine($"Checked at: {latestResult.CheckedAt}");
}

// Get recent health history for trend analysis
var recentResults = await repository.GetRecentAsync(
    serviceId: "api-service-01",
    limit: 100
);
Console.WriteLine($"Found {recentResults.Count} recent health checks");

// Get all health checks for a specific service
var serviceResults = await repository.GetByServiceIdAsync("api-service-01");
Console.WriteLine($"Total health checks for service: {serviceResults.Count}");

// Get aggregated statistics for monitoring dashboards
var statistics = await repository.GetStatisticsAsync(
    serviceId: "api-service-01",
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: DateTime.UtcNow
);
Console.WriteLine($"Healthy: {statistics.HealthyCount}, Failed: {statistics.FailedCount}");
Console.WriteLine($"Average response time: {statistics.AverageResponseTimeMs}ms");
Console.WriteLine($"Max response time: {statistics.MaxResponseTimeMs}ms");
Console.WriteLine($"Min response time: {statistics.MinResponseTimeMs}ms");

// Clean up old health records (keeps 30 days of history by default)
bool cleanupSuccess = await repository.DeleteOlderThanAsync(
    DateTime.UtcNow.AddDays(-30)
);
Console.WriteLine($"Cleanup successful: {cleanupSuccess}");

// Delete a specific health check result
bool deleteSuccess = await repository.DeleteAsync(resultId);
Console.WriteLine($"Health check result deleted: {deleteSuccess}");
```

### InfrastructureOptions Properties

- **HttpTimeoutMs** (int, default: 30000) - HTTP request timeout in milliseconds
- **MaxRetries** (int, default: 3) - Maximum number of retry attempts
- **LogPath** (string, default: "logs/app.log") - Path to the log file
- **MinLogLevel** (LogLevel, default: LogLevel.Info) - Minimum log level to record
- **RateLimitCapacity** (int, default: 100) - Maximum number of requests allowed in the bucket
- **RateLimitRefillRate** (int, default: 10) - Number of tokens added per second

## ServicePort

The `ServicePort` type represents port configuration for exposing services to external traffic. It defines how internal service ports are mapped to external ports with protocol specification, and includes validation logic to ensure proper port configuration and security constraints. Service ports are used by the system to generate Caddy reverse proxy rules, systemd unit configurations, and health check endpoints.

### Example Usage

```csharp
// Create a service port mapping for a web application
var webPort = new ServicePort
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "web-app-01",
    InternalPort = 3000,
    ExternalPort = 80,
    Protocol = PortProtocol.Tcp,
    Description = "HTTP web traffic",
    IsPublic = true,
    CreatedAt = DateTime.UtcNow
};

// Validate the port configuration
webPort.Validate();

// Get port mapping string for Caddy/systemd configuration
string portMapping = webPort.GetPortMapping();
Console.WriteLine($"Port mapping: {portMapping}"); // Outputs: "3000:80/tcp"

// Create a secure HTTPS port mapping
var httpsPort = new ServicePort
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "web-app-01",
    InternalPort = 3001,
    ExternalPort = 443,
    Protocol = PortProtocol.Tcp,
    Description = "HTTPS web traffic",
    IsPublic = true,
    CreatedAt = DateTime.UtcNow
};

// Create an internal database port (non-public)
var dbPort = new ServicePort
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "database-01",
    InternalPort = 5432,
    ExternalPort = 5432,
    Protocol = PortProtocol.Tcp,
    Description = "PostgreSQL database",
    IsPublic = false,
    CreatedAt = DateTime.UtcNow
};

// Generate Caddy configuration for the service with these ports
Console.WriteLine("Generated Caddy configuration:");
Console.WriteLine($"  HTTP: :80 -> 3000/tcp");
Console.WriteLine($"  HTTPS: :443 -> 3001/tcp");
Console.WriteLine($"  DB: :5432 -> 5432/tcp (internal only)");
```

**Key Properties:**

- **Id**: Unique identifier for the port mapping
- **ServiceId**: Reference to the parent service
- **InternalPort**: The port the service listens on internally (1-65535)
- **ExternalPort**: The port exposed externally (1-65535)
- **Protocol**: Transport protocol (TCP or UDP)
- **Description**: Human-readable description of the port usage
- **IsPublic**: Whether the port is exposed to external traffic
- **CreatedAt**: Timestamp when the port mapping was created
- **Validate()**: Validates port ranges and security constraints
- **GetPortMapping()**: Returns formatted port mapping string (e.g., "3000:80/tcp")

## PaginationHelper

The `PaginationHelper` class provides utility methods for paginating, sorting, and filtering collections in a fluent and type-safe manner. It includes both simple static methods for quick operations and a fluent `QueryBuilder` API for building complex data queries with chained operations.

### Example Usage

```csharp
// Sample data model for pagination
public class ServiceDto
{
    public string Name { get; set; }
    public int Port { get; set; }
    public string Domain { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// Create a list of services
var services = new List<ServiceDto>
{
    new ServiceDto { Name = "api-service", Port = 8080, Domain = "api.example.com", CreatedAt = DateTime.Now.AddDays(-5), IsActive = true },
    new ServiceDto { Name = "web-app", Port = 3000, Domain = "web.example.com", CreatedAt = DateTime.Now.AddDays(-2), IsActive = true },
    new ServiceDto { Name = "cache-service", Port = 6379, Domain = "cache.example.com", CreatedAt = DateTime.Now.AddDays(-10), IsActive = false },
    new ServiceDto { Name = "database", Port = 5432, Domain = "db.example.com", CreatedAt = DateTime.Now.AddDays(-1), IsActive = true },
    new ServiceDto { Name = "monitoring", Port = 9090, Domain = "monitor.example.com", CreatedAt = DateTime.Now.AddDays(-15), IsActive = true }
};

// Example 1: Simple pagination
var page1 = PaginationHelper.Paginate(services, page: 1, pageSize: 2);
Console.WriteLine($"Page 1: {page1.Items.Count} items");
foreach (var item in page1.Items)
{
    Console.WriteLine($"  - {item.Name} on port {item.Port}");
}

// Example 2: Sort by property name
var sortedByName = PaginationHelper.SortBy(services, "Name");
Console.WriteLine("\nSorted by name:");
foreach (var item in sortedByName)
{
    Console.WriteLine($"  - {item.Name}");
}

// Example 3: Sort descending by date
var sortedByDate = PaginationHelper.SortBy(services, "CreatedAt", ascending: false);
Console.WriteLine("\nSorted by creation date (newest first):");
foreach (var item in sortedByDate)
{
    Console.WriteLine($"  - {item.Name} created {item.CreatedAt:yyyy-MM-dd}");
}

// Example 4: Filter by property value
var activeServices = PaginationHelper.FilterBy(services, "IsActive", true);
Console.WriteLine($"\nActive services: {activeServices.Count}");

// Example 5: Filter with predicate
var recentServices = PaginationHelper.Filter(services, s => s.CreatedAt > DateTime.Now.AddDays(-7));
Console.WriteLine($"\nServices created in last 7 days: {recentServices.Count}");

// Example 6: Fluent QueryBuilder API
var queryResult = new QueryBuilder<ServiceDto>(services)
    .Where(s => s.IsActive)
    .SortBy("CreatedAt", ascending: false)
    .Page(1)
    .PageSize(3)
    .Execute();

Console.WriteLine($"\nQuery result - Page {queryResult.Page} of {queryResult.TotalCount} items:");
foreach (var item in queryResult.Items)
{
    Console.WriteLine($"  - {item.Name} (created: {item.CreatedAt:yyyy-MM-dd})");
}

// Example 7: Unpaged query for all matching items
var allActive = new QueryBuilder<ServiceDto>(services)
    .Where(s => s.IsActive)
    .SortBy("Name")
    .ExecuteUnpaged();

Console.WriteLine($"\nAll active services ({allActive.Count}):");
foreach (var item in allActive)
{
    Console.WriteLine($"  - {item.Name}");
}
```

## ManagedService

The `ManagedService` type represents a service that is managed by the system. It has the following properties:

* Id: a unique identifier for the service
* Name: the name of the service
* Description: a brief description of the service
* Type: the type of service (e.g. Windows service, Linux service)
* ExecutablePath: the path to the executable that runs the service
* WorkingDirectory: the working directory for the service
* Arguments: the arguments passed to the service when it is started
* Status: the current status of the service (e.g. running, stopped)
* Port: the port number used by the service
* HostBinding: the host binding for the service (e.g. localhost, all interfaces)
* AutoStart: whether the service starts automatically when the system boots
* Priority: the priority of the service (e.g. high, normal, low)
* EnvironmentVariables: environment variables set for the service
* CreatedAt: the date and time the service was created
* UpdatedAt: the date and time the service was last updated
* SystemdUnitName: the name of the systemd unit for the service (if applicable)
* ExposedPorts: a list of ports exposed by the service
* HealthCheck: configuration for health checks on the service
* IsEnabled: whether the service is enabled or disabled

Example usage:

```csharp
var myService = new ManagedService
{
    Id = Guid.NewGuid().ToString(),
    Name = "my-web-app",
    Description = "My web application service",
    Type = ServiceType.WebApplication,
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/my-app",
    Arguments = "MyApp.dll --environment Production",
    Status = ServiceStatus.Stopped,
    Port = 8080,
    HostBinding = "0.0.0.0",
    AutoStart = true,
    Priority = 100,
    EnvironmentVariables = "ASPNETCORE_ENVIRONMENT=Production\nLOG_LEVEL=Info",
    SystemdUnitName = "my-web-app.service",
    ExposedPorts = new List<ServicePort>
    {
        new ServicePort { Port = 8080, Protocol = "tcp" },
        new ServicePort { Port = 8443, Protocol = "tcp" }
    },
    HealthCheck = new HealthCheckConfig
    {
        Url = "http://localhost:8080/health",
        Interval = TimeSpan.FromSeconds(30),
        Timeout = TimeSpan.FromSeconds(5),
        Retries = 3
    },
    IsEnabled = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

myService.Validate();
```

## ArgumentParser

The `ArgumentParser` class provides command-line argument parsing functionality for the CLI tool. It parses command-line arguments into structured command objects using a simple key-value pattern for flags and supports positional arguments. The parser efficiently handles both boolean flags (like `--verbose`, `--force`) and flags with values (like `--port 8080` or `--domain example.com`), using span-based comparisons to avoid unnecessary string allocations.

**Example Usage:**

```csharp
// Create argument parser with command-line arguments
var args = new[] { "service", "add", "my-service", "--port", "8080", "--domain", "example.com", "--verbose" };
var parser = new ArgumentParser(args);

// Get the command name
string command = parser.GetCommand(); // Returns "service"

// Get positional arguments
string serviceName = parser.GetPositional(0); // Returns "my-service"

// Check for boolean flags
bool isVerbose = parser.HasFlag("verbose"); // Returns true
bool isForce = parser.HasFlag("force"); // Returns false

// Get flag values
string portValue = parser.GetFlagValue("port"); // Returns "8080"
string domainValue = parser.GetFlagValue("domain"); // Returns "example.com"

// Get all positional arguments
var allPositionals = parser.GetAllPositional(); // Returns ["my-service"]

// Get all flags provided
var allFlags = parser.GetAllFlags(); // Returns ["port", "domain", "verbose"]
```

## IErrorHandler

The `IErrorHandler` interface provides centralized error handling for the application by catching exceptions, logging them, and converting them to structured error responses. It enables consistent error handling across the codebase through a pipeline of specialized handlers that can process specific exception types and return appropriate user-friendly messages with exit codes.

The error handling pipeline supports registering custom handlers for specific exception types using predicates, allowing fine-grained control over error responses while maintaining a default fallback for unhandled exceptions.

### Public Members

- `Task<ErrorResponse> HandleAsync(Exception ex)` - Processes an exception and returns a structured error response
- `void AddHandler(Func<Exception, bool> predicate, Func<Exception, Task<ErrorResponse>> handler)` - Registers a custom error handler for exceptions matching the predicate

### ErrorResponse Properties

- `int ExitCode` - The exit code to return when this error occurs
- `string Code` - A machine-readable error code (e.g., "SERVICE_NOT_FOUND", "CONFIGURATION_ERROR")
- `string Message` - A user-friendly error message
- `string Details` - Detailed error information including stack trace (for debugging)

### Example Usage

```csharp
// Create error handler with logger
var logger = new FileLogger("/var/log/caddy-vps-toolkit/errors.log", LogLevel.Error);
var errorHandler = new ErrorHandlingPipeline(logger);

// Add a custom handler for specific exceptions
// For example, handle database connection failures
var dbLogger = new FileLogger("/var/log/caddy-vps-toolkit/database-errors.log", LogLevel.Error);
errorHandler.AddHandler(
    ex => ex is DatabaseConnectionException,
    async ex =>
    {
        await dbLogger.LogErrorAsync($"Database connection failed: {ex.Message}");
        return new ErrorResponse
        {
            ExitCode = 5,
            Message = "Database connection unavailable. Please check database status and retry.",
            Code = "DATABASE_UNAVAILABLE"
        };
    }
);

// Handle an exception
try
{
    // Some operation that might throw
    await service.StartAsync();
}
catch (Exception ex)
{
    var errorResponse = await errorHandler.HandleAsync(ex);
    
    Console.WriteLine($"Error occurred (Code: {errorResponse.Code}): {errorResponse.Message}");
    Console.WriteLine($"Exit code: {errorResponse.ExitCode}");
    
    if (!string.IsNullOrEmpty(errorResponse.Details))
    {
        Console.WriteLine($"Details: {errorResponse.Details}");
    }
    
    Environment.Exit(errorResponse.ExitCode);
}

// Default handlers are automatically registered for common exceptions:
// - ServiceNotFoundException -> ExitCode 1, Code "SERVICE_NOT_FOUND"
// - ServiceConfigurationException -> ExitCode 2, Code "CONFIGURATION_ERROR"
// - ArgumentException -> ExitCode 3, Code "INVALID_ARGUMENT"
// - All other exceptions -> ExitCode 255, Code "UNEXPECTED_ERROR"
```

## IServiceDiscoveryClient

The `IServiceDiscoveryClient` interface provides a service discovery abstraction for locating service endpoints in distributed systems. It enables dynamic service registration, deregistration, and discovery, supporting integration with service registries like Consul, Eureka, or custom implementations. This interface is particularly useful for service-to-service communication where endpoints may change dynamically.

**Key Features:**
- Discover individual service instances by name
- Retrieve all available instances of a service
- Register services with metadata for discovery
- Deregister services when they become unavailable

**Example Usage:**

```csharp
// Create a service instance to register
var serviceInstance = new ServiceInstance
{
    Id = Guid.NewGuid().ToString(),
    ServiceName = "api-service",
    Host = "192.168.1.100",
    Port = 5000,
    Metadata = new Dictionary<string, string>
    {
        {"version", "1.2.3"},
        {"environment", "production"},
        {"region", "us-west-2"}
    }
};

// Create service discovery client (using in-memory implementation for local development)
var discoveryClient = new InMemoryServiceDiscoveryClient();

// Register the service so it can be discovered by other services
await discoveryClient.RegisterAsync(serviceInstance);

// Discover a specific service instance
var instance = await discoveryClient.DiscoverAsync("api-service");
Console.WriteLine($"Discovered service at: {instance.GetUrl()}");
// Output: Discovered service at: http://192.168.1.100:5000

// Discover all available instances of a service
var allInstances = await discoveryClient.DiscoverAllAsync("api-service");
Console.WriteLine($"Found {allInstances.Count} instances of api-service");

// Get service URL for connection
string serviceUrl = instance.GetUrl();
Console.WriteLine($"Service URL: {serviceUrl}");

// Deregister service when it's shutting down
await discoveryClient.DeregisterAsync(serviceInstance.Id);
Console.WriteLine("Service deregistered");
```

## IHttpClient

The `IHttpClient` interface provides a clean abstraction for HTTP communication with built-in retry, timeout, and error handling. It simplifies making HTTP requests by handling common concerns like retry policies, timeouts, and JSON serialization/deserialization, allowing service code to focus on business logic rather than HTTP plumbing.

**Key Features:**
- Automatic retry on transient failures
- Configurable timeout (default: 30 seconds)
- JSON serialization/deserialization
- Response wrapper with status, data, and error information
- Header management
- Async/await support

**Example Usage:**

```csharp
// Create HTTP client with default timeout (30s) and retry policy
var httpClient = new HttpClientWrapper();

// Make a GET request to retrieve data
var getResponse = await httpClient.GetAsync<ApiResponse>(
    url: "https://api.example.com/users/123",
    headers: new Dictionary<string, string> { { "Authorization", "Bearer token123" } }
);

if (getResponse.IsSuccess)
{
    Console.WriteLine($"Retrieved user: {getResponse.Data.Name}");
    Console.WriteLine($"Status: {getResponse.StatusCode}");
}
else
{
    Console.WriteLine($"Error: {getResponse.Error}");
}

// Make a POST request to create a resource
var newUser = new { Name = "John Doe", Email = "john@example.com" };
var postResponse = await httpClient.PostAsync<ApiResponse>(
    url: "https://api.example.com/users",
    data: newUser,
    headers: new Dictionary<string, string> { { "Authorization", "Bearer token123" } }
);

if (postResponse.IsSuccess)
{
    Console.WriteLine($"Created user with ID: {postResponse.Data.Id}");
}

// Make a PUT request to update a resource
var updateData = new { Name = "John Updated", Status = "active" };
var putResponse = await httpClient.PutAsync<ApiResponse>(
    url: $"https://api.example.com/users/{userId}",
    data: updateData
);

// Make a DELETE request
var deleteResponse = await httpClient.DeleteAsync(
    url: $"https://api.example.com/users/{userId}"
);

Console.WriteLine($"Delete successful: {deleteResponse.IsSuccess}");
```

## IWebhookHandler
{
    ServiceName = "api-service",
    Port = 8080,
    Domain = "api.example.com",
    Status = "created"
};

bool success = await webhookHandler.TriggerAsync(
    WebhookEventType.ServiceCreated,
    serviceCreatedPayload
);

Console.WriteLine($"Webhook triggered successfully: {success}");

// Get all registered URLs for a specific event type
var healthCheckUrls = webhookHandler.GetRegistrations(WebhookEventType.HealthCheckFailed);
foreach (var url in healthCheckUrls)
{
    Console.WriteLine($"Health check webhook registered at: {url}");
}

// Unregister a webhook URL
webhookHandler.Unregister(
    url: "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
    eventType: WebhookEventType.ServiceCreated
);
```

## HealthAwareRoutingPolicy

`HealthAwareRoutingPolicy` is the unified entry point for health-aware adaptive load balancing. It combines real-time health monitoring, circuit breaker logic, and machine-learning-based scoring to select the optimal upstream server for each request.

The policy first attempts to use adaptive scoring based on historical performance data. When the scoring model lacks sufficient data (e.g., new servers with no observations), it transparently falls back to the pool's configured load-balancing strategy. All circuit-breaker checks are applied before scoring, ensuring callers receive clear failure reasons like "circuit open" or "pool disabled".

Key features:
- **Adaptive Scoring**: Uses `IAdaptiveLoadBalancer` to rank upstream candidates based on recent performance
- **Circuit Breaker**: Enforces health ratio thresholds to prevent routing to unhealthy pools
- **Fallback Strategy**: Delegates to pool's configured strategy when scoring confidence is low
- **Real-time Feedback**: Records request outcomes to improve future routing decisions
- **Diagnostic API**: Provides scored candidate lists for health dashboards and CLI monitoring

**Example Usage:**

```csharp
// Setup dependencies
var upstreamManager = new UpstreamManagerService(/* dependencies */);
var adaptiveBalancer = new AdaptiveLoadBalancer(/* metrics aggregator */);
var loadBalancingOptions = new LoadBalancingOptions
{
    CircuitBreakerEnabled = true,
    CircuitBreakerHealthThreshold = 0.5, // 50% healthy servers minimum
    DefaultStrategy = LoadBalancingStrategy.RoundRobin
};

// Create the routing policy
var routingPolicy = new HealthAwareRoutingPolicy(
    upstreamManager,
    adaptiveBalancer,
    loadBalancingOptions
);

// Route a request to select the best upstream
var context = new UpstreamSelectionContext("api-pool")
{
    ClientIp = "192.168.1.100",
    SessionToken = "abc123"
};

var result = await routingPolicy.RouteAsync(context);

if (result.IsSuccess)
{
    var selectedServer = result.Data;
    Console.WriteLine($"Selected upstream: {selectedServer.Address}:{selectedServer.Port}");
    
    // Simulate request completion and report outcome
    await routingPolicy.NotifyOutcomeAsync(
        poolId: "api-pool",
        upstreamId: selectedServer.Id,
        responseTimeMs: 125,
        succeeded: true
    );
}
else
{
    Console.WriteLine($"Routing failed: {result.ErrorMessage}");
    Console.WriteLine($"Error code: {result.ErrorCode}");
}

// Get scored candidates for health dashboard
var scoredCandidates = await routingPolicy.GetScoredCandidatesAsync("api-pool");
foreach (var score in scoredCandidates.OrderByDescending(s => s.CompositeScore))
{
    Console.WriteLine($"Server {score.UpstreamId}: Score={score.CompositeScore:F2}, " +
                     $"Health={score.IsHealthy}, Weight={score.EffectiveWeight}");
}

// Force recalibration when pool configuration changes
await routingPolicy.RecalibrateAsync("api-pool");

// Get effective weight for a specific upstream
var weight = await routingPolicy.GetEffectiveWeightAsync(selectedServer.Id);
Console.WriteLine($"Effective weight: {weight}");
```

## LoadBalancingOptions

`LoadBalancingOptions` provides runtime configuration for the upstream management and health-aware load-balancing subsystem. It controls default behaviors such as health probing, retry policies, sticky sessions, and circuit breaker settings that apply to all upstream pools unless overridden at the pool level.

The options govern active health checks, passive failure detection, retry strategies, session affinity, and circuit breaking behavior across your load-balanced services.

## UpstreamServer

The `UpstreamServer` type represents a backend server that can receive traffic from the load balancer. It maintains comprehensive health monitoring, performance tracking, and connection state information to enable intelligent routing decisions. Upstream servers are organized into pools and are automatically evaluated for availability based on health probes and performance metrics.

Upstream servers support weighted load balancing, health status tracking, circuit breaking, and real-time performance monitoring through continuous health checks and metrics aggregation.

```csharp
// Example: Creating and configuring an upstream server for a load-balanced service pool
var upstreamServer = new UpstreamServer
{
    Id = "api-server-01",
    Address = "192.168.1.100",
    Port = 8080,
    Weight = 100, // Higher weight = more traffic
    Status = UpstreamServerStatus.Healthy,
    IsHealthy = true,
    LastCheckedAt = DateTime.UtcNow,
    ConsecutiveFailures = 0,
    ConsecutiveSuccesses = 150,
    AverageResponseTimeMs = 45,
    ActiveConnections = 8,
    Tags = "production,high-priority",
    Notes = "Primary API server in us-west-2 region",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the upstream server configuration
upstreamServer.Validate();

// Check if the server is available for routing
if (upstreamServer.IsAvailable)
{
    Console.WriteLine($"Server {upstreamServer.Id} is available at {upstreamServer.GetUpstreamAddress()}");
}

// Record a health probe result (automatically updates health status and metrics)
var probeResult = new HealthProbeResult
{
    Success = true,
    ResponseTimeMs = 42,
    StatusCode = 200,
    CheckedAt = DateTime.UtcNow
};

upstreamServer.RecordHealthProbeResult(probeResult);

// The server's health status and metrics are automatically updated
Console.WriteLine($"Server health: {upstreamServer.Status}");
Console.WriteLine($"Average response time: {upstreamServer.AverageResponseTimeMs}ms");
Console.WriteLine($"Active connections: {upstreamServer.ActiveConnections}");
```

## UpstreamPool

The `UpstreamPool` type represents a named pool of backend upstream servers that shares a load-balancing strategy, health-check policy, and retry configuration. A pool is owned by a single `ManagedService` and serves as the primary unit of Caddy configuration generation for dynamic upstream management in v2.

Upstream pools enable sophisticated load balancing scenarios including:
- Multiple load balancing strategies (Round Robin, Least Connections, Random, Weighted Random, IP Hash)
- Active and passive health monitoring with configurable thresholds
- Retry policies for failed requests
- Sticky sessions via cookie-based affinity
- Weighted distribution based on server capacity

```csharp
// Example: Creating a load-balanced pool with multiple upstream servers
var pool = new UpstreamPool
{
    Name = "api-pool",
    ServiceId = "api-service-123",
    Strategy = LoadBalancingStrategy.RoundRobin,
    Servers = new List<UpstreamServer>
    {
        new UpstreamServer
        {
            Id = "api-server-01",
            Address = "192.168.1.100",
            Port = 8080,
            Weight = 100,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new UpstreamServer
        {
            Id = "api-server-02",
            Address = "192.168.1.101",
            Port = 8080,
            Weight = 75,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new UpstreamServer
        {
            Id = "api-server-03",
            Address = "192.168.1.102",
            Port = 8080,
            Weight = 50,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    },
    PassiveHealthEnabled = true,
    ActiveHealthEnabled = true,
    HealthCheckIntervalSeconds = 30,
    UnhealthyThreshold = 3,
    HealthyThreshold = 2,
    MaxRetries = 2,
    RetryDurationSeconds = 30,
    StickyCookieName = "api_session",
    HealthProbePath = "/health",
    IsEnabled = true
};

// Validate the pool configuration
pool.Validate();

// Get available servers (only healthy, active servers)
var availableServers = pool.GetAvailableServers();
Console.WriteLine($"Available servers: {availableServers.Count}");

// Get total active connections across all servers
var totalConnections = pool.GetTotalActiveConnections();
Console.WriteLine($"Total active connections: {totalConnections}");

// Generate Caddy reverse_proxy configuration block
var caddyConfig = pool.GenerateCaddyUpstreamBlock("/*");
Console.WriteLine("Generated Caddy configuration:");
Console.WriteLine(caddyConfig);
```

## UpstreamMetricsSummary

The `UpstreamMetricsSummary` type provides an immutable point-in-time summary of aggregated request performance metrics for a single upstream server. It captures key performance indicators including latency percentiles (P50, P95, P99), error rates, throughput, and sample statistics, enabling intelligent load balancing decisions based on recent service behavior.

The summary is derived from a sliding observation window of request samples and is designed to be used by the adaptive scoring model to evaluate upstream health and performance for routing decisions.



### Example Usage

```csharp
// Create a metrics window for tracking upstream performance
var metricsWindow = new UpstreamMetricsWindow("api-server-01", maxSamples: 200);

// Add request observations to the window (automatically maintains sliding window)
metricsWindow.Add(responseTimeMs: 45, succeeded: true);
metricsWindow.Add(responseTimeMs: 89, succeeded: true);
metricsWindow.Add(responseTimeMs: 125, succeeded: false);
metricsWindow.Add(responseTimeMs: 67, succeeded: true);
metricsWindow.Add(responseTimeMs: 203, succeeded: true);

// Generate a summary of current metrics
var summary = metricsWindow.Summarize();

if (summary != null)
{
    Console.WriteLine(summary.ToString());
    // Output: [api-server-01] n=5 p50=67.0ms p99=203.0ms err=20.0% rps=X.XX
    
    Console.WriteLine($"Upstream: {summary.UpstreamId}");
    Console.WriteLine($"Samples: {summary.SampleCount}");
    Console.WriteLine($"P50 Latency: {summary.P50LatencyMs:F1}ms");
    Console.WriteLine($"P99 Latency: {summary.P99LatencyMs:F1}ms");
    Console.WriteLine($"Mean Latency: {summary.MeanLatencyMs:F1}ms");
    Console.WriteLine($"Error Rate: {summary.ErrorRate:P1}");
    Console.WriteLine($"Throughput: {summary.ThroughputRps:F2} req/s");
    Console.WriteLine($"Window: {summary.WindowStartUtc:u} to {summary.WindowEndUtc:u}");
    Console.WriteLine($"Statistically Significant: {summary.IsStatisticallySignificant}");
}

// Clear the window to start fresh
metricsWindow.Clear();
```

## SlidingWindowMetricsAggregator

`SlidingWindowMetricsAggregator` is a thread-safe utility class that maintains a rolling window of metrics over time, enabling real-time aggregation and analysis of upstream performance data. It tracks request counts, response times, error rates, and other key performance indicators across a configurable time window, allowing the adaptive load balancer to make data-driven routing decisions based on recent service behavior rather than historical averages.

This aggregator is particularly useful for load balancing scenarios where service performance fluctuates over time, as it provides up-to-date metrics that reflect current conditions while smoothing out short-term spikes and anomalies.

**Example Usage:**

```csharp
// Configure load balancing options in your service setup
var loadBalancingOptions = new LoadBalancingOptions
{
    DefaultStrategy = LoadBalancingStrategy.RoundRobin,
    HealthCheckIntervalSeconds = 30,
    HealthProbeTimeoutMs = 5000,
    HealthProbePath = "/health",
    ActiveHealthEnabled = true,
    PassiveHealthEnabled = true,
    UnhealthyThreshold = 3,
    HealthyThreshold = 2,
    MaxRetries = 2,
    RetryDurationSeconds = 30,
    StickySessionEnabled = true,
    DefaultStickyCookieName = "my_session",
    CircuitBreakerEnabled = true,
    CircuitBreakerHealthThreshold = 0.3,
    CircuitBreakerRecoverySeconds = 60,
    ConnectionDrainTimeoutSeconds = 30
};

// Register with your service collection
services.AddUpstreamManagement(options =>
{
    options.DefaultStrategy = loadBalancingOptions.DefaultStrategy;
    options.HealthCheckIntervalSeconds = loadBalancingOptions.HealthCheckIntervalSeconds;
    options.HealthProbeTimeoutMs = loadBalancingOptions.HealthProbeTimeoutMs;
    options.HealthProbePath = loadBalancingOptions.HealthProbePath;
    options.ActiveHealthEnabled = loadBalancingOptions.ActiveHealthEnabled;
    options.PassiveHealthEnabled = loadBalancingOptions.PassiveHealthEnabled;
    options.UnhealthyThreshold = loadBalancingOptions.UnhealthyThreshold;
    options.HealthyThreshold = loadBalancingOptions.HealthyThreshold;
    options.MaxRetries = loadBalancingOptions.MaxRetries;
    options.RetryDurationSeconds = loadBalancingOptions.RetryDurationSeconds;
    options.StickySessionEnabled = loadBalancingOptions.StickySessionEnabled;
    options.DefaultStickyCookieName = loadBalancingOptions.DefaultStickyCookieName;
    options.CircuitBreakerEnabled = loadBalancingOptions.CircuitBreakerEnabled;
    options.CircuitBreakerHealthThreshold = loadBalancingOptions.CircuitBreakerHealthThreshold;
    options.CircuitBreakerRecoverySeconds = loadBalancingOptions.CircuitBreakerRecoverySeconds;
    options.ConnectionDrainTimeoutSeconds = loadBalancingOptions.ConnectionDrainTimeoutSeconds;
});
```

## UpstreamServer

The `UpstreamServer` type represents a backend server that can receive traffic from the load balancer. It maintains comprehensive health monitoring, performance tracking, and connection state information to enable intelligent routing decisions. Upstream servers are organized into pools and are automatically evaluated for availability based on health probes and performance metrics.

Upstream servers support weighted load balancing, health status tracking, circuit breaking, and real-time performance monitoring through continuous health checks and metrics aggregation.

```csharp
// Example: Creating and configuring an upstream server for a load-balanced service pool
var upstreamServer = new UpstreamServer
{
    Id = "api-server-01",
    Address = "192.168.1.100",
    Port = 8080,
    Weight = 100, // Higher weight = more traffic
    Status = UpstreamServerStatus.Healthy,
    IsHealthy = true,
    LastCheckedAt = DateTime.UtcNow,
    ConsecutiveFailures = 0,
    ConsecutiveSuccesses = 150,
    AverageResponseTimeMs = 45,
    ActiveConnections = 8,
    Tags = "production,high-priority",
    Notes = "Primary API server in us-west-2 region",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Validate the upstream server configuration
upstreamServer.Validate();

// Check if the server is available for routing
if (upstreamServer.IsAvailable)
{
    Console.WriteLine($"Server {upstreamServer.Id} is available at {upstreamServer.GetUpstreamAddress()}");
}

// Record a health probe result (automatically updates health status and metrics)
var probeResult = new HealthProbeResult
{
    Success = true,
    ResponseTimeMs = 42,
    StatusCode = 200,
    CheckedAt = DateTime.UtcNow
};

upstreamServer.RecordHealthProbeResult(probeResult);

// The server's health status and metrics are automatically updated
Console.WriteLine($"Server health: {upstreamServer.Status}");
Console.WriteLine($"Average response time: {upstreamServer.AverageResponseTimeMs}ms");
Console.WriteLine($"Active connections: {upstreamServer.ActiveConnections}");
```

## UpstreamPool

The `UpstreamPool` type represents a named pool of backend upstream servers that shares a load-balancing strategy, health-check policy, and retry configuration. A pool is owned by a single `ManagedService` and serves as the primary unit of Caddy configuration generation for dynamic upstream management in v2.

Upstream pools enable sophisticated load balancing scenarios including:
- Multiple load balancing strategies (Round Robin, Least Connections, Random, Weighted Random, IP Hash)
- Active and passive health monitoring with configurable thresholds
- Retry policies for failed requests
- Sticky sessions via cookie-based affinity
- Weighted distribution based on server capacity

```csharp
// Example: Creating a load-balanced pool with multiple upstream servers
var pool = new UpstreamPool
{
    Name = "api-pool",
    ServiceId = "api-service-123",
    Strategy = LoadBalancingStrategy.RoundRobin,
    Servers = new List<UpstreamServer>
    {
        new UpstreamServer
        {
            Id = "api-server-01",
            Address = "192.168.1.100",
            Port = 8080,
            Weight = 100,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new UpstreamServer
        {
            Id = "api-server-02",
            Address = "192.168.1.101",
            Port = 8080,
            Weight = 75,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new UpstreamServer
        {
            Id = "api-server-03",
            Address = "192.168.1.102",
            Port = 8080,
            Weight = 50,
            Status = UpstreamServerStatus.Active,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    },
    PassiveHealthEnabled = true,
    ActiveHealthEnabled = true,
    HealthCheckIntervalSeconds = 30,
    UnhealthyThreshold = 3,
    HealthyThreshold = 2,
    MaxRetries = 2,
    RetryDurationSeconds = 30,
    StickyCookieName = "api_session",
    HealthProbePath = "/health",
    IsEnabled = true
};

// Validate the pool configuration
pool.Validate();

// Get available servers (only healthy, active servers)
var availableServers = pool.GetAvailableServers();
Console.WriteLine($"Available servers: {availableServers.Count}");

// Get total active connections across all servers
var totalConnections = pool.GetTotalActiveConnections();
Console.WriteLine($"Total active connections: {totalConnections}");

// Generate Caddy reverse_proxy configuration block
var caddyConfig = pool.GenerateCaddyUpstreamBlock("/*");
Console.WriteLine("Generated Caddy configuration:");
Console.WriteLine(caddyConfig);
```

## UpstreamMetricsSummary

The `UpstreamMetricsSummary` type provides an immutable point-in-time summary of aggregated request performance metrics for a single upstream server. It captures key performance indicators including latency percentiles (P50, P95, P99), error rates, throughput, and sample statistics, enabling intelligent load balancing decisions based on recent service behavior.

The summary is derived from a sliding observation window of request samples and is designed to be used by the adaptive scoring model to evaluate upstream health and performance for routing decisions.



### Example Usage

```csharp
// Create a metrics window for tracking upstream performance
var metricsWindow = new UpstreamMetricsWindow("api-server-01", maxSamples: 200);

// Add request observations to the window (automatically maintains sliding window)
metricsWindow.Add(responseTimeMs: 45, succeeded: true);
metricsWindow.Add(responseTimeMs: 89, succeeded: true);
metricsWindow.Add(responseTimeMs: 125, succeeded: false);
metricsWindow.Add(responseTimeMs: 67, succeeded: true);
metricsWindow.Add(responseTimeMs: 203, succeeded: true);

// Generate a summary of current metrics
var summary = metricsWindow.Summarize();

if (summary != null)
{
    Console.WriteLine(summary.ToString());
    // Output: [api-server-01] n=5 p50=67.0ms p99=203.0ms err=20.0% rps=X.XX
    
    Console.WriteLine($"Upstream: {summary.UpstreamId}");
    Console.WriteLine($"Samples: {summary.SampleCount}");
    Console.WriteLine($"P50 Latency: {summary.P50LatencyMs:F1}ms");
    Console.WriteLine($"P99 Latency: {summary.P99LatencyMs:F1}ms");
    Console.WriteLine($"Mean Latency: {summary.MeanLatencyMs:F1}ms");
    Console.WriteLine($"Error Rate: {summary.ErrorRate:P1}");
    Console.WriteLine($"Throughput: {summary.ThroughputRps:F2} req/s");
    Console.WriteLine($"Window: {summary.WindowStartUtc:u} to {summary.WindowEndUtc:u}");
    Console.WriteLine($"Statistically Significant: {summary.IsStatisticallySignificant}");
}

// Clear the window to start fresh
metricsWindow.Clear();
```

## SlidingWindowMetricsAggregator

`SlidingWindowMetricsAggregator` is a thread-safe utility class that maintains a rolling window of metrics over time, enabling real-time aggregation and analysis of upstream performance data. It tracks request counts, response times, error rates, and other key performance indicators across a configurable time window, allowing the adaptive load balancer to make data-driven routing decisions based on recent service behavior rather than historical averages.

This aggregator is particularly useful for load balancing scenarios where service performance fluctuates over time, as it provides up-to-date metrics that reflect current conditions while smoothing out short-term spikes and anomalies.

**Example Usage:**

```csharp
// Create a metrics aggregator with a 5-minute sliding window
var metricsAggregator = new SlidingWindowMetricsAggregator(
    windowSize: TimeSpan.FromMinutes(5),
    bucketSize: TimeSpan.FromSeconds(10)
);

// Record metrics for upstream requests
await metricsAggregator.Record(
    upstreamAddress: "192.168.1.100:8080",
    responseTime: TimeSpan.FromMilliseconds(125),
    statusCode: 200,
    requestSize: 1024,
    responseSize: 2048
);

// Record another request
await metricsAggregator.Record(
    upstreamAddress: "192.168.1.100:8080",
    responseTime: TimeSpan.FromMilliseconds(85),
    statusCode: 200,
    requestSize: 512,
    responseSize: 1024
);

// Get aggregated metrics summary
var summary = metricsAggregator.GetSummary("192.168.1.100:8080");

if (summary != null)
{
    Console.WriteLine($"Total Requests: {summary.TotalRequests}");
    Console.WriteLine($"Average Response Time: {summary.AverageResponseTime.TotalMilliseconds:F2}ms");
    Console.WriteLine($"Error Rate: {summary.ErrorRate:P2}");
    Console.WriteLine($"Throughput: {summary.ThroughputPerSecond:F2} req/s");
    Console.WriteLine($"95th Percentile: {summary.Percentile95.TotalMilliseconds:F2}ms");
    Console.WriteLine($"Last 5 Minutes: Success={summary.SuccessCount}, Errors={summary.ErrorCount}");
}

// Reset the aggregator to clear all metrics
metricsAggregator.Reset();

// Use with AdaptiveLoadBalancer for dynamic routing decisions
var loadBalancer = new AdaptiveLoadBalancer(
    metricsAggregator,
    healthCheckInterval: TimeSpan.FromSeconds(30)
);

// The load balancer will automatically use the aggregator's metrics
// to evaluate pool health and make routing decisions
```

## DateTimeExtensionsTests

`DateTimeExtensionsTests` provides unit tests for the `DateTimeExtensions` utility class, which extends `DateTime` with common formatting and calculation methods. This test suite validates that all extension methods correctly handle various date and time scenarios, including relative time formatting, ISO 8601 serialization, start-of-day calculations, past/future checks, and working day counting.

```csharp
// Example: Using DateTimeExtensions for human-readable time formatting and calculations
var now = DateTime.UtcNow;

// Convert to relative time string (e.g., "5 minutes ago", "just now")
string relativeTime = now.AddMinutes(-5).ToRelativeTime();
Console.WriteLine(relativeTime); // Outputs: "5 minutes ago"

// Convert to ISO 8601 format
string iso8601 = now.ToIso8601();
Console.WriteLine(iso8601); // Outputs: "2026-07-16T12:34:56.7890000Z"

// Get start of day (midnight)
DateTime startOfDay = now.StartOfDay();
Console.WriteLine(startOfDay); // Outputs: "2026-07-16T00:00:00.0000000"

// Check if a date is in the past
bool isPast = new DateTime(2020, 1, 1).IsPast();
Console.WriteLine(isPast); // Outputs: true

// Count working days between two dates (Monday-Friday only)
int workingDays = new DateTime(2026, 7, 13).WorkingDaysBetween(new DateTime(2026, 7, 17));
Console.WriteLine(workingDays); // Outputs: 5
```

## Testing

Run the unit test suite:

```bash
dotnet test
```

Run with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers argument parsing and validation (`ArgumentParserTests`), string extension utilities (`StringExtensionsTests`), configuration validation helpers (`ValidationHelperTests`), and date/time utility methods (`DateTimeExtensionsTests`).

## CaddyConfigurationServiceTests

`CaddyConfigurationServiceTests` provides unit tests for the `CaddyConfigurationService` class, which validates the Caddy configuration generation functionality. This test suite verifies that the service correctly handles null inputs, generates valid Caddyfile configurations, creates proper route blocks, and validates configuration content, ensuring robust error handling and correct behavior for all public methods.

```csharp
// Example: Using CaddyConfigurationService with validation
var serviceRepositoryMock = Substitute.For<IServiceRepository>();
var serviceManager = new ServiceManagementService(serviceRepositoryMock);
var caddyService = new CaddyConfigurationService(serviceManager);

// Test null global configuration handling
Func<Task> nullConfigTest = async () => await caddyService.GenerateCaddyfileAsync(null!, new List<CaddyRoute>());
await nullConfigTest.Should().ThrowAsync<ArgumentNullException>();

// Test valid configuration generation
var config = new CaddyConfig { AdminEmail = "admin@example.com" };
var routes = new List<CaddyRoute> 
{
    new CaddyRoute { Domain = "test.com", UpstreamUrl = "http://localhost:8080", IsActive = true }
};

var result = await caddyService.GenerateCaddyfileAsync(config, routes);
result.Should().NotBeNullOrWhiteSpace();
result.Should().Contain("test.com {");
result.Should().Contain("reverse_proxy http://localhost:8080");

// Test route block generation with valid service
var service = new ManagedService { Id = Guid.NewGuid().ToString(), HostBinding = "127.0.0.1", Port = 5000 };
var route = caddyService.GenerateRouteForService(service, "app.test.com");
route.Should().NotBeNull();
route.Domain.Should().Be("app.test.com");
route.UpstreamUrl.Should().Be("http://127.0.0.1:5000");

// Test null route validation
Action nullRouteTest = () => caddyService.GenerateRouteBlock(null!);
nullRouteTest.Should().Throw<ArgumentNullException>();

// Test null service validation
Action nullServiceTest = () => caddyService.GenerateRouteForService(null!, "domain.com");
nullServiceTest.Should().Throw<ArgumentNullException>();

// Test empty content validation
Func<Task> emptyContentTest = async () => await caddyService.ValidateCaddyfileAsync(string.Empty);
await emptyContentTest.Should().ThrowAsync<ArgumentException>();
```

## SystemdUnitConfig

The `SystemdUnitConfig` type represents a systemd unit file configuration for managing services on Linux systems. It provides a structured way to define service execution parameters, restart policies, environment variables, and service dependencies, enabling automated generation of valid systemd unit files.

This type is used by the toolkit to create systemd units for managed services, ensuring consistent service management across different Linux distributions and systemd versions.



### Example Usage

```csharp
// Create a systemd unit configuration for a web application service
var systemdConfig = new SystemdUnitConfig
{
    Id = Guid.NewGuid().ToString(),
    ServiceId = "web-app-01",
    UnitName = "web-app",
    ExecStart = "/usr/bin/dotnet /opt/web-app/WebApp.dll --urls http://*:5000",
    ExecReload = "/bin/kill -s SIGUSR1 $MAINPID",
    ExecStop = "/bin/kill -s SIGTERM $MAINPID",
    User = "www-data",
    Group = "www-data",
    RestartPolicy = RestartPolicy.Always,
    RestartDelaySeconds = 5,
    StartLimitIntervalSec = 60,
    StartLimitBurst = 3,
    Type = "notify",
    StandardOutput = "journal",
    StandardError = "journal",
    WorkingDirectory = "/opt/web-app",
    Environment = new Dictionary<string, string>
    {
        ["ASPNETCORE_ENVIRONMENT"] = "Production",
        ["LOG_LEVEL"] = "Information"
    },
    EnvironmentFiles = new List<string> { "/etc/default/web-app" },
    After = new List<string> { "network.target", "postgresql.service" },
    Wants = new List<string> { "postgresql.service" },
    LimitNoFile = true,
    LimitNoFileValue = 65535
};

// Validate the configuration
systemdConfig.Validate();

// Generate the systemd unit file content
string unitContent = systemdConfig.GenerateSystemdContent();
Console.WriteLine(unitContent);

// Write to file
File.WriteAllText("/etc/systemd/system/web-app.service", unitContent);

// Reload systemd daemon
Console.WriteLine("sudo systemctl daemon-reload");
```

**Key Properties:**

- **Id**: Unique identifier for the configuration
- **ServiceId**: Identifier for the associated service
- **UnitName**: Name of the systemd unit file (without .service extension)
- **ExecStart**: Command to start the service (required)
- **ExecReload**: Command to reload the service configuration
- **ExecStop**: Command to stop the service
- **User**: User to run the service as (default: "root")
- **Group**: Group to run the service as (default: "root")
- **RestartPolicy**: When to restart the service (default: Always)
- **RestartDelaySeconds**: Delay before restarting (default: 10)
- **StartLimitIntervalSec**: Time window for start rate limiting (default: 300)
- **StartLimitBurst**: Maximum starts allowed in the time window (default: 5)
- **Type**: Service type (simple, forking, oneshot, dbus, notify, idle) (default: "simple")
- **StandardOutput**: Where to send stdout (journal, file, inherit, null) (default: "journal")
- **StandardError**: Where to send stderr (journal, file, inherit, null) (default: "journal")
- **Environment**: Dictionary of environment variables to set
- **EnvironmentFiles**: List of files containing environment variables to load
- **After**: List of units that must start before this one
- **Wants**: List of optional dependencies
- **WorkingDirectory**: Working directory for the service process
- **LimitNoFile**: Whether to set file descriptor limit (default: false)
- **LimitNoFileValue**: File descriptor limit value (default: 65535)
- **CreatedAt**: Timestamp when configuration was created
- **UpdatedAt**: Timestamp when configuration was last updated



## CollectionExtensionsTests

`CollectionExtensionsTests` provides unit tests for the `CollectionExtensions` utility class, which extends standard .NET collection types with safe access, batching, partitioning, and conditional operations. This test suite validates that all extension methods correctly handle edge cases including null collections, out-of-range indices, empty collections, and various predicate conditions, ensuring robust error handling and predictable behavior for collection operations.

```csharp
// Example: Using CollectionExtensions for safe collection operations
var services = new List<ManagedService>
{
    new ManagedService { Name = "api", Port = 8080, Domain = "api.example.com" },
    new ManagedService { Name = "web", Port = 3000, Domain = "web.example.com" },
    new ManagedService { Name = "cache", Port = 6379, Domain = "cache.example.com" }
};

// SafeGet - safely access elements without throwing IndexOutOfRangeException
var firstService = services.SafeGet(0); // Returns "api" service
var outOfRange = services.SafeGet(10); // Returns null/default instead of throwing
var negativeIndex = services.SafeGet(-1); // Returns null/default instead of throwing

// SafeGet with custom default value
var customDefault = services.SafeGet(10, new ManagedService { Name = "default", Port = 0 });

// IsNullOrEmpty - check if collection is null or empty
bool isNull = ((List<ManagedService>?)null).IsNullOrEmpty(); // Returns true
bool isEmpty = new List<ManagedService>().IsNullOrEmpty(); // Returns true
bool isNotEmpty = services.IsNullOrEmpty(); // Returns false

// Batch - split collection into batches of specified size
var batches = services.Batch(2);
foreach (var batch in batches)
{
    Console.WriteLine($"Batch contains {batch.Count} services");
}

// Partition - split collection into matching and not matching groups
var (evenPorts, oddPorts) = services.Partition(s => s.Port % 2 == 0);
Console.WriteLine($"Services with even ports: {evenPorts.Count}");
Console.WriteLine($"Services with odd ports: {oddPorts.Count}");

// RemoveWhere - remove items matching a predicate
services.RemoveWhere(s => s.Port > 5000); // Removes cache service

// AddRangeIfNotExists - add items only if they don't already exist
services.AddRangeIfNotExists(new[]
{
    new ManagedService { Name = "database", Port = 5432, Domain = "db.example.com" },
    new ManagedService { Name = "api", Port = 8080, Domain = "api.example.com" } // Won't add - already exists
});
```

## ValidationHelperTests

`ValidationHelperTests` provides unit tests for input validation utilities used throughout the application. It validates port numbers, domain names, service identifiers, and error result merging, ensuring that configuration inputs meet the required constraints before being processed by other components. The test class exercises both success and failure paths for common validation scenarios.

```csharp
// Example: Validating service configuration inputs
var validation = new ValidationHelperTests();

// Test port validation - zero is invalid
var portResult = validation.ValidatePort_PortZero_ReturnsInvalidResult();
Assert.False(portResult.IsValid);
Assert.Contains("Port must be greater than 0", portResult.ErrorMessages);

// Test domain validation - well-formed domain passes
var domainResult = validation.ValidateDomain_WellFormedDomain_ReturnsValidResult();
Assert.True(domainResult.IsValid);
Assert.Empty(domainResult.ErrorMessages);

// Test service name validation - names shorter than 3 characters fail
var nameResult = validation.ValidateServiceName_LessThanThreeChars_ReturnsError();
Assert.False(nameResult.IsValid);
Assert.Contains("Service name must be at least 3 characters", nameResult.ErrorMessages);

// Test combining multiple validation failures
var failure1 = validation.ValidatePort_PortZero_ReturnsInvalidResult();
var failure2 = validation.ValidateServiceName_LessThanThreeChars_ReturnsError();
var combined = ValidationHelper.Combine(failure1, failure2);
Assert.False(combined.IsValid);
Assert.Contains("Port must be greater than 0", combined.ErrorMessages);
Assert.Contains("Service name must be at least 3 characters", combined.ErrorMessages);

// Test health check result creation
var healthResult = validation.HealthCheckResult_CreateSuccess_SetsHealthyProperties();
Assert.True(healthResult.IsHealthy);
Assert.Equal(0, healthResult.ResponseTimeMs);

// Test slow response detection
var slowResult = validation.HealthCheckResult_IsSlowResponse_ReturnsTrueOnlyAboveThreshold();
Assert.True(slowResult.IsSlowResponse);

// Test systemd unit name formatting with spaces
var serviceName = "My Production Service";
var unitName = validation.ManagedService_GetSystemdUnitName_WithSpacesInName_FormatsCorrectly(serviceName);
Assert.Equal("my-production-service.service", unitName);
```

## TemplateEngineTests

`TemplateEngineTests` provides unit tests for the `TemplateEngine` class, which enables template-based string substitution with support for variables, null safety, and dictionary-based rendering. This test suite validates that the template engine correctly handles single and multiple variable substitutions, null and empty inputs, missing variables, and constructor-based initialization, ensuring robust text templating functionality for configuration generation and message formatting.

```csharp
// Example: Using TemplateEngine for configuration string templating
var engine = new TemplateEngine();

// Set variables for template substitution
engine.Set("service", "caddy-vps-toolkit");
engine.Set("version", "2.0.0");
engine.Set("port", 8080);

// Render a template with single variable substitution
var serviceConfig = engine.Render("Service: {{service}}");
Console.WriteLine(serviceConfig); // Outputs: Service: caddy-vps-toolkit

// Render a template with multiple variable substitutions
var connectionString = engine.Render("http://{{host}}:{{port}}/api/v{{version}}");
Console.WriteLine(connectionString); // Outputs: http://:8080/api/v2.0.0

// Set additional variables and render again
engine.Set("host", "localhost");
var fullConnectionString = engine.Render("http://{{host}}:{{port}}/api/v{{version}}");
Console.WriteLine(fullConnectionString); // Outputs: http://localhost:8080/api/v2.0.0

// Render with unknown variable - placeholder remains unchanged
var unknownVarResult = engine.Render("Path: {{unknownPath}}");
Console.WriteLine(unknownVarResult); // Outputs: Path: {{unknownPath}}

// Handle null template gracefully
var nullResult = engine.Render(null!);
Console.WriteLine(nullResult); // Outputs: (null)

// Handle empty template
var emptyResult = engine.Render(string.Empty);
Console.WriteLine(emptyResult); // Outputs: (empty string)

// Static method for quick rendering from dictionary
var templateVars = new Dictionary<string, object>
{
    ["app"] = "web-server",
    ["env"] = "production"
};

var staticRender = TemplateEngine.Render("{{app}} in {{env}} environment", templateVars);
Console.WriteLine(staticRender); // Outputs: web-server in production environment

// Constructor with initial variables
var preConfiguredEngine = new TemplateEngine(new Dictionary<string, object> { ["key"] = "value" });
var preRender = preConfiguredEngine.Render("{{key}}");
Console.WriteLine(preRender); // Outputs: value

// Handle null variable values - substitutes empty string
var nullValueEngine = new TemplateEngine();
nullValueEngine.Set("missing", null!);
var nullValueResult = nullValueEngine.Render("Value: {{missing}}");
Console.WriteLine(nullValueResult); // Outputs: Value: 
```

## StateMachineTests

`StateMachineTests` provides unit tests for the `StateMachine<TState, TTrigger>` class, a generic finite state machine implementation for managing state transitions in applications. This test suite validates state transition behavior, validation of valid/invalid transitions, callback invocation on state entry/exit, reset functionality, and available transition queries, ensuring robust state management for workflows and service lifecycle operations.

```csharp
// Example: Using StateMachine for service lifecycle management
var serviceMachine = new StateMachine<ServiceState, ServiceTrigger>(ServiceState.Stopped);

// Configure valid state transitions
serviceMachine.Configure(ServiceState.Stopped, ServiceTrigger.Start, ServiceState.Running);
serviceMachine.Configure(ServiceState.Running, ServiceTrigger.Pause, ServiceState.Paused);
serviceMachine.Configure(ServiceState.Running, ServiceTrigger.Stop, ServiceState.Stopped);
serviceMachine.Configure(ServiceState.Paused, ServiceTrigger.Resume, ServiceState.Running);
serviceMachine.Configure(ServiceState.Paused, ServiceTrigger.Stop, ServiceState.Stopped);

// Check if a transition is possible
bool canStart = serviceMachine.CanFire(ServiceTrigger.Start); // Returns true
bool canStop = serviceMachine.CanFire(ServiceTrigger.Stop); // Returns false (not configured from Stopped)

// Fire a valid transition
bool transitionSucceeded = serviceMachine.Fire(ServiceTrigger.Start); // Returns true, state changes to Running

// Get current state
ServiceState currentState = serviceMachine.GetCurrentState(); // Returns Running

// Register callbacks for state entry/exit
bool enterCalled = false;
bool exitCalled = false;

serviceMachine.OnEnter(ServiceState.Running, () => enterCalled = true);
serviceMachine.OnExit(ServiceState.Stopped, () => exitCalled = true);

// Fire transition to trigger callbacks
serviceMachine.Fire(ServiceTrigger.Start); // enterCalled becomes true

// Get available transitions from current state
var availableTransitions = serviceMachine.GetAvailableTransitions();
// Returns: [ServiceTrigger.Pause, ServiceTrigger.Stop]

// Reset to a specific state
serviceMachine.Reset(ServiceState.Stopped);

// Fire invalid transition (returns false, state unchanged)
bool invalidTransition = serviceMachine.Fire(ServiceTrigger.Resume); // Returns false, state remains Stopped
```

## ServiceRepositoryTests

`ServiceRepositoryTests` provides unit tests for the `ServiceRepository` class, which handles database operations for managed services including creation, retrieval, updating, and deletion. This test suite validates that all repository methods correctly handle database operations, return expected results, and maintain data integrity when working with service configurations.

The test class exercises both success and failure paths for common service repository operations including CRUD operations, count queries, and proper cleanup of test databases.




```csharp
// Example: Using ServiceRepository for service management operations
var serviceRepository = new ServiceRepository();

// Initialize the repository with an in-memory database for testing
// In production, this would use the configured SQLite database path
var connectionStringField = typeof(ServiceRepository).GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance);
connectionStringField?.SetValue(serviceRepository, "Data Source=services.db;Version=3;");

// Add a new service to the repository
var newService = new ManagedService
{
    Name = "my-web-app",
    Description = "Web application service",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/my-app",
    Port = 8080,
    Status = ServiceStatus.Stopped
};

string serviceId = await serviceRepository.AddAsync(newService);
Console.WriteLine($"Added service with ID: {serviceId}");

// Retrieve the service by ID
ManagedService retrievedService = await serviceRepository.GetByIdAsync(serviceId);
Console.WriteLine($"Retrieved service: {retrievedService.Name} on port {retrievedService.Port}");

// Update the service
retrievedService.Description = "Updated web application service";
retrievedService.Port = 9090;
bool updateSuccess = await serviceRepository.UpdateAsync(retrievedService);
Console.WriteLine($"Service updated successfully: {updateSuccess}");

// Get the updated service to verify changes
ManagedService updatedService = await serviceRepository.GetByIdAsync(serviceId);
Console.WriteLine($"Updated service description: {updatedService.Description}");

// Get the total count of services
int serviceCount = await serviceRepository.GetCountAsync();
Console.WriteLine($"Total services in repository: {serviceCount}");

// Delete the service when done
bool deleteSuccess = await serviceRepository.DeleteAsync(serviceId);
Console.WriteLine($"Service deleted successfully: {deleteSuccess}");

// Verify the service was deleted
ManagedService deletedService = await serviceRepository.GetByIdAsync(serviceId);
Console.WriteLine($"Service exists after deletion: {deletedService != null}"); // Should be false
```

## PaginationHelperTests

`PaginationHelperTests` contains unit tests for the `PaginationHelper` utility class, which provides methods for paginating, sorting, and filtering collections. This test suite validates that pagination returns correct slices for various page numbers and page sizes, handles edge cases like null collections and out-of-bounds pages, and ensures sorting and filtering operations work correctly with different property names and predicates.


The test class exercises both success and failure paths for common pagination scenarios including clamping invalid page/pageSize values, handling null inputs, and verifying correct behavior when requesting pages beyond the total count.

```csharp
// Example: Using PaginationHelper for paginating service configurations
var services = new List<ManagedService>
{
    new ManagedService { Name = "api", Port = 8080, Domain = "api.example.com" },
    new ManagedService { Name = "web", Port = 3000, Domain = "web.example.com" },
    new ManagedService { Name = "cache", Port = 6379, Domain = "cache.example.com" },
    new ManagedService { Name = "database", Port = 5432, Domain = "db.example.com" },
    new ManagedService { Name = "auth", Port = 5000, Domain = "auth.example.com" },
    new ManagedService { Name = "storage", Port = 9000, Domain = "storage.example.com" }
};

// Paginate services - get first page with 3 items per page
var firstPage = PaginationHelper.Paginate(services, page: 1, pageSize: 3);
Console.WriteLine($"Page {firstPage.Page} of {firstPage.TotalPages}:");
foreach (var service in firstPage.Items)
{
    Console.WriteLine($"  - {service.Name} on port {service.Port}");
}
// Output:
// Page 1 of 2:
//   - api on port 8080
//   - web on port 3000
//   - cache on port 6379

// Get second page
var secondPage = PaginationHelper.Paginate(services, page: 2, pageSize: 3);
Console.WriteLine($"\nPage {secondPage.Page} of {secondPage.TotalPages}:");
foreach (var service in secondPage.Items)
{
    Console.WriteLine($"  - {service.Name} on port {service.Port}");
}
// Output:
// Page 2 of 2:
//   - database on port 5432
//   - auth on port 5000
//   - storage on port 9000

// Sort services by port number in descending order
var sortedByPort = PaginationHelper.SortBy(services, nameof(ManagedService.Port), ascending: false);
Console.WriteLine("\nServices sorted by port (descending):");
foreach (var service in sortedByPort)
{
    Console.WriteLine($"  - {service.Name} on port {service.Port}");
}
// Output:
// Services sorted by port (descending):
//   - storage on port 9000
//   - database on port 5432
//   - api on port 8080
//   - auth on port 5000
//   - web on port 3000
//   - cache on port 6379

// Filter services by domain containing "example"
var filteredServices = PaginationHelper.Filter(services, s => s.Domain.Contains("example"));
Console.WriteLine($"\nFiltered services ({filteredServices.Count}):");
foreach (var service in filteredServices)
{
    Console.WriteLine($"  - {service.Name}");
}
// Output:
// Filtered services (6):
//   - api
//   - web
//   - cache
//   - database
//   - auth
//   - storage

// Using QueryBuilder for fluent pagination and filtering
var queryResult = new QueryBuilder<ManagedService>(services)
    .Where(s => s.Port > 5000)  // Filter services with port > 5000
    .Page(1)
    .PageSize(2)
    .Execute();

Console.WriteLine($"\nQuery result - Page {queryResult.Page} of {queryResult.TotalPages}:");
foreach (var service in queryResult.Items)
{
    Console.WriteLine($"  - {service.Name} on port {service.Port}");
}
// Output:
// Query result - Page 1 of 2:
//   - api on port 8080
//   - database on port 5432
```

## SystemdUnitConfigTests

`SystemdUnitConfigTests` validates the `SystemdUnitConfig` class, which generates and validates systemd unit files for managed services. This test suite verifies that unit file generation produces correct `[Unit]`, `[Service]`, and `[Install]` sections with proper directives, handles environment variables and files correctly, validates required fields, and maintains proper section ordering in the generated content.

The tests cover validation scenarios (missing required fields, negative values), directive emission (environment variables, working directory, restart policies), and structural requirements (section ordering, lowercase directives).

```csharp
// Example: Creating and validating a systemd unit configuration
var unitConfig = new SystemdUnitConfig
{
    ServiceId = "my-service-123",
    UnitName = "my-web-app.service",
    Description = "My web application service",
    ExecStart = "/usr/bin/dotnet /opt/my-app/MyApp.dll",
    User = "www-data",
    Group = "www-data",
    WorkingDirectory = "/opt/my-app",
    RestartPolicy = RestartPolicy.OnFailure,
    Environment = new Dictionary<string, string>
    {
        {"ASPNETCORE_ENVIRONMENT", "Production"},
        {"PORT", "8080"}
    },
    Wants = new List<string> { "postgresql.service", "redis.service" }
};

// Validate the configuration - throws ValidationException if invalid
unitConfig.Validate();

// Generate the systemd unit file content
string systemdContent = unitConfig.GenerateSystemdContent();

// The generated content contains all three required sections in order:
// [Unit] - Contains Description, After, and Wants directives
// [Service] - Contains ExecStart, User, Group, WorkingDirectory, Restart, Environment, etc.
// [Install] - Contains WantedBy directive

Console.WriteLine(systemdContent);
```

## ServiceManagementServiceTests

`ServiceManagementServiceTests` provides unit tests for the `ServiceManagementService` class, which manages the lifecycle of systemd services including creation, retrieval, updating, and deletion. This test suite validates that service operations correctly handle null inputs, duplicate service names, running service constraints, and valid status transitions, ensuring robust error handling and correct behavior for service management workflows.

```csharp
// Example: Managing services programmatically via ServiceManagementService
var serviceRepositoryMock = Substitute.For<IServiceRepository>();
var serviceManagementService = new ServiceManagementService(serviceRepositoryMock);

// Test creating a new service
var newService = new ManagedService
{
    Name = "my-web-app",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/my-app",
    Port = 8080,
    Description = "My web application service"
};

// Create a service - returns service ID
string serviceId = await serviceManagementService.CreateServiceAsync(newService);
Console.WriteLine($"Created service with ID: {serviceId}");

// Test retrieving a service by ID
ManagedService retrievedService = await serviceManagementService.GetServiceAsync(serviceId);
Console.WriteLine($"Retrieved service: {retrievedService.Name} on port {retrievedService.Port}");

// Test updating service status
bool statusUpdated = await serviceManagementService.UpdateServiceStatusAsync(
    serviceId, 
    ServiceStatus.Running
);
Console.WriteLine($"Status updated: {statusUpdated}");

// Test deleting a service (will throw if service is running)
try
{
    await serviceManagementService.DeleteServiceAsync(serviceId);
}
catch (ServiceConfigurationException ex)
{
    Console.WriteLine($"Cannot delete running service: {ex.Message}");
}

// Test error scenarios
try
{
    // This will throw ArgumentNullException
    await serviceManagementService.CreateServiceAsync(null!);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

try
{
    // This will throw ServiceConfigurationException for duplicate name
    await serviceManagementService.CreateServiceAsync(new ManagedService
    {
        Name = "my-web-app", // Same name as existing service
        ExecutablePath = "/usr/bin/node",
        WorkingDirectory = "/opt/app",
        Port = 3000
    });
}
catch (ServiceConfigurationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## ResultGenericTests

`ResultGenericTests` validates the generic `Result<T>` class, which provides a type-safe wrapper for operation results with support for both success and failure states. The test suite verifies that successful results properly set the `IsSuccess` flag and return the provided data (or default values when no data is provided), while failure results correctly set the `IsSuccess` flag to false and populate the error message and optional error code. This pattern is commonly used throughout the application to handle operations that may fail without throwing exceptions.

```csharp
// Example: Using Result<T> for a successful operation with data
var userResult = Result<User>.Success(new User { Id = 1, Name = "Alice" });

if (userResult.IsSuccess)
{
    User user = userResult.Data;
    Console.WriteLine($"Retrieved user: {user.Name}");
}

// Example: Using Result<T> for a successful operation with default data
var countResult = Result<int>.Success();
Console.WriteLine($"Default count: {countResult.Data}"); // Outputs: Default count: 0

// Example: Using Result<T> for a failed operation with error message
var errorResult = Result<string>.Failure("User not found");

if (!errorResult.IsSuccess)
{
    Console.WriteLine($"Error: {errorResult.ErrorMessage}"); // Outputs: Error: User not found
}

// Example: Using Result<T> for a failed operation with error code
var apiError = Result<ApiResponse>.Failure("Invalid request", "BAD_REQUEST");
Console.WriteLine($"Error code: {apiError.ErrorCode}"); // Outputs: Error code: BAD_REQUEST
```

Integration tests require a local SQLite database, which is created automatically on first run.

## ArgumentValidatorTests

`ArgumentValidatorTests` provides unit tests for the `ArgumentValidator` class, which validates CLI command arguments against their expected descriptors. The test suite verifies error handling for null descriptors, missing required positional arguments, unknown flags, and validates that error messages are properly joined. These tests ensure the argument validation logic correctly identifies and reports invalid command inputs before they reach the application logic.

```csharp
// Example: Validating CLI command arguments
var argumentParser = new ArgumentParser(new[] { "service", "update", "my-service", "--verbose" });
var commandDescriptor = new CommandDescriptor("service", "Service management commands")
{
    Name = "service",
    RequiredArguments = new List<string> { "action", "serviceName" },
    OptionalFlags = new List<string> { "verbose", "force" }
};

// Validate arguments against descriptor
var validator = new ArgumentValidator();
var validationResult = validator.Validate(argumentParser, commandDescriptor);

if (!validationResult.IsValid)
{
    Console.WriteLine("Validation failed:");
    Console.WriteLine(validationResult.GetErrorMessage());
    return;
}

// Arguments are valid - proceed with command execution
Console.WriteLine("Arguments are valid!");
```

## HealthMonitoringServiceTests

`HealthMonitoringServiceTests` provides unit tests for the `HealthMonitoringService` class, which validates health monitoring functionality including service health checks, health status retrieval, health history tracking, and database record cleanup. This test suite verifies that the service correctly handles null inputs, invalid parameters, and valid health monitoring operations, ensuring robust error handling and correct behavior for service health monitoring workflows.

```csharp
// Example: Using HealthMonitoringService for service health monitoring
var healthRepositoryMock = Substitute.For<IHealthCheckRepository>();
var serviceRepositoryMock = Substitute.For<IServiceRepository>();
var serviceManager = new ServiceManagementService(serviceRepositoryMock);
var healthMonitoringService = new HealthMonitoringService(healthRepositoryMock, serviceManager);

// Test service health check - throws HealthCheckException when service has no health check
var serviceWithoutHealthCheck = new ManagedService { Id = "svc1", Name = "Test", ExecutablePath = "/bin", WorkingDirectory = "/", Port = 80, HealthCheck = null };
serviceRepositoryMock.GetByIdAsync("svc1").Returns(serviceWithoutHealthCheck);

Func<Task> healthCheckTest = async () => await healthMonitoringService.CheckServiceHealthAsync("svc1");
await healthCheckTest.Should().ThrowAsync<HealthCheckException>();

// Test retrieving latest health status
var latestHealthStatus = new HealthCheckResult { ServiceId = "svc1", IsHealthy = true };
healthRepositoryMock.GetLatestAsync("svc1").Returns(latestHealthStatus);

var result = await healthMonitoringService.GetLatestHealthStatusAsync("svc1");
result.Should().BeEquivalentTo(latestHealthStatus);

// Test retrieving health history for the last 24 hours
var healthHistory = new List<HealthCheckResult> { new HealthCheckResult(), new HealthCheckResult() };
healthRepositoryMock.GetRecentAsync("svc1", 24).Returns(healthHistory);

var history = await healthMonitoringService.GetHealthHistoryAsync("svc1", 24);
history.Should().HaveCount(2);

// Test cleanup of old health records
healthRepositoryMock.DeleteOlderThanAsync(Arg.Any<DateTime>()).Returns(true);

var cleanupResult = await healthMonitoringService.CleanupOldRecordsAsync(30);
cleanupResult.Should().BeTrue();
```

## LogAggregationServiceTests

`LogAggregationServiceTests` validates the `LogAggregationService` class, which aggregates and filters log files from a specified directory. This test suite verifies that the service correctly handles empty directories, parses standard log formats, filters by minimum log level, respects line limits, returns logs in chronological order, and filters by date ranges. It also validates that the service can identify and return available log sources.

```csharp
// Example: Using LogAggregationService to view and filter service logs
var logAggregationService = new LogAggregationService("/var/log/my-service");

// Get all logs from the directory
var allLogs = await logAggregationService.GetLogsAsync();
Console.WriteLine($"Found {allLogs.Count} log entries");

// Filter logs by minimum level (Info, Warning, Error, etc.)
var warningAndAbove = await logAggregationService.GetLogsAsync(new LogQueryOptions
{
    MinLevel = "Warning",
    Lines = 100
});

// Filter logs by date range (only show entries from the last 24 hours)
var since = DateTime.UtcNow.AddHours(-24);
var recentLogs = await logAggregationService.GetLogsAsync(new LogQueryOptions
{
    Since = since,
    Lines = 50
});

// Get available log sources/files
var logSources = logAggregationService.GetLogSources();
foreach (var source in logSources)
{
    Console.WriteLine($"Log source: {source.Name} ({source.Size} bytes)");
}

// Example: Testing various log aggregation scenarios
var tempDir = Path.Combine(Path.GetTempPath(), "log-agg-test");
Directory.CreateDirectory(tempDir);

// Test empty directory handling
var emptyService = new LogAggregationService(tempDir);
var emptyLogs = await emptyService.GetLogsAsync();
emptyLogs.Should().BeEmpty();

// Test standard log format parsing
var logContent = "[2025-01-15T10:00:00.000+00:00] [Info] Application started\n" +
                 "[2025-01-15T10:01:00.000+00:00] [Error] Database connection failed";
File.WriteAllText(Path.Combine(tempDir, "app.log"), logContent);

var parsedLogs = await emptyService.GetLogsAsync();
parsedLogs.Should().HaveCount(2);
parsedLogs[0].Level.Should().Be("Info");
parsedLogs[1].Level.Should().Be("Error");
```

## EventBusTests

`EventBusTests` validates the `EventBus` class, a lightweight publish-subscribe event bus implementation that enables decoupled communication between components. The tests cover core functionality including event publishing with subscribers, null event handling, multiple subscriber scenarios, handler management (subscribe/unsubscribe), subscriber counting, event type isolation, and concurrent publishing.

## HealthCheckConfigTests

`HealthCheckConfigTests` validates the `HealthCheckConfig` class, which provides configuration validation and URL generation for health check monitoring. The test suite verifies validation logic for health check intervals, timeouts, endpoint requirements, and URL construction for different health check types (HTTP, TCP). It ensures that health monitoring configurations meet minimum requirements and properly construct health check URLs based on the configured type.

```csharp
// Example: Validating and using health check configuration
var config = new HealthCheckConfig
{
    Type = HealthCheckType.Http,
    Endpoint = "http://localhost:8080/health",
    Interval = TimeSpan.FromSeconds(30),
    Timeout = TimeSpan.FromSeconds(5),
    Retries = 3
};

// Validate configuration - throws ValidationException if invalid
var validator = new HealthCheckConfigTests();
validator.Validate_WithValidData_ShouldNotThrow(config);

// Generate health check URL
string healthCheckUrl = config.GetHealthCheckUrl();
Console.WriteLine($"Health check URL: {healthCheckUrl}"); // Outputs: Health check URL: http://localhost:8080/health

// Example with TCP type (returns null for URL)
var tcpConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Tcp,
    Endpoint = "localhost:5432",
    Interval = TimeSpan.FromSeconds(10),
    Timeout = TimeSpan.FromSeconds(2)
};

string tcpUrl = tcpConfig.GetHealthCheckUrl();
Console.WriteLine(tcpUrl); // Outputs: (null)

// Example: Configuration validation scenarios
try
{
    // This will throw ValidationException - interval less than 5 seconds
    var invalidIntervalConfig = new HealthCheckConfig
    {
        Type = HealthCheckType.Http,
        Endpoint = "http://localhost:8080/health",
        Interval = TimeSpan.FromSeconds(3), // Too short
        Timeout = TimeSpan.FromSeconds(5)
    };
    validator.Validate_WithIntervalLessThan5_ShouldThrowValidationException(invalidIntervalConfig);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

try
{
    // This will throw ValidationException - timeout greater than interval
    var invalidTimeoutConfig = new HealthCheckConfig
    {
        Type = HealthCheckType.Http,
        Endpoint = "http://localhost:8080/health",
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(15) // Greater than interval
    };
    validator.Validate_WithTimeoutGreaterThanInterval_ShouldThrowValidationException(invalidTimeoutConfig);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

try
{
    // This will throw ValidationException - missing endpoint for HTTP type
    var missingEndpointConfig = new HealthCheckConfig
    {
        Type = HealthCheckType.Http,
        Interval = TimeSpan.FromSeconds(30),
        Timeout = TimeSpan.FromSeconds(5)
        // Missing Endpoint property
    };
    validator.Validate_WithMissingEndpointForHttp_ShouldThrowValidationException(missingEndpointConfig);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## HealthCheckConfigEdgeCaseTests

`HealthCheckConfigEdgeCaseTests` validates edge-case behavior for the `HealthCheckConfig` class, focusing on boundary conditions and validation edge cases. This test suite ensures that health check configurations properly handle minimum/maximum values, zero values, and type-specific requirements. It validates that the configuration validation logic correctly identifies and reports invalid configurations that fall outside expected boundaries.

## BackupServiceTests

`BackupServiceTests` provides unit tests for the `BackupService` class, which handles the creation and restoration of configuration backups for the caddy-vps-toolkit application. This test suite verifies that backup operations correctly handle file system interactions, JSON serialization, error conditions, and restore operations, ensuring data integrity and recovery scenarios work as expected.

```csharp
// Example: Creating and restoring backups programmatically
var serviceRepo = Substitute.For<IServiceRepository>();
var configRepo = Substitute.For<IConfigurationRepository>();
var backupService = new BackupService(serviceRepo, configRepo);

// Create a backup with a specific output path and description
var backupPath = "/path/to/backups/my-config.backup.json";
var createdBackupPath = await backupService.CreateBackupAsync(
    outputPath: backupPath,
    description: "Weekly configuration backup"
);

Console.WriteLine($"Backup created at: {createdBackupPath}");

// Create a backup with auto-generated timestamped filename in the config directory
var autoBackupPath = await backupService.CreateBackupAsync(
    description: "Automatic backup"
);
Console.WriteLine($"Auto-generated backup: {autoBackupPath}");

// List all available backups in the config directory
var allBackups = await backupService.ListBackupsAsync();
foreach (var backup in allBackups)
{
    Console.WriteLine($"Found backup: {Path.GetFileName(backup)}");
}

// Restore a backup - this will recreate services, configuration, and Caddyfile
var manifest = await backupService.RestoreBackupAsync(backupPath);
Console.WriteLine($"Restored {manifest.ServiceCount} services from backup");
Console.WriteLine($"Backup created at: {manifest.CreatedAt}");
Console.WriteLine($"Description: {manifest.Description}");
```

## CaddyVpsException

`CaddyVpsException` is the base exception class for the caddy-vps-toolkit application. It provides structured error handling with an `ErrorCode` property for categorizing failures and a `Details` property for additional context. This exception type serves as the foundation for all application-specific exceptions in the toolkit.

### Members

- `ErrorCode` (string): A string identifier for categorizing the exception type
- `Details` (object): An optional object containing additional error context

### Usage Example

```csharp
try
{
    // Example: Attempting to start a non-existent service
    var serviceManagement = new ServiceManagementService(serviceRepository);
    await serviceManagement.StartServiceAsync("non-existent-service");
}
catch (CaddyVpsException ex)
{
    // Handle application-specific exceptions
    Console.WriteLine($"Error Code: {ex.ErrorCode}");
    Console.WriteLine($"Message: {ex.Message}");
    
    if (ex.Details != null)
    {
        Console.WriteLine($"Details: {ex.Details}");
    }
    
    // Re-throw or handle specific error codes
    if (ex.ErrorCode == "SERVICE_NOT_FOUND")
    {
        Console.WriteLine("Service not found - please check the service name");
    }
}
```

## CaddyConfigPipelineIntegrationTests

`CaddyConfigPipelineIntegrationTests` provides end-to-end integration tests for the complete Caddy configuration generation pipeline. This test suite validates the full workflow from service configuration inputs through route generation, Caddyfile output, and validation, ensuring all components work together correctly in realistic scenarios.

The tests cover:
- Route combination handling (multiple active routes, inactive route exclusion)
- Null and empty input handling with appropriate fallback behavior
- Global configuration presence in generated output
- Route block generation with various features (path matchers, custom headers, rate limiting, authentication, TLS DNS providers, path stripping)
- Caddyfile validation for both well-formed and malformed configurations
- JSON structure generation for programmatic consumption
- End-to-end pipeline validation with balanced braces

```csharp
// Example: Testing the complete Caddy configuration pipeline
var serviceRepositoryMock = Substitute.For<IServiceRepository>();
var serviceManager = new ServiceManagementService(serviceRepositoryMock);
var caddyService = new CaddyConfigurationService(serviceManager);

// Create a service configuration
var service = new ManagedService
{
    Id = Guid.NewGuid().ToString(),
    Name = "api-backend",
    Description = "Backend API Service",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/api",
    HostBinding = "127.0.0.1",
    Port = 5000
};

// Generate route configuration for the service
var route = caddyService.GenerateRouteForService(service, "api.example.com", "cloudflare");

// Generate Caddy configuration with multiple routes
var globalConfig = new CaddyConfig { AdminEmail = "ops@example.com" };
var routes = new List<CaddyRoute>
{
    route,
    new CaddyRoute
    {
        Domain = "www.example.com",
        UpstreamUrl = "http://localhost:3000",
        IsActive = true,
        CustomHeaders = new Dictionary<string, string>
        {
            { "X-Real-IP", "{remote_host}" },
            { "X-Request-ID", "{uuid}" }
        },
        RateLimitRule = "100r/s",
        BasicAuthEnabled = true,
        BasicAuthUsername = "admin",
        BasicAuthPasswordHash = "$2a$14$hashed",
        StripPath = true
    }
};

// Generate the complete Caddyfile
var caddyfile = await caddyService.GenerateCaddyfileAsync(globalConfig, routes);

// Validate the generated configuration
bool isValid = await caddyService.ValidateCaddyfileAsync(caddyfile);

// Generate JSON structure for programmatic use
string jsonConfig = caddyService.GenerateCaddyJsonAsync(globalConfig, routes);

// The generated Caddyfile should contain all configured routes
caddyfile.Should().Contain("api.example.com");
caddyfile.Should().Contain("www.example.com");
caddyfile.Should().Contain("http://localhost:5000");
caddyfile.Should().Contain("http://localhost:3000");
```

## CaddyConfigTests

`CaddyConfigTests` provides unit tests for the `CaddyConfig` class, which validates Caddy server configuration settings and generates Caddyfile global directives. This test suite verifies validation logic for admin ports, HTTP/HTTPS ports, timeouts, email addresses, metrics configuration, and auto HTTPS settings, ensuring that Caddy configurations meet required constraints before being applied.

```csharp
// Example: Creating and validating a Caddy configuration
var config = new CaddyConfig
{
    AdminPort = 2019,
    HttpPort = 80,
    HttpsPort = 443,
    AdminEmail = "admin@example.com",
    CertificateEmail = "cert@example.com",
    EnableMetrics = true,
    AutoHttpsDisabled = false,
    ReadTimeout = 30,
    WriteTimeout = 30,
    IdleTimeout = 120
};

// Validate configuration - throws ValidationException if invalid
config.Validate();

// Set default values for missing emails
config.SetDefaultValues();
Console.WriteLine($"Admin Email: {config.AdminEmail}"); // Outputs: Admin Email: admin@example.com
Console.WriteLine($"Certificate Email: {config.CertificateEmail}"); // Outputs: Certificate Email: cert@example.com

// Generate Caddyfile global directives
string caddyfileGlobals = config.GenerateCaddyfileGlobals();
Console.WriteLine(caddyfileGlobals);

// Example: Configuration validation scenarios
try
{
    // This will throw ValidationException - admin port out of range
    var invalidPortConfig = new CaddyConfig
    {
        AdminPort = 70000, // Invalid port (> 65535)
        HttpPort = 80,
        HttpsPort = 443
    };
    invalidPortConfig.Validate();
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}"); // Outputs: Validation failed: Admin port must be between 1 and 65535
}

try
{
    // This will throw ValidationException - negative timeout
    var negativeTimeoutConfig = new CaddyConfig
    {
        AdminPort = 2019,
        HttpPort = 80,
        HttpsPort = 443,
        ReadTimeout = -5 // Invalid negative timeout
    };
    negativeTimeoutConfig.Validate();
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}"); // Outputs: Validation failed: Timeouts cannot be negative
}

// Example: Generate Caddyfile globals with metrics enabled
var metricsConfig = new CaddyConfig
{
    AdminPort = 2019,
    HttpPort = 80,
    HttpsPort = 443,
    EnableMetrics = true
};

string globalsWithMetrics = metricsConfig.GenerateCaddyfileGlobals();
Console.WriteLine(globalsWithMetrics.Contains("metrics")); // Outputs: True

// Example: Generate Caddyfile globals with auto HTTPS disabled
var noAutoHttpsConfig = new CaddyConfig
{
    AdminPort = 2019,
    HttpPort = 80,
    HttpsPort = 443,
    AutoHttpsDisabled = true
};

string globalsWithoutAutoHttps = noAutoHttpsConfig.GenerateCaddyfileGlobals();
Console.WriteLine(globalsWithoutAutoHttps.Contains("auto_https off")); // Outputs: True
```

```csharp
// Example: Testing health check configuration edge cases
var edgeCaseTests = new HealthCheckConfigEdgeCaseTests();

// Test minimum interval boundary - 4 seconds is below minimum of 5
var belowMinimumConfig = new HealthCheckConfig
{
    IntervalSeconds = 4,
    TimeoutSeconds = 2
};

try
{
    edgeCaseTests.Validate_IntervalBelowMinimum_ThrowsValidationException();
    Console.WriteLine("Minimum interval validation works correctly");
}
catch (Exception ex)
{
    Console.WriteLine($"Test failed: {ex.Message}");
}

// Test exact minimum interval - 5 seconds is the minimum allowed
var minimumConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Tcp,
    IntervalSeconds = 5,
    TimeoutSeconds = 1
};

edgeCaseTests.Validate_IntervalExactlyMinimum_DoesNotThrow();
Console.WriteLine("Exact minimum interval validation passed");
```

## CaddyRouteEdgeCaseTests

`CaddyRouteEdgeCaseTests` validates edge-case behavior for the `CaddyRoute` class, focusing on validation boundaries, path matching, and configuration requirements. This test suite ensures that Caddy route configurations properly handle null values, empty strings, invalid URLs, and timeout constraints. It validates that the route validation logic correctly identifies and reports invalid configurations before they reach the Caddy configuration generator.

```csharp
// Example: Validating Caddy route configurations for edge cases
var routeTests = new CaddyRouteEdgeCaseTests();

// Test null domain validation - should throw ValidationException
var nullDomainRoute = new CaddyRoute
{
    Domain = null!,
    UpstreamUrl = "http://localhost:5000"
};

try
{
    routeTests.Validate_NullDomain_ThrowsValidationException();
    Console.WriteLine("Null domain validation works correctly");
}
catch (Exception ex)
{
    Console.WriteLine($"Test failed: {ex.Message}");
}

// Test empty domain validation - should throw ValidationException
var emptyDomainRoute = new CaddyRoute
{
    Domain = "",
    UpstreamUrl = "http://localhost:5000"
};

routeTests.Validate_EmptyDomain_ThrowsValidationException();
Console.WriteLine("Empty domain validation passed");

// Test invalid upstream URL validation - should throw ValidationException
var invalidUrlRoute = new CaddyRoute
{
    Domain = "example.com",
    UpstreamUrl = "not-a-url"
};

routeTests.Validate_InvalidUpstreamUrl_ThrowsValidationException();
Console.WriteLine("Invalid URL validation works correctly");

// Test zero timeout validation - should throw ValidationException
var zeroTimeoutRoute = new CaddyRoute
{
    Domain = "example.com",
    UpstreamUrl = "http://localhost:5000",
    TimeoutSeconds = 0
};

routeTests.Validate_ZeroTimeout_ThrowsValidationException();
Console.WriteLine("Zero timeout validation works correctly");

// Test negative timeout validation - should throw ValidationException
var negativeTimeoutRoute = new CaddyRoute
{
    Domain = "example.com",
    UpstreamUrl = "http://localhost:5000",
    TimeoutSeconds = -1
};

routeTests.Validate_NegativeTimeout_ThrowsValidationException();
Console.WriteLine("Negative timeout validation works correctly");

// Test basic auth enabled without username validation - should throw ValidationException
var basicAuthNoUserRoute = new CaddyRoute
{
    Domain = "example.com",
    UpstreamUrl = "http://localhost:5000",
    BasicAuthEnabled = true,
    BasicAuthUsername = null!
};

routeTests.Validate_BasicAuthEnabledWithoutUsername_ThrowsValidationException();
Console.WriteLine("Basic auth username validation works correctly");

// Test valid route - should not throw
var validRoute = new CaddyRoute
{
    Domain = "example.com",
    UpstreamUrl = "http://localhost:5000",
    TimeoutSeconds = 30
};

routeTests.Validate_ValidRoute_DoesNotThrow();
Console.WriteLine("Valid route validation passed");

// Test path matcher generation with null path - should return empty string
var nullPathRoute = new CaddyRoute { Path = null! };
string nullPathMatcher = nullPathRoute.GetCaddyPathMatcher();
Console.WriteLine($"Null path matcher: '{nullPathMatcher}'"); // Outputs: ''

// Test path matcher generation with slash path - should return empty string
var slashPathRoute = new CaddyRoute { Path = "/" };
string slashPathMatcher = slashPathRoute.GetCaddyPathMatcher();
Console.WriteLine($"Slash path matcher: '{slashPathMatcher}'"); // Outputs: ''

// Test path matcher generation with custom path - should return the path
var customPathRoute = new CaddyRoute { Path = "/api/v1" };
string customPathMatcher = customPathRoute.GetCaddyPathMatcher();
Console.WriteLine($"Custom path matcher: '{customPathMatcher}'"); // Outputs: '/api/v1'

// Test route path generation with custom path - should concatenate domain and path
var routeWithPath = new CaddyRoute { Domain = "example.com", Path = "/api" };
string routePath = routeWithPath.GenerateRoutePath();
Console.WriteLine($"Route path with custom path: '{routePath}'"); // Outputs: 'example.com/api'

// Test route path generation with slash path - should return domain only
var routeWithSlash = new CaddyRoute { Domain = "example.com", Path = "/" };
string slashRoutePath = routeWithSlash.GenerateRoutePath();
Console.WriteLine($"Route path with slash: '{slashRoutePath}'"); // Outputs: 'example.com'
```

```csharp
// Example: Testing health check configuration edge cases
var edgeCaseTests = new HealthCheckConfigEdgeCaseTests();

// Test minimum interval boundary - 4 seconds is below minimum of 5
var belowMinimumConfig = new HealthCheckConfig
{
    IntervalSeconds = 4,
    TimeoutSeconds = 2
};

try
{
    edgeCaseTests.Validate_IntervalBelowMinimum_ThrowsValidationException();
    Console.WriteLine("Minimum interval validation works correctly");
}
catch (Exception ex)
{
    Console.WriteLine($"Test failed: {ex.Message}");
}

// Test exact minimum interval - 5 seconds is the minimum allowed
var minimumConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Tcp,
    IntervalSeconds = 5,
    TimeoutSeconds = 1
};

edgeCaseTests.Validate_IntervalExactlyMinimum_DoesNotThrow();
Console.WriteLine("Exact minimum interval validation passed");

// Test timeout greater than interval - should throw validation exception
var invalidTimeoutConfig = new HealthCheckConfig
{
    IntervalSeconds = 10,
    TimeoutSeconds = 15
};

edgeCaseTests.Validate_TimeoutGreaterThanInterval_ThrowsValidationException();
Console.WriteLine("Timeout greater than interval validation works correctly");

// Test timeout equals interval - should be valid
var equalTimeoutConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Tcp,
    IntervalSeconds = 10,
    TimeoutSeconds = 10
};

edgeCaseTests.Validate_TimeoutEqualsInterval_DoesNotThrow();
Console.WriteLine("Equal timeout and interval validation passed");

// Test zero timeout - should throw validation exception
var zeroTimeoutConfig = new HealthCheckConfig
{
    IntervalSeconds = 10,
    TimeoutSeconds = 0
};

edgeCaseTests.Validate_ZeroTimeout_ThrowsValidationException();
Console.WriteLine("Zero timeout validation works correctly");

// Test zero unhealthy threshold - should throw validation exception
var zeroUnhealthyConfig = new HealthCheckConfig
{
    IntervalSeconds = 10,
    TimeoutSeconds = 5,
    UnhealthyThreshold = 0
};

edgeCaseTests.Validate_ZeroUnhealthyThreshold_ThrowsValidationException();
Console.WriteLine("Zero unhealthy threshold validation works correctly");

// Test zero healthy threshold - should throw validation exception
var zeroHealthyConfig = new HealthCheckConfig
{
    IntervalSeconds = 10,
    TimeoutSeconds = 5,
    HealthyThreshold = 0
};

edgeCaseTests.Validate_ZeroHealthyThreshold_ThrowsValidationException();
Console.WriteLine("Zero healthy threshold validation works correctly");

// Test HTTP type without endpoint - should throw validation exception
var httpWithoutEndpointConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Http,
    IntervalSeconds = 10,
    TimeoutSeconds = 5,
    Endpoint = null
};

edgeCaseTests.Validate_HttpTypeWithoutEndpoint_ThrowsValidationException();
Console.WriteLine("HTTP type without endpoint validation works correctly");

// Test URL generation for HTTP type
var httpConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Http,
    Endpoint = "/health"
};

string httpUrl = httpConfig.GetHealthCheckUrl("localhost", 5000);
Console.WriteLine($"HTTP health check URL: {httpUrl}");
// Outputs: HTTP health check URL: http://localhost:5000/health

// Test URL generation for non-HTTP type (returns null)
var tcpConfig = new HealthCheckConfig
{
    Type = HealthCheckType.Tcp
};

string tcpUrl = tcpConfig.GetHealthCheckUrl("localhost", 5000);
Console.WriteLine($"TCP health check URL: {tcpUrl}");
// Outputs: TCP health check URL: (null)
```

## RetryPolicyTests

`RetryPolicyTests` provides unit tests for retry policy implementations that handle transient failures in service operations. This test suite validates that retry mechanisms correctly handle immediate success, transient failures followed by success, maximum retry limits, null operation validation, and void overload scenarios, ensuring robust error handling for service operations that may temporarily fail.

```csharp
// Example: Using retry policies for resilient service operations
var retryPolicy = new RetryPolicy(maxRetries: 3, delay: TimeSpan.FromSeconds(1));

// Execute an operation that might fail transiently
var result = await retryPolicy.ExecuteAsync(async () => 
{
    // Your service operation that might throw transient exceptions
    var response = await httpClient.GetAsync("https://api.example.com/data");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
});

Console.WriteLine($"Operation succeeded after retries: {result}");

// Example with linear backoff retry policy
var linearBackoffPolicy = new LinearBackoffRetryPolicy(
    maxRetries: 5,
    initialDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromSeconds(30)
);

// Use the retry policy with exponential backoff
var serviceResult = await linearBackoffPolicy.ExecuteAsync(async () => 
{
    var serviceResponse = await serviceClient.GetServiceStatusAsync("database-service");
    if (!serviceResponse.IsHealthy)
    {
        throw new ServiceUnavailableException("Service temporarily unavailable");
    }
    return serviceResponse;
});

Console.WriteLine($"Service status: {serviceResult.Status}");

// Example with no retry policy (fail immediately)
var noRetryPolicy = new NoRetryPolicy();

try
{
    await noRetryPolicy.ExecuteAsync(async () => 
    {
        // This operation will fail immediately without retries
        throw new InvalidOperationException("Service configuration error");
    });
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Operation failed immediately: {ex.Message}");
}
```

## ManagedServiceTests

`ManagedServiceTests` provides unit tests for the `ManagedService` class, which represents a managed systemd service configuration. This test suite validates service validation logic, status management, and systemd unit name generation, ensuring that service configurations meet required constraints before being processed by other components.

```csharp
// Example: Creating and validating a managed service
var service = new ManagedService
{
    Name = "my-web-app",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/my-app",
    Port = 8080
};

// Validate the service configuration - throws ValidationException if invalid
service.Validate();

// Update service status
service.UpdateStatus(ServiceStatus.Running);
Console.WriteLine($"Service status: {service.Status}"); // Outputs: Service status: Running

// Get systemd unit name - uses explicit name if provided, otherwise generates from service name
var explicitUnitName = service.GetSystemdUnitName();
Console.WriteLine(explicitUnitName); // Outputs: custom.service (if SystemdUnitName was set)

// Generate systemd unit name from service name when not explicitly set
var generatedUnitName = new ManagedService { Name = "My Production App" }.GetSystemdUnitName();
Console.WriteLine(generatedUnitName); // Outputs: vps-my-production-app.service

// Test validation scenarios
try
{
    // This will throw ValidationException - missing required Name property
    var invalidService = new ManagedService
    {
        ExecutablePath = "/usr/bin/dotnet",
        WorkingDirectory = "/opt/my-app",
        Port = 8080
    };
    invalidService.Validate();
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}"); // Outputs: Validation failed: name is required
}

try
{
    // This will throw ValidationException - invalid port number
    var invalidPortService = new ManagedService
    {
        Name = "my-service",
        ExecutablePath = "/usr/bin/dotnet",
        WorkingDirectory = "/opt/my-app",
        Port = 0 // Invalid port
    };
    invalidPortService.Validate();
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}"); // Outputs: Validation failed: Port must be between 1 and 65535
}

// Verify status transitions and timestamps
var testService = new ManagedService();
var originalTime = testService.UpdatedAt;
System.Threading.Thread.Sleep(10);
testService.UpdateStatus(ServiceStatus.Stopped);
Console.WriteLine($"Status changed: {testService.Status}"); // Outputs: Status changed: Stopped
Console.WriteLine($"Updated at changed: {testService.UpdatedAt > originalTime}"); // Outputs: true
```

## ArgumentParserEdgeCaseTests

`ArgumentParserEdgeCaseTests` validates the edge case behavior of the CLI argument parser, ensuring robust handling of null values, empty argument lists, out-of-bounds access, and various flag parsing scenarios. These tests verify that the parser gracefully handles malformed inputs and edge cases without throwing exceptions, maintaining application stability in production environments.

```csharp
// Example: Testing argument parser with null and edge cases
var parser = new ArgumentParser(null);

// Test null arguments handling
var nullArgsTest = new ArgumentParserEdgeCaseTests();
nullArgsTest.Constructor_NullArgs_DoesNotThrow();

// Test empty arguments list
var emptyArgsResult = parser.GetCommand("");
Assert.Equal(string.Empty, emptyArgsResult);

// Test single argument command extraction
var singleArgCommand = parser.GetCommand("start");
Assert.Equal("start", singleArgCommand);

// Test out-of-bounds positional argument access
var outOfBoundsResult = parser.GetPositional(10);
Assert.Null(outOfBoundsResult);

// Test valid positional argument access
var validPositional = parser.GetPositional(0);
Assert.Equal("start", validPositional);

// Test flag value parsing with various formats
var flagParser = new ArgumentParser(new[] { "--ssl", "--timeout", "30" });
var booleanFlag = flagParser.HasFlag("ssl");
Assert.True(booleanFlag);

var valueFlag = flagParser.GetFlagValue("timeout");
Assert.Equal("30", valueFlag);

// Test case-insensitive flag detection
var caseInsensitive = flagParser.HasFlag("SSL");
Assert.True(caseInsensitive);
```

```csharp
// Example: Using EventBus for service health monitoring notifications
var eventBus = new EventBus();

// Define a custom event type for service health changes
public class ServiceHealthChangedEvent : DomainEvent
{
    public string ServiceName { get; }
    public bool IsHealthy { get; }
    public string PreviousStatus { get; }

    public ServiceHealthChangedEvent(
        string aggregateId,
        string serviceName,
        bool isHealthy,
        string previousStatus)
        : base(aggregateId)
    {
        ServiceName = serviceName;
        IsHealthy = isHealthy;
        PreviousStatus = previousStatus;
    }
}

// Subscribe to health change events (e.g., in a monitoring service)
eventBus.Subscribe<ServiceHealthChangedEvent>(async healthEvent =>
{
    var message = healthEvent.IsHealthy
        ? $"Service {healthEvent.ServiceName} is now healthy"
        : $"Service {healthEvent.ServiceName} became unhealthy (was {healthEvent.PreviousStatus}) - sending alert!";

    Console.WriteLine(message);
    
    // Could also send webhook notifications, update dashboards, etc.
    await notifier.SendAlertAsync(message);
});

// Publish an event when service health changes (e.g., in health monitoring service)
var healthEvent = new ServiceHealthChangedEvent(
    aggregateId: "api-service-123",
    serviceName: "api-service",
    isHealthy: true,
    previousStatus: "unhealthy"
);

await eventBus.PublishAsync(healthEvent);

// Check how many subscribers are listening for this event type
int subscriberCount = eventBus.GetSubscriberCount<ServiceHealthChangedEvent>();
Console.WriteLine($"Health change events have {subscriberCount} subscribers");

// Unsubscribe when no longer needed (e.g., during cleanup)
eventBus.Unsubscribe<ServiceHealthChangedEvent>(healthHandler);
```

## SslCertStatusCheckerTests

`SslCertStatusCheckerTests` provides unit tests for the `SslCertStatusChecker` class, which validates SSL/TLS certificate monitoring functionality. This test suite verifies that certificate status checks correctly handle various scenarios including local host bindings, empty domains, valid certificates, expired certificates, certificates expiring soon, and error conditions. The tests ensure robust error handling and correct status classification for SSL certificate monitoring workflows.

```csharp
// Example: Checking SSL certificate status for multiple services
var sslMonitor = new SslCertStatusChecker();

// Check all registered services
var allServices = new List<ManagedService>
{
    new ManagedService
    {
        Name = "web-app",
        Domain = "example.com",
        HostBinding = "0.0.0.0",
        Port = 443,
        ExecutablePath = "/usr/bin/web-app",
        WorkingDirectory = "/opt/web-app"
    },
    new ManagedService
    {
        Name = "api-service",
        Domain = "api.example.com",
        HostBinding = "127.0.0.1",
        Port = 5001,
        ExecutablePath = "/usr/bin/api-server",
        WorkingDirectory = "/opt/api-server"
    }
};

// Check all services asynchronously
var results = await sslMonitor.CheckAllServicesAsync(allServices, CancellationToken.None);

foreach (var result in results)
{
    Console.WriteLine($"Domain: {result.Domain}");
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Days until expiry: {result.Certificate?.DaysUntilExpiry ?? 0}");
    Console.WriteLine($"Message: {result.Message}");
    Console.WriteLine();
}

// Check a specific domain
var domainResult = await sslMonitor.CheckCertificateAsync("example.com");

if (domainResult.IsSuccess)
{
    var certInfo = domainResult.Data;
    Console.WriteLine($"Certificate valid until: {certInfo.ExpiresAt}");
    Console.WriteLine($"Days remaining: {certInfo.DaysUntilExpiry}");
}
else
{
    Console.WriteLine($"Error checking certificate: {domainResult.ErrorMessage}");
}
```

## MemoryCacheTests

`MemoryCacheTests` provides unit tests for the `MemoryCache` class, which implements an in-memory key-value store with asynchronous operations and optional time-based expiration. The test suite validates core caching functionality including value storage and retrieval, existence checks, entry removal, cache clearing, expiration handling, and key validation, ensuring robust behavior for caching scenarios throughout the application.

```csharp
// Example: Using MemoryCache for service configuration caching
var cache = new MemoryCache();

// Store a service configuration in cache
await cache.SetAsync("api-service-config", new ServiceConfig
{
    Name = "api-service",
    Port = 5000,
    Domain = "api.example.com"
});

// Retrieve the cached configuration
var cachedConfig = await cache.GetAsync<ServiceConfig>("api-service-config");
Console.WriteLine($"Retrieved cached service: {cachedConfig?.Name} on port {cachedConfig?.Port}");

// Check if a key exists before attempting retrieval
bool exists = await cache.ExistsAsync("api-service-config");
if (exists)
{
    var config = await cache.GetAsync<ServiceConfig>("api-service-config");
    // Use the cached configuration
}

// Remove a specific entry from cache
await cache.RemoveAsync("api-service-config");

// Clear all cached entries
await cache.ClearAsync();

// Store with expiration - cache entry will be automatically removed after 5 minutes
await cache.SetAsync("temp-data", result, TimeSpan.FromMinutes(5));

// Use GetOrSetAsync for computed values - computes and caches only if missing
var expensiveResult = await cache.GetOrSetAsync("expensive-query-result", async () =>
{
    // This expensive operation will only run if the key doesn't exist
    return await database.QueryExpensiveDataAsync();
});

// Generate consistent cache keys from multiple parts
string cacheKey = CacheExtensions.MakeCacheKey("service", "health", "status");
// Returns: "service:health:status"
```

## ServiceLifecycleIntegrationTests

`ServiceLifecycleIntegrationTests` provides end-to-end integration tests that validate the complete service lifecycle workflow. These tests verify service creation, status transitions, Caddy configuration generation, health monitoring, caching, event bus integration, retry policies, and state management. The test suite demonstrates how all components work together in realistic scenarios, including concurrent operations and configuration combinations.

```csharp
// Example: Creating a service and generating its Caddy configuration
var serviceRepo = Substitute.For<IServiceRepository>();
var healthRepo = Substitute.For<IHealthCheckRepository>();
var serviceManager = new ServiceManagementService(serviceRepo);
var healthMonitor = new HealthMonitoringService(healthRepo, serviceManager);
var caddyService = new CaddyConfigurationService(serviceManager);

// Create a new service
var newService = new ManagedService
{
    Name = "api-backend",
    Description = "Backend API",
    ExecutablePath = "/usr/bin/dotnet",
    WorkingDirectory = "/opt/api",
    Port = 5000,
    Status = ServiceStatus.Stopped
};

// Add the service
var serviceId = await serviceManager.CreateServiceAsync(newService);

// Update service status
var updated = await serviceManager.UpdateServiceStatusAsync(serviceId, ServiceStatus.Running);

// Build Caddy route configuration
var route = new CaddyRoute
{
    Domain = "api.example.com",
    UpstreamUrl = $"http://localhost:{newService.Port}",
    IsActive = true,
    EnableHttps = true
};

// Generate Caddy configuration
var globalConfig = new CaddyConfig { AdminEmail = "ops@example.com" };
var caddyfile = await caddyService.GenerateCaddyfileAsync(globalConfig, new List<CaddyRoute> { route });

// Verify the generated configuration
Console.WriteLine(caddyfile);

// Monitor service health
var healthHistory = await healthMonitor.GetHealthHistoryAsync(serviceId, 24);

// Delete the service when done
var deleted = await serviceManager.DeleteServiceAsync(serviceId);
```

The test suite includes the following scenarios:

- **Full Workflow**: Create service → Generate Caddy config → Verify output
- **Status Transitions**: Stopped → Running → Stopped state changes
- **Lifecycle Management**: Create → Update → Delete complete cycle
- **Health Monitoring**: Retrieve health history for the last 24 hours
- **Concurrent Operations**: Memory cache, event bus, retry policies, and state machines
- **Configuration Combinations**: Multiple active routes, empty routes, null handling
- **Pagination & Filtering**: Service list pagination with query builder
- **Template Rendering**: Systemd unit file template generation
- **Integration Examples**: Real-world use cases from README documentation



Integration tests require a local SQLite database, which is created automatically on first run.

## Performance

caddy-vps-toolkit is optimized for minimal overhead on resource-constrained VPS environments.

| Operation | Typical Latency | Throughput |
|---|---|---|
| Caddy config generation (100 services) | <5 ms | — |
| Single service health check | 50–150 ms* | ~500 concurrent checks/s |
| Bulk service import (1,000 entries) | <200 ms | — |
| SQLite config lookup | <1 ms | ~10,000 reads/s |
| Metrics collection cycle | <20 ms | — |
| Audit log write | <2 ms | ~5,000 writes/s |

\* Network-latency dependent. Benchmarks measured on a 2 vCPU / 2 GB VPS (AMD EPYC 7R13, Ubuntu 22.04, .NET 10, single core).


## CaddyConfigBenchmarks

The `CaddyConfigBenchmarks` class measures the performance of core Caddy configuration operations including route path generation, Caddyfile global configuration generation, configuration validation, and path matcher creation. These benchmarks help identify performance regressions in configuration serialization and validation logic.


```csharp
// Create benchmark instance
var benchmarks = new CaddyConfigBenchmarks();

// Benchmark simple route path generation (e.g., "api.example.com")
var simplePath = benchmarks.GenerateRoutePath_Simple();

// Benchmark route path generation with path prefix (e.g., "app.example.com/api/v1")
var prefixedRoute = new CaddyRoute
{
    Domain = "app.example.com",
    Path = "/api/v1",
    UpstreamUrl = "http://127.0.0.1:3000"
};
var pathWithPrefix = benchmarks.GenerateRoutePath_WithPath(prefixedRoute);

// Benchmark Caddyfile global configuration generation
var globals = benchmarks.GenerateCaddyfileGlobals();

// Benchmark configuration validation
benchmarks.ValidateConfig();

// Benchmark route validation
benchmarks.ValidateRoute(simpleRoute);

// Benchmark path matcher generation for root path
var rootMatcher = benchmarks.GetCaddyPathMatcher_Root();

// Benchmark path matcher generation for prefixed path
var prefixedMatcher = benchmarks.GetCaddyPathMatcher_Prefixed(prefixedRoute);
```
### Micro-benchmark Results

Run via [BenchmarkDotNet](https://benchmarkdotnet.org/) v0.14.0 on the same VPS (AMD EPYC 7R13, .NET 10.0.0, X64 RyuJIT AVX2).

#### CLI Argument Parsing

| Method | Mean | Error | Allocated |
|---|---:|---:|---:|
| `GetCommand_Small` | 21.4 ns | ±0.3 ns | 32 B |
| `HasFlag_Present` | 81 ns | ±1.2 ns | 0 B |
| `HasFlag_Absent` | 93 ns | ±1.4 ns | 0 B |
| `GetFlagValue_SpaceSyntax` | 76 ns | ±0.9 ns | 0 B |
| `GetFlagValue_EqualsSyntax` | 143 ns | ±2.1 ns | 0 B |
| `GetAllFlags_Large` | 298 ns | ±3.8 ns | 184 B |

Span-based flag matching and `FrozenSet` boolean-flag lookup eliminate per-call `$"--{flag}"` string allocations, keeping `HasFlag` and `GetFlagValue` at **0 B** allocated.

#### String Utilities

| Method | Mean | Error | Allocated |
|---|---:|---:|---:|
| `IsNumeric_NonDigits` | 11 ns | ±0.2 ns | 0 B |
| `IsNumeric_Digits` | 17 ns | ±0.3 ns | 0 B |
| `StartsWithAny_Match` | 24 ns | ±0.4 ns | 0 B |
| `StartsWithAny_NoMatch` | 29 ns | ±0.5 ns | 0 B |
| `Truncate` | 35 ns | ±0.6 ns | 56 B |
| `ToCamelCase` | 418 ns | ±6.1 ns | 88 B |
| `ToKebabCase` | 1,012 ns | ±14 ns | 72 B |

`IsNumeric` uses `SearchValues<char>` with SIMD acceleration. `StartsWithAny` uses `Span.StartsWith` — no temporary prefix strings. `string.Create` in `Repeat` avoids intermediate arrays.

#### Caddy Config Generation

| Method | Mean | Error | Allocated |
|---|---:|---:|---:|
| `SetDefaultValues_NoOp` | 7 ns | ±0.1 ns | 0 B |
| `ValidateConfig` | 46 ns | ±0.7 ns | 0 B |
| `ValidateRoute` | 62 ns | ±0.9 ns | 0 B |
| `GetCaddyPathMatcher_Root` | 14 ns | ±0.2 ns | 0 B |
| `GenerateRoutePath_Simple` | 14 ns | ±0.2 ns | 48 B |
| `GenerateRoutePath_WithPath` | 17 ns | ±0.3 ns | 56 B |
| `GenerateCaddyfileGlobals` | 318 ns | ±4.6 ns | 312 B |

To run the full benchmark suite yourself:

```bash
cd benchmarks/caddy-vps-toolkit.Benchmarks
dotnet run -c Release
```

## ArgumentParserBenchmarks

The `ArgumentParserBenchmarks` class measures the performance of CLI argument parsing operations — the hot path on every command invocation. These benchmarks evaluate different argument parsing methods including command extraction, flag value retrieval (both equals and space syntax), flag presence detection, and bulk flag collection.




```csharp
// Create argument parser with sample arguments
var smallArgs = new[] { "add-service", "--name", "my-api", "--port", "8080", "--verbose" };
var largeArgs = new[] { "deploy", "--name=web-app", "--domain=example.com", "--port=443", 
  "--type=web", "--ssl", "--upstream=backend:8080", "--health-path=/health", "--timeout=30", "--force" };

// Create benchmark instance
var benchmarks = new ArgumentParserBenchmarks();

// Benchmark command extraction from small argument list
var command = benchmarks.GetCommand_Small();

// Benchmark flag value retrieval using equals syntax (e.g., "--name=web-app")
var nameValue = benchmarks.GetFlagValue_EqualsSyntax();

// Benchmark flag value retrieval using space syntax (e.g., "--port 8080")
var portValue = benchmarks.GetFlagValue_SpaceSyntax();

// Benchmark flag presence detection when flag is present
var hasSsl = benchmarks.HasFlag_Present();

// Benchmark flag presence detection when flag is absent
var hasNonexistent = benchmarks.HasFlag_Absent();

// Benchmark collection of all flags from large argument list
var allFlags = benchmarks.GetAllFlags_Large();
```

### Micro-benchmark Results

Run via [BenchmarkDotNet](https://benchmarkdotnet.org/) v0.14.0 on the same VPS (AMD EPYC 7R13, .NET 10.0.0, X64 RyuJIT AVX2).



| Method | Mean | Error | Allocated |
|---|---:|---:|---:|
| `GetCommand_Small` | 21.4 ns | ±0.3 ns | 32 B |
| `HasFlag_Present` | 81 ns | ±1.2 ns | 0 B |
| `HasFlag_Absent` | 93 ns | ±1.4 ns | 0 B |
| `GetFlagValue_SpaceSyntax` | 76 ns | ±0.9 ns | 0 B |
| `GetFlagValue_EqualsSyntax` | 143 ns | ±2.1 ns | 0 B |
| `GetAllFlags_Large` | 298 ns | ±3.8 ns | 184 B |

## DomainEvent

The `DomainEvent` class is a lightweight event bus implementation that enables publish-subscribe communication between components in the application. It provides a simple way to decouple event producers from event consumers, allowing for flexible event-driven architecture patterns.

### Key Features

- **Type-safe event handling**: Strongly-typed event subscriptions using generic methods
- **Asynchronous publishing**: Supports async event handlers for non-blocking operations
- **Dynamic subscription management**: Add and remove event handlers at runtime
- **Aggregate-based event tracking**: Track events by aggregate ID for domain-driven design patterns
- **Subscriber counting**: Query the number of subscribers for a specific event type

### Public Members

```csharp
public string EventId { get; }
public DateTime OccurredAt { get; }
public string AggregateId { get; }
public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent
public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : DomainEvent
public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
public int GetSubscriberCount<TEvent>() where TEvent : DomainEvent
```

### Usage Example

```csharp
// Define a custom event type
public class ServiceHealthChangedEvent : DomainEvent
{
    public string ServiceName { get; }
    public bool IsHealthy { get; }
    public string PreviousStatus { get; }
    
    public ServiceHealthChangedEvent(
        string aggregateId,
        string serviceName,
        bool isHealthy,
        string previousStatus)
        : base(aggregateId)
    {
        ServiceName = serviceName;
        IsHealthy = isHealthy;
        PreviousStatus = previousStatus;
    }
}

// In your application startup or service initialization
var eventBus = new EventBus();

// Subscribe to health change events
// This could be in a monitoring service or health check handler
eventBus.Subscribe<ServiceHealthChangedEvent>(async healthEvent =>
{
    var message = healthEvent.IsHealthy
        ? $"Service {healthEvent.ServiceName} is now healthy"
        : $"Service {healthEvent.ServiceName} became unhealthy (was {healthEvent.PreviousStatus}) - sending alert!";
    
    Console.WriteLine(message);
    
    // Could also send webhook notifications, update dashboards, etc.
    await notifier.SendAlertAsync(message);
});

// Publish an event when service health changes
// This would typically be in your health monitoring service
var healthEvent = new ServiceHealthChangedEvent(
    aggregateId: "api-service-123",
    serviceName: "api-service",
    isHealthy: true,
    previousStatus: "unhealthy"
);

await eventBus.PublishAsync(healthEvent);

// Check how many subscribers are listening for this event type
int subscriberCount = eventBus.GetSubscriberCount<ServiceHealthChangedEvent>();
Console.WriteLine($"Health change events have {subscriberCount} subscribers");

// Unsubscribe when no longer needed (e.g., during cleanup)
eventBus.Unsubscribe<ServiceHealthChangedEvent>(alertHandler);
```

### Common Event Patterns

---

## MemoryCacheTests

`MemoryCacheTests` provides unit tests for the `MemoryCache` class, which implements an in-memory key-value store with asynchronous operations and optional time-based expiration. The test suite validates core caching functionality including value storage and retrieval, existence checks, entry removal, cache clearing, expiration handling, and key validation, ensuring robust behavior for caching scenarios throughout the application.

```csharp
// Example: Using MemoryCache for service configuration caching
var cache = new MemoryCache();

// Store a service configuration in cache
await cache.SetAsync("api-service-config", new ServiceConfig
{
    Name = "api-service",
    Port = 5000,
    Domain = "api.example.com"
});

// Retrieve the cached configuration
var cachedConfig = await cache.GetAsync<ServiceConfig>("api-service-config");
Console.WriteLine($"Retrieved cached service: {cachedConfig?.Name} on port {cachedConfig?.Port}");

// Check if a key exists before attempting retrieval
bool exists = await cache.ExistsAsync("api-service-config");
if (exists)
{
    var config = await cache.GetAsync<ServiceConfig>("api-service-config");
    // Use the cached configuration
}

// Remove a specific entry from cache
await cache.RemoveAsync("api-service-config");

// Clear all cached entries
await cache.ClearAsync();

// Store with expiration - cache entry will be automatically removed after 5 minutes
await cache.SetAsync("temp-data", result, TimeSpan.FromMinutes(5));

// Use GetOrSetAsync for computed values - computes and caches only if missing
var expensiveResult = await cache.GetOrSetAsync("expensive-query-result", async () =>
{
    // This expensive operation will only run if the key doesn't exist
    return await database.QueryExpensiveDataAsync();
});

// Generate consistent cache keys from multiple parts
string cacheKey = CacheExtensions.MakeCacheKey("service", "health", "status");
// Returns: "service:health:status"
```

## ServiceLifecycleIntegrationTests

`ServiceLifecycleIntegrationTests` provides end-to-end integration tests that validate the complete service lifecycle workflow. These tests verify service creation, status transitions, Caddy configuration generation, health monitoring, caching, event bus integration, retry policies, and state management. The test suite demonstrates how all components work together in realistic scenarios, including concurrent operations and configuration combinations.

## AppConfigurationBuilderTests

`AppConfigurationBuilderTests` provides unit tests for the `AppConfigurationBuilder` class, which validates the configuration builder functionality for application settings. This test suite verifies that the builder correctly handles setting additions, default value merging, JSON file loading, and type-safe value retrieval for integers and booleans, ensuring robust configuration management for application settings.

```csharp
// Example: Building application configuration with various sources
var builder = new AppConfigurationBuilder()
    .WithSetting("Database:ConnectionString", "Server=localhost;Database=appdb")
    .WithSetting("Database:Port", "5432")
    .WithSetting("FeatureFlags:EnableNewDashboard", "true")
    .WithSetting("FeatureFlags:MaxConnections", "100");

// Build the configuration
var config = builder.Build();

// Retrieve values with type safety
string connectionString = config.GetString("Database:ConnectionString");
int port = config.GetInt("Database:Port", 5432); // Returns 5432 if key not found or invalid
bool enableDashboard = config.GetBool("FeatureFlags:EnableNewDashboard");

// Add default values for missing keys
var defaults = new Dictionary<string, object>
{
    { "Database:Timeout", 30 },
    { "FeatureFlags:EnableLegacyMode", "false" }
};
builder.WithDefaults(defaults);

// Build updated configuration
var configWithDefaults = builder.Build();
int timeout = configWithDefaults.GetInt("Database:Timeout"); // Returns 30

// Load configuration from JSON file
builder.WithJsonFile("appsettings.json");
var configFromFile = builder.Build();
```

**1. Domain Event Pattern**: Use `DomainEvent` as a base class for domain events in DDD applications

```csharp
public abstract class DomainEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string AggregateId { get; }
    
    protected DomainEvent(string aggregateId)
    {
        AggregateId = aggregateId;
    }
}
```

**2. Integration Events**: Use for cross-cutting concerns like logging, monitoring, or notifications

```csharp
public class AuditLogEntryCreatedEvent : DomainEvent
{
    public string UserId { get; }
    public string Action { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    
    public AuditLogEntryCreatedEvent(
        string aggregateId,
        string userId,
        string action,
        string entityType,
        string entityId)
        : base(aggregateId)
    {
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
    }
}
```

**3. Health Monitoring Integration**: Combine with health check system for real-time notifications

```csharp
// In your health monitoring service
public class HealthMonitoringService
{
    private readonly EventBus _eventBus;
    
    public HealthMonitoringService(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public async Task RecordHealthStatusAsync(string serviceName, bool isHealthy, string previousStatus)
    {
        var healthEvent = new ServiceHealthChangedEvent(
            aggregateId: serviceName,
            serviceName: serviceName,
            isHealthy: isHealthy,
            previousStatus: previousStatus
        );
        
        await _eventBus.PublishAsync(healthEvent);
    }
}
```

### Best Practices

- **Keep event handlers focused**: Each handler should do one thing well
- **Handle exceptions**: Wrap handler logic in try-catch blocks
- **Consider ordering**: Events are processed in subscription order
- **Use meaningful aggregate IDs**: Choose IDs that represent the entity or aggregate root
- **Keep events immutable**: Once published, events should not be modified

## LogEntry

The `LogEntry` type represents a single parsed log entry from any log source. It captures structured log data including timestamp, severity level, message content, and source information.

Usage example:

```csharp
// Create a log entry for an application event
var logEntry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Info",
    Message = "Service started successfully",
    Source = "/var/log/myapp/service.log",
    ServiceId = "api-service-01"
};

// Log an error event
var errorEntry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Error",
    Message = "Failed to connect to database: connection timeout",
    Source = "/var/log/myapp/database.log",
    ServiceId = "api-service-01"
};

// Create a warning entry for monitoring
var warningEntry = new LogEntry
{
    Timestamp = DateTime.UtcNow,
    Level = "Warning",
    Message = "High memory usage detected: 92%",
    Source = "/var/log/system/memory.log"
};
```

## ServiceCreatedEvent

The `ServiceCreatedEvent` is raised when a new service is created and registered in the system. This event provides essential information about the newly created service, enabling other components to react to service creation events for logging, monitoring, configuration generation, or integration purposes.

### Public Members

```csharp
public string ServiceName { get; set; }
public ServiceType ServiceType { get; set; }
public int Port { get; set; }
public string ExecutablePath { get; set; }
```

### Usage Example

```csharp
// Create a new service
var newService = new ManagedService
{
    Name = "api-service",
    Port = 8080,
    Domain = "api.example.com",
    ExecutablePath = "/usr/local/bin/api-server",
    ServiceType = ServiceType.WebApi
};

// Add the service (this would typically trigger the ServiceCreatedEvent)
await serviceManagementService.AddServiceAsync(newService);

// Subscribe to service creation events
var eventBus = new EventBus();
eventBus.Subscribe<ServiceCreatedEvent>(serviceEvent =>
{
    Console.WriteLine($"Service created: {serviceEvent.ServiceName}");
    Console.WriteLine($"Type: {serviceEvent.ServiceType}");
    Console.WriteLine($"Port: {serviceEvent.Port}");
    Console.WriteLine($"Executable: {serviceEvent.ExecutablePath}");
    
    // Could trigger downstream processes like:
    // - Generate systemd unit file
    // - Update Caddy configuration
    // - Send notification to monitoring system
    // - Log the event to audit trail
});

// The event would be published automatically when the service is created
```

## Related Projects

Part of a collection of .NET libraries and tools. See more at [github.com/sarmkadan](https://github.com/sarmkadan).

### Integration Examples

**Managing services programmatically via `ServiceManagementService`:**

```csharp
var svc = host.Services.GetRequiredService<ServiceManagementService>();
var result = await svc.AddServiceAsync(new ManagedService
{
    Name    = "my-api",
    Port    = 8080,
    Domain  = "api.example.com",
    HealthCheckUrl = "http://localhost:8080/health"
});
Console.WriteLine(result.IsSuccess ? "Service registered." : result.Error);
```

**Reacting to health state changes through the event bus:**

```csharp
var bus = host.Services.GetRequiredService<EventBus>();
bus.Subscribe<ServiceHealthChangedEvent>(async evt =>
{
    if (!evt.IsHealthy)
        await notifier.SendAlertAsync($"{evt.ServiceName} is unhealthy");
});
await monitor.StartMonitoringAsync(TimeSpan.FromSeconds(30));
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

## StateMachine

The `StateMachine<TState, TTrigger>` class provides a simple yet powerful state machine implementation for managing state workflows with transition validation and callback support. It's ideal for scenarios requiring explicit state transitions, workflow orchestration, and state-dependent behavior validation.

### Public Members

- `Configure(TState from, TTrigger trigger, TState to)`: Defines a transition from one state to another when a specific trigger occurs
- `OnEnter(TState state, Action callback)`: Registers a callback to execute when entering a specific state
- `OnExit(TState state, Action callback)`: Registers a callback to execute when exiting a specific state
- `CanFire(TTrigger trigger)`: Checks if the current state allows the specified trigger to fire
- `Fire(TTrigger trigger)`: Attempts to transition to the next state using the specified trigger, returns true if successful
- `GetCurrentState()`: Returns the current state
- `Reset(TState state)`: Resets the state machine to the specified state
- `GetAvailableTransitions()`: Returns a list of triggers that can be fired from the current state

### Example Usage

```csharp
// Define states and triggers
public enum ServiceState { Stopped, Starting, Running, Stopping, Failed }
public enum ServiceTrigger { Start, Stop, Restart, Fail }

// Create a state machine for service lifecycle management
var stateMachine = new StateMachine<ServiceState, ServiceTrigger>(ServiceState.Stopped);

// Configure state transitions
stateMachine.Configure(ServiceState.Stopped, ServiceTrigger.Start, ServiceState.Starting);
stateMachine.Configure(ServiceState.Starting, ServiceTrigger.Restart, ServiceState.Starting);
stateMachine.Configure(ServiceState.Starting, ServiceTrigger.Fail, ServiceState.Failed);
stateMachine.Configure(ServiceState.Starting, ServiceTrigger.Stop, ServiceState.Stopped);
stateMachine.Configure(ServiceState.Running, ServiceTrigger.Stop, ServiceState.Stopping);
stateMachine.Configure(ServiceState.Running, ServiceTrigger.Restart, ServiceState.Starting);
stateMachine.Configure(ServiceState.Stopping, ServiceTrigger.Stop, ServiceState.Stopped);
stateMachine.Configure(ServiceState.Failed, ServiceTrigger.Start, ServiceState.Starting);

// Register state entry/exit callbacks
stateMachine.OnEnter(ServiceState.Starting, () => Console.WriteLine("Entering Starting state"));
stateMachine.OnExit(ServiceState.Starting, () => Console.WriteLine("Exiting Starting state"));
stateMachine.OnEnter(ServiceState.Running, () => Console.WriteLine("Service is now running!"));

// Check if a transition is allowed
bool canStart = stateMachine.CanFire(ServiceTrigger.Start); // true when in Stopped state
Console.WriteLine($"Can start: {canStart}");

// Fire a trigger to transition states
bool transitionSuccess = stateMachine.Fire(ServiceTrigger.Start);
Console.WriteLine($"Transition successful: {transitionSuccess}"); // true
Console.WriteLine($"Current state: {stateMachine.GetCurrentState()}"); // Starting

// Get available transitions from current state
var available = stateMachine.GetAvailableTransitions();
Console.WriteLine($"Available triggers from Starting: {string.Join(", ", available)}");

// Reset to initial state
stateMachine.Reset(ServiceState.Stopped);
Console.WriteLine($"Reset to: {stateMachine.GetCurrentState()}"); // Stopped
```

## License

MIT License - see [LICENSE](./LICENSE) file for details.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)

## SerializationHelper

The `SerializationHelper` class provides utility methods for serializing and deserializing objects to/from JSON and XML formats, as well as converting objects to dictionaries and creating deep clones. It's designed to simplify common serialization tasks throughout the application with support for both JSON and XML formats.

### Example Usage

```csharp
// Serialize an object to JSON
var config = new Dictionary<string, object>
{
    ["ServiceName"] = "api-service",
    ["Port"] = 8080,
    ["Domain"] = "api.example.com",
    ["IsEnabled"] = true
};

string json = SerializationHelper.ToJson(config);
Console.WriteLine(json);
// Output: {"ServiceName":"api-service","Port":8080,"Domain":"api.example.com","IsEnabled":true}

// Deserialize JSON back to an object
var deserializedConfig = SerializationHelper.FromJson<Dictionary<string, object>>(json);
Console.WriteLine(deserializedConfig["ServiceName"]); // Output: api-service

// Try to deserialize with error handling
var parsedConfig = SerializationHelper.TryFromJson<Dictionary<string, object>>(json);
if (parsedConfig != null)
{
    Console.WriteLine("Successfully parsed JSON");
}

// Serialize to XML
string xml = SerializationHelper.ToXml(config);
Console.WriteLine(xml);

// Deserialize from XML
var xmlConfig = SerializationHelper.FromXml<Dictionary<string, object>>(xml);
Console.WriteLine(xmlConfig["Port"]); // Output: 8080

// Convert object to dictionary
var service = new ManagedService
{
    Name = "web-app",
    Port = 3000,
    Domain = "web.example.com",
    IsEnabled = true
};

var serviceDict = SerializationHelper.ToDictionary(service);
Console.WriteLine(serviceDict["Name"]); // Output: web-app

// Create a deep clone of an object
var clonedService = SerializationHelper.DeepClone(service);
Console.WriteLine(clonedService.Name); // Output: web-app
```

## StringExtensionsBenchmarks

The `StringExtensionsBenchmarks` class provides performance benchmarks for string extension methods that convert and analyze strings. These methods are commonly used throughout the application for configuration parsing, service naming, and data formatting.

Example usage:
```csharp
var benchmarks = new StringExtensionsBenchmarks();

// Convert strings to different cases
string kebabCase = "HelloWorldService".ToKebabCase();  // "hello-world-service"
string camelCase = "HelloWorldService".ToCamelCase();  // "helloWorldService"

// Truncate long strings
string truncated = "This is a very long service name that needs to be shortened".Truncate(20);  // "This is a very long..."

// Check if strings contain only digits
bool isNumericDigits = "12345".IsNumeric_Digits;      // true
bool isNumericNonDigits = "123-abc".IsNumeric_NonDigits; // false

// Check if strings start with any of the specified prefixes
bool startsWithMatch = "api-service".StartsWithAny_Match;     // true (matches "api")
bool startsWithNoMatch = "web-service".StartsWithAny_NoMatch;  // true (no match)
```

## Notification

The `Notification` type represents a structured notification that can be sent through the `NotificationService` to deliver messages to external systems or internal components. Notifications contain metadata for routing, priority levels for filtering, and tracking information for audit purposes.

```csharp
var notification = new Notification
{
    Id = Guid.NewGuid().ToString(),
    Title = "Service Health Alert",
    Message = "Service 'api-service' has become unhealthy",
    Priority = NotificationPriority.High,
    CreatedAt = DateTime.UtcNow,
    Metadata = new Dictionary<string, string>
    {
        ["service"] = "api-service",
        ["status"] = "unhealthy",
        ["timestamp"] = DateTime.UtcNow.ToString("o")
    }
};

var notificationService = new NotificationService();
notificationService.Register("slack", new SlackWebhookHandler("https://hooks.slack.com/services/..."));

bool sent = await notificationService.SendAsync(notification);
```

## UpstreamManagerServiceTests

`UpstreamManagerServiceTests` provides unit tests for the `UpstreamManagerService` class, which manages upstream pool registration and retrieval for load balancing configurations. This test suite verifies that the service correctly handles null inputs, invalid pool configurations, non-existent service references, and valid pool operations, ensuring robust error handling and correct behavior for upstream pool management.

```csharp
// Example: Using UpstreamManagerService for load balancing pool management
var serviceRepo = Substitute.For<IServiceRepository>();
var healthRepo = Substitute.For<IHealthCheckRepository>();

var serviceManager = new ServiceManagementService(serviceRepo);
var healthMonitor = new HealthMonitoringService(healthRepo, serviceManager);
var caddyConfig = new CaddyConfigurationService(serviceManager);

var options = new LoadBalancingOptions();
var upstreamManager = new UpstreamManagerService(serviceManager, healthMonitor, caddyConfig, options);

// Test null pool registration - should throw ArgumentNullException
Func<Task> nullPoolTest = async () => await upstreamManager.RegisterPoolAsync(null!);
await nullPoolTest.Should().ThrowAsync<ArgumentNullException>();

// Test invalid pool registration - should throw ServiceConfigurationException
var invalidPool = new UpstreamPool { Id = "pool1", Name = "pool1", ServiceId = "svc1" };
invalidPool.Servers = new List<UpstreamServer>();
Func<Task> invalidPoolTest = async () => await upstreamManager.RegisterPoolAsync(invalidPool);
await invalidPoolTest.Should().ThrowAsync<ServiceConfigurationException>();

// Test service not found scenario - should throw ServiceNotFoundException
var poolWithMissingService = new UpstreamPool
{
    Id = "pool1",
    Name = "pool1", 
    ServiceId = "nonexistent",
    Servers = new List<UpstreamServer> { new UpstreamServer { Address = "127.0.0.1", Port = 80 } }
};
serviceRepo.GetByIdAsync("nonexistent").Returns(Task.FromResult<ManagedService?>(null));
Func<Task> missingServiceTest = async () => await upstreamManager.RegisterPoolAsync(poolWithMissingService);
await missingServiceTest.Should().ThrowAsync<ServiceNotFoundException>();

// Test valid pool registration - should return pool ID
var validPool = new UpstreamPool
{
    Id = "pool1",
    Name = "pool1",
    ServiceId = "svc1",
    Servers = new List<UpstreamServer> { new UpstreamServer { Address = "127.0.0.1", Port = 80 } }
};
serviceRepo.GetByIdAsync("svc1").Returns(Task.FromResult<ManagedService?>(new ManagedService { Id = "svc1" }));

var poolId = await upstreamManager.RegisterPoolAsync(validPool);
poolId.Should().Be("pool1");

// Test retrieving existing pool - should return the pool
var retrievedPool = await upstreamManager.GetPoolAsync("pool1");
retrievedPool.Should().NotBeNull();
retrievedPool!.Id.Should().Be("pool1");

// Test retrieving non-existent pool - should return null
var nonexistentPool = await upstreamManager.GetPoolAsync("unknown");
nonexistentPool.Should().BeNull();
```

## ServiceCreatedEventHandler

`ServiceCreatedEventHandler` is an event handler that responds to `ServiceCreatedEvent` notifications. It logs the creation of new services and triggers webhook notifications to external systems, enabling integration with monitoring, alerting, and automation platforms.


The handler requires `ILogger` and `IWebhookHandler` dependencies for logging service creation events and dispatching webhook notifications respectively.

### Real-World Usage Example

```csharp
// Setup dependency injection container
var services = new ServiceCollection();

// Register required services
services.AddLogging(configure => configure.AddConsole());
services.AddSingleton<IWebhookHandler, SlackWebhookHandler>();
services.AddSingleton<ILogger, ConsoleLogger>();
services.AddSingleton<ServiceCreatedEventHandler>();

var serviceProvider = services.BuildServiceProvider();

// Resolve the event handler
var eventHandler = serviceProvider.GetRequiredService<ServiceCreatedEventHandler>();

// Create and publish a service creation event
var serviceEvent = new ServiceCreatedEvent(
    serviceName: "api-service",
    port: 8080,
    serviceType: ServiceType.WebApi,
    executablePath: "/usr/local/bin/api-server"
);

// Handle the event (logs and triggers webhook)
await eventHandler.HandleAsync(serviceEvent);

// Output:
// [INFO] Service created: {api-service} on port 8080
// Webhook notification sent to external system with service details
```

## UpstreamHealthTrackerExtensions

The `UpstreamHealthTrackerExtensions` class provides extension methods for the `IUpstreamHealthTracker` interface, enabling health monitoring and management of upstream servers within load-balanced pools. It offers methods to record probe results, retrieve health snapshots, drain unhealthy upstreams, and wait for healthy states, making it ideal for implementing health-aware load balancing strategies.

### Example Usage

```csharp
using CaddyVpsToolkit.LoadBalancing;
using Microsoft.Extensions.DependencyInjection;

// Set up dependency injection
var services = new ServiceCollection();
services.AddUpstreamHealthTracking(); // Register health tracker services

var serviceProvider = services.BuildServiceProvider();
var healthTracker = serviceProvider.GetRequiredService<IUpstreamHealthTracker>();

// Record probe results for an upstream server
var probeResult = new UpstreamProbeResult
{
    PoolId = "api-pool",
    UpstreamId = "api-server-01",
    ProbeSucceeded = true,
    ResponseTimeMs = 45,
    CheckedAt = DateTime.UtcNow
};

await healthTracker.RecordProbeResultsAsync(probeResult);

// Get all health snapshots
var allSnapshots = await healthTracker.GetAllSnapshotsAsync();
Console.WriteLine($"Total upstreams tracked: {allSnapshots.Count}");

// Get unhealthy upstreams only
var unhealthy = await healthTracker.GetUnhealthyUpstreamsAsync();
if (unhealthy.Count > 0)
{
    Console.WriteLine("Unhealthy upstreams:");
    foreach (var snapshot in unhealthy)
    {
        Console.WriteLine($"  - {snapshot.UpstreamId}: {snapshot.Status}");
    }
}

// Get healthy upstreams
var healthy = await healthTracker.GetHealthyUpstreamsAsync();
Console.WriteLine($"Healthy upstreams: {healthy.Count}");

// Wait for all upstreams to become healthy (with timeout)
bool allHealthy = await healthTracker.WaitForHealthyAsync(
    timeout: TimeSpan.FromSeconds(30),
    pollInterval: TimeSpan.FromSeconds(2)
);

Console.WriteLine($"All upstreams healthy: {allHealthy}");

// Get pool health summaries
var poolHealth = await healthTracker.GetPoolHealthSummariesAsync();
foreach (var pool in poolHealth)
{
    Console.WriteLine($"Pool {pool.PoolName}:");
    Console.WriteLine($"  - Active: {pool.ActiveServers}");
    Console.WriteLine($"  - Unhealthy: {pool.UnhealthyServers}");
    Console.WriteLine($"  - Draining: {pool.DrainingServers}");
    Console.WriteLine($"  - Disabled: {pool.DisabledServers}");
}

// Get system-wide health summary
var systemHealth = await healthTracker.GetSystemHealthSummaryAsync();
Console.WriteLine($"System health: {systemHealth.HealthPercentage:P0}");
Console.WriteLine($"Total pools: {systemHealth.TotalPools}, Healthy: {systemHealth.HealthyPools}");

// Drain unhealthy upstreams for maintenance
await healthTracker.DrainAsync("api-pool", drainTimeout: TimeSpan.FromMinutes(5));
```
