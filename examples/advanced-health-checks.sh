#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Advanced Health Checks Configuration Examples
# Demonstrates various health check patterns and recovery strategies

set -euo pipefail

# Example 1: Basic Health Check with Retries
echo "=========================================="
echo "Example 1: Basic Health Check"
echo "=========================================="
caddy-vps-toolkit service add \
    --name web-app \
    --port 3000 \
    --domain app.example.com \
    --health-check-url http://localhost:3000/health \
    --health-check-interval 30s \
    --health-check-timeout 5s \
    --health-check-retries 3

# Example 2: Critical Service with Aggressive Monitoring
echo ""
echo "=========================================="
echo "Example 2: Critical Service - Fast Detection"
echo "=========================================="
caddy-vps-toolkit service add \
    --name payment-api \
    --port 5000 \
    --domain payments.example.com \
    --health-check-url http://localhost:5000/health \
    --health-check-interval 10s \
    --health-check-timeout 3s \
    --health-check-retries 2

# Example 3: Slow Starting Service
echo ""
echo "=========================================="
echo "Example 3: Slow Starting Service"
echo "=========================================="
caddy-vps-toolkit service add \
    --name data-processor \
    --port 7000 \
    --domain processor.example.com \
    --health-check-url http://localhost:7000/ready \
    --health-check-interval 60s \
    --health-check-timeout 15s \
    --health-check-retries 5

# Example 4: Service with Custom Recovery Script
echo ""
echo "=========================================="
echo "Example 4: Service with Custom Recovery"
echo "=========================================="

# Create recovery script for API service
cat > /tmp/api-recovery.sh << 'EOF'
#!/bin/bash
# Recovery script for API service
set -e

SERVICE_NAME="api"
SERVICE_PORT=8080
LOG_FILE="/var/log/api-recovery.log"

echo "[$(date)] Starting recovery for $SERVICE_NAME" >> $LOG_FILE

# Check if service is running
if ! pgrep -f "dotnet.*api.dll" > /dev/null; then
    echo "[$(date)] Service not running, starting..." >> $LOG_FILE
    systemctl start $SERVICE_NAME
fi

# Wait for startup
sleep 5

# Test health endpoint
if curl -f http://localhost:$SERVICE_PORT/health > /dev/null 2>&1; then
    echo "[$(date)] Service recovered successfully" >> $LOG_FILE
    exit 0
else
    echo "[$(date)] Service still unhealthy, forcing restart" >> $LOG_FILE
    systemctl restart $SERVICE_NAME
    sleep 10

    if curl -f http://localhost:$SERVICE_PORT/health > /dev/null 2>&1; then
        echo "[$(date)] Service recovered after restart" >> $LOG_FILE
        exit 0
    else
        echo "[$(date)] Service recovery failed" >> $LOG_FILE
        exit 1
    fi
fi
EOF

chmod +x /tmp/api-recovery.sh

caddy-vps-toolkit service add \
    --name api \
    --port 8080 \
    --domain api.example.com \
    --health-check-url http://localhost:8080/health \
    --health-check-interval 30s \
    --health-check-timeout 5s \
    --health-check-retries 3 \
    --restart-policy on-failure

# Example 5: Database Service (TCP-style, no HTTP)
echo ""
echo "=========================================="
echo "Example 5: Database Service"
echo "=========================================="
caddy-vps-toolkit service add \
    --name postgres \
    --port 5432 \
    --domain db.example.com \
    --health-check-interval 60s \
    --health-check-timeout 10s

# Example 6: Monitoring with Health Checks
echo ""
echo "=========================================="
echo "Example 6: Start Health Monitoring"
echo "=========================================="
echo "Starting continuous health monitoring for all services..."
echo "Monitor will check every 30 seconds"
echo ""
echo "Run this in a separate terminal:"
echo "  caddy-vps-toolkit health monitor --interval 30s"
echo ""

# Example 7: View Health History
echo "=========================================="
echo "Example 7: View Health History"
echo "=========================================="
echo "View recent health checks for a service:"
echo "  caddy-vps-toolkit health history web-app --limit 50"
echo ""
echo "View only unhealthy checks:"
echo "  caddy-vps-toolkit health history web-app --unhealthy-only"
echo ""
echo "View checks from last 24 hours:"
echo "  caddy-vps-toolkit health history web-app --start-time \$(date -d '24 hours ago' -Is)"
echo ""

# Example 8: Webhook Notifications
echo "=========================================="
echo "Example 8: Configure Webhook Notifications"
echo "=========================================="
echo "Send alerts to Slack when services fail:"
echo "  caddy-vps-toolkit notification add-webhook \\"
echo "    --name slack \\"
echo "    --url https://hooks.slack.com/services/YOUR/WEBHOOK/URL \\"
echo "    --on-health-failure"
echo ""
echo "Send alerts to PagerDuty:"
echo "  caddy-vps-toolkit notification add-webhook \\"
echo "    --name pagerduty \\"
echo "    --url https://events.pagerduty.com/v2/enqueue \\"
echo "    --on-health-failure"
echo ""

# Example 9: Batch Health Checks
echo "=========================================="
echo "Example 9: Perform Batch Health Checks"
echo "=========================================="
echo "Check specific service:"
echo "  caddy-vps-toolkit health check web-app"
echo ""
echo "Check all services with verbose output:"
echo "  for service in \$(caddy-vps-toolkit service list --format json | jq -r '.[].name'); do"
echo "    echo \"Checking \$service...\""
echo "    caddy-vps-toolkit health check \$service"
echo "  done"
echo ""

# Example 10: Health Summary
echo "=========================================="
echo "Example 10: View Health Summary"
echo "=========================================="
echo "Get overall health status:"
echo "  caddy-vps-toolkit health summary"
echo ""
echo "Get summary in JSON format:"
echo "  caddy-vps-toolkit health summary --format json"
echo ""

# Display current configuration
echo "=========================================="
echo "Current Service Configuration"
echo "=========================================="
caddy-vps-toolkit service list --format table

echo ""
echo "=========================================="
echo "Health Check Best Practices"
echo "=========================================="
cat << 'EOF'
1. INTERVAL SELECTION:
   - Critical services: 10-15s (fast failure detection)
   - Standard services: 30-60s (balanced)
   - Non-critical: 60-120s (reduced load)

2. TIMEOUT CONFIGURATION:
   - Network: 3-5s (normal latency)
   - Cloud: 5-10s (higher latency)
   - Slow services: 15-30s (startup/processing time)

3. RETRY STRATEGY:
   - Aggressive: 2 retries (fast recovery)
   - Balanced: 3 retries (standard)
   - Conservative: 5+ retries (stability)

4. RECOVERY SCRIPTS:
   - Quick restarts: simple restart command
   - Complex services: custom recovery logic
   - Notification: webhook alerts on failure

5. MONITORING:
   - Enable continuous monitoring for production
   - Export metrics to Prometheus/Grafana
   - Archive health history regularly
   - Set up alerting on thresholds

6. TROUBLESHOOTING:
   - Test endpoint with curl first
   - Check service logs with journalctl
   - Verify network connectivity
   - Increase timeout if needed
   - Review audit log for issues
EOF

echo ""
echo "Health checks configured!"
echo "Run 'caddy-vps-toolkit health summary' to view status"
