# CLAUDE.md

## Overview
.NET 10 CLI tool (`caddy-vps-toolkit`) for managing multiple services on a single Linux VPS: Caddy reverse-proxy config, systemd units, health monitoring, SSL cert tracking, backups. SQLite persistence.

## Build
- `dotnet restore` / `dotnet build -c Debug` (or `make build`, `make build-release`)
- `make publish` - self-contained release build; `make install` - installs to system
- Docker: `make docker-build`, `docker-compose.yml` for local stack
- SDK pinned in `global.json` (10.0.100, rollForward latestMinor)

## Test
- All: `dotnet test` (or `make test` = unit + integration)
- Unit only: `dotnet test --filter Category=Unit` (`make test-unit`)
- Integration only: `dotnet test --filter Category=Integration` (`make test-integration`)
- Coverage: `make test-coverage`
- Stack: xunit + FluentAssertions + NSubstitute. Test project: `tests/caddy-vps-toolkit.Tests/`, references root csproj. Extra test csproj in `tests/Cli/`.
- Benchmarks (BenchmarkDotNet): `benchmarks/caddy-vps-toolkit.Benchmarks/`, `make bench`

## Lint / Format
- `dotnet format` (`make format`); CI runs `dotnet format --verify-no-changes` (`make format-check`)
- Lint = build with warnings as errors: `make lint` (`dotnet build /p:TreatWarningsAsErrors=true`)
- `make check` = format-check + lint + test; `make ci-local` = clean + restore + lint + test
- Style rules in `.editorconfig`. `Nullable` and `ImplicitUsings` enabled via `Directory.Build.props`.
- `BannedSymbols.txt` (BannedApiAnalyzers): `Path.Combine` and `Path.Join` are banned - use `PathUtilities.SafeCombine`.
- CI: `.github/workflows/` (build+test, lint, security-scan) on push/PR to `main`/`develop`.

## Key directories / entry points
- `Program.cs` - DI container setup (`ServiceCollection`), resolves `CliCommandHandler` and dispatches args
- `Core/` - `CliCommandHandler`, `Constants`, `Enums`, exceptions
- `Cli/` - `ArgumentParser`, `ArgumentValidator`, `CommandRegistry`
- `Domain/Models/` - rich domain models (`ManagedService`, `CaddyRoute`, `CaddyConfig`, `SystemdUnitConfig`, `HealthCheck*`, `UpstreamServer/Pool`, `SslCertificateInfo`)
- `Data/` - repository interfaces + SQLite implementations
- `Services/` - business logic (`ServiceManagementService`, `HealthMonitoringService`, `CaddyConfigurationService`, `SystemdUnitService`, `BackupService`, `SslCertificateMonitoringService`)
- `Configuration/` - options classes + `AppConfigurationBuilder`; template in `appsettings.json`
- `Middleware/`, `Events/`, `BackgroundWorkers/`, `LoadBalancing/`, `Caching/`, `Monitoring/`, `Notifications/`, `Processing/`, `Results/`, `Auditing/`, `Integration/`
- `Utilities/` - `PathUtilities`, `StringExtensions`, `RetryPolicy`, `TemplateEngine`, `StateMachine`, etc.
- `Formatters/` - CLI output formatters; `DependencyInjection/` - `IServiceCollection` extension registrations
- `docs/`, `examples/`, `ARCHITECTURE.md` - further reference
- Root csproj excludes `tests/**` and `benchmarks/**` from compilation.

## Conventions
- Root namespace `CaddyVpsToolkit`; sub-namespace mirrors folder (`CaddyVpsToolkit.Utilities`, `CaddyVpsToolkit.Domain.Models`).
- Every file starts with `#nullable enable` and the author header block; block-scoped namespaces with braces.
- One type per file, file name = type name. Extension methods live in `<Type>Extensions.cs`; JSON helpers in `<Type>JsonExtensions.cs`; fluent builders in `<Type>FluentExtensions.cs`. Same suffix scheme in tests.
- Patterns: Repository (`I*Repository`), Service layer, constructor DI, rich domain models with `Validate()` methods, `Result` types in `Results/`, event bus in `Events/`.
- Classes sealed / static where possible; XML doc comments on public members (`GenerateDocumentationFile` on).
- Tests: xunit `[Trait("Category", "Unit"|"Integration")]`, FluentAssertions, NSubstitute for mocks.
- Never use `Path.Combine`/`Path.Join` outside `PathUtilities.cs`.
