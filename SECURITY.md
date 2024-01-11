# Security Policy

## Supported Versions

| Version | Supported | Until |
|---------|-----------|-------|
| 1.2.x   | ✅ Yes    | Jan 2027 |
| 1.1.x   | ✅ Yes    | Dec 2026 |
| 1.0.x   | ⚠️ Patch only | Oct 2026 |
| < 1.0   | ❌ No     | - |

## Reporting a Vulnerability

**Do not open public issues for security vulnerabilities.**

If you discover a security vulnerability in caddy-vps-toolkit, please report it using one of these methods:

1. **GitHub Private Vulnerability Reporting** (Preferred):
   https://github.com/sarmkadan/caddy-vps-toolkit/security/advisories/new

2. **Email**:
   rutova2@gmail.com

Please include:

1. **Description**: Brief description of the vulnerability
2. **Impact**: Potential impact on users
3. **Steps to Reproduce**: How to reproduce the issue
4. **Suggested Fix**: If available
5. **Your Name**: For acknowledgment (optional)

We will:
- Acknowledge receipt within 48 hours
- Provide an estimated timeline for a fix
- Issue a patch update as soon as possible
- Credit you in the advisory (if desired)

## Security Best Practices

### For Users

1. **Keep Updated**: Always run the latest version
   ```bash
   caddy-vps-toolkit --version
   # Compare with latest release
   ```

2. **Restrict File Permissions**: Configuration database contains sensitive data
   ```bash
   chmod 700 ~/.caddy-vps-toolkit/
   chmod 600 ~/.caddy-vps-toolkit/services.db
   ```

3. **Use Dedicated User**: Run toolkit as non-root user
   ```bash
   sudo useradd -m -s /bin/bash caddy-toolkit
   sudo chown caddy-toolkit:caddy-toolkit ~/.caddy-vps-toolkit
   ```

4. **Enable Audit Logging**: Track all modifications
   ```bash
   # In appsettings.json
   "Monitoring": {
     "EnableAuditLog": true
   }
   ```

5. **Monitor Health Checks**: Setup webhook notifications for failures
   ```bash
   caddy-vps-toolkit notification add-webhook \
     --name slack \
     --url https://hooks.slack.com/services/YOUR/WEBHOOK \
     --on-health-failure
   ```

6. **Backup Regularly**: Export configuration periodically
   ```bash
   caddy-vps-toolkit config export --format json > backup.json
   ```

### For Deployment

1. **Use HTTPS**: Always use encrypted connections
   - Caddy automatically handles Let's Encrypt certificates
   - Configure TLS in health check URLs

2. **Network Isolation**: Restrict access to management ports
   ```bash
   # UFW (Ubuntu)
   sudo ufw default deny incoming
   sudo ufw allow ssh
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw limit 9090/tcp  # Metrics (restrict to admin only)
   ```

3. **Database Security**:
   - Restrict database file access (chmod 600)
   - Consider encrypted filesystem for database directory
   - Regular backups to secure location

4. **Webhook Security**:
   - Use HTTPS endpoints for webhooks
   - Validate HMAC signatures on received webhooks
   - Never log webhook URLs with secrets

5. **Environment Variables**:
   - Store sensitive vars in `/etc/default/` (chmod 600)
   - Use `EnvironmentFile` in systemd units
   - Never commit secrets to version control

## Security Features

### Input Validation

All user inputs are validated:
- Service names: alphanumeric + hyphens
- Ports: 1-65535 range
- URLs: RFC 3986 compliance
- Environment variables: no shell metacharacters
- Command arguments: sanitized before execution

### Database Security

- SQL queries use parameterized statements (no SQL injection)
- Database file restricted to owner only (600 permissions)
- Connection pooling prevents resource exhaustion
- Automatic query timeouts prevent deadlocks

### Process Security

- Runs with minimal required privileges
- Systemd units restrict capabilities
- Resource limits prevent DoS
- Proper signal handling for graceful shutdown

### Communication Security

- HTTPS support for health checks
- Webhook HMAC signing optional
- Certificate validation
- Timeout protection against hanging connections

### Audit Trail

- All service changes logged with timestamp
- User/IP information recorded (when available)
- Audit log cannot be disabled (can be rotated)
- Export audit log for compliance

## Known Issues

There are currently no known unpatched security vulnerabilities.

## Security Disclosure

We follow responsible disclosure practices:

1. Vulnerability reported privately
2. Acknowledgment within 48 hours
3. Investigation and patch development
4. Security advisory issued before/with patch
5. Patch release with CVE information
6. Public disclosure after patch available

## Dependencies

We regularly audit third-party dependencies:

```bash
# Check for vulnerabilities
dotnet audit

# Update dependencies
dotnet outdated
dotnet package update
```

### Notable Dependencies

- **.NET Runtime**: Regularly patched by Microsoft
- **SQLite**: Embedded, no network exposure
- **Caddy**: Managed separately, not included in binary
- **systemd**: System library, not bundled

## Compliance

caddy-vps-toolkit is designed with compliance in mind:

- **Audit Logging**: Complete audit trail for compliance requirements
- **Data Protection**: Sensitive data stored encrypted (optional)
- **User Permissions**: Role-based access control ready
- **Documentation**: Full documentation for compliance reviews

## Report a Bug

Security bugs should be reported privately (see above).

Non-security bugs can be reported on GitHub:
https://github.com/Sarmkadan/caddy-vps-toolkit/issues

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE/SANS Top 25](https://cwe.mitre.org/top25/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [Caddy Security](https://caddyserver.com/docs/security/)
- [systemd Security](https://www.freedesktop.org/software/systemd/man/systemd.exec.html)

## Security Advisories

### Published Advisories

None at this time.

### Advisories from Upstream

Monitor advisories from:
- [Microsoft .NET Security](https://github.com/dotnet/announcements)
- [Caddy Security](https://github.com/caddyserver/caddy/security)
- [Linux Kernel](https://www.kernel.org/doc/html/latest/admin-guide/security-bugs.html)

## Contact

Security inquiries: https://github.com/sarmkadan/caddy-vps-toolkit/security/advisories/new or rutova2@gmail.com

---

**Last Updated**: January 15, 2026
