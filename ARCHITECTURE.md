# Caddy VPS Toolkit - Architecture Overview

## Project Structure

```
caddy-vps-toolkit/
├── Core/                          # Infrastructure and shared utilities
│   ├── Enums.cs                  # ServiceStatus, ServiceType, HealthCheckType
│   ├── Constants.cs              # Application-wide constants
│   ├── Exceptions.cs             # Custom exception types
│   └── CliCommandHandler.cs       # CLI command processing
├── Domain/
│   └── Models/                    # Domain entities with business logic
│       ├── ManagedService.cs      # Service definition (400+ lines)
│       ├── HealthCheckConfig.cs   # Health check configuration
│       ├── HealthCheckResult.cs   # Health check results
│       ├── CaddyRoute.cs          # Reverse proxy routes
│       ├── CaddyConfig.cs         # Caddy global configuration
│       ├── SystemdUnitConfig.cs   # Systemd unit definitions
│       └── ServicePort.cs         # Port mapping configuration
├── Data/                          # Data access layer
│   ├── IServiceRepository.cs      # Repository interfaces + stats model
│   ├── ServiceRepository.cs       # SQLite service persistence (400+ lines)
│   ├── HealthCheckRepository.cs   # SQLite health check persistence (300+ lines)
│   └── ConfigurationRepository.cs # SQLite config persistence
├── Services/                      # Business logic services
│   ├── ServiceManagementService.cs       # CRUD + status ops (200+ lines)
│   ├── HealthMonitoringService.cs        # Health checks + analytics (350+ lines)
│   ├── CaddyConfigurationService.cs      # Caddy config generation (250+ lines)
│   ├── SystemdUnitService.cs             # Systemd management (320+ lines)
│   └── ConfigurationService.cs           # App config management (200+ lines)
├── Program.cs                     # Dependency injection setup
├── caddy-vps-toolkit.csproj       # Project file with NuGet references
├── appsettings.json              # Configuration template
├── LICENSE                        # MIT License
├── README.md                      # User documentation
├── ARCHITECTURE.md                # This file
└── .gitignore                     # Git ignore rules
```

## Design Patterns

### 1. **Repository Pattern**
- `IServiceRepository`, `IHealthCheckRepository`, `IConfigurationRepository`
- Abstracts data access from business logic
- Enables easy testing and future database switching

### 2. **Dependency Injection**
- Setup in `Program.cs` using `ServiceCollection`
- Constructor-based injection in all services
- Follows Microsoft.Extensions patterns

### 3. **Service Layer Pattern**
- Clear separation of concerns
- Each service handles specific domain (services, health, caddy, systemd, config)
- Services depend on repositories and each other through DI

### 4. **Domain Models**
- Rich domain objects with validation and business logic
- Not just data holders - include methods like `Validate()`, `GetSystemdUnitName()`
- Self-documenting through properties and enums

## Key Features by Service

### ServiceManagementService
- CRUD operations on `ManagedService`
- Status management and updates
- Service search and filtering
- Auto-start and priority management
- Prevents deletion of running services

### HealthMonitoringService
- HTTP and TCP health checks
- Async health check execution with timeouts
- Health statistics and analytics
- Consecutive failure/success tracking
- Old record cleanup

### CaddyConfigurationService
- Generate Caddyfile content from routes
- Write/read Caddyfile from disk
- Basic Caddyfile syntax validation
- JSON configuration generation
- Route block generation with headers, auth, rate limiting

### SystemdUnitService
- Create and delete unit files
- Start/stop/restart services
- Enable/disable on boot
- Query service status and detailed info
- Command execution with timeouts

### ConfigurationService
- Key-value configuration storage
- Typed get/set with defaults
- JSON file import/export
- In-memory caching for performance
- Type-safe configuration helpers

## Data Model

### Services Database Schema
```sql
Services
├── Id (PRIMARY KEY)
├── Name (UNIQUE)
├── Type (enum)
├── Status (enum)
├── ExecutablePath
├── WorkingDirectory
├── Port
├── AutoStart (bool)
├── Priority
├── CreatedAt (timestamp)
└── UpdatedAt (timestamp)

HealthCheckResults
├── Id (PRIMARY KEY)
├── ServiceId (FOREIGN KEY)
├── IsHealthy (bool)
├── ResponseTimeMs
├── HttpStatusCode
├── CheckedAt (timestamp)
└── [Index: ServiceId, CheckedAt]

Configuration
├── Key (PRIMARY KEY)
├── Value
├── CreatedAt
└── UpdatedAt
```

## Exception Handling

Custom exception hierarchy for better error context:
- `CaddyVpsException` - Base exception
- `ServiceNotFoundException` - Service not found
- `ServiceConfigurationException` - Invalid configuration
- `SystemdOperationException` - Systemd failures
- `CaddyOperationException` - Caddy failures
- `HealthCheckException` - Health check failures
- `DatabaseException` - Database errors

## Command Processing

CLI commands handled by `CliCommandHandler`:
- `list-services` - Display all services
- `service-status` - Get service details
- `health-check` - Execute health check
- `health-summary` - Overall health overview
- `config-get/set` - Configuration management
- `version` / `help` - Information commands

## Dependencies

### NuGet Packages
- `Microsoft.Extensions.DependencyInjection` - Dependency injection
- `Microsoft.Extensions.Configuration` - Configuration management
- `System.Data.SQLite` - SQLite database access
- `Newtonsoft.Json` - JSON serialization

### System Requirements
- .NET 8.0+
- systemd (for service management)
- Caddy (for reverse proxy functionality)

## Code Statistics

- **Total Files**: 26
- **Total Lines**: 3,232
- **C# Source Files**: 21
- **Domain Models**: 7 classes (641 LOC)
- **Services**: 5 classes (1,291 LOC)
- **Data Access**: 4 classes (760 LOC)
- **Core Infrastructure**: 4 classes (452 LOC)

## Future Enhancements

- REST API layer on top of services
- Web UI dashboard
- Event-based notifications
- Database connection pooling
- Service restart policies
- Load balancing integration
- SSL/TLS certificate management
- Docker container support
