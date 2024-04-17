# SystemdUnitConfig

`SystemdUnitConfig` represents the declarative specification for a systemd service unit. It holds all the properties required to generate a valid `.service` unit file, including execution commands, process credentials, restart behaviour, environment configuration, and ordering dependencies. Instances of this type are consumed by the toolkit’s unit-file writer to produce the final on-disk unit definition.

## API

### `public string Id`
A unique identifier for the configuration object within the toolkit. This is not the systemd unit name; it is used internally for lookups and correlation.

### `public string ServiceId`
The short service identifier, typically derived from the application or role name (e.g. `caddy`). Combined with a suffix to form the full unit name.

### `public string UnitName`
The full systemd unit name as it will appear on disk and in `systemctl` commands (e.g. `caddy.service`). Must end with `.service`.

### `public string ExecStart`
The command line executed when the service starts. Passed directly to `ExecStart=` in the generated unit. Must be an absolute path or a single command token; shell syntax is not interpreted unless a shell is explicitly invoked.

### `public string ExecReload`
The command line executed on `systemctl reload`. Maps to `ExecReload=`. Can be `null` or empty if the service does not support reload.

### `public string ExecStop`
The command line executed when the service is stopped. Maps to `ExecStop=`. If omitted, systemd sends `SIGTERM` followed by `SIGKILL` after a timeout.

### `public string User`
The Unix user under which the service processes run. Maps to `User=`. An empty string implies systemd’s default (typically `root`).

### `public string Group`
The Unix group under which the service processes run. Maps to `Group=`. An empty string implies the default group for the configured user.

### `public RestartPolicy RestartPolicy`
An enumeration specifying the restart behaviour. Serialised to `Restart=`. Common values include `no`, `on-failure`, `always`, and `on-abnormal`.

### `public int RestartDelaySeconds`
The time in seconds systemd waits before restarting the service. Maps to `RestartSec=`. Only meaningful when `RestartPolicy` permits restarts.

### `public int StartLimitIntervalSec`
The interval in seconds over which the start limit burst count is evaluated. Maps to `StartLimitIntervalSec=`.

### `public int StartLimitBurst`
The maximum number of start attempts allowed within `StartLimitIntervalSec`. Maps to `StartLimitBurst=`. Exceeding this count causes systemd to refuse further automatic starts.

### `public string Type`
The systemd service type. Maps to `Type=`. Typical values are `simple`, `forking`, `oneshot`, `notify`, or `idle`. An empty string defaults to `simple`.

### `public string StandardOutput`
Controls where stdout is sent. Maps to `StandardOutput=`. Common values: `journal`, `syslog`, `null`, `file:/path`.

### `public string StandardError`
Controls where stderr is sent. Maps to `StandardError=`. Accepts the same values as `StandardOutput`.

### `public Dictionary<string, string> Environment`
A dictionary of environment variables injected into the service process. Each entry produces an `Environment="KEY=VALUE"` line. Keys and values are written verbatim; no shell expansion occurs.

### `public List<string> EnvironmentFiles`
A list of absolute file paths read by systemd to load additional environment variables. Each entry produces an `EnvironmentFile=` directive. Files are read in order; later files override earlier ones for duplicate keys.

### `public List<string> After`
Unit names that this service should start *after*. Produces `After=` directives. Does not imply a requirement dependency—only ordering.

### `public List<string> Wants`
Unit names that this service *wants* to have started. Produces `Wants=` directives. Failure of a wanted unit does not prevent this service from starting.

### `public string WorkingDirectory`
The working directory for the executed processes. Maps to `WorkingDirectory=`. Must be an absolute path. An empty string means systemd’s default (typically the root directory).

## Usage

### Example 1: Basic long-running service with restart

```csharp
var config = new SystemdUnitConfig
{
    Id = "caddy-primary",
    ServiceId = "caddy",
    UnitName = "caddy.service",
    ExecStart = "/usr/bin/caddy run --config /etc/caddy/Caddyfile",
    ExecReload = "/usr/bin/caddy reload --config /etc/caddy/Caddyfile",
    ExecStop = "/usr/bin/caddy stop",
    User = "caddy",
    Group = "caddy",
    RestartPolicy = RestartPolicy.OnFailure,
    RestartDelaySeconds = 5,
    StartLimitIntervalSec = 60,
    StartLimitBurst = 3,
    Type = "notify",
    StandardOutput = "journal",
    StandardError = "journal",
    WorkingDirectory = "/var/lib/caddy",
    After = new List<string> { "network-online.target" },
    Wants = new List<string> { "network-online.target" }
};

// The toolkit serialises this to /etc/systemd/system/caddy.service
```

### Example 2: Environment-injected worker with environment files

```csharp
var config = new SystemdUnitConfig
{
    Id = "worker-queue",
    ServiceId = "worker",
    UnitName = "worker-queue.service",
    ExecStart = "/opt/app/worker",
    User = "appuser",
    Group = "appgroup",
    RestartPolicy = RestartPolicy.Always,
    RestartDelaySeconds = 10,
    Type = "simple",
    Environment = new Dictionary<string, string>
    {
        ["QUEUE_URL"] = "amqp://localhost:5672",
        ["LOG_LEVEL"] = "info"
    },
    EnvironmentFiles = new List<string>
    {
        "/etc/worker/env",
        "/etc/worker/secrets.env"
    },
    After = new List<string> { "rabbitmq-server.service" },
    Wants = new List<string> { "rabbitmq-server.service" }
};
```

## Notes

- All string properties that map to systemd directives are written verbatim. No escaping or transformation is applied by the configuration object itself; the serialisation layer is responsible for quoting where systemd syntax requires it.
- `RestartDelaySeconds`, `StartLimitIntervalSec`, and `StartLimitBurst` are plain integers. Negative values are not validated at this level and will produce invalid unit files if passed through to serialisation.
- `Environment` dictionary keys and values are not checked for characters that are illegal in systemd environment specifications (e.g. unescaped `$` or newlines). Callers must ensure validity.
- `After` and `Wants` lists may contain unit names with or without suffixes. The serialiser does not append `.service` automatically; provide the full unit name if targeting a specific type.
- This type is a plain data container. It is not thread-safe for concurrent mutation. If multiple threads modify lists or the dictionary while another thread reads or serialises the object, the behaviour is undefined. Synchronisation is the caller’s responsibility.
- Setting `ExecReload` or `ExecStop` to `null` or empty signals that the directive should be omitted. The systemd default behaviour then applies (no reload command; default stop signal sequence).
- `WorkingDirectory` must be an absolute path if set. Relative paths are not rejected by this type but will cause systemd to fail unit loading.
