#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Multi-Service Setup Example
# Demonstrates complete setup of a typical microservices architecture
# with API, Web, Cache, and Database services

set -euo pipefail

DOMAIN="${DOMAIN:-example.com}"
WEBHOOK_URL="${WEBHOOK_URL:-}"

echo "=========================================="
echo "Multi-Service Setup Example"
echo "=========================================="
echo "Domain: $DOMAIN"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Verify caddy-vps-toolkit is installed
if ! command -v caddy-vps-toolkit &> /dev/null; then
    error "caddy-vps-toolkit is not installed"
    exit 1
fi

info "Starting service configuration..."

# Step 1: Initialize configuration
info "Initializing configuration database..."
caddy-vps-toolkit config init

# Step 2: Create Database Service
info "Adding PostgreSQL database service..."
caddy-vps-toolkit service add \
    --name postgres \
    --port 5432 \
    --domain "db.${DOMAIN}" \
    --health-check-url "http://localhost:5432/health" \
    --health-check-interval 30s \
    --restart-policy on-failure

# Step 3: Create Cache Service
info "Adding Redis cache service..."
caddy-vps-toolkit service add \
    --name redis \
    --port 6379 \
    --domain "cache.${DOMAIN}" \
    --health-check-url "http://localhost:6379/health" \
    --health-check-interval 30s \
    --restart-policy on-failure

# Step 4: Create API Service (depends on database and cache)
info "Adding API service..."
caddy-vps-toolkit service add \
    --name api \
    --port 8080 \
    --domain "api.${DOMAIN}" \
    --health-check-url "http://localhost:8080/health" \
    --health-check-interval 15s \
    --health-check-timeout 5s \
    --health-check-retries 3 \
    --env NODE_ENV=production \
    --env LOG_LEVEL=info \
    --depends-on postgres \
    --depends-on redis \
    --restart-policy on-failure

# Step 5: Create Web Service (depends on API)
info "Adding Web service..."
caddy-vps-toolkit service add \
    --name web \
    --port 3000 \
    --domain "${DOMAIN}" \
    --health-check-url "http://localhost:3000/health" \
    --health-check-interval 20s \
    --health-check-timeout 5s \
    --env NODE_ENV=production \
    --env API_URL="http://api.${DOMAIN}" \
    --depends-on api \
    --restart-policy on-failure

# Step 6: Create Admin Dashboard Service
info "Adding Admin dashboard service..."
caddy-vps-toolkit service add \
    --name admin \
    --port 9000 \
    --domain "admin.${DOMAIN}" \
    --health-check-url "http://localhost:9000/health" \
    --health-check-interval 30s \
    --env NODE_ENV=production \
    --depends-on api \
    --restart-policy on-failure

# Step 7: List all services
info "Configured services:"
caddy-vps-toolkit service list --format table

# Step 8: Generate and preview Caddyfile
info "Generating Caddy configuration..."
CADDYFILE_PREVIEW=$(caddy-vps-toolkit caddy generate --format minimal)
echo "$CADDYFILE_PREVIEW"

# Step 9: Validate configuration
info "Validating Caddy configuration..."
if echo "$CADDYFILE_PREVIEW" | caddy-vps-toolkit caddy validate; then
    info "Caddy configuration is valid"
else
    error "Caddy configuration validation failed"
    exit 1
fi

# Step 10: Optionally set up webhook notifications
if [ -n "$WEBHOOK_URL" ]; then
    info "Configuring Slack webhook notifications..."
    caddy-vps-toolkit notification add-webhook \
        --name slack \
        --url "$WEBHOOK_URL" \
        --on-health-failure
fi

# Step 11: Instructions for applying configuration
echo ""
echo "=========================================="
echo "Next Steps:"
echo "=========================================="
echo ""
echo "1. Apply Caddy configuration:"
echo "   sudo bash -c 'caddy-vps-toolkit caddy generate > /etc/caddy/Caddyfile'"
echo "   sudo systemctl reload caddy"
echo ""
echo "2. Generate systemd units for services:"
for service in postgres redis api web admin; do
    echo "   sudo bash -c 'caddy-vps-toolkit systemd generate $service > /etc/systemd/system/$service.service'"
done
echo ""
echo "3. Reload systemd and enable services:"
echo "   sudo systemctl daemon-reload"
for service in postgres redis api web admin; do
    echo "   sudo systemctl enable $service"
    echo "   sudo systemctl start $service"
done
echo ""
echo "4. Verify services are running:"
echo "   caddy-vps-toolkit health summary"
echo ""
echo "5. Start health monitoring:"
echo "   sudo systemctl enable caddy-toolkit-monitor"
echo "   sudo systemctl start caddy-toolkit-monitor"
echo ""
echo "=========================================="
echo ""

info "Configuration complete!"
