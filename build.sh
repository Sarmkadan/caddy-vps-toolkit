#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Build script for caddy-vps-toolkit
# Compiles the project and creates installation package

set -euo pipefail

# Configuration
PROJECT_NAME="caddy-vps-toolkit"
VERSION="1.2.0"
OUTPUT_DIR="./publish"
BUILD_CONFIG="Release"

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

show_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Build script for $PROJECT_NAME v$VERSION

OPTIONS:
    -c, --clean         Clean build artifacts before building
    -d, --debug         Build in Debug mode (default: Release)
    -p, --package       Create installation package
    -i, --install       Install to system (requires sudo)
    -t, --test          Run tests
    -a, --all           Clean, build, test, and package
    -h, --help          Show this help message

EXAMPLES:
    $0                  # Build in Release mode
    $0 --all            # Full build pipeline
    $0 -p               # Build and create package
    $0 -c -t            # Clean and run tests

EOF
}

# Parse arguments
CLEAN=false
TEST=false
PACKAGE=false
INSTALL=false
DEBUG=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--clean)
            CLEAN=true
            shift
            ;;
        -d|--debug)
            BUILD_CONFIG="Debug"
            shift
            ;;
        -t|--test)
            TEST=true
            shift
            ;;
        -p|--package)
            PACKAGE=true
            shift
            ;;
        -i|--install)
            INSTALL=true
            shift
            ;;
        -a|--all)
            CLEAN=true
            TEST=true
            PACKAGE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

echo ""
echo "=========================================="
echo "$PROJECT_NAME Build Script"
echo "=========================================="
echo "Version: $VERSION"
echo "Configuration: $BUILD_CONFIG"
echo ""

# Check prerequisites
info "Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    error ".NET SDK is not installed"
    echo "Visit: https://dotnet.microsoft.com/en-us/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
info ".NET version: $DOTNET_VERSION"

# Clean if requested
if [ "$CLEAN" = true ]; then
    info "Cleaning build artifacts..."
    dotnet clean -c $BUILD_CONFIG > /dev/null 2>&1 || true
    rm -rf "$OUTPUT_DIR" bin obj *.db
    success "Cleaned"
    echo ""
fi

# Restore dependencies
info "Restoring dependencies..."
if dotnet restore --nologo --verbosity quiet; then
    success "Dependencies restored"
else
    error "Failed to restore dependencies"
    exit 1
fi
echo ""

# Build
info "Building project..."
if dotnet build -c $BUILD_CONFIG --no-restore --nologo; then
    success "Build completed successfully"
else
    error "Build failed"
    exit 1
fi
echo ""

# Run tests if requested
if [ "$TEST" = true ]; then
    info "Running tests..."
    if dotnet test -c $BUILD_CONFIG --no-build --nologo --verbosity normal; then
        success "All tests passed"
    else
        error "Tests failed"
        exit 1
    fi
    echo ""
fi

# Publish release build
info "Publishing application..."
if dotnet publish -c $BUILD_CONFIG -o "$OUTPUT_DIR" --no-build --nologo > /dev/null 2>&1; then
    success "Application published to $OUTPUT_DIR"
else
    error "Publish failed"
    exit 1
fi
echo ""

# Create package if requested
if [ "$PACKAGE" = true ]; then
    info "Creating installation package..."

    PACKAGE_NAME="${PROJECT_NAME}-linux-x64-${VERSION}"

    # Create tar.gz
    tar -czf "${PACKAGE_NAME}.tar.gz" -C "$OUTPUT_DIR" . 2>/dev/null
    PACKAGE_SIZE=$(du -h "${PACKAGE_NAME}.tar.gz" | cut -f1)

    # Create SHA256 checksum
    sha256sum "${PACKAGE_NAME}.tar.gz" > "${PACKAGE_NAME}.tar.gz.sha256"

    success "Package created: ${PACKAGE_NAME}.tar.gz ($PACKAGE_SIZE)"
    success "Checksum: ${PACKAGE_NAME}.tar.gz.sha256"
    echo ""

    # Show install instructions
    echo "Installation instructions:"
    echo "  tar -xzf ${PACKAGE_NAME}.tar.gz"
    echo "  sudo mv caddy-vps-toolkit /usr/local/bin/"
    echo "  sudo chmod +x /usr/local/bin/caddy-vps-toolkit"
    echo ""
fi

# Install if requested
if [ "$INSTALL" = true ]; then
    info "Installing to system..."

    if [ ! -f "$OUTPUT_DIR/caddy-vps-toolkit" ]; then
        error "Executable not found: $OUTPUT_DIR/caddy-vps-toolkit"
        error "Run with --package first or ensure publish succeeded"
        exit 1
    fi

    # Check if sudo is available
    if ! command -v sudo &> /dev/null; then
        error "sudo is required for installation"
        exit 1
    fi

    # Copy to system directory
    if sudo cp "$OUTPUT_DIR/caddy-vps-toolkit" /usr/local/bin/; then
        sudo chmod +x /usr/local/bin/caddy-vps-toolkit
        success "Installation completed"
        echo ""

        # Verify installation
        VERSION_OUTPUT=$(caddy-vps-toolkit --version)
        success "Installed: $VERSION_OUTPUT"
    else
        error "Installation failed"
        exit 1
    fi
    echo ""
fi

# Summary
echo "=========================================="
echo "Build Summary"
echo "=========================================="
echo "Project: $PROJECT_NAME"
echo "Version: $VERSION"
echo "Configuration: $BUILD_CONFIG"
echo "Output Directory: $(pwd)/$OUTPUT_DIR"
echo ""

if [ "$CLEAN" = true ]; then
    echo "✓ Cleaned artifacts"
fi
echo "✓ Built project"
if [ "$TEST" = true ]; then
    echo "✓ Tests passed"
fi
if [ "$PACKAGE" = true ]; then
    echo "✓ Created package"
fi
if [ "$INSTALL" = true ]; then
    echo "✓ Installed to system"
fi

echo ""
echo "Next steps:"
if [ "$PACKAGE" = true ] && [ "$INSTALL" = false ]; then
    echo "  1. Verify package: tar -tzf ${PROJECT_NAME}-linux-x64-${VERSION}.tar.gz"
    echo "  2. Install: sudo bash -c 'tar -xzf ${PROJECT_NAME}-linux-x64-${VERSION}.tar.gz -C /usr/local/bin'"
fi
if [ "$INSTALL" = false ]; then
    echo "  1. Test: caddy-vps-toolkit --version"
    echo "  2. Initialize: caddy-vps-toolkit config init"
    echo "  3. Create service: caddy-vps-toolkit service add --name api --port 8080 --domain api.example.com"
fi

success "Build completed!"
