# Changelog

All notable changes to caddy-vps-toolkit are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-01-15

### Added
- Health check recovery script support with configurable timeout
- Webhook notifications for health failures (Slack, PagerDuty integration)
- Prometheus metrics endpoint on port 9090
- Audit logging for all service modifications with timestamp and details
- Rate limiting configuration per service
- Batch service import/export in JSON and YAML formats
- Configuration validation with detailed error messages
- Health check history with filtering by date range and service
- Systemd unit template support for creating instances
- Docker container for toolkit with mounted volumes

### Changed
- Improved CLI argument parsing with better error messages
- Database migration system for schema updates
- Health check interval validation (minimum 5 seconds)
- Generated Caddyfile now includes health check configuration
- Systemd units include automatic ordering constraints

### Fixed
- Database locking issues under high concurrency
- Health check timeouts on slow networks
- Caddy reload blocking on configuration errors
- Service dependency circular reference detection
- Memory leak in long-running health monitor

### Security
- Input validation for all CLI arguments
- SQL injection prevention in repository layer
- Webhook request signing with HMAC
- Environment variable sanitization in systemd units

## [1.1.0] - 2025-12-01

### Added
- Health check customization per service (interval, timeout, retries)
- Service dependency management with systemd integration
- Configuration backup and restore functionality
- Service status polling with history
- Environment variable support in service configuration
- Event bus for decoupled event handling
- Custom middleware pipeline implementation
- Performance monitoring and metrics collection

### Changed
- Refactored service repository for better scalability
- Improved error handling with custom exception types
- CLI command structure for better organization
- Database schema with foreign keys and constraints

### Fixed
- Systemd unit generation for complex service configurations
- Caddy configuration validation before reload
- Health check result persistence

## [1.0.0] - 2025-10-15

### Added
- Initial release of caddy-vps-toolkit
- Service management (CRUD operations)
- Caddy Caddyfile generation from service configuration
- Systemd unit file generation
- Basic health check monitoring
- SQLite database for service persistence
- Configuration management with appsettings.json
- CLI command parser and registry
- Comprehensive error handling

### Features
- **Service Management**: Add, update, delete, and list services
- **Caddy Integration**: Automatic reverse proxy configuration generation
- **Systemd Integration**: Create and manage systemd unit files
- **Health Monitoring**: HTTP endpoint health checks with configurable intervals
- **Configuration Storage**: SQLite-based service and configuration repository
- **CLI Interface**: User-friendly command-line interface

## [0.3.0] - 2025-09-01

### Added
- Template engine for flexible configuration generation
- Service port management with multiple port support
- Caddyfile validation before applying changes
- Health check result analytics

## [0.2.0] - 2025-08-15

### Added
- Background worker implementation for health checks
- Middleware pipeline for request processing
- Result type for error handling

## [0.1.0] - 2025-08-01

### Added
- Initial project structure
- Core domain models (ManagedService, HealthCheckConfig, etc.)
- Repository pattern implementation
- Basic CLI argument parsing
- SQLite database integration

---

## Migration Guides

### From 1.0.0 to 1.1.0

No breaking changes. Existing configurations remain compatible.

**Recommended updates:**
1. Export current configuration: `caddy-vps-toolkit config export --format json > backup.json`
2. Run new version: `caddy-vps-toolkit --version`
3. Update services with dependencies: `caddy-vps-toolkit service update SERVICE --depends-on OTHER_SERVICE`

### From 1.1.0 to 1.2.0

**Breaking Changes:** None

**Important Updates:**
1. New audit logging is enabled by default in appsettings.json
2. Prometheus metrics available at `http://localhost:9090/metrics`
3. Webhook notifications require explicit configuration

**Steps:**
1. Backup database: `cp ~/.caddy-vps-toolkit/services.db ~/backup.db`
2. Update configuration in appsettings.json with new options
3. Restart health monitor: `sudo systemctl restart caddy-toolkit-monitor`

---

## Upcoming Features (Roadmap)

### v2.0.0 (Q3 2026)
- [ ] Distributed mode for multi-VPS management
- [ ] Web UI dashboard
- [ ] GraphQL API alongside CLI
- [ ] Kubernetes Helm chart export
- [ ] Advanced scheduling (cron-based operations)
- [ ] Service mesh integration (Istio/Linkerd)

### v1.3.0 (Q2 2026)
- [ ] PostgreSQL backend support (alternative to SQLite)
- [ ] Advanced filtering and search
- [ ] Metrics export to CloudWatch/Datadog
- [ ] Automated backup scheduling
- [ ] Custom alert templates

### v1.2.1 (Q1 2026)
- [ ] Bug fixes and performance improvements
- [ ] Additional webhook integrations
- [ ] Improved documentation
- [ ] Community-contributed examples

---

## Version Support

| Version | Release Date | Support Until |
|---------|-------------|---------------|
| 1.2.0   | Jan 15, 2026 | Jan 15, 2027  |
| 1.1.0   | Dec 1, 2025  | Dec 1, 2026   |
| 1.0.0   | Oct 15, 2025 | Oct 15, 2026  |
| 0.x.x   | Various      | Unsupported   |

---

## Notes

### Breaking Changes Policy
We follow semantic versioning. Breaking changes only occur in major version releases (e.g., 1.x to 2.x) and are announced at least one minor version in advance.

### Security Updates
Security updates are released as patch versions (e.g., 1.2.1) and are applied to the latest minor version. Users are encouraged to upgrade immediately.

### Deprecation Warnings
Features marked as deprecated in a release are usually removed in the next major version. Users will receive warnings in logs and CLI output.

---

## Contributors

Thanks to all contributors who have helped improve caddy-vps-toolkit:

- Vladyslav Zaiets (Author & Maintainer)
- Community contributors and testers

---

## Contact

For questions about releases or roadmap:
- GitHub Issues: https://github.com/Sarmkadan/caddy-vps-toolkit/issues
- Email: https://sarmkadan.com

---

**Last Updated:** January 15, 2026
