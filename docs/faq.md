# Frequently Asked Questions

## Installation & Setup

### Q: What are the system requirements?

**A:** Minimum requirements:
- Linux kernel 4.15+ (Ubuntu 18.04+, Debian 9+, CentOS 7+)
- .NET 10 runtime
- Caddy 2.7+
- 1 GB RAM
- 500 MB disk space
- systemd

Recommended for production:
- 2+ GB RAM
- 10+ GB disk space
- SSD storage for database
- Dedicated user account

### Q: Can I run caddy-vps-toolkit on non-Linux systems?

**A:** Not currently. The toolkit is optimized for Linux with tight systemd integration. The codebase is .NET and cross-platform capable, but:
- Windows/macOS support is not currently implemented
- Systemd unit generation wouldn't work on other systems
- Health monitoring assumes Linux network interfaces

Future versions may support Docker-based cross-platform deployment.

### Q: Do I need to uninstall anything before installation?

**A:** No, caddy-vps-toolkit is a new tool and doesn't conflict with existing systems. You'll need:
- Caddy already installed and running (or we'll install it)
- .NET 10 runtime
- systemd (included in modern Linux distributions)

If you're replacing an existing service management system, plan a migration period.

### Q: Can I run multiple instances of the toolkit?

**A:** Not recommended on the same VPS. The toolkit maintains a centralized SQLite database that could have locking issues with concurrent writes. For multi-server management, consider using the distributed mode (planned for v2.0).

## Configuration & Usage

### Q: How do I add an existing service to the toolkit?

**A:** If you already have a service running:

```bash
# Determine the service details
sudo systemctl status my-service
# Note the port number

# Add to toolkit
caddy-vps-toolkit service add \
  --name my-service \
  --port 8080 \
  --domain service.example.com \
  --health-check-url http://localhost:8080/health
```

### Q: What happens if a service port is already in use?

**A:** The toolkit doesn't prevent adding services with duplicate ports, but:
1. Only one service will actually bind to that port
2. Health checks will fail for both
3. Caddy reverse proxy will fail to route

Before adding a service, verify the port is available:
```bash
sudo netstat -tlnp | grep :8080
sudo lsof -i :8080
```

### Q: Can I manage services that aren't on localhost?

**A:** Yes, you can reverse proxy to remote services:

```bash
caddy-vps-toolkit service add \
  --name remote-api \
  --port 8080 \
  --domain api.example.com \
  --health-check-url http://10.0.1.5:8080/health
```

However:
- Health checks must be HTTP/HTTPS
- TCP checks are not yet supported
- Network connectivity must be established

### Q: How do I manage service dependencies?

**A:** Use the `--depends-on` flag to specify startup order:

```bash
# Create database service first
caddy-vps-toolkit service add --name database --port 5432 --domain db.example.com

# Create app service that depends on database
caddy-vps-toolkit service add \
  --name app \
  --port 3000 \
  --domain app.example.com \
  --depends-on database
```

When generating systemd units, dependencies are respected:
```bash
caddy-vps-toolkit systemd generate app
# Generated unit will have "After=database.service"
```

### Q: Can I use environment variables with services?

**A:** Yes, add environment variables when creating or updating services:

```bash
caddy-vps-toolkit service add \
  --name api \
  --port 8080 \
  --domain api.example.com \
  --env NODE_ENV=production \
  --env LOG_LEVEL=debug \
  --env DATABASE_URL=postgres://localhost/mydb
```

These are included in the generated systemd unit files.

## Health Monitoring

### Q: My health checks keep failing. Why?

**A:** Common causes:
1. **Wrong URL**: Verify with `curl -v http://localhost:PORT/health`
2. **Service not running**: `sudo systemctl status service-name`
3. **Timeout too short**: Increase with `--health-check-timeout 15s`
4. **Endpoint doesn't exist**: Check your application logs
5. **Port binding issue**: `sudo lsof -i :PORT`

Troubleshoot with:
```bash
# Manual health check with verbose output
caddy-vps-toolkit health check service-name -v

# Increase logging
caddy-vps-toolkit health check service-name --verbose 2>&1 | head -20

# Check service logs
journalctl -u service-name -n 50
```

### Q: How often are health checks performed?

**A:** Default interval is 30 seconds, configurable per service:

```bash
caddy-vps-toolkit service update api --health-check-interval 15s
```

For high-traffic systems, balance between:
- **Faster detection** (10s): More responsive, higher load
- **Standard** (30s): Recommended for most systems
- **Slower** (60s+): Lower load, delayed failure detection

### Q: What happens when a service fails a health check?

**A:** By default, the toolkit:
1. Records the failed check
2. Marks service as unhealthy
3. Retries according to `health-check-retries` (default: 3)
4. Sends webhook notification if configured
5. Optionally runs recovery script

### Q: How long are health check results kept?

**A:** The toolkit keeps unlimited historical data. For systems with thousands of services:
```bash
# Cleanup old data (manual)
caddy-vps-toolkit maintenance cleanup-health-history --older-than 90d

# Future: Schedule automated cleanup in cron
```

## Caddy Integration

### Q: Will the toolkit overwrite my existing Caddyfile?

**A:** No, by default it generates configuration and requires you to apply it:

```bash
# Preview changes
caddy-vps-toolkit caddy generate --format minimal

# Safe: append to existing file
caddy-vps-toolkit caddy generate >> /etc/caddy/Caddyfile

# Careful: replace file (backup first!)
sudo cp /etc/caddy/Caddyfile /etc/caddy/Caddyfile.backup
sudo bash -c 'caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile'

# Reload Caddy
sudo systemctl reload caddy
```

### Q: How do I add custom Caddy configuration?

**A:** The toolkit generates basic reverse proxy rules. For custom configuration:

1. Keep custom settings in a separate file:
```bash
# /etc/caddy/custom.conf
(custom_headers) {
    header X-Custom-Header "value"
    header X-Powered-By "Caddy VPS Toolkit"
}
```

2. Include in main Caddyfile:
```
import /etc/caddy/custom.conf

api.example.com {
    import custom_headers
    reverse_proxy localhost:8080
}
```

3. Generate toolkit config separately:
```bash
caddy-vps-toolkit caddy generate >> /etc/caddy/toolkit-generated.conf
# Then import in main Caddyfile
```

### Q: Can I manage HTTPS certificates with the toolkit?

**A:** The toolkit doesn't directly manage certificates, but integrates with Caddy's automatic HTTPS:

```bash
# In generated Caddyfile, toolkit creates:
api.example.com {
    reverse_proxy localhost:8080
}

# Caddy automatically:
# - Obtains certificate from Let's Encrypt
# - Renews before expiration
# - Redirects HTTP to HTTPS
```

For custom certificate management, add to Caddy config manually.

### Q: How do I use a custom TLS certificate?

**A:** Add to Caddy configuration outside of toolkit generation:

```caddy
api.example.com {
    tls /etc/ssl/certs/api.crt /etc/ssl/private/api.key
    reverse_proxy localhost:8080
}
```

The toolkit won't override manually configured TLS.

## Systemd Integration

### Q: How do I ensure services start in the correct order?

**A:** Use service dependencies:

```bash
# Define dependency chain
caddy-vps-toolkit service add --name database --port 5432 --domain db.example.com
caddy-vps-toolkit service add --name cache --port 6379 --domain cache.example.com
caddy-vps-toolkit service add --name app --port 3000 --domain app.example.com \
  --depends-on database --depends-on cache
```

Generate units with:
```bash
caddy-vps-toolkit systemd generate database
caddy-vps-toolkit systemd generate cache
caddy-vps-toolkit systemd generate app
# Generated app.service will have "After=database.service cache.service"
```

### Q: Can I customize the systemd unit files?

**A:** The toolkit generates standard unit files. For customization:

1. Generate initial file:
```bash
caddy-vps-toolkit systemd generate api > /tmp/api.service
```

2. Edit as needed:
```bash
sudo nano /etc/systemd/system/api.service
```

3. Reload systemd:
```bash
sudo systemctl daemon-reload
sudo systemctl start api
```

Any manual edits will persist across toolkit updates.

### Q: How do I handle graceful shutdown of services?

**A:** Configure in systemd unit:

```bash
caddy-vps-toolkit systemd generate api \
  > /etc/systemd/system/api.service
```

Edit `/etc/systemd/system/api.service`:
```ini
[Service]
KillMode=mixed
KillSignal=SIGTERM
TimeoutStopSec=30
```

## Backup & Recovery

### Q: How do I backup my configuration?

**A:** Export all services to file:

```bash
# JSON format
caddy-vps-toolkit config export --format json > backup.json

# YAML format
caddy-vps-toolkit config export --format yaml > backup.yaml

# With history
caddy-vps-toolkit config export --format json --include-history > backup-with-history.json
```

Also backup the database:
```bash
cp ~/.caddy-vps-toolkit/services.db ~/backup.db
```

### Q: How do I restore configuration?

**A:** Import from backup:

```bash
# Ensure database is initialized
caddy-vps-toolkit config init

# Import configuration
caddy-vps-toolkit config import --file backup.json --format json

# Verify
caddy-vps-toolkit service list
```

### Q: Can I migrate to a different server?

**A:** Yes, export and import:

On old server:
```bash
caddy-vps-toolkit config export --format json > migration.json
```

On new server:
```bash
caddy-vps-toolkit config init
caddy-vps-toolkit config import --file migration.json
```

Then regenerate Caddy and systemd configurations on new server.

## Troubleshooting

### Q: I get "permission denied" errors

**A:** Ensure proper permissions:

```bash
# Toolkit needs write access to config directory
ls -la ~/.caddy-vps-toolkit/
# Should be: drwx------ (700)

# Fix permissions
chmod 700 ~/.caddy-vps-toolkit

# For systemd operations, use sudo
sudo caddy-vps-toolkit systemd generate api > /etc/systemd/system/api.service

# For Caddy operations, ensure Caddy can be reloaded
sudo systemctl reload caddy
```

### Q: Database is locked

**A:** SQLite locks prevent concurrent writes:

```bash
# Stop all toolkit operations
sudo systemctl stop caddy-toolkit-monitor

# Wait 30 seconds
sleep 30

# Check for locked database
lsof /root/.caddy-vps-toolkit/services.db

# Restart
sudo systemctl start caddy-toolkit-monitor
```

Prevent in high-concurrency scenarios by limiting background workers.

### Q: Services not showing in health status

**A:** Verify services exist and are enabled:

```bash
# List all services
caddy-vps-toolkit service list

# Check specific service
caddy-vps-toolkit service show service-name

# Ensure health monitoring is running
sudo systemctl status caddy-toolkit-monitor

# Check logs for errors
sudo journalctl -u caddy-toolkit-monitor -n 50
```

### Q: Configuration validation fails

**A:** Debug configuration issues:

```bash
# Validate configuration
caddy-vps-toolkit config validate -v

# Check specific service
caddy-vps-toolkit service show service-name

# Validate Caddyfile
caddy-vps-toolkit caddy validate --file /etc/caddy/Caddyfile -v
```

## Performance & Optimization

### Q: How many services can the toolkit manage?

**A:** Theoretically unlimited, practically:
- **Single VPS**: 20-30 active services recommended
- **Database size**: ~500KB per service (with history)
- **Health check overhead**: 1-5% CPU with 30s interval

For 50+ services, consider:
- Increasing health check interval (60s)
- Distributed mode (v2.0 planned)
- Dedicated monitoring system

### Q: How do I reduce CPU usage?

**A:** Tune health checks:

```bash
# Increase interval (default 30s)
caddy-vps-toolkit service update api --health-check-interval 60s

# Increase timeout (reduces retries)
caddy-vps-toolkit service update api --health-check-timeout 10s

# Reduce retries (default 3)
# (Modify directly in database or future UI)
```

### Q: How do I monitor resource usage?

**A:** Check metrics:

```bash
# Prometheus-compatible endpoint
curl http://localhost:9090/metrics | grep caddy_vps

# System resource usage
ps aux | grep caddy-vps-toolkit
top -p $(pgrep -f caddy-vps-toolkit)

# Database size
du -h ~/.caddy-vps-toolkit/services.db
```

## Getting Help

### Q: Where do I report bugs?

**A:** File issues on GitHub:
https://github.com/Sarmkadan/caddy-vps-toolkit/issues

Include:
- caddy-vps-toolkit version (`caddy-vps-toolkit --version`)
- Linux distribution and version
- Reproduction steps
- Logs (`journalctl -u caddy-toolkit-monitor`)

### Q: How do I request features?

**A:** Create feature requests on GitHub Discussions:
https://github.com/Sarmkadan/caddy-vps-toolkit/discussions

### Q: Where's the community?

**A:** Connect via:
- GitHub Issues & Discussions
- Personal contact: https://sarmkadan.com

## Additional Resources

- [Getting Started Guide](./getting-started.md)
- [API Reference](./api-reference.md)
- [Deployment Guide](./deployment.md)
- [Architecture Deep Dive](./architecture-deep-dive.md)
- [GitHub Repository](https://github.com/Sarmkadan/caddy-vps-toolkit)
