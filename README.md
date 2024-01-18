# Caddy VPS Toolkit

A comprehensive .NET CLI tool for managing multiple services on a single VPS with integrated Caddy reverse proxy configuration, systemd unit management, and health monitoring.

## Features

- **Service Management**: Create, update, delete, and manage services
- **Health Monitoring**: HTTP and TCP health checks with detailed statistics
- **Caddy Integration**: Automatic reverse proxy configuration generation
- **Systemd Integration**: Create and manage systemd unit files
- **Configuration Management**: Centralized configuration storage
- **Status Tracking**: Real-time service status monitoring
- **Health Statistics**: Comprehensive health check analytics

## Architecture

### Domain Models
- `ManagedService`: Core service entity with full configuration
- `HealthCheckConfig`: Health check configuration and rules
- `HealthCheckResult`: Health check execution results
- `CaddyRoute`: Reverse proxy route configuration
- `CaddyConfig`: Global Caddy settings
- `SystemdUnitConfig`: Systemd unit configuration
- `ServicePort`: Port mapping and exposure rules

### Services
- `ServiceManagementService`: CRUD operations and status management
- `HealthMonitoringService`: Health check execution and analytics
- `CaddyConfigurationService`: Caddyfile generation and validation
- `SystemdUnitService`: Systemd unit file management
- `ConfigurationService`: Application-level configuration

### Data Access
- `ServiceRepository`: Service persistence (SQLite)
- `HealthCheckRepository`: Health check history (SQLite)
- `ConfigurationRepository`: Application configuration (SQLite)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run -- list-services
dotnet run -- service-status <service-id>
dotnet run -- health-check <service-id>
dotnet run -- health-summary
```

## Commands

| Command | Description |
|---------|-------------|
| `version` | Show version information |
| `help` | Show help information |
| `list-services` | List all managed services |
| `service-status <id>` | Get detailed service status |
| `health-check <id>` | Perform health check on service |
| `health-summary` | Get overall health summary |
| `config-get <key>` | Get configuration value |
| `config-set <key> <value>` | Set configuration value |

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

## Author

**Vladyslav Zaiets**
- CTO & Software Architect
- https://sarmkadan.com
