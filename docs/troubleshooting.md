# Troubleshooting Guide

This page covers recovery procedures for situations where caddy-vps-toolkit leaves
the system in a broken state, as well as common diagnostic steps.

---

## Recovering from a Corrupted Caddyfile

A partial write or an interrupted `caddy-vps apply` can leave the Caddyfile with
unmatched braces or missing directives. Caddy will refuse to reload until the file
is valid.

### 1. Identify the problem

```bash
# Validate the current Caddyfile without reloading
caddy validate --config /etc/caddy/Caddyfile
```

Caddy prints the first parse error with a line number. Use that to locate the
broken section.

### 2. Restore from the automatic backup

Every time caddy-vps writes a new Caddyfile it first copies the previous version to:

```
/etc/caddy/Caddyfile.bak
```

Inspect it:

```bash
caddy validate --config /etc/caddy/Caddyfile.bak
```

If the backup is valid, roll back manually:

```bash
sudo cp /etc/caddy/Caddyfile.bak /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

### 3. Roll back with the toolkit

If the backup passes validation you can also use the built-in restore command:

```bash
sudo caddy-vps restore --last-good
```

This replaces the current Caddyfile with the most-recent backup that passes
`caddy validate` and then reloads Caddy.

### 4. Validate before applying

Always validate a generated config before writing it to disk. Use `--dry-run` to
preview the diff without touching any files:

```bash
caddy-vps apply --dry-run
```

Or validate manually:

```bash
caddy-vps caddy generate > /tmp/Caddyfile.new
caddy validate --config /tmp/Caddyfile.new
```

Only apply once the validation succeeds:

```bash
sudo cp /tmp/Caddyfile.new /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

### 5. Emergency manual recovery

If both the live Caddyfile and the `.bak` are corrupt:

```bash
# Stop Caddy to prevent it from repeatedly logging errors
sudo systemctl stop caddy

# Write a minimal working Caddyfile that at least answers on HTTP
sudo tee /etc/caddy/Caddyfile <<'EOF'
:80 {
    respond "Caddy is recovering" 200
}
EOF

caddy validate --config /etc/caddy/Caddyfile

sudo systemctl start caddy
```

Then re-run `caddy-vps apply` (with `--dry-run` first) to restore the full
service configuration.

---

## Caddy Refuses to Start After Apply

### Check systemd logs

```bash
sudo journalctl -u caddy -n 50 --no-pager
```

### Check Caddy's own error output

```bash
sudo caddy run --config /etc/caddy/Caddyfile 2>&1 | head -40
```

### Common causes

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `unmatched closing brace` | Partial write interrupted | Restore from `.bak` |
| `unknown directive` | Caddy plugin not loaded | Install required Caddy module |
| `bind: address already in use` | Port 80/443 used by another process | `sudo lsof -i :80` |
| `certificate not found` | TLS path wrong | Check `tls` directive paths |

---

## Health Monitor Reporting False Positives

If `caddy-vps health check --all` marks services as unhealthy immediately after a
restart, the toolkit may be probing the HTTP endpoint before the service process is
ready. Since caddy-vps 1.x the health monitor checks the systemd `ActiveState`
before firing the HTTP probe; if the unit is in `activating` or `reloading` state
the check is deferred and reported as `Degraded` rather than `Unhealthy`.

To confirm the current state:

```bash
systemctl is-active <unit-name>
# activating → still starting up
# active     → ready
# failed     → crashed
```

Increase the health-check startup delay if services take more than a few seconds to
become ready:

```bash
caddy-vps service update my-service --health-check-initial-delay 15s
```

---

## Dry-Run Mode

All mutating commands (`apply`, `add-service`, `remove-service`, `reload`) accept a
`--dry-run` flag. In dry-run mode caddy-vps prints a diff of every file it would
change without touching disk or reloading Caddy:

```bash
caddy-vps apply --dry-run
caddy-vps add-service my-app --domain app.example.com --port 8080 --dry-run
```

Use this on production servers to review changes before committing them.

---

## Additional Resources

- [Getting Started](./getting-started.md)
- [Deployment Guide](./deployment.md)
- [FAQ](./faq.md)
- [Architecture Deep Dive](./architecture-deep-dive.md)
