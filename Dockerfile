# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Dockerfile for caddy-vps-toolkit
# Multi-stage build for optimized image size

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 as builder

WORKDIR /src

# Copy project files
COPY . .

# Restore dependencies
RUN dotnet restore

# Build application
RUN dotnet build -c Release --no-restore

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0

LABEL maintainer="Vladyslav Zaiets <https://sarmkadan.com>"
LABEL description="Caddy VPS Toolkit - Service management CLI for Linux VPS"
LABEL version="1.2.0"

WORKDIR /app

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    caddy \
    systemd \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Copy published application from builder
COPY --from=builder /app/publish /app

# Create configuration directory
RUN mkdir -p /root/.caddy-vps-toolkit /var/log/caddy-toolkit

# Volume mounts for persistent data
VOLUME ["/root/.caddy-vps-toolkit", "/etc/caddy", "/var/log/caddy-toolkit"]

# Default command
ENTRYPOINT ["dotnet", "/app/caddy-vps-toolkit.dll"]
CMD ["--help"]

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD dotnet /app/caddy-vps-toolkit.dll health summary || exit 1

# Expose metrics port
EXPOSE 9090

# Labels for Docker metadata
LABEL org.opencontainers.image.source="https://github.com/Sarmkadan/caddy-vps-toolkit"
LABEL org.opencontainers.image.documentation="https://github.com/Sarmkadan/caddy-vps-toolkit#readme"
LABEL org.opencontainers.image.licenses="MIT"
