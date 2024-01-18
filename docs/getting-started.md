# Getting Started with caddy-vps-toolkit

This guide walks you through installing and using caddy-vps-toolkit for the first time.

## Prerequisites

- Ubuntu 20.04+ or Debian 10+ (other Linux distributions supported)
- Sudo access for system-level operations
- Caddy 2.7+ installed (`caddy --version` to check)
- .NET 10 runtime

## Installation Steps

### Step 1: Install .NET 10 Runtime

```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest --runtime dotnet
source ~/.bashrc

# Verify installation
dotnet --version
```

### Step 2: Download and Install Caddy

```bash
# Using apt (Ubuntu/Debian)
sudo apt update
sudo apt install caddy

# Or using xcaddy (for custom modules)
curl https://getcaddy.com | bash -s personal
sudo mv caddy /usr/bin/
```

### Step 3: Install caddy-vps-toolkit

**Option A: Pre-built Binary**

```bash
wget https://github.com/Sarmkadan/caddy-vps-toolkit/releases/download/v1.2.0/caddy-vps-toolkit-linux-x64.tar.gz
tar -xzf caddy-vps-toolkit-linux-x64.tar.gz
sudo mv caddy-vps-toolkit /usr/local/bin/
caddy-vps-toolkit --version
```

**Option B: Build from Source**

```bash
git clone https://github.com/Sarmkadan/caddy-vps-toolkit.git
cd caddy-vps-toolkit
dotnet publish -c Release -o ./publish
sudo cp ./publish/caddy-vps-toolkit /usr/local/bin/
sudo chmod +x /usr/local/bin/caddy-vps-toolkit
```

### Step 4: Initialize Configuration

```bash
# Create configuration database
caddy-vps-toolkit config init

# Verify configuration
caddy-vps-toolkit config validate
```

## First Service Setup

### Scenario: Deploy a Node.js Web Application

1. **Create a service entry**

```bash
caddy-vps-toolkit service add \
  --name web-app \
  --port 3000 \
  --domain myapp.example.com \
  --health-check-url http://localhost:3000/health \
  --health-check-interval 30s
```

2. **Verify the service was created**

```bash
caddy-vps-toolkit service show web-app
```

Output:
```
Service: web-app
Port: 3000
Domain: myapp.example.com
Health Check URL: http://localhost:3000/health
Health Check Interval: 30s
Status: Healthy
```

3. **Generate Caddy configuration**

```bash
caddy-vps-toolkit caddy generate
```

This outputs a Caddyfile snippet:
```
myapp.example.com {
    reverse_proxy localhost:3000
}
```

4. **Apply Caddy configuration**

```bash
# Append to existing Caddyfile
caddy-vps-toolkit caddy generate >> /etc/caddy/Caddyfile

# Or replace completely (if new setup)
sudo bash -c 'caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile'

# Reload Caddy
sudo systemctl reload caddy
```

5. **Generate systemd unit file**

```bash
caddy-vps-toolkit systemd generate web-app
```

6. **Create and enable the systemd service**

```bash
# Generate and save the unit file
sudo bash -c 'caddy-vps-toolkit systemd generate web-app > /etc/systemd/system/web-app.service'

# Reload systemd daemon
sudo systemctl daemon-reload

# Enable and start the service
sudo systemctl enable web-app
sudo systemctl start web-app

# Verify it's running
sudo systemctl status web-app
```

7. **Test health monitoring**

```bash
# Manual health check
caddy-vps-toolkit health check web-app

# Start continuous monitoring
caddy-vps-toolkit health monitor --interval 30s --service web-app
```

## Common Tasks

### Add Multiple Services

```bash
# Service 1: API backend
caddy-vps-toolkit service add \
  --name api \
  --port 8080 \
  --domain api.example.com

# Service 2: Admin panel
caddy-vps-toolkit service add \
  --name admin \
  --port 9000 \
  --domain admin.example.com

# Service 3: Database
caddy-vps-toolkit service add \
  --name postgres \
  --port 5432 \
  --domain db.example.com

# View all services
caddy-vps-toolkit service list
```

### Update Service Configuration

```bash
# Change port
caddy-vps-toolkit service update web-app --port 3001

# Add environment variable
caddy-vps-toolkit service update web-app --env NODE_ENV=production

# Update health check
caddy-vps-toolkit service update web-app \
  --health-check-url http://localhost:3001/health \
  --health-check-timeout 10s
```

### Monitor All Services

```bash
# Start background health monitoring (all services)
caddy-vps-toolkit health monitor --interval 30s

# View health history
caddy-vps-toolkit health history web-app --limit 50

# Get summary
caddy-vps-toolkit health summary
```

### Backup Configuration

```bash
# Export all services to JSON
caddy-vps-toolkit config export --format json > backup.json

# Export to YAML
caddy-vps-toolkit config export --format yaml > backup.yaml

# Backup database file
sudo cp ~/.caddy-vps-toolkit/services.db ~/backups/
```

### Restore Configuration

```bash
# Import from JSON
caddy-vps-toolkit config import --file backup.json --format json

# Import from YAML
caddy-vps-toolkit config import --file backup.yaml --format yaml
```

## Troubleshooting

### "Command not found: caddy-vps-toolkit"

```bash
# Verify installation path
which caddy-vps-toolkit

# If not found, add to PATH
export PATH="/usr/local/bin:$PATH"
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### Service shows "Unhealthy"

```bash
# Check if service is actually running
sudo systemctl status web-app

# Test health check endpoint manually
curl -v http://localhost:3000/health

# Check service logs
journalctl -u web-app -n 20

# Increase health check timeout
caddy-vps-toolkit service update web-app --health-check-timeout 15s
```

### Caddy reload fails

```bash
# Validate current Caddyfile
sudo caddy validate

# Check Caddy logs
journalctl -u caddy -n 20

# Regenerate config from scratch
sudo bash -c 'caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile'
sudo systemctl reload caddy
```

### Permission denied errors

```bash
# Ensure proper permissions for config files
sudo chown $USER:$USER ~/.caddy-vps-toolkit/
sudo chmod 700 ~/.caddy-vps-toolkit/

# Run toolkit with sudo for system operations
sudo caddy-vps-toolkit config init
```

## Next Steps

- Read [API Reference](./api-reference.md) for detailed command documentation
- Check [Deployment Guide](./deployment.md) for production setups
- Explore [examples/](../examples/) for real-world scenarios
- Review [Architecture](./architecture-deep-dive.md) for system design details

## Support

For issues or questions:
- Check [FAQ](./faq.md)
- Review GitHub Issues: https://github.com/Sarmkadan/caddy-vps-toolkit/issues
- Contact: https://sarmkadan.com
