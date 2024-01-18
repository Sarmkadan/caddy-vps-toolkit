#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Installation script for caddy-vps-toolkit
# Downloads and installs the latest release

set -euo pipefail

# Configuration
PROJECT_NAME="caddy-vps-toolkit"
REPO="Sarmkadan/caddy-vps-toolkit"
INSTALL_DIR="/usr/local/bin"
RELEASE_URL="https://api.github.com/repos/${REPO}/releases/latest"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Functions
info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root for installation
if [ "$EUID" -ne 0 ] && [ "$INSTALL_DIR" = "/usr/local/bin" ]; then
    error "This script must be run as root for system installation"
    echo "Run: sudo bash $0"
    exit 1
fi

echo ""
echo "=========================================="
echo "$PROJECT_NAME Installation Script"
echo "=========================================="
echo ""

# Check prerequisites
info "Checking prerequisites..."

# Check for curl
if ! command -v curl &> /dev/null; then
    error "curl is required but not installed"
    echo "Install with: sudo apt install curl"
    exit 1
fi

# Check for tar
if ! command -v tar &> /dev/null; then
    error "tar is required but not installed"
    echo "Install with: sudo apt install tar"
    exit 1
fi

info "Prerequisites verified"
echo ""

# Get the latest release
info "Fetching latest release information..."

RELEASE_JSON=$(curl -s "$RELEASE_URL")

if ! echo "$RELEASE_JSON" | grep -q "tag_name"; then
    error "Failed to fetch release information"
    echo "Check GitHub API: $RELEASE_URL"
    exit 1
fi

VERSION=$(echo "$RELEASE_JSON" | grep -o '"tag_name": "[^"]*' | cut -d'"' -f4)
DOWNLOAD_URL=$(echo "$RELEASE_JSON" | grep -o '"browser_download_url": "[^"]*linux-x64[^"]*' | head -1 | cut -d'"' -f4)

if [ -z "$VERSION" ] || [ -z "$DOWNLOAD_URL" ]; then
    error "Could not determine download URL"
    exit 1
fi

info "Latest version: $VERSION"
success "Download URL found"
echo ""

# Download
info "Downloading $PROJECT_NAME $VERSION..."

TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

cd "$TEMP_DIR"

if curl -L -o "${PROJECT_NAME}.tar.gz" "$DOWNLOAD_URL"; then
    success "Downloaded successfully"
else
    error "Failed to download"
    exit 1
fi

# Extract
info "Extracting archive..."

if tar -xzf "${PROJECT_NAME}.tar.gz"; then
    success "Extracted successfully"
else
    error "Failed to extract archive"
    exit 1
fi

# Verify executable
if [ ! -f "caddy-vps-toolkit" ] && [ ! -f "caddy-vps-toolkit.dll" ]; then
    error "Executable not found in archive"
    exit 1
fi

# For DLL-based deployment
if [ -f "caddy-vps-toolkit.dll" ] && [ ! -f "caddy-vps-toolkit" ]; then
    info "Creating wrapper script for DLL execution..."
    cat > "caddy-vps-toolkit" << 'EOF'
#!/bin/bash
dotnet /usr/local/lib/caddy-vps-toolkit/caddy-vps-toolkit.dll "$@"
EOF
    chmod +x "caddy-vps-toolkit"
fi

# Install
info "Installing to $INSTALL_DIR..."

if [ -f "caddy-vps-toolkit" ]; then
    cp "caddy-vps-toolkit" "$INSTALL_DIR/"
    chmod +x "$INSTALL_DIR/caddy-vps-toolkit"
    success "Installed to $INSTALL_DIR/caddy-vps-toolkit"
else
    error "Could not find executable to install"
    exit 1
fi

echo ""

# Verify installation
info "Verifying installation..."

if ! command -v caddy-vps-toolkit &> /dev/null; then
    warn "caddy-vps-toolkit not in PATH"
    echo "Add to PATH: export PATH=\"$INSTALL_DIR:\$PATH\""
else
    success "Installation verified"
fi

# Display version
VERSION_OUTPUT=$(caddy-vps-toolkit --version 2>/dev/null || echo "Unable to get version")
info "Installed version: $VERSION_OUTPUT"

echo ""

# Initialize if needed
if [ ! -d ~/.caddy-vps-toolkit ]; then
    read -p "Initialize configuration database? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        info "Initializing configuration..."
        if caddy-vps-toolkit config init; then
            success "Configuration initialized"
        else
            error "Failed to initialize configuration"
            exit 1
        fi
    fi
fi

echo ""
echo "=========================================="
echo "Installation Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo ""
echo "1. View help:"
echo "   caddy-vps-toolkit --help"
echo ""
echo "2. Initialize configuration (if not done):"
echo "   caddy-vps-toolkit config init"
echo ""
echo "3. Create your first service:"
echo "   caddy-vps-toolkit service add \\"
echo "     --name api \\"
echo "     --port 8080 \\"
echo "     --domain api.example.com \\"
echo "     --health-check-url http://localhost:8080/health"
echo ""
echo "4. List services:"
echo "   caddy-vps-toolkit service list"
echo ""
echo "5. View documentation:"
echo "   https://github.com/$REPO#readme"
echo ""
echo "Documentation:"
echo "  - Getting Started: docs/getting-started.md"
echo "  - API Reference: docs/api-reference.md"
echo "  - Deployment Guide: docs/deployment.md"
echo ""

success "Ready to use!"
