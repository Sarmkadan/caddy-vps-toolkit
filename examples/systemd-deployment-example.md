# Systemd Deployment Example

Complete guide for creating and managing systemd units with caddy-vps-toolkit.

## Overview

The toolkit generates systemd unit files that integrate with the standard Linux service management system. This guide shows how to properly deploy services using systemd.

## Example 1: Simple Web Application

### Generate Unit File

```bash
caddy-vps-toolkit systemd generate web-app
```

### Output

```ini
[Unit]
Description=Service: web-app
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=/opt/web-app/web-app
Restart=on-failure
RestartSec=5
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

### Deploy

```bash
# Create systemd unit file
sudo bash -c 'caddy-vps-toolkit systemd generate web-app > /etc/systemd/system/web-app.service'

# Reload systemd daemon
sudo systemctl daemon-reload

# Enable at startup
sudo systemctl enable web-app

# Start service
sudo systemctl start web-app

# Verify status
sudo systemctl status web-app

# View logs
sudo journalctl -u web-app -n 50
```

## Example 2: Service with Dependencies

### Scenario
- API depends on PostgreSQL and Redis
- Ensure startup order

### Generate Units

```bash
# Database service (no dependencies)
caddy-vps-toolkit systemd generate postgres \
  --restart-policy on-failure > /tmp/postgres.service

# Cache service (no dependencies)
caddy-vps-toolkit systemd generate redis \
  --restart-policy on-failure > /tmp/redis.service

# API service (depends on both)
caddy-vps-toolkit systemd generate api \
  --depends-on postgres \
  --depends-on redis \
  --restart-policy on-failure > /tmp/api.service
```

### Output (api.service)

```ini
[Unit]
Description=Service: api
After=postgres.service redis.service
Requires=postgres.service redis.service
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

### Deploy

```bash
# Install all units
for service in postgres redis api; do
    sudo bash -c "caddy-vps-toolkit systemd generate $service > /etc/systemd/system/$service.service"
done

# Reload systemd
sudo systemctl daemon-reload

# Enable all services
sudo systemctl enable postgres redis api

# Start services (in order)
sudo systemctl start postgres
sudo systemctl start redis
sudo systemctl start api

# Verify order
sudo systemctl list-dependencies --reverse api
```

## Example 3: Advanced Configuration

### Custom Restart Policy

```bash
# Service restarts up to 5 times, with 10-second delay
caddy-vps-toolkit systemd generate critical-app \
  --restart-policy on-failure \
  --max-restarts 5 \
  --restart-delay 10s > /tmp/critical-app.service
```

### Output

```ini
[Unit]
Description=Service: critical-app
After=network-online.target

[Service]
Type=simple
ExecStart=/opt/critical-app/critical-app
Restart=on-failure
RestartSec=10
StartLimitBurst=5
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

### Deploy and Test

```bash
# Deploy
sudo bash -c 'caddy-vps-toolkit systemd generate critical-app \
  --restart-policy on-failure \
  --max-restarts 5 \
  --restart-delay 10s > /etc/systemd/system/critical-app.service'

sudo systemctl daemon-reload
sudo systemctl enable critical-app
sudo systemctl start critical-app

# Test restart behavior
sudo systemctl stop critical-app
sudo systemctl start critical-app

# Monitor restarts
sudo journalctl -u critical-app -f
```

## Example 4: Service Group Management

### Create Target Unit

```bash
# Group related services
sudo tee /etc/systemd/system/web-stack.target > /dev/null <<EOF
[Unit]
Description=Web Stack Services
Wants=postgres.service redis.service api.service web.service

[Install]
WantedBy=multi-user.target
EOF
```

### Use the Target

```bash
# Start entire stack
sudo systemctl start web-stack.target

# Check all services
sudo systemctl list-dependencies web-stack.target

# Stop entire stack
sudo systemctl stop web-stack.target
```

## Example 5: Environment Variables

### Option A: In systemd Unit

```bash
# Create unit with environment variables
sudo tee /etc/systemd/system/app.service > /dev/null <<EOF
[Unit]
Description=Service: app
After=network-online.target

[Service]
Type=simple
ExecStart=/opt/app/app
Environment="NODE_ENV=production"
Environment="LOG_LEVEL=info"
Environment="DATABASE_URL=postgresql://localhost/mydb"
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl start app
```

### Option B: From Configuration File

```bash
# Create environment file
sudo tee /etc/default/app > /dev/null <<EOF
NODE_ENV=production
LOG_LEVEL=info
DATABASE_URL=postgresql://localhost/mydb
EOF

# Create systemd unit
sudo tee /etc/systemd/system/app.service > /dev/null <<EOF
[Unit]
Description=Service: app
After=network-online.target

[Service]
Type=simple
ExecStart=/opt/app/app
EnvironmentFile=/etc/default/app
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl start app
```

## Example 6: Resource Limits

```bash
# Create unit with resource constraints
sudo tee /etc/systemd/system/heavy-worker.service > /dev/null <<EOF
[Unit]
Description=Service: heavy-worker
After=network-online.target

[Service]
Type=simple
ExecStart=/opt/worker/worker
Restart=on-failure

# CPU and Memory limits
CPUQuota=50%
MemoryLimit=512M
MemoryMax=1G

# File descriptor limits
LimitNOFILE=10000
LimitNPROC=10000

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl start heavy-worker
```

## Example 7: Multiple Instances

### Using Service Templates

```bash
# Create template unit
sudo tee /etc/systemd/system/worker@.service > /dev/null <<EOF
[Unit]
Description=Worker Instance %i
After=network-online.target

[Service]
Type=simple
ExecStart=/opt/worker/worker --instance=%i
Environment="INSTANCE=%i"
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF

# Start multiple instances
sudo systemctl daemon-reload
sudo systemctl enable worker@1 worker@2 worker@3
sudo systemctl start worker@1 worker@2 worker@3

# Check all instances
sudo systemctl list-units worker@*

# Monitor logs from all instances
sudo journalctl -u worker@\*.service -f
```

## Management Operations

### Start/Stop Services

```bash
# Start single service
sudo systemctl start api

# Stop service
sudo systemctl stop api

# Restart service
sudo systemctl restart api

# Reload configuration (if supported by service)
sudo systemctl reload api

# Start all web-stack services
sudo systemctl start web-stack.target
```

### Check Status

```bash
# View service status
sudo systemctl status api

# Check if enabled
sudo systemctl is-enabled api

# List all toolkit-managed services
sudo systemctl list-units --pattern '*.service' | grep "web-\|api\|db"

# Check failed services
sudo systemctl list-units --state=failed
```

### View Logs

```bash
# Recent logs
sudo journalctl -u api -n 50

# Live logs
sudo journalctl -u api -f

# Logs from specific time
sudo journalctl -u api --since "30 minutes ago"

# JSON format for parsing
sudo journalctl -u api -o json | jq '.MESSAGE'

# Logs for entire stack
sudo journalctl -u web-stack.target -n 100
```

### Enable/Disable Services

```bash
# Enable at startup
sudo systemctl enable api

# Disable from startup (but keep running)
sudo systemctl disable api --no-reload

# Check if enabled
sudo systemctl is-enabled api

# Enable multiple services
sudo systemctl enable postgres redis api
```

## Troubleshooting

### Service Won't Start

```bash
# Check unit file syntax
sudo systemd-analyze verify /etc/systemd/system/api.service

# View error details
sudo journalctl -u api -n 20

# Check dependencies are running
sudo systemctl list-dependencies api

# Try manual start with verbose output
sudo systemctl start api -vvv
```

### Port Already in Use

```bash
# Find process using port
sudo lsof -i :8080

# Kill existing process
sudo kill -9 PID

# Or restart the service
sudo systemctl restart api
```

### Resource Limits Exceeded

```bash
# Check memory usage
sudo systemctl status api

# View resource limits
cat /proc/$(systemctl show -p MainPID --value api)/limits

# Increase limits in unit file
sudo nano /etc/systemd/system/api.service
# Edit MemoryLimit value
```

### Dependency Issues

```bash
# View dependency tree
sudo systemctl list-dependencies api

# Check if dependency is running
sudo systemctl is-active postgres

# Force dependency restart
sudo systemctl restart postgres
sudo systemctl restart api
```

## Monitoring Script

```bash
#!/bin/bash
# Monitor all toolkit-managed services

SERVICES=("postgres" "redis" "api" "web")

echo "Service Status Report"
echo "===================="

for service in "${SERVICES[@]}"; do
    status=$(sudo systemctl is-active $service)
    enabled=$(sudo systemctl is-enabled $service)
    restarts=$(sudo journalctl -u $service -p 6 | wc -l)
    
    echo "$service:"
    echo "  Status: $status"
    echo "  Enabled: $enabled"
    echo "  Recent restarts: $restarts"
done

echo ""
echo "Failed services:"
sudo systemctl list-units --state=failed

echo ""
echo "Memory usage:"
ps aux | grep -E "postgres|redis|api|web" | grep -v grep
```

## Best Practices

1. **Always reload systemd:** `sudo systemctl daemon-reload` after changes
2. **Test before enabling:** Start and verify before `enable`
3. **Monitor logs:** Keep terminal open during development with `journalctl -f`
4. **Use dependencies:** Define service order with `Depends-On` and `After`
5. **Set restart policy:** Handle failures gracefully with `Restart=on-failure`
6. **Resource limits:** Set memory/CPU limits to prevent resource exhaustion
7. **Environment files:** Keep sensitive variables in separate files
8. **Regular backups:** Backup unit file changes
9. **Documentation:** Comment unit files with purpose and configuration
10. **Gradual rollout:** Test changes on non-critical services first

## Quick Reference

```bash
# Generate and deploy unit
caddy-vps-toolkit systemd generate SERVICE_NAME \
  --depends-on DEPENDENCY \
  --restart-policy on-failure \
  | sudo tee /etc/systemd/system/SERVICE_NAME.service

# Apply changes
sudo systemctl daemon-reload
sudo systemctl enable SERVICE_NAME
sudo systemctl start SERVICE_NAME

# Check status
sudo systemctl status SERVICE_NAME

# View logs
sudo journalctl -u SERVICE_NAME -f

# Troubleshoot
sudo systemd-analyze verify /etc/systemd/system/SERVICE_NAME.service
```
