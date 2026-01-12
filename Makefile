# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Makefile for caddy-vps-toolkit
# Provides convenient build, test, and deployment targets

.PHONY: help build release clean test test-unit test-integration lint format publish install uninstall

# Variables
PROJECT_NAME := caddy-vps-toolkit
SOLUTION_FILE := caddy-vps-toolkit.csproj
OUTPUT_DIR := ./publish
BUILD_CONFIG := Release
DOTNET := dotnet
VERSION := 1.2.0

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
RED := \033[0;31m
NC := \033[0m

## help: Display this help message
help:
	@echo "$(BLUE)Caddy VPS Toolkit - Build & Development$(NC)"
	@echo ""
	@grep -E '^## ' Makefile | sed 's/## //'

## build: Build the project in Debug mode
build:
	@echo "$(BLUE)Building project...$(NC)"
	$(DOTNET) build -c Debug

## build-release: Build the project in Release mode
build-release:
	@echo "$(BLUE)Building release...$(NC)"
	$(DOTNET) build -c Release

## clean: Clean build artifacts
clean:
	@echo "$(BLUE)Cleaning build artifacts...$(NC)"
	$(DOTNET) clean
	rm -rf $(OUTPUT_DIR)
	rm -rf bin/ obj/
	rm -f *.db
	@echo "$(GREEN)Clean complete$(NC)"

## restore: Restore NuGet packages
restore:
	@echo "$(BLUE)Restoring packages...$(NC)"
	$(DOTNET) restore

## publish: Publish release build
publish: clean build-release
	@echo "$(BLUE)Publishing application...$(NC)"
	$(DOTNET) publish -c Release -o $(OUTPUT_DIR)
	@echo "$(GREEN)Published to $(OUTPUT_DIR)$(NC)"

## install: Install to system (requires sudo)
install: publish
	@echo "$(BLUE)Installing to /usr/local/bin...$(NC)"
	sudo cp $(OUTPUT_DIR)/caddy-vps-toolkit /usr/local/bin/
	sudo chmod +x /usr/local/bin/caddy-vps-toolkit
	@echo "$(GREEN)Installation complete$(NC)"
	@echo "Run: caddy-vps-toolkit --version"

## uninstall: Remove from system (requires sudo)
uninstall:
	@echo "$(BLUE)Uninstalling from /usr/local/bin...$(NC)"
	sudo rm -f /usr/local/bin/caddy-vps-toolkit
	@echo "$(GREEN)Uninstallation complete$(NC)"

## test: Run all tests
test: test-unit test-integration

## test-unit: Run unit tests
test-unit:
	@echo "$(BLUE)Running unit tests...$(NC)"
	$(DOTNET) test --configuration Debug --filter Category=Unit -v minimal

## test-integration: Run integration tests
test-integration:
	@echo "$(BLUE)Running integration tests...$(NC)"
	$(DOTNET) test --configuration Debug --filter Category=Integration -v minimal

## test-coverage: Run tests with code coverage
test-coverage:
	@echo "$(BLUE)Running tests with coverage...$(NC)"
	$(DOTNET) test --configuration Debug /p:CollectCoverage=true /p:CoverageFormat=lcov

## lint: Run code analysis
lint:
	@echo "$(BLUE)Running code analysis...$(NC)"
	$(DOTNET) build --configuration Debug /p:TreatWarningsAsErrors=true -v minimal

## format: Format code
format:
	@echo "$(BLUE)Formatting code...$(NC)"
	$(DOTNET) format

## format-check: Check code formatting without changes
format-check:
	@echo "$(BLUE)Checking code formatting...$(NC)"
	$(DOTNET) format --verify-no-changes

## security-scan: Run security vulnerability scan
security-scan:
	@echo "$(BLUE)Scanning for vulnerabilities...$(NC)"
	@command -v dotnet-depends >/dev/null 2>&1 || { echo "Installing tool..."; $(DOTNET) tool install -g dotnet-depends; }
	dotnet-depends

## run: Run the application
run:
	@echo "$(BLUE)Running application...$(NC)"
	$(DOTNET) run -- service list

## run-monitor: Start health monitoring in foreground
run-monitor:
	@echo "$(BLUE)Starting health monitor...$(NC)"
	$(DOTNET) run -- health monitor --interval 30s

## docker-build: Build Docker image
docker-build:
	@echo "$(BLUE)Building Docker image...$(NC)"
	docker build -t sarmkadan/caddy-vps-toolkit:latest .
	docker tag sarmkadan/caddy-vps-toolkit:latest sarmkadan/caddy-vps-toolkit:$(VERSION)
	@echo "$(GREEN)Docker image built$(NC)"

## docker-push: Push Docker image to registry
docker-push: docker-build
	@echo "$(BLUE)Pushing Docker image...$(NC)"
	docker push sarmkadan/caddy-vps-toolkit:latest
	docker push sarmkadan/caddy-vps-toolkit:$(VERSION)
	@echo "$(GREEN)Docker image pushed$(NC)"

## release: Create release package
release: publish
	@echo "$(BLUE)Creating release package...$(NC)"
	tar -czf $(PROJECT_NAME)-linux-x64-$(VERSION).tar.gz -C $(OUTPUT_DIR) .
	sha256sum $(PROJECT_NAME)-linux-x64-$(VERSION).tar.gz > $(PROJECT_NAME)-linux-x64-$(VERSION).tar.gz.sha256
	@echo "$(GREEN)Release package created$(NC)"
	@ls -lh $(PROJECT_NAME)-linux-x64-$(VERSION).*

## bench: Run performance benchmarks
bench:
	@echo "$(BLUE)Running benchmarks...$(NC)"
	$(DOTNET) run --configuration Release -- bench
	@echo "$(GREEN)Benchmarks complete$(NC)"

## docs: Generate documentation
docs:
	@echo "$(BLUE)Generating documentation...$(NC)"
	@echo "Documentation is in docs/ directory"
	@echo "README.md - Quick start and overview"
	@echo "docs/getting-started.md - Installation guide"
	@echo "docs/api-reference.md - Full CLI reference"
	@echo "docs/architecture-deep-dive.md - Technical architecture"
	@echo "docs/deployment.md - Production deployment"
	@echo "docs/faq.md - FAQ and troubleshooting"

## ci-local: Run CI pipeline locally
ci-local: clean restore lint test
	@echo "$(GREEN)CI pipeline complete$(NC)"

## version: Display version information
version:
	@echo "$(BLUE)$(PROJECT_NAME) $(VERSION)$(NC)"
	@echo "Built with .NET 10"
	@$(DOTNET) --version

## info: Display project information
info:
	@echo "$(BLUE)Project Information$(NC)"
	@echo "Name: $(PROJECT_NAME)"
	@echo "Version: $(VERSION)"
	@echo ".NET Version: 10"
	@echo "Configuration: $(BUILD_CONFIG)"
	@echo "Output Directory: $(OUTPUT_DIR)"

## dev-setup: Set up development environment
dev-setup: restore
	@echo "$(BLUE)Setting up development environment...$(NC)"
	$(DOTNET) tool restore
	$(DOTNET) format --install-code-style
	@echo "$(GREEN)Development environment ready$(NC)"

## check: Run pre-commit checks
check: format-check lint test
	@echo "$(GREEN)All checks passed$(NC)"

## watch: Watch for changes and rebuild
watch:
	@echo "$(BLUE)Watching for changes...$(NC)"
	$(DOTNET) watch build

## init-db: Initialize database
init-db:
	@echo "$(BLUE)Initializing database...$(NC)"
	$(DOTNET) run -- config init
	@echo "$(GREEN)Database initialized$(NC)"

## migrate: Run database migrations
migrate:
	@echo "$(BLUE)Running database migrations...$(NC)"
	$(DOTNET) run -- config migrate
	@echo "$(GREEN)Migrations complete$(NC)"

## health-check: Perform health check
health-check:
	@echo "$(BLUE)Performing health checks...$(NC)"
	$(DOTNET) run -- health summary

## config-validate: Validate configuration
config-validate:
	@echo "$(BLUE)Validating configuration...$(NC)"
	$(DOTNET) run -- config validate

## logs: View application logs
logs:
	@echo "$(BLUE)Viewing logs...$(NC)"
	journalctl -u caddy-toolkit-monitor -f

## status: Display application status
status:
	@echo "$(BLUE)Application Status$(NC)"
	systemctl status caddy-toolkit-monitor || echo "Service not running"

## start: Start the application
start:
	@echo "$(BLUE)Starting application...$(NC)"
	sudo systemctl start caddy-toolkit-monitor
	@echo "$(GREEN)Started$(NC)"

## stop: Stop the application
stop:
	@echo "$(BLUE)Stopping application...$(NC)"
	sudo systemctl stop caddy-toolkit-monitor
	@echo "$(GREEN)Stopped$(NC)"

## restart: Restart the application
restart: stop start
	@echo "$(GREEN)Restarted$(NC)"

## backup: Create configuration backup
backup:
	@echo "$(BLUE)Creating backup...$(NC)"
	mkdir -p backups
	$(DOTNET) run -- config export --format json > backups/backup-$$(date +%Y%m%d-%H%M%S).json
	@echo "$(GREEN)Backup created$(NC)"

## restore-backup: Restore from backup
restore-backup:
	@echo "$(BLUE)Restoring from backup...$(NC)"
	@read -p "Enter backup file path: " backup; \
	$(DOTNET) run -- config import --file $$backup

.DEFAULT_GOAL := help

# Print variable values for debugging
print-vars:
	@echo "PROJECT_NAME=$(PROJECT_NAME)"
	@echo "SOLUTION_FILE=$(SOLUTION_FILE)"
	@echo "OUTPUT_DIR=$(OUTPUT_DIR)"
	@echo "BUILD_CONFIG=$(BUILD_CONFIG)"
	@echo "DOTNET=$(DOTNET)"
	@echo "VERSION=$(VERSION)"
