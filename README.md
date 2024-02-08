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

## Testing

Run the unit test suite:

```bash
dotnet test
```

Run with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers argument parsing and validation (`ArgumentParserTests`), string extension utilities (`StringExtensionsTests`), and configuration validation helpers (`ValidationHelperTests`). Integration tests require a local SQLite database, which is created automatically on first run.

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

## License

MIT License - see [LICENSE](./LICENSE) file for details.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
