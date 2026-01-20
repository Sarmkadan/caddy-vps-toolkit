# Changelog

All notable changes to caddy-vps-toolkit are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-08-11
### Added
- Add dynamic upstream management with health-aware load balancing
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

## [1.0.0] - 2025-10-13

### Added
- Stable release incorporating all v0.x work
- Webhook notifications for health events (Slack, PagerDuty, custom endpoints)
- Prometheus-compatible metrics endpoint on configurable port
- Audit logging for all service changes with timestamp and actor details
- Rate limiting per service with configurable thresholds and status codes
- Batch service import and export in JSON and YAML formats
- Configuration backup and restore with point-in-time snapshots
- Recovery scripts per service with configurable execution timeout
- Health check result history with date-range and service-name filtering
- Docker container with volume mounts for config, data, and generated files
- `caddy-vps-toolkit config export/import` commands
- `caddy-vps-toolkit audit log` command with limit and date-range flags
- `caddy-vps-toolkit notification add-webhook` command
- `caddy-vps-toolkit metrics collect` command
- Health check customization per service (interval, timeout, retries)
- Service dependency management with systemd `After=` / `Requires=` generation
- Environment variable support in service configuration
- Configuration backup and restore functionality

### Changed
- CLI argument validation now returns structured error objects with field-level details
- Health check failures are grouped before triggering webhook calls
- Generated Caddyfile includes optional health-check stanzas per service
- Systemd units include `After=` ordering for all declared dependencies
- `ServiceRepository` now returns `IAsyncEnumerable` for list operations
- Improved exception hierarchy with domain-specific types (`SystemdOperationException`, `CaddyOperationException`, `HealthCheckException`)
- Database schema migrated to v3 with composite indexes on frequently queried columns

### Fixed
- Database locking under concurrent health check writes
- Caddy reload blocking indefinitely on config syntax errors
- Circular dependency detection in service dependency graph
- Memory leak in `HealthCheckWorker` on long-running instances
- Systemd unit generation for services with spaces in names
- Caddy config reload not propagating errors to the caller

### Security
- HMAC signing for outgoing webhook requests
- Environment variable sanitization in generated systemd units
- SQL parameterization audit across all repository methods
- Input validation for all CLI arguments

## [0.3.0] - 2025-09-08

### Added
- Template engine (`Utilities/TemplateEngine.cs`) for flexible config generation
- Multiple port support per service via `ServicePort` domain model
- Caddyfile validation before writing to disk
- Health check result analytics (average response latency, failure rate)
- Event bus (`Events/EventBus.cs`) for decoupled service event handling
- Performance monitoring and lightweight metrics collection via `MetricsCollector`
- `PaginationHelper` for paginated repository queries
- Output formatter supporting text, JSON, and CSV output modes

### Changed
- Refactored `CaddyConfigurationService` to use the template engine
- Improved error messages for invalid domain and port inputs

### Fixed
- `ArgumentParser` not handling quoted argument values with spaces
- Config path not expanding `~` on Linux

## [0.2.0] - 2025-08-18

### Added
- Background worker infrastructure (`BackgroundWorkers/`) with `WorkerCoordinator`
- `HealthCheckWorker` for continuous endpoint polling
- `MaintenanceWorker` for periodic database cleanup and vacuuming
- Middleware pipeline (`Middleware/`) with logging, error handling, and rate-limiting middleware
- `Result<T>` type (`Results/Result.cs`) for explicit error propagation without exceptions
- Cache service (`Caching/CacheService.cs`) for in-process service-record caching
- Notification service skeleton (`Notifications/NotificationService.cs`)
- Batch processor (`Processing/BatchProcessor.cs`) for multi-service operations
- Retry policy utility (`Utilities/RetryPolicy.cs`) with exponential back-off

### Fixed
- Health check interval not respecting minimum-5-second floor
- Missing null checks in `ServiceManagementService.RemoveServiceAsync`

## [0.1.0] - 2025-07-28

### Added
- Initial project structure targeting .NET 10
- Core domain models: `ManagedService`, `HealthCheckConfig`, `HealthCheckResult`,
  `CaddyConfig`, `CaddyRoute`, `SystemdUnitConfig`, `ServicePort`
- Repository pattern: `ServiceRepository`, `ConfigurationRepository`,
  `HealthCheckRepository` backed by SQLite
- `ArgumentParser` and `ArgumentValidator` for CLI input
- `CommandRegistry` for mapping CLI verbs to handlers
- `ServiceManagementService`, `CaddyConfigurationService`,
  `SystemdUnitService`, `HealthMonitoringService`, `ConfigurationService`
- `appsettings.json` configuration loading via `Microsoft.Extensions.Configuration`
- Dependency injection wiring in `Program.cs`
- MIT license and initial README

---

## Upcoming Features (Roadmap)

### v2.0.0 (2026)
- [ ] Distributed mode for multi-VPS management
- [ ] Web UI dashboard
- [ ] GraphQL API alongside CLI
- [ ] Kubernetes Helm chart export
- [ ] Service mesh integration (Istio/Linkerd)

---

## Version Support

| Version | Release Date    | Support Until  |
|---------|----------------|----------------|
| 1.1.0   | Jun 11, 2026   | Jun 11, 2027   |
| 1.0.0   | Oct 13, 2025   | Oct 13, 2026   |
| 0.x.x   | Various        | Unsupported    |

---

## Notes

### Breaking Changes Policy
We follow semantic versioning. Breaking changes only occur in major version releases (e.g., 1.x to 2.x) and are announced at least one minor version in advance.

### Security Updates
Security updates are released as patch versions (e.g., 1.0.1) and applied to the latest minor version. Users are encouraged to upgrade immediately.

### Deprecation Warnings
Features marked as deprecated in a release are removed in the next major version. Warnings appear in CLI output and logs before removal.

---

## Contributors

Thanks to all contributors who have helped improve caddy-vps-toolkit:

- Vladyslav Zaiets (Author & Maintainer)
- Community contributors and testers

---

## Contact

For questions about releases or roadmap:
- GitHub Issues: https://github.com/Sarmkadan/caddy-vps-toolkit/issues
- Website: https://sarmkadan.com

---

**Last Updated:** May 14, 2026
