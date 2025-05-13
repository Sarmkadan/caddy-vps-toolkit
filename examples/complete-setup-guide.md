# Complete Setup Guide for caddy-vps-toolkit

This guide walks through a complete production setup of caddy-vps-toolkit on a fresh Linux VPS.

## Prerequisites

- Ubuntu 20.04+ or Debian 10+
- Sudo access
- 1GB+ RAM, 10GB+ disk space
- A domain name (for SSL/TLS)

## Step 1: System Preparation

### Update System

```bash
sudo apt update && sudo apt upgrade -y
```

### Create Dedicated User

```bash
# Create user for toolkit
sudo useradd -m -s /bin/bash -d /home/caddy-toolkit caddy-toolkit

# Create directories
sudo mkdir -p /var/lib/caddy-toolkit
sudo mkdir -p /var/log/caddy-toolkit
sudo chown -R caddy-toolkit:caddy-toolkit /var/lib/caddy-toolkit /var/log/caddy-toolkit
```

## Step 2: Install Dependencies

### Install .NET 10

```bash
# Add Microsoft GPG key
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET
sudo apt update
sudo apt install -y dotnet-runtime-10.0

# Verify
dotnet --version
```

### Install Caddy

```bash
sudo apt install -y caddy

# Verify
caddy --version

# Start Caddy
sudo systemctl start caddy
sudo systemctl enable caddy
```

## Step 3: Install caddy-vps-toolkit

### Option A: Using Install Script

```bash
curl -fsSL https://raw.githubusercontent.com/Sarmkadan/caddy-vps-toolkit/main/install.sh | sudo bash
```

### Option B: Manual Installation

```bash
# Download latest release
RELEASE_URL=$(curl -s https://api.github.com/repos/Sarmkadan/caddy-vps-toolkit/releases/latest | grep browser_download_url | grep linux-x64 | head -1 | cut -d'"' -f4)
wget $RELEASE_URL
tar -xzf caddy-vps-toolkit-linux-x64-*.tar.gz

# Install
sudo mv caddy-vps-toolkit /usr/local/bin/
sudo chmod +x /usr/local/bin/caddy-vps-toolkit

# Verify
caddy-vps-toolkit --version
```

## Step 4: Initialize Configuration

```bash
# Initialize
caddy-vps-toolkit config init

# Verify configuration
caddy-vps-toolkit config validate

# List initial services (should be empty)
caddy-vps-toolkit service list
```

## Step 5: Deploy Your First Service

### Example: Node.js API Service

Create service entry:

```bash
caddy-vps-toolkit service add \
  --name api \
  --port 8080 \
  --domain api.example.com \
  --health-check-url http://localhost:8080/health \
  --health-check-interval 30s \
  --restart-policy on-failure
```

### Generate Caddy Configuration

```bash
# Preview changes
caddy-vps-toolkit caddy generate

# Apply to Caddy
sudo bash -c 'caddy-vps-toolkit caddy generate >> /etc/caddy/Caddyfile'

# Reload Caddy
sudo systemctl reload caddy

# Verify
curl https://api.example.com/health
```

### Generate Systemd Unit

```bash
# Generate and install
sudo bash -c 'caddy-vps-toolkit systemd generate api > /etc/systemd/system/api.service'

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable api
sudo systemctl start api

# Verify
sudo systemctl status api
```

## Step 6: Set Up Monitoring

### Create Health Check Service

```bash
# Create systemd service for health monitoring
sudo tee /etc/systemd/system/caddy-toolkit-monitor.service > /dev/null <<EOF
[Unit]
Description=Caddy VPS Toolkit Health Monitor
After=network-online.target caddy.service
Wants=network-online.target

[Service]
Type=simple
ExecStart=/usr/local/bin/caddy-vps-toolkit health monitor --interval 30s
Restart=on-failure
RestartSec=10
User=caddy-toolkit
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable caddy-toolkit-monitor
sudo systemctl start caddy-toolkit-monitor
```

### Configure Webhook Notifications

```bash
# For Slack (replace with your webhook URL)
caddy-vps-toolkit notification add-webhook \
  --name slack \
  --url https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
  --on-health-failure
```

## Step 7: Set Up Monitoring (Optional)

### Install Prometheus

```bash
# Create prometheus user
sudo useradd --no-create-home --shell /bin/false prometheus

# Download and install
cd /tmp
wget https://github.com/prometheus/prometheus/releases/download/v2.50.0/prometheus-2.50.0.linux-amd64.tar.gz
tar -xzf prometheus-2.50.0.linux-amd64.tar.gz
sudo mv prometheus-2.50.0.linux-amd64 /opt/prometheus

# Create config
sudo cp examples/prometheus.yml /opt/prometheus/prometheus.yml
sudo chown -R prometheus:prometheus /opt/prometheus

# Create systemd unit
sudo tee /etc/systemd/system/prometheus.service > /dev/null <<EOF
[Unit]
Description=Prometheus
After=network.target

[Service]
User=prometheus
ExecStart=/opt/prometheus/prometheus --config.file=/opt/prometheus/prometheus.yml
SyslogIdentifier=prometheus
Restart=always

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable prometheus
sudo systemctl start prometheus
```

### Install Grafana

```bash
# Add Grafana repository
sudo apt-get install -y software-properties-common
sudo add-apt-repository "deb https://packages.grafana.com/oss/deb stable main"
sudo apt-get update

# Install Grafana
sudo apt-get install -y grafana-server

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable grafana-server
sudo systemctl start grafana-server

# Access at http://localhost:3000 (admin/admin)
```

## Step 8: Backup Configuration

### Create Backup Script

```bash
sudo tee /usr/local/bin/backup-caddy-toolkit.sh > /dev/null <<'EOF'
#!/bin/bash
BACKUP_DIR="/var/backups/caddy-toolkit"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"

# Backup configuration
caddy-vps-toolkit config export --format json > "$BACKUP_DIR/config_$TIMESTAMP.json"

# Cleanup old backups (30 days)
find "$BACKUP_DIR" -type f -mtime +30 -delete

echo "Backup completed: $BACKUP_DIR/config_$TIMESTAMP.json"
EOF

sudo chmod +x /usr/local/bin/backup-caddy-toolkit.sh
```

### Schedule Daily Backups

```bash
# Add to crontab
sudo crontab -u caddy-toolkit -e

# Add this line:
# 0 2 * * * /usr/local/bin/backup-caddy-toolkit.sh
```

## Step 9: Security Hardening

### Firewall Configuration

```bash
# Enable UFW
sudo ufw enable

# Allow SSH
sudo ufw allow ssh

# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow metrics (restrict to localhost)
sudo ufw allow from 127.0.0.1 to 127.0.0.1 port 9090
```

### File Permissions

```bash
# Restrict configuration directory
chmod 700 ~/.caddy-vps-toolkit
chmod 600 ~/.caddy-vps-toolkit/services.db
```

## Step 10: Deploy Additional Services

Repeat Step 5 for each service:

```bash
# Web application
caddy-vps-toolkit service add \
  --name web \
  --port 3000 \
  --domain example.com \
  --health-check-url http://localhost:3000/health \
  --depends-on api

# Database
caddy-vps-toolkit service add \
  --name postgres \
  --port 5432 \
  --domain db.example.com

# Cache
caddy-vps-toolkit service add \
  --name redis \
  --port 6379 \
  --domain cache.example.com
```

## Step 11: Verify Complete Setup

```bash
# Check all services
caddy-vps-toolkit service list

# View health status
caddy-vps-toolkit health summary

# Check systemd services
systemctl list-units --pattern='api|web|postgres|redis'

# View Caddy status
systemctl status caddy

# Check logs
sudo journalctl -u caddy-toolkit-monitor -f

# Access metrics
curl http://localhost:9090/metrics
```

## Maintenance

### Daily Tasks

```bash
# Check health
caddy-vps-toolkit health summary

# View recent logs
sudo journalctl -u caddy-toolkit-monitor -n 20
```

### Weekly Tasks

```bash
# Review audit log
caddy-vps-toolkit audit log --limit 50

# Check disk usage
du -sh ~/.caddy-vps-toolkit/

# Verify backups
ls -la /var/backups/caddy-toolkit/
```

### Monthly Tasks

```bash
# Optimize database
sqlite3 ~/.caddy-vps-toolkit/services.db "VACUUM; ANALYZE;"

# Check for updates
curl -s https://api.github.com/repos/Sarmkadan/caddy-vps-toolkit/releases/latest | grep tag_name

# Review security advisories
# Check https://github.com/Sarmkadan/caddy-vps-toolkit/security
```

## Troubleshooting

### Service won't start

```bash
# Check systemd error
systemd-analyze verify /etc/systemd/system/api.service

# View service logs
sudo journalctl -u api -n 50
```

### Health checks failing

```bash
# Manually test endpoint
curl -v http://localhost:8080/health

# Check service is running
sudo systemctl status api

# Increase timeout
caddy-vps-toolkit service update api --health-check-timeout 15s
```

### Caddy reload fails

```bash
# Validate configuration
sudo caddy validate --config /etc/caddy/Caddyfile -v

# Check Caddy logs
sudo journalctl -u caddy -n 20

# Regenerate from toolkit
sudo bash -c 'caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile'
sudo systemctl reload caddy
```

## Next Steps

1. **Scale**: Add more services as needed
2. **Monitor**: Set up Grafana dashboards
3. **Automate**: Create deployment scripts
4. **Document**: Document your configuration
5. **Backup**: Test backup and restore procedures

## Support

- Documentation: https://github.com/Sarmkadan/caddy-vps-toolkit/tree/main/docs
- Examples: https://github.com/Sarmkadan/caddy-vps-toolkit/tree/main/examples
- FAQ: https://github.com/Sarmkadan/caddy-vps-toolkit/blob/main/docs/faq.md
- Issues: https://github.com/Sarmkadan/caddy-vps-toolkit/issues

---

Congratulations! Your caddy-vps-toolkit installation is complete and ready for production use.
