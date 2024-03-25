# SystemdUnitConfigTests

`SystemdUnitConfigTests` is the test suite for the `SystemdUnitConfig` builder class in the `caddy-vps-toolkit` project. It validates both the configuration validation logic and the systemd unit file content generation, ensuring that valid configurations produce correct unit file sections and directives while invalid configurations throw appropriate exceptions.

## API

All members are public parameterless test methods returning `void`. They are designed to be executed by a test runner such as xUnit or NUnit. Each method asserts a specific behaviour of the `SystemdUnitConfig` class.

### Validation Tests

- **`Validate_MinimalValidConfig_DoesNotThrow`**  
  Verifies that a configuration object populated with the minimum required fields (unit name and `ExecStart`) passes validation without throwing an exception.

- **`Validate_MissingUnitName_ThrowsValidationException`**  
  Confirms that calling `Validate()` on a configuration that lacks a unit name throws a `ValidationException`.

- **`Validate_MissingExecStart_ThrowsValidationException`**  
  Confirms that calling `Validate()` on a configuration that lacks an `ExecStart` command throws a `ValidationException`.

- **`Validate_NegativeRestartDelay_ThrowsValidationException`**  
  Confirms that setting a negative restart delay value and calling `Validate()` throws a `ValidationException`.

### Generation Tests — Unit Section

- **`GenerateSystemdContent_ContainsUnitSection`**  
  Asserts that the generated systemd unit file content includes a `[Unit]` section header.

- **`GenerateSystemdContent_WithWants_EmitsWantsDirective`**  
  Verifies that when the configuration specifies one or more `Wants` targets, the generated output contains a `Wants=` directive listing them.

- **`GenerateSystemdContent_WithoutWants_DoesNotEmitWantsDirective`**  
  Verifies that when no `Wants` targets are configured, the generated output does not contain a `Wants=` line.

### Generation Tests — Service Section

- **`GenerateSystemdContent_ContainsServiceSection`**  
  Asserts that the generated systemd unit file content includes a `[Service]` section header.

- **`GenerateSystemdContent_RestartPolicyIsLowercase`**  
  Verifies that the restart policy value emitted in the `Restart=` directive is rendered in lowercase (e.g., `always`, `on-failure`).

- **`GenerateSystemdContent_WithWorkingDirectory_EmitsDirective`**  
  Confirms that when a working directory is specified, the output contains a `WorkingDirectory=` directive with the correct path.

- **`GenerateSystemdContent_WithoutWorkingDirectory_OmitsDirective`**  
  Confirms that when no working directory is specified, the output does not contain a `WorkingDirectory=` line.

- **`GenerateSystemdContent_WithExecReloadAndStop_EmitsDirectives`**  
  Verifies that when both `ExecReload` and `ExecStop` commands are configured, the generated output includes both `ExecReload=` and `ExecStop=` directives.

- **`GenerateSystemdContent_WithEnvironmentVariables_EmitsEnvironmentDirectives`**  
  Verifies that environment variables specified in the configuration are emitted as individual `Environment=` directives.

- **`GenerateSystemdContent_WithEnvironmentFile_EmitsEnvironmentFileDirective`**  
  Verifies that when an environment file path is configured, the output contains an `EnvironmentFile=` directive.

- **`GenerateSystemdContent_WithLimitNoFile_EmitsLimitNOFILE`**  
  Confirms that when a file descriptor limit is set, the output contains a `LimitNOFILE=` directive with the specified value.

- **`GenerateSystemdContent_WithoutLimitNoFile_OmitsLimitNOFILE`**  
  Confirms that when no file descriptor limit is set, the output does not contain a `LimitNOFILE=` line.

### Generation Tests — Install Section and Ordering

- **`GenerateSystemdContent_ContainsInstallSection`**  
  Asserts that the generated systemd unit file content includes an `[Install]` section header.

- **`GenerateSystemdContent_ContainsAllThreeSectionsInOrder`**  
  Verifies that the complete generated output contains the `[Unit]`, `[Service]`, and `[Install]` sections in that exact order, with no other sections interspersed.

## Usage

```csharp
// Example 1: Validating a minimal configuration before generation
var config = new SystemdUnitConfig
{
    UnitName = "my-service",
    ExecStart = "/usr/bin/my-app --serve"
};

// Validation should succeed
config.Validate();

string unitFileContent = config.GenerateSystemdContent();
File.WriteAllText("/etc/systemd/system/my-service.service", unitFileContent);
```

```csharp
// Example 2: Full configuration with optional directives, then asserting output
var config = new SystemdUnitConfig
{
    UnitName = "caddy-reverse-proxy",
    ExecStart = "/usr/bin/caddy run --config /etc/caddy/Caddyfile",
    ExecReload = "/usr/bin/caddy reload --config /etc/caddy/Caddyfile",
    ExecStop = "/usr/bin/caddy stop",
    Wants = new[] { "network-online.target" },
    RestartPolicy = "on-failure",
    RestartDelaySec = 5,
    WorkingDirectory = "/var/lib/caddy",
    EnvironmentVariables = new Dictionary<string, string>
    {
        { "CADDY_HOME", "/var/lib/caddy" },
        { "LOG_LEVEL", "info" }
    },
    EnvironmentFile = "/etc/caddy/env",
    LimitNOFILE = 65536
};

config.Validate();
string content = config.GenerateSystemdContent();

// content now contains [Unit], [Service], and [Install] sections in order,
// with all specified directives properly formatted.
```

## Notes

- **Validation must precede generation.** The `Validate()` method is expected to be called explicitly before `GenerateSystemdContent()`. The tests assume this contract; generation methods do not implicitly validate.
- **Section ordering is deterministic.** The `[Unit]`, `[Service]`, and `[Install]` sections always appear in that sequence. Tests enforce this invariant.
- **Directive casing.** Restart policy values are normalised to lowercase during generation. Other directive names (e.g., `ExecStart`, `Wants`) follow systemd conventions and are case-sensitive as emitted.
- **Optional directives are entirely omitted when not set.** No empty `Wants=`, `WorkingDirectory=`, `EnvironmentFile=`, or `LimitNOFILE=` lines are emitted when the corresponding configuration properties are null, empty, or zero.
- **Thread safety.** These test methods are stateless and operate on locally constructed `SystemdUnitConfig` instances. They are safe for parallel execution by test runners. The underlying `SystemdUnitConfig` class is not guaranteed to be thread-safe for concurrent mutation and generation; typical usage constructs, validates, and generates on a single thread.
