# CliCommandHandler

The `CliCommandHandler` class parses the command-line arguments of `caddy-vps-toolkit`, dispatches them to the matching service, and prints the result to the console. It is defined in `Core/CliCommandHandler.cs` in the `CaddyVpsToolkit.Core` namespace. `Program.cs` resolves it from the DI container and passes the raw `args` array to `HandleAsync`.

The class is `public sealed`. It holds no mutable state; all work is delegated to the services it receives through its constructor.

## API

### `public sealed class CliCommandHandler`
The entry point for all CLI commands. Register it as a service and resolve it once at startup. `Program.cs` registers it with `AddSingleton<CliCommandHandler>()`.

### Constructor
```csharp
public CliCommandHandler(
    ServiceManagementService serviceManager,
    HealthMonitoringService healthMonitor,
    CaddyConfigurationService caddyConfig,
    SystemdUnitService systemdUnit,
    ConfigurationService config,
    IBackupService backup,
    ILogAggregationService logAggregation,
    ISslCertificateMonitoringService sslMonitor)
```

The constructor stores each dependency in a private field. It does not check for `null`.

**Parameters:**
* `serviceManager`: Lists and loads managed services (`list-services`, `service-status`, `ssl-check`).
* `healthMonitor`: Runs health checks and builds the health summary (`health-check`, `health-summary`).
* `caddyConfig`: Stored for Caddy configuration work. No current command calls it.
* `systemdUnit`: Stored for systemd unit work. No current command calls it.
* `config`: Reads and writes configuration values (`config-get`, `config-set`).
* `backup`: Creates, restores, and lists backups (`backup-create`, `backup-restore`, `backup-list`).
* `logAggregation`: Reads aggregated log entries (`logs-view`).
* `sslMonitor`: Checks SSL certificates (`ssl-check`).

### `HandleAsync`
```csharp
public async Task HandleAsync(string[] args)
```

Runs one command. `args[0]` is the command name, and the remaining elements are its arguments. The command name is matched case-insensitively (`ToLower()`), so `VERSION` and `version` behave the same.

* **Parameters**:
  * `args`: The command-line arguments, without the program name. A `null` or empty array prints the usage text and returns.
* **Return Value**: A `Task` that completes when the command has finished printing.
* **Exit behavior**: The method does not return an exit code. It calls `Environment.Exit(1)` in these cases:
  * The command name is not recognized. `Unknown command: <name>` is written to stderr, followed by the usage text on stdout.
  * Any exception escapes the command. `[ERROR] <message>` is written to stderr.
  * `health-check` fails with a `HealthCheckException`. `Health check failed: <message>` is written to stderr.

  Because `Environment.Exit` terminates the process, code that calls `HandleAsync` in-process (for example in a test) cannot observe these failures as a return value.

## Commands

The table below lists every command that `HandleAsync` accepts. The usage text printed by a bare invocation shows the same list.

| Command | Arguments | Behavior |
|---------|-----------|----------|
| `version` | none | Prints the application name, version, author, and repository URL. |
| `help` | none | Prints a short description of the toolkit's features. |
| `list-services` | `--json` (optional) | Prints all services as a table, or as indented JSON with `--json`. |
| `service-status` | `<id>` | Prints the status, type, executable, port, and timestamps of one service. |
| `health-check` | `<id>` | Runs a health check and prints the result. |
| `health-summary` | none | Prints total, healthy, unhealthy, unchecked, and disabled counts and the healthy percentage. |
| `config-get` | `<key>` | Prints `key=value`, or a "not found" message if the key is missing. |
| `config-set` | `<key> <value>` | Stores the value and prints the new `key=value`. |
| `backup-create` | `--output <path>`, `--description <text>` (both optional) | Creates a backup and prints its path. |
| `backup-restore` | `<path>` | Restores a backup file and prints a summary of the restored manifest. |
| `backup-list` | `--dir <path>` (optional) | Lists backup files in the directory, or in the default config directory. |
| `logs-view` | `--lines N`, `--level L`, `--service ID`, `--since <date>` (all optional) | Prints aggregated log entries in a table. |
| `ssl-check` | `[<domain>]` | Checks one domain's certificate, or every enabled service's domain when no domain is given. |

### Argument checks
Commands that need a positional argument check `args.Length` before calling a service. A missing argument throws an `ArgumentException`, which is reported as `[ERROR] ...` and exits with code 1:

* `service-status` and `health-check`: `Service ID required`
* `config-get`: `Configuration key required`
* `config-set`: `Configuration key and value required`
* `backup-restore`: `Backup file path required`

### Option parsing
Options are matched case-insensitively and are read by scanning the remaining arguments in order. Unrecognized arguments are ignored.

* `backup-create` and `backup-list` scan `args[1]` up to `args[Length - 2]`. A flag placed last, with no value after it, is ignored.
* `logs-view` accepts a value only when the flag is followed by another argument. `--lines` must be a positive integer, otherwise it is ignored. `--since` is parsed with the invariant culture and converted to UTC. An unparseable `--since` is ignored.
* `ssl-check` treats the first argument after the command as a domain unless it starts with `--`.

## Usage examples

### Dispatching a command from `Program.cs`
```csharp
var services = new ServiceCollection();
ConfigureServices(services);
var serviceProvider = services.BuildServiceProvider();

var cli = serviceProvider.GetRequiredService<CliCommandHandler>();
await cli.HandleAsync(args);
```

### Listing services as JSON
```bash
caddy-vps-toolkit list-services --json
```

Calls `GetAllServicesAsync()` and writes the result with `JsonSerializer.Serialize` using `WriteIndented = true`.

### Checking the health of one service
```bash
caddy-vps-toolkit health-check 3f2c9a1e-8b7d-4c6a-9e0f-1a2b3c4d5e6f
```

```text
Health Check Result:
Status: HEALTHY
Response Time: 42ms
HTTP Status: 200
Checked At: 2026-10-25T04:30:00.0000000+00:00
```

### Creating a backup with a description
```bash
caddy-vps-toolkit backup-create --output /var/backups/toolkit.backup.json --description "before nginx removal"
```

Calls `CreateBackupAsync(outputPath, description)` and prints `Backup created: <path>`.

### Viewing recent errors for one service
```bash
caddy-vps-toolkit logs-view --lines 50 --level Error --service blog --since 2026-10-24T00:00:00
```

Builds a `LogQueryOptions` from the flags, calls `GetLogsAsync`, and prints a table with timestamp, level, source, and message. If nothing matches, it prints `No log entries found matching the given filters.`

### Checking certificates
```bash
# One domain
caddy-vps-toolkit ssl-check example.com

# Every enabled service with a public domain
caddy-vps-toolkit ssl-check
```

With a domain, the result is printed as a block with subject, issuer, issue date, and expiry date. Without one, the output is a table. If the single-domain check fails, the error is written to stderr, but the process still exits with code 0.

### Using the handler in a test
Because `HandleAsync` writes to `Console` and can call `Environment.Exit`, test only the success paths in-process. Replace the services with NSubstitute mocks and capture the output with `Console.SetOut`:

```csharp
var backup = Substitute.For<IBackupService>();
backup.ListBackupsAsync(Arg.Any<string?>())
      .Returns(new List<string> { "/var/backups/a.backup.json" });

var handler = new CliCommandHandler(
    serviceManager, healthMonitor, caddyConfig, systemdUnit,
    config, backup, logAggregation, sslMonitor);

var output = new StringWriter();
Console.SetOut(output);

await handler.HandleAsync(new[] { "backup-list", "--dir", "/var/backups" });

output.ToString().Should().Contain("Available backups (1):");
```

Do not write tests that trigger an unknown command, a missing argument, or an exception, because those paths end the test process.
