# API Reference

Complete reference for all CLI commands and API methods in caddy-vps-toolkit.

## Service Commands

### service add

Creates a new managed service.

**Usage**
```bash
caddy-vps-toolkit service add [OPTIONS]
```

**Options**
```
--name, -n <NAME>              Service name (required, alphanumeric + hyphens)
--port <PORT>                  Port number (required, 1-65535)
--domain <DOMAIN>              Domain name (required)
--health-check-url <URL>       Health check endpoint (optional)
--health-check-interval <DUR>  Check interval (default: 30s)
--health-check-timeout <DUR>   Check timeout (default: 5s)
--health-check-retries <N>     Retry count (default: 3)
--env <KEY=VALUE>              Environment variable (repeatable)
--depends-on <SERVICE>         Service dependency (repeatable)
--restart-policy <POLICY>      on-failure|always|no (default: on-failure)
--enabled <BOOL>               Enable/disable service (default: true)
```

**Examples**
```bash
# Simple service
caddy-vps-toolkit service add --name api --port 8080 --domain api.example.com

# With health check
caddy-vps-toolkit service add \
  --name web \
  --port 3000 \
  --domain web.example.com \
  --health-check-url http://localhost:3000/health \
  --health-check-interval 15s

# With dependencies and environment
caddy-vps-toolkit service add \
  --name app \
  --port 5000 \
  --domain app.example.com \
  --depends-on database \
  --env NODE_ENV=production \
  --env LOG_LEVEL=info
```

**Return Value**
```json
{
  "success": true,
  "message": "Service 'api' created successfully",
  "service": {
    "id": 1,
    "name": "api",
    "port": 8080,
    "domain": "api.example.com",
    "enabled": true,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### service list

Lists all managed services.

**Usage**
```bash
caddy-vps-toolkit service list [OPTIONS]
```

**Options**
```
--format <FORMAT>    json|yaml|table|csv (default: table)
--filter <FILTER>    Filter expression (e.g., "enabled=true")
--limit <N>          Maximum results (default: 100)
--offset <N>         Skip N results (default: 0)
```

**Examples**
```bash
# Default table format
caddy-vps-toolkit service list

# JSON format
caddy-vps-toolkit service list --format json

# Filter enabled services
caddy-vps-toolkit service list --filter "enabled=true"

# YAML with limit
caddy-vps-toolkit service list --format yaml --limit 50
```

**Output (table format)**
```
NAME      PORT  DOMAIN                STATUS
api       8080  api.example.com       Healthy
web       3000  web.example.com       Healthy
cache     6379  cache.example.com     Unhealthy
```

### service show

Displays detailed information about a specific service.

**Usage**
```bash
caddy-vps-toolkit service show <NAME>
```

**Examples**
```bash
caddy-vps-toolkit service show api
```

**Output**
```json
{
  "id": 1,
  "name": "api",
  "port": 8080,
  "domain": "api.example.com",
  "healthCheckUrl": "http://localhost:8080/health",
  "healthCheckInterval": "30s",
  "healthCheckTimeout": "5s",
  "healthCheckRetries": 3,
  "environment": {
    "NODE_ENV": "production"
  },
  "dependencies": [],
  "restartPolicy": "on-failure",
  "enabled": true,
  "status": "Healthy",
  "lastHealthCheck": "2024-01-15T10:35:00Z",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### service update

Updates service configuration.

**Usage**
```bash
caddy-vps-toolkit service update <NAME> [OPTIONS]
```

**Options**
```
--port <PORT>                  New port
--domain <DOMAIN>              New domain
--health-check-url <URL>       New health check URL
--health-check-interval <DUR>  New check interval
--health-check-timeout <DUR>   New check timeout
--health-check-retries <N>     New retry count
--env <KEY=VALUE>              Update environment variable
--depends-on <SERVICE>         Update dependency
--restart-policy <POLICY>      Update restart policy
--enabled <BOOL>               Enable/disable
```

**Examples**
```bash
caddy-vps-toolkit service update api --port 8081

caddy-vps-toolkit service update api \
  --health-check-interval 15s \
  --enabled false
```

### service remove

Removes a service.

**Usage**
```bash
caddy-vps-toolkit service remove <NAME> [OPTIONS]
```

**Options**
```
--force, -f    Skip confirmation prompt
```

**Examples**
```bash
caddy-vps-toolkit service remove old-service
caddy-vps-toolkit service remove old-service --force
```

## Caddy Commands

### caddy generate

Generates Caddyfile configuration from services.

**Usage**
```bash
caddy-vps-toolkit caddy generate [OPTIONS]
```

**Options**
```
--format <FORMAT>      full|minimal (default: full)
--services <NAMES>     Filter to specific services (comma-separated)
--output <FILE>        Write to file instead of stdout
--include-comments     Include configuration comments
--validate             Validate output before returning
```

**Examples**
```bash
# Generate and display
caddy-vps-toolkit caddy generate

# Generate and write to file
caddy-vps-toolkit caddy generate --output /tmp/Caddyfile

# Minimal format (comments removed)
caddy-vps-toolkit caddy generate --format minimal

# Filter to specific services
caddy-vps-toolkit caddy generate --services api,web
```

**Output (example)**
```
# Reverse proxy for api service
api.example.com {
    reverse_proxy localhost:8080 {
        health_path /health
        health_interval 30s
        health_timeout 5s
    }
}

# Reverse proxy for web service
web.example.com {
    reverse_proxy localhost:3000
}
```

### caddy validate

Validates Caddy configuration syntax.

**Usage**
```bash
caddy-vps-toolkit caddy validate [OPTIONS]
```

**Options**
```
--file <FILE>    File to validate (stdin if not provided)
--verbose, -v    Show detailed validation output
```

**Examples**
```bash
# Validate generated config
caddy-vps-toolkit caddy generate | caddy-vps-toolkit caddy validate

# Validate existing file
caddy-vps-toolkit caddy validate --file /etc/caddy/Caddyfile

# With detailed output
caddy-vps-toolkit caddy validate --file /etc/caddy/Caddyfile -v
```

### caddy preview

Shows configuration for a specific service.

**Usage**
```bash
caddy-vps-toolkit caddy preview <SERVICE_NAME>
```

**Examples**
```bash
caddy-vps-toolkit caddy preview api
```

**Output**
```
# Service: api
api.example.com {
    reverse_proxy localhost:8080 {
        health_path /health
        health_interval 30s
        health_timeout 5s
    }
}
```

## Systemd Commands

### systemd generate

Generates systemd unit file for a service.

**Usage**
```bash
caddy-vps-toolkit systemd generate <SERVICE_NAME> [OPTIONS]
```

**Options**
```
--output <FILE>           Write to file instead of stdout
--restart-policy <POLICY> on-failure|always|no (default: on-failure)
--max-restarts <N>        Max restart count (with on-failure policy)
--restart-delay <DUR>     Delay between restarts (default: 5s)
--depends-on <SERVICE>    Add dependency (repeatable)
--type <TYPE>             notify|simple|forking (default: simple)
```

**Examples**
```bash
# Generate unit file
caddy-vps-toolkit systemd generate api

# Write to systemd directory
sudo bash -c 'caddy-vps-toolkit systemd generate api > /etc/systemd/system/api.service'

# With dependencies
caddy-vps-toolkit systemd generate api \
  --depends-on database \
  --depends-on cache \
  --restart-policy on-failure

# Custom restart configuration
caddy-vps-toolkit systemd generate api \
  --restart-policy on-failure \
  --max-restarts 5 \
  --restart-delay 10s
```

**Output (example)**
```ini
[Unit]
Description=Service: api
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=/opt/api/api
Restart=on-failure
RestartSec=5
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

### systemd list

Lists all systemd units managed by the toolkit.

**Usage**
```bash
caddy-vps-toolkit systemd list [OPTIONS]
```

**Options**
```
--format <FORMAT>    json|yaml|table (default: table)
--state <STATE>      running|stopped|failed
```

**Examples**
```bash
caddy-vps-toolkit systemd list
caddy-vps-toolkit systemd list --state running
caddy-vps-toolkit systemd list --format json
```

### systemd status

Shows status of a specific systemd unit.

**Usage**
```bash
caddy-vps-toolkit systemd status <SERVICE_NAME>
```

**Examples**
```bash
caddy-vps-toolkit systemd status api
```

**Output (example)**
```
Service: api
Status: active (running)
PID: 1234
Restart Count: 0
Memory: 45.2 MB
CPU: 2.1%
Uptime: 2h 15m
Last Start: 2024-01-15 10:30:00
```

## Health Commands

### health check

Performs immediate health check on a service.

**Usage**
```bash
caddy-vps-toolkit health check <SERVICE_NAME> [OPTIONS]
```

**Options**
```
--verbose, -v        Show detailed check output
--timeout <DUR>      Custom timeout for this check
```

**Examples**
```bash
caddy-vps-toolkit health check api
caddy-vps-toolkit health check api -v
caddy-vps-toolkit health check api --timeout 10s
```

**Output**
```json
{
  "serviceName": "api",
  "isHealthy": true,
  "httpStatusCode": 200,
  "responseTime": "125ms",
  "message": "Service is healthy",
  "checkedAt": "2024-01-15T10:35:00Z"
}
```

### health monitor

Starts continuous health monitoring.

**Usage**
```bash
caddy-vps-toolkit health monitor [OPTIONS]
```

**Options**
```
--interval <DUR>       Check interval (default: 30s)
--service <NAME>       Monitor specific service
--webhook-url <URL>    Send notifications to webhook
--log-file <FILE>      Write results to file
```

**Examples**
```bash
# Monitor all services every 30 seconds
caddy-vps-toolkit health monitor

# Monitor single service with 15s interval
caddy-vps-toolkit health monitor --service api --interval 15s

# With webhook notifications
caddy-vps-toolkit health monitor \
  --interval 30s \
  --webhook-url https://hooks.example.com/health
```

### health history

Shows health check history for a service.

**Usage**
```bash
caddy-vps-toolkit health history <SERVICE_NAME> [OPTIONS]
```

**Options**
```
--limit <N>         Number of results (default: 50)
--start-time <TIME> Start timestamp
--end-time <TIME>   End timestamp
--unhealthy-only    Show only failed checks
```

**Examples**
```bash
caddy-vps-toolkit health history api
caddy-vps-toolkit health history api --limit 100
caddy-vps-toolkit health history api --unhealthy-only
caddy-vps-toolkit health history api --start-time "2024-01-15T10:00:00Z"
```

**Output (example)**
```
SERVICE  TIME                      STATUS      RESPONSE TIME
api      2024-01-15T10:35:00Z      Healthy     125ms
api      2024-01-15T10:34:30Z      Healthy     142ms
api      2024-01-15T10:34:00Z      Unhealthy   5000ms
api      2024-01-15T10:33:30Z      Healthy     138ms
```

### health summary

Shows overall health summary for all services.

**Usage**
```bash
caddy-vps-toolkit health summary [OPTIONS]
```

**Options**
```
--format <FORMAT>    json|yaml|table (default: table)
```

**Examples**
```bash
caddy-vps-toolkit health summary
caddy-vps-toolkit health summary --format json
```

**Output**
```
Total Services: 3
Healthy: 2
Unhealthy: 1
Average Response Time: 128ms
Last Updated: 2024-01-15T10:35:00Z
```

## Configuration Commands

### config init

Initializes configuration database.

**Usage**
```bash
caddy-vps-toolkit config init [OPTIONS]
```

**Options**
```
--path <PATH>       Custom config directory (default: ~/.caddy-vps-toolkit)
--reset             Reset existing configuration
```

**Examples**
```bash
caddy-vps-toolkit config init
caddy-vps-toolkit config init --path /opt/caddy-toolkit
caddy-vps-toolkit config init --reset
```

### config export

Exports configuration to file.

**Usage**
```bash
caddy-vps-toolkit config export [OPTIONS]
```

**Options**
```
--format <FORMAT>  json|yaml (default: json)
--output <FILE>    Output file path
--include-history  Include audit log
```

**Examples**
```bash
caddy-vps-toolkit config export --format json > backup.json
caddy-vps-toolkit config export --format yaml --output backup.yaml
caddy-vps-toolkit config export --format json --include-history > backup-with-history.json
```

### config import

Imports configuration from file.

**Usage**
```bash
caddy-vps-toolkit config import --file <FILE> [OPTIONS]
```

**Options**
```
--file <FILE>      File to import from (required)
--format <FORMAT>  json|yaml (auto-detected if not provided)
--merge            Merge with existing config instead of replace
```

**Examples**
```bash
caddy-vps-toolkit config import --file backup.json
caddy-vps-toolkit config import --file backup.yaml --merge
```

### config validate

Validates configuration.

**Usage**
```bash
caddy-vps-toolkit config validate [OPTIONS]
```

**Options**
```
--file <FILE>    File to validate (uses current config if not provided)
--verbose, -v    Show detailed validation output
```

**Examples**
```bash
caddy-vps-toolkit config validate
caddy-vps-toolkit config validate --file backup.json -v
```

## Audit Commands

### audit log

Views audit log entries.

**Usage**
```bash
caddy-vps-toolkit audit log [OPTIONS]
```

**Options**
```
--limit <N>          Number of results (default: 50)
--service <NAME>     Filter by service
--action <ACTION>    Filter by action (add, update, delete, health_check)
--start-time <TIME>  Start timestamp
--end-time <TIME>    End timestamp
--format <FORMAT>    json|yaml|table (default: table)
```

**Examples**
```bash
caddy-vps-toolkit audit log
caddy-vps-toolkit audit log --limit 100
caddy-vps-toolkit audit log --service api
caddy-vps-toolkit audit log --action delete --start-time "2024-01-01"
caddy-vps-toolkit audit log --service api --action update --format json
```

**Output**
```
TIMESTAMP                  SERVICE  ACTION  DETAILS
2024-01-15T10:35:00Z       api      update  port changed from 8080 to 8081
2024-01-15T10:30:00Z       api      add     Service created
2024-01-15T10:29:00Z       web      delete  Service removed
```

### audit export

Exports audit log to file.

**Usage**
```bash
caddy-vps-toolkit audit export [OPTIONS]
```

**Options**
```
--format <FORMAT>    json|csv (default: json)
--output <FILE>      Output file path (required)
--start-time <TIME>  Start timestamp
--end-time <TIME>    End timestamp
```

**Examples**
```bash
caddy-vps-toolkit audit export --format json --output audit.json
caddy-vps-toolkit audit export --format csv --output audit.csv \
  --start-time "2024-01-01" --end-time "2024-01-31"
```

## Global Options

All commands support these options:

```
--config <FILE>    Path to config file (default: ~/.caddy-vps-toolkit/config.json)
--verbose, -v      Enable verbose output
--quiet, -q        Suppress non-error output
--format <FORMAT>  Override output format
--output <FILE>    Write to file instead of stdout
--help, -h         Show command help
--version          Show version
```

## Exit Codes

- `0`: Success
- `1`: General error
- `2`: Invalid arguments
- `3`: Service not found
- `4`: Permission denied
- `5`: Configuration error
- `6`: Database error
- `7`: Network error (health check timeout)
