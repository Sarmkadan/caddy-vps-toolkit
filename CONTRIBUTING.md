# Contributing to caddy-vps-toolkit

Thank you for your interest in contributing to caddy-vps-toolkit! This document provides guidelines and instructions for contributing.

## Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please be respectful of all contributors and users.

## Getting Started

### Prerequisites

- .NET 10 SDK installed
- Linux environment (WSL on Windows works)
- Git configured with your name and email
- Familiarity with C# and .NET development

### Development Setup

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/caddy-vps-toolkit.git
cd caddy-vps-toolkit

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Start development
make dev-setup
```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or for bug fixes
git checkout -b fix/bug-description
```

Use descriptive branch names:
- `feature/health-check-improvements`
- `fix/service-list-pagination`
- `docs/api-reference-update`

### 2. Make Your Changes

Follow the coding standards:

#### Code Style

- Use 4-space indentation
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Use meaningful variable and method names
- Keep methods focused and under 30 lines when possible
- Add XML comments for public methods

#### Example

```csharp
/// <summary>
/// Retrieves a service by name with health status.
/// </summary>
/// <param name="name">Service name to retrieve</param>
/// <returns>Service with current health status</returns>
public async Task<ServiceWithHealth> GetServiceWithHealthAsync(string name)
{
    var service = await _repository.GetByNameAsync(name);
    var health = await _healthMonitoring.CheckHealthAsync(name);
    
    return new ServiceWithHealth 
    { 
        Service = service, 
        Health = health 
    };
}
```

### 3. Test Your Changes

```bash
# Run all tests
make test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
make test-coverage

# Format code
make format

# Check for issues
make lint
```

### 4. Commit Your Changes

Write clear, descriptive commit messages:

```bash
git add .
git commit -m "feat: Add health check recovery scripts support

- Implement IRecoveryProvider interface
- Add SystemdRecoveryProvider for systemd-based recovery
- Add CustomScriptRecoveryProvider for custom recovery scripts
- Document recovery configuration in appsettings.json
- Add tests for recovery execution

Closes #123"
```

**Commit message format:**
- `feat:` for new features
- `fix:` for bug fixes
- `docs:` for documentation
- `test:` for tests
- `refactor:` for code refactoring
- `perf:` for performance improvements
- `chore:` for build/tooling changes

### 5. Push and Create Pull Request

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub with:
- Clear title describing the change
- Description of what was changed and why
- Reference to related issues (if applicable)
- Screenshots (for UI changes)
- Testing notes

## Pull Request Guidelines

### Before Submitting

- [ ] Code is properly formatted (`make format`)
- [ ] All tests pass (`make test`)
- [ ] No lint warnings (`make lint`)
- [ ] Code coverage is maintained or improved
- [ ] Commit messages are clear and descriptive
- [ ] Branch is up-to-date with main

### PR Description Template

```markdown
## Description
Brief description of the changes.

## Related Issues
Closes #123

## Changes
- Specific change 1
- Specific change 2
- Specific change 3

## Testing
How to test these changes:
1. Step 1
2. Step 2
3. Step 3

## Screenshots
(if applicable)

## Breaking Changes
- None
- (or describe breaking changes)
```

## Project Structure

```
caddy-vps-toolkit/
├── Domain/              # Domain models and entities
├── Services/            # Business logic services
├── Data/                # Data access and repositories
├── Cli/                 # Command-line interface
├── Integration/         # External system integration
├── Middleware/          # Request pipeline middleware
├── Events/              # Event bus and handlers
├── Utilities/           # Helper and utility functions
├── Tests/               # Unit and integration tests
├── docs/                # Documentation
└── examples/            # Example scripts
```

## Coding Standards

### Naming Conventions

- **Classes/Interfaces**: PascalCase
- **Methods/Properties**: PascalCase
- **Private fields**: _camelCase
- **Local variables**: camelCase
- **Constants**: UPPER_SNAKE_CASE

### Error Handling

Always handle exceptions appropriately:

```csharp
try
{
    await _serviceManagement.AddServiceAsync(service);
}
catch (ValidationException ex)
{
    _logger.LogWarning($"Validation failed: {ex.Message}");
    throw;
}
catch (Exception ex)
{
    _logger.LogError($"Unexpected error: {ex}");
    throw;
}
```

### Async/Await

Always use async/await properly:

```csharp
// Good
public async Task<ServiceResult> GetServiceAsync(string name)
{
    return await _repository.GetByNameAsync(name);
}

// Bad
public Task<ServiceResult> GetServiceAsync(string name)
{
    return _repository.GetByNameAsync(name);
}
```

## Testing Requirements

### Unit Tests

- Test in-memory operations
- Mock external dependencies
- Aim for >80% code coverage
- Use AAA pattern (Arrange, Act, Assert)

```csharp
[Fact]
public async Task AddService_WithValidInput_CreatesService()
{
    // Arrange
    var service = new ManagedService { Name = "api", Port = 8080 };
    
    // Act
    var result = await _serviceManagement.AddServiceAsync(service);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("api", result.Name);
}
```

### Integration Tests

- Test with real database
- Test CLI integration
- Test systemd integration
- Clean up test data after running

## Documentation

### Code Comments

- Only document the "why", not the "what"
- Keep comments concise
- Update comments when changing code

Good:
```csharp
// Use exponential backoff to avoid overwhelming the service
await Task.Delay(Math.Pow(2, retryCount) * 1000);
```

Bad:
```csharp
// Increment retry count
retryCount++;
```

### Documentation Files

- Update README.md for user-facing changes
- Update docs/ files for detailed documentation
- Add examples for new features
- Update CHANGELOG.md

## Release Process

Releases are automated via GitHub Actions, but you should:

1. Update CHANGELOG.md
2. Update version in relevant files
3. Create a git tag: `git tag v1.2.0`
4. Push the tag: `git push origin v1.2.0`

## Reporting Bugs

File issues on GitHub with:

- **Title**: Clear, concise description
- **Description**: What happened vs. expected behavior
- **Steps to reproduce**: Exact steps to trigger the bug
- **System information**:
  - Linux distribution and version
  - .NET version
  - caddy-vps-toolkit version
- **Logs**: Relevant error messages or logs

## Requesting Features

Create feature requests with:

- **Title**: Brief feature description
- **Use case**: Why this feature is needed
- **Expected behavior**: How the feature should work
- **Alternatives**: Other solutions you've considered

## Questions & Support

- GitHub Issues: Bug reports and feature requests
- GitHub Discussions: Questions and general discussion
- Email: https://sarmkadan.com

## Recognition

Contributors will be recognized in:
- CHANGELOG.md
- GitHub contributors page
- Project documentation (if applicable)

## Legal

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to caddy-vps-toolkit! 🎉
