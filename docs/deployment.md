# Deployment Guide

Production deployment guidelines for caddy-vps-toolkit.

## Pre-Deployment Checklist

- [ ] Linux VPS with minimum 1GB RAM, 10GB disk
- [ ] Caddy 2.7+ installed and running
- [ ] .NET 10 runtime installed
- [ ] systemd available and functional
- [ ] Firewall rules allowing needed ports
- [ ] Regular backup strategy planned
- [ ] Monitoring system in place
- [ ] Team trained on toolkit usage

## System Preparation

### 1. Create Dedicated User

```bash
# Create user for toolkit
sudo useradd -m -s /bin/bash -d /home/caddy-toolkit caddy-toolkit

# Add to necessary groups
sudo usermod -aG docker caddy-toolkit  # If using Docker
sudo usermod -aG caddy caddy-toolkit   # If sharing Caddy

# Set permissions
sudo mkdir -p /opt/caddy-toolkit
sudo chown caddy-toolkit:caddy-toolkit /opt/caddy-toolkit
sudo chmod 750 /opt/caddy-toolkit
```

### 2. Install .NET Runtime

```bash
# Ubuntu 20.04+
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --runtime dotnet --install-dir /opt/dotnet

# Add to PATH globally
sudo tee /etc/profile.d/dotnet.sh > /dev/null <<EOF
export DOTNET_ROOT=/opt/dotnet
export PATH=\$DOTNET_ROOT:\$PATH
EOF

source /etc/profile.d/dotnet.sh
dotnet --version
```

### 3. Install Caddy

```bash
# Method 1: Package manager (Ubuntu/Debian)
sudo apt update
sudo apt install -y caddy

# Method 2: Manual installation
sudo mkdir -p /opt/caddy
cd /tmp
wget https://github.com/caddyserver/caddy/releases/download/v2.7.6/caddy_2.7.6_linux_amd64.tar.gz
tar -xzf caddy_2.7.6_linux_amd64.tar.gz
sudo mv caddy /opt/caddy/
sudo chmod +x /opt/caddy/caddy

# Create systemd unit for Caddy
sudo tee /etc/systemd/system/caddy.service > /dev/null <<'EOF'
[Unit]
Description=Caddy HTTP/HTTPS server
Documentation=https://caddyserver.com/docs/
After=network-online.target
Wants=network-online.target

[Service]
User=caddy
Group=caddy
ExecStart=/usr/bin/caddy run --config /etc/caddy/Caddyfile
ExecReload=/usr/bin/caddy reload --config /etc/caddy/Caddyfile
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable caddy
sudo systemctl start caddy
```

## Installation

### Option A: Binary Release

```bash
# Download latest release
RELEASE_URL="https://github.com/Sarmkadan/caddy-vps-toolkit/releases/download/v1.2.0"
wget ${RELEASE_URL}/caddy-vps-toolkit-linux-x64.tar.gz
tar -xzf caddy-vps-toolkit-linux-x64.tar.gz

# Install to system directory
sudo mv caddy-vps-toolkit /usr/local/bin/
sudo chmod +x /usr/local/bin/caddy-vps-toolkit

# Verify installation
caddy-vps-toolkit --version
```

### Option B: Build from Source

```bash
cd /opt/caddy-toolkit
sudo -u caddy-toolkit git clone https://github.com/Sarmkadan/caddy-vps-toolkit.git .

sudo -u caddy-toolkit bash -c '
  dotnet build -c Release
  dotnet publish -c Release -o ./publish
'

sudo cp ./publish/caddy-vps-toolkit /usr/local/bin/
sudo chmod +x /usr/local/bin/caddy-vps-toolkit
```

### Option C: Docker

```bash
# Pull image
docker pull sarmkadan/caddy-vps-toolkit:1.2.0

# Create persistent volume for configuration
docker volume create caddy-toolkit-config

# Run container
docker run -d \
  --name caddy-toolkit \
  --volume caddy-toolkit-config:/root/.caddy-vps-toolkit \
  --volume /var/run/docker.sock:/var/run/docker.sock \
  sarmkadan/caddy-vps-toolkit:1.2.0

# Initialize config
docker exec caddy-toolkit caddy-vps-toolkit config init
```

## Configuration

### 1. Initialize Toolkit

```bash
# As regular user
caddy-vps-toolkit config init

# Verify
caddy-vps-toolkit config validate
```

### 2. Update appsettings.json

```json
{
  "CaddyVpsToolkit": {
    "Database": {
      "ConnectionString": "Data Source=/var/lib/caddy-toolkit/services.db",
      "EnableMigrations": true
    },
    "Caddy": {
      "ConfigPath": "/etc/caddy/Caddyfile",
      "BinaryPath": "/usr/bin/caddy",
      "ReloadCommandTimeout": "10s"
    },
    "Systemd": {
      "UnitDirectory": "/etc/systemd/system",
      "EnableOnCreation": true,
      "RestartPolicy": "on-failure"
    },
    "HealthCheck": {
      "DefaultInterval": "30s",
      "DefaultTimeout": "5s",
      "DefaultRetries": 3,
      "EnableAutoRecovery": true
    },
    "Logging": {
      "LogLevel": "Information",
      "EnableFileLogging": true,
      "LogFilePath": "/var/log/caddy-toolkit"
    },
    "Monitoring": {
      "EnableMetrics": true,
      "MetricsPort": 9090,
      "EnableAuditLog": true
    }
  }
}
```

### 3. Set Up Logging

```bash
# Create log directory
sudo mkdir -p /var/log/caddy-toolkit
sudo chown caddy-toolkit:caddy-toolkit /var/log/caddy-toolkit
sudo chmod 755 /var/log/caddy-toolkit

# Create logrotate config
sudo tee /etc/logrotate.d/caddy-toolkit > /dev/null <<'EOF'
/var/log/caddy-toolkit/*.log {
    daily
    rotate 14
    missingok
    notifempty
    compress
    delaycompress
    postrotate
        systemctl reload caddy-toolkit-monitor > /dev/null 2>&1 || true
    endscript
}
EOF
```

## Systemd Integration

### Background Health Monitor Service

```bash
# Create systemd unit
sudo tee /etc/systemd/system/caddy-toolkit-monitor.service > /dev/null <<'EOF'
[Unit]
Description=Caddy VPS Toolkit Health Monitor
After=network-online.target caddy.service
Wants=network-online.target
PartOf=caddy-toolkit.target

[Service]
Type=simple
User=caddy-toolkit
ExecStart=/usr/local/bin/caddy-vps-toolkit health monitor --interval 30s
Restart=on-failure
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable caddy-toolkit-monitor
sudo systemctl start caddy-toolkit-monitor
```

### Target Unit (Optional)

```bash
# Group related units
sudo tee /etc/systemd/system/caddy-toolkit.target > /dev/null <<'EOF'
[Unit]
Description=Caddy VPS Toolkit Suite
Wants=caddy-toolkit-monitor.service

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable caddy-toolkit
sudo systemctl start caddy-toolkit
```

## Database Management

### Initial Setup

```bash
# Initialize database
caddy-vps-toolkit config init

# Verify database
sqlite3 ~/.caddy-vps-toolkit/services.db ".tables"
```

### Backup Strategy

```bash
# Create backup script
sudo tee /usr/local/bin/backup-caddy-toolkit.sh > /dev/null <<'EOF'
#!/bin/bash
set -e

BACKUP_DIR="/var/backups/caddy-toolkit"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DB_PATH="$HOME/.caddy-vps-toolkit/services.db"

mkdir -p "$BACKUP_DIR"

# Backup database
cp "$DB_PATH" "$BACKUP_DIR/services_$TIMESTAMP.db"

# Backup configuration
caddy-vps-toolkit config export --format json > "$BACKUP_DIR/config_$TIMESTAMP.json"

# Cleanup old backups (keep 30 days)
find "$BACKUP_DIR" -type f -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR"
EOF

sudo chmod +x /usr/local/bin/backup-caddy-toolkit.sh

# Add to crontab
(sudo crontab -u caddy-toolkit -l 2>/dev/null; echo "0 2 * * * /usr/local/bin/backup-caddy-toolkit.sh") | sudo crontab -u caddy-toolkit -
```

### Disaster Recovery

```bash
# Export full state
caddy-vps-toolkit config export --format json > production-backup.json

# On new system, import
caddy-vps-toolkit config init
caddy-vps-toolkit config import --file production-backup.json
```

## Monitoring and Alerting

### Prometheus Metrics

```bash
# Metrics endpoint runs on port 9090
curl http://localhost:9090/metrics

# Add to Prometheus scrape config
echo '
- job_name: caddy-toolkit
  static_configs:
  - targets: ["localhost:9090"]
' | sudo tee -a /etc/prometheus/prometheus.yml

sudo systemctl reload prometheus
```

### Webhook Notifications

```bash
# Configure Slack webhook
caddy-toolkit notification add-webhook \
  --name slack \
  --url https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
  --on-health-failure

# Configure PagerDuty
caddy-toolkit notification add-webhook \
  --name pagerduty \
  --url https://events.pagerduty.com/v2/enqueue \
  --on-health-failure \
  --payload-template /opt/templates/pagerduty.json
```

### Systemd Journal Monitoring

```bash
# View recent logs
sudo journalctl -u caddy-toolkit-monitor -n 100

# Watch in real-time
sudo journalctl -u caddy-toolkit-monitor -f

# Check for errors in last 24 hours
sudo journalctl -u caddy-toolkit-monitor --since "24 hours ago" | grep -i error
```

## Security Hardening

### User and Permissions

```bash
# Create restricted user
sudo useradd -r -s /bin/false -d /var/lib/caddy-toolkit caddy-toolkit
sudo mkdir -p /var/lib/caddy-toolkit
sudo chown caddy-toolkit:caddy-toolkit /var/lib/caddy-toolkit
sudo chmod 700 /var/lib/caddy-toolkit

# Restrict config directory
sudo chmod 700 ~/.caddy-vps-toolkit
```

### Firewall Rules

```bash
# UFW (Ubuntu)
sudo ufw allow 80/tcp comment "Caddy HTTP"
sudo ufw allow 443/tcp comment "Caddy HTTPS"
sudo ufw allow 9090/tcp comment "Caddy Toolkit Metrics"

# iptables
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 9090 -j ACCEPT
```

### TLS/SSL

```bash
# Enable auto HTTPS in Caddy
echo "*.example.com {
    tls email@example.com
}" | sudo tee -a /etc/caddy/Caddyfile

sudo systemctl reload caddy
```

### Database Encryption

```bash
# Enable SQLite encryption (Enterprise feature)
# Standard deployment uses file permissions for protection
sudo chmod 600 ~/.caddy-vps-toolkit/services.db
```

## Performance Tuning

### Database Optimization

```bash
# Analyze and optimize database
sudo -u caddy-toolkit bash -c '
sqlite3 ~/.caddy-vps-toolkit/services.db <<EOF
ANALYZE;
VACUUM;
PRAGMA optimize;
EOF
'
```

### Health Check Optimization

```bash
# Adjust health check settings for high-traffic systems
caddy-toolkit config set health-check.parallelism 5
caddy-toolkit config set health-check.cache-ttl 10s
```

### Caddy Configuration

```
# Add to Caddyfile for performance
{
    http {
        max_header_size 1MB
        timeouts {
            read 5s
            write 5s
        }
    }
}
```

## Scaling Considerations

### Single VPS Limits

- ~20-30 active services before considering distribution
- Health check interval impacts: 10s interval × 50 services = 5 checks/sec
- Database size: ~500KB per service (historical data included)

### Multi-Server Setup (Future)

Plan for distributed mode:

```bash
# Central coordinator (v2.0)
caddy-toolkit cluster init --role coordinator

# Worker nodes (v2.0)
caddy-toolkit cluster join --coordinator 10.0.1.1 --role worker
```

## Upgrade Procedure

### Backup Before Upgrading

```bash
sudo systemctl stop caddy-toolkit-monitor
caddy-vps-toolkit config export --format json > backup.json
cp ~/.caddy-vps-toolkit/services.db ~/backup.db
```

### In-Place Upgrade

```bash
# Download new version
RELEASE_URL="https://github.com/Sarmkadan/caddy-vps-toolkit/releases/download/v1.3.0"
wget ${RELEASE_URL}/caddy-vps-toolkit-linux-x64.tar.gz
tar -xzf caddy-vps-toolkit-linux-x64.tar.gz

# Install
sudo mv caddy-vps-toolkit /usr/local/bin/
sudo chmod +x /usr/local/bin/caddy-vps-toolkit

# Test
caddy-vps-toolkit config validate

# Restart
sudo systemctl start caddy-toolkit-monitor
```

### Rollback Procedure

```bash
# If upgrade fails
sudo systemctl stop caddy-toolkit-monitor

# Restore from backup
caddy-vps-toolkit config init --reset
caddy-vps-toolkit config import --file backup.json

# Downgrade binary
# (Keep previous binary or rebuild from source)

sudo systemctl start caddy-toolkit-monitor
```

## Post-Deployment Verification

```bash
# Verify toolkit is running
caddy-vps-toolkit --version
caddy-vps-toolkit config validate

# Check Caddy integration
sudo systemctl status caddy
caddy reload

# Test health monitoring
caddy-vps-toolkit health summary

# Verify logs
sudo journalctl -u caddy-toolkit-monitor -n 20

# Test metrics endpoint
curl http://localhost:9090/metrics

# Check audit log
caddy-vps-toolkit audit log --limit 10
```

## Troubleshooting Production Issues

### High Memory Usage

```bash
# Check database size
du -sh ~/.caddy-vps-toolkit/services.db

# Optimize database
sqlite3 ~/.caddy-vps-toolkit/services.db "VACUUM;"

# Check running processes
ps aux | grep caddy-vps-toolkit
```

### Health Checks Timing Out

```bash
# Increase timeout
caddy-toolkit service update SERVICE_NAME --health-check-timeout 15s

# Check network connectivity
ping 127.0.0.1
netstat -tuln | grep LISTEN
```

### Configuration Not Applying

```bash
# Verify permissions
ls -la ~/.caddy-vps-toolkit/

# Force reload
sudo systemctl restart caddy-toolkit-monitor
sudo systemctl reload caddy
```

## Maintenance Schedule

**Daily:**
- Monitor logs for errors
- Check health summary
- Verify Caddy is running

**Weekly:**
- Review audit log
- Backup configuration
- Test disaster recovery

**Monthly:**
- Analyze metrics
- Plan updates
- Review security settings

**Quarterly:**
- Full system audit
- Database optimization
- Capacity planning
