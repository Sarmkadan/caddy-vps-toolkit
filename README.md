## SslCertificateInfoExtensions

The `SslCertificateInfoExtensions` class provides a set of extension methods for working with `SslCertificateInfo` objects. These methods simplify common certificate operations, such as determining the certificate's status, formatting its validity period, and checking its issuer.

Example usage:
```csharp
var certificate = new SslCertificateInfo { ExpiresAt = DateTime.UtcNow.AddDays(30) };
var (status, message) = certificate.GetStatus();
Console.WriteLine($"Certificate status: {status}, Message: {message}");

var validityPeriod = certificate.FormatValidityPeriod();
Console.WriteLine($"Validity period: {validityPeriod}");

var daysUntilExpiry = certificate.FormatDaysUntilExpiry();
Console.WriteLine($"Days until expiry: {daysUntilExpiry}");

var isIssuedBy = certificate.IsIssuedBy("Example CA");
Console.WriteLine($"Is issued by: {isIssuedBy}");

var expiredCertificates = certificate.GetExpiredCertificates();
Console.WriteLine($"Expired certificates: {expiredCertificates.Count()}");

var expiringCertificates = certificate.GetExpiringCertificates();
Console.WriteLine($"Expiring certificates: {expiringCertificates.Count()}");

var issuerOrganization = certificate.GetIssuerOrganization();
Console.WriteLine($"Issuer organization: {issuerOrganization}");
```

## ServicePortExtensions

`ServicePortExtensions` adds a collection of helper methods for the `ServicePort` model, making it easy to reason about port numbers, protocols, and their classifications (privileged, well‑known, registered, dynamic, HTTP, etc.). It also provides utilities for grouping ports, detecting conflicts, and generating human‑readable descriptions.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;

class Program
{
    static void Main()
    {
        // Create a few ServicePort instances
        var httpPort = new ServicePort
        {
            InternalPort = 8080,
            ExternalPort = 80,
            Protocol = "tcp",
            Description = "HTTP traffic"
        };

        var sshPort = new ServicePort
        {
            InternalPort = 22,
            ExternalPort = 22,
            Protocol = "tcp",
            Description = "SSH access"
        };

        // Individual checks
        Console.WriteLine($"httpPort.IsPrivilegedPort(): {httpPort.IsPrivilegedPort()}");
        Console.WriteLine($"sshPort.IsWellKnownPort(): {sshPort.IsWellKnownPort()}");
        Console.WriteLine($"sshPort.IsHttpPort(): {sshPort.IsHttpPort()}");
        Console.WriteLine($"httpPort.GetIanaServiceName(): {httpPort.GetIanaServiceName()}");
        Console.WriteLine($"sshPort.GetPortCategory(): {sshPort.GetPortCategory()}");
        Console.WriteLine($"httpPort.GetFormattedDescription(): {httpPort.GetFormattedDescription()}");

        // Collection helpers
        var ports = new List<ServicePort> { httpPort, sshPort };
        var byInternal = ports.GetPortsByInternalPort(22);
        var byExternal = ports.GetPortsByExternalPort(80);

        Console.WriteLine($"Ports with internal 22: {byInternal.Count}");
        Console.WriteLine($"Ports with external 80: {byExternal.Count}");

        // Conflict detection
        var anotherHttp = new ServicePort
        {
            InternalPort = 8081,
            ExternalPort = 80,
            Protocol = "tcp",
            Description = "Another HTTP service"
        };
        Console.WriteLine($"httpPort conflicts with anotherHttp: {httpPort.ConflictsWith(anotherHttp)}");
    }
}
```

The example demonstrates how to:

* Determine port classifications (`IsPrivilegedPort`, `IsWellKnownPort`, `IsRegisteredPort`, `IsDynamicPort`, `IsHttpPort`).
* Retrieve IANA service names and human‑readable categories (`GetIanaServiceName`, `GetPortCategory`).
* Search collections for ports sharing the same internal or external port (`GetPortsByInternalPort`, `GetPortsByExternalPort`).
* Detect port conflicts (`ConflictsWith`).
* Produce a formatted description that includes protocol, category, and optional IANA name (`GetFormattedDescription`).

## ConfigurationServiceJsonExtensions

`ConfigurationServiceJsonExtensions` adds JSON‑serialization helpers for `ConfigurationService`. It lets you turn the whole configuration into a JSON string, recreate a service from JSON, and safely attempt deserialization without throwing exceptions. Internally it uses a lightweight in‑memory `IConfigurationRepository` implementation to hold the deserialized key/value pairs.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;

class Program
{
    static async Task Main()
    {
        // Create a service and populate some configuration values
        var configService = new ConfigurationService();
        await configService.SetValueAsync("AppName", "Demo");
        await configService.SetValueAsync("Version", "1.0");

        // Serialize the whole configuration to JSON (indented for readability)
        string json = configService.ToJson(indented: true);
        Console.WriteLine("Serialized JSON:");
        Console.WriteLine(json);

        // Deserialize back to a new ConfigurationService instance
        var deserialized = ConfigurationServiceJsonExtensions.FromJson(json);
        Console.WriteLine("\nDeserialized value:");
        Console.WriteLine(await deserialized!.GetValueAsync("AppName"));

        // Try‑parse example that never throws
        if (ConfigurationServiceJsonExtensions.TryFromJson(json, out var parsed))
        {
            Console.WriteLine("\nTryFromJson succeeded:");
            Console.WriteLine(await parsed.GetValueAsync("Version"));
        }
    }
}
```

The example demonstrates the three public extension members (`ToJson`, `FromJson`, `TryFromJson`) together with the repository methods (`GetValueAsync`, `SetValueAsync`, `DeleteAsync`, `GetAllAsync`) that are used internally by the deserialized service.

## ServiceRepositoryJsonExtensions

`ServiceRepositoryJsonExtensions` provides JSON serialization and deserialization helpers for `ManagedService` objects and collections. It simplifies working with service configurations by allowing you to serialize services to JSON strings and deserialize them back into objects, with support for both individual services and collections. The extension methods handle null safety and provide both throwing and non-throwing variants for robust error handling.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Domain.Models;

class Program
{
    static void Main()
    {
        // Create a sample managed service
        var service = new ManagedService
        {
            Id = "web-app-01",
            Name = "Web Application",
            Description = "Main web application service",
            Status = ServiceStatus.Running,
            Ports = new List<ServicePort>
            {
                new ServicePort { InternalPort = 8080, ExternalPort = 80, Protocol = "tcp" }
            },
            EnvironmentVariables = new Dictionary<string, string>
            {
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "ASPNETCORE_URLS", "http://+:8080" }
            }
        };

        // Serialize a single service to JSON
        string json = service.ToJson(indented: true);
        Console.WriteLine("Single service JSON:");
        Console.WriteLine(json);

        // Deserialize back to a service
        var deserialized = ServiceRepositoryJsonExtensions.FromJson(json);
        Console.WriteLine($"\nDeserialized service: {deserialized?.Name}");

        // Try-parse example that never throws
        if (ServiceRepositoryJsonExtensions.TryFromJson(json, out var parsed))
        {
            Console.WriteLine($"TryFromJson succeeded: {parsed?.Id}");
        }

        // Serialize a collection of services
        var services = new List<ManagedService> { service };
        string collectionJson = services.ToJson(indented: true);
        Console.WriteLine("\nCollection JSON:");
        Console.WriteLine(collectionJson);

        // Deserialize collection back to a list
        var deserializedList = ServiceRepositoryJsonExtensions.FromJsonToList(collectionJson);
        Console.WriteLine($"\nDeserialized list count: {deserializedList.Count}");

        // Try-parse collection example
        if (ServiceRepositoryJsonExtensions.TryFromJsonToList(collectionJson, out var parsedList))
        {
            Console.WriteLine($"TryFromJsonToList succeeded: {parsedList.Count} services");
        }
    }
}
```

The example demonstrates all five public extension members: `ToJson` and `FromJson` for individual services, `TryFromJson` for safe parsing, `ToJson` and `FromJsonToList` for collections, and `TryFromJsonToList` for safe collection parsing.

## AppConfigurationBuilderExtensions

The `AppConfigurationBuilderExtensions` class provides extension methods for configuring application settings. It simplifies loading configuration from various sources including JSON files, environment variables, settings files, default settings, JSON strings, or object-based configurations.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Configuration;

class Program
{
    static void Main()
    {
        // Configure application settings from a JSON file
        var builder1 = new AppConfigurationBuilder()
            .WithJsonFile("appsettings.json");

        // Configure application settings from environment variables
        var builder2 = new AppConfigurationBuilder()
            .WithEnvironmentVariables();

        // Configure application settings from a settings file
        var builder3 = new AppConfigurationBuilder()
            .WithSettings("custom-settings.json");

        // Configure application settings with default settings
        var builder4 = new AppConfigurationBuilder()
            .WithDefaultSettings();

        // Configure application settings from a JSON string
        var builder5 = new AppConfigurationBuilder()
            .WithJsonString("{\"Key\": \"Value\"}");

        // Configure application settings from an object configuration
        var builder6 = new AppConfigurationBuilder()
            .WithObjectConfiguration(config =>
            {
                config["CustomKey"] = "CustomValue";
            });
    }
}
```

## Error Handling Convention

This toolkit defines a consistent error-handling convention for extension methods:

### Expected Failures vs. Programmer Errors

- **Expected failures**: Use `Result<T>` pattern - methods return a `Result<T>` or `Result` object containing either the successful result or an error message
- **Programmer errors**: Throw exceptions - methods throw `ArgumentNullException`, `ArgumentException`, or other appropriate exceptions for invalid arguments or null references

### When to Use Each Pattern

**Use `Result<T>` for:**
- JSON deserialization operations that can fail due to invalid input
- External service calls that may fail (network issues, timeouts)
- Operations that can legitimately fail in normal usage (file parsing, network requests)

**Throw exceptions for:**
- Null arguments (use `ArgumentNullException.ThrowIfNull()`)
- Invalid argument values (use `ArgumentException.ThrowIfNullOrEmpty()`)
- Programming errors (wrong types, invalid states)
- Configuration errors that should fail fast during development

### Examples

```csharp
// Good: Using Result<T> for expected failure
var result = ProcessUtilitiesJsonExtensions.FromJson(invalidJson);
if (result.IsSuccess)
{
    var config = result.Data;
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}

// Good: Throwing exception for programmer error
var certificate = new SslCertificateInfo();
// This will throw ArgumentNullException if certificate is null
certificate.GetStatus();

// Good: Using Result<T> for expected failure  
var serviceResult = ConfigurationServiceJsonExtensions.FromJson(invalidJson);
if (result.IsSuccess)
{
    var service = result.Data;
}
else
{
    Console.WriteLine($"Failed to deserialize: {result.ErrorMessage}");
}
```

## UpstreamManagerServiceExtensions

The `UpstreamManagerServiceExtensions` class provides extension methods for `UpstreamManagerService` that add convenience and batch operations for upstream pool management, health monitoring, and configuration generation. These methods simplify common operations like retrieving pools, checking health status, generating Caddy configuration, and recording upstream results.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Services;
using CaddyVpsToolkit.Domain.Models;

class Program
{
    static async Task Main()
    {
        // Create and initialize the upstream manager service
        var upstreamManager = new UpstreamManagerService();
        // Assume service is initialized with some pools and upstreams
        
        // Try to get a specific pool without throwing exceptions
        var (success, pool) = await upstreamManager.TryGetPoolAsync("my-pool");
        if (success && pool != null)
        {
            Console.WriteLine($"Found pool: {pool.Name}");
        }
        
        // Get all pools for a specific service
        var servicePools = await upstreamManager.GetPoolsAsync(p => p.ServiceId == "my-service");
        Console.WriteLine($"Found {servicePools.Count} pools for service");
        
        // Get summary of all pools with health information
        var poolSummaries = await upstreamManager.GetPoolSummariesAsync();
        foreach (var summary in poolSummaries)
        {
            Console.WriteLine($"Pool {summary.Name}: {summary.HealthyUpstreams}/{summary.TotalUpstreams} healthy upstreams");
        }
        
        // Get total active connections across all pools
        var totalConnections = await upstreamManager.GetTotalActiveConnectionsAsync();
        Console.WriteLine($"Total active connections: {totalConnections}");
        
        // Get total healthy upstreams across all pools
        var totalHealthy = await upstreamManager.GetTotalHealthyUpstreamsAsync();
        Console.WriteLine($"Total healthy upstreams: {totalHealthy}");
        
        // Get list of unhealthy upstream IDs
        var unhealthyUpstreams = await upstreamManager.GetUnhealthyUpstreamIdsAsync();
        Console.WriteLine($"Unhealthy upstreams: {string.Join(", ", unhealthyUpstreams)}");
        
        // Generate Caddy configuration for all enabled pools of a service
        var caddyConfig = await upstreamManager.GenerateCaddyConfigForAllEnabledPoolsAsync("my-service");
        Console.WriteLine($"Generated Caddy config:\n{caddyConfig}");
        
        // Select an upstream server from a pool
        var selectedUpstream = await upstreamManager.SelectUpstreamAsync("my-pool");
        if (selectedUpstream != null)
        {
            Console.WriteLine($"Selected upstream: {selectedUpstream.UpstreamId}");
        }
        
        // Record results for multiple upstreams in a batch
        var results = new List<(string poolId, string upstreamId, bool succeeded, int responseTimeMs)>
        {
            ("my-pool", "upstream-1", true, 45),
            ("my-pool", "upstream-2", false, 0)
        };
        await upstreamManager.RecordUpstreamResultsAsync(results);
        
        // Remove a pool if needed
        var removed = await upstreamManager.TryRemovePoolAsync("old-pool");
        Console.WriteLine($"Pool removed: {removed}");
    }
}
```

## UpstreamSelectorTests

`UpstreamSelectorTests` is an xUnit test suite that validates the upstream selection strategies implemented by `UpstreamSelector`, covering weighted random, least-connections, uniform random, round-robin, and IP-hash (client affinity) algorithms. The tests verify correct behavior under normal conditions—respecting configured weights, choosing the server with the fewest active connections, cycling through servers evenly, and pinning a client IP to the same server—as well as edge cases such as empty server lists, single-server pools, and zero total weight falling back to uniform selection.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests.Services;

class Program
{
    static void Main()
    {
        // Exercise the upstream selection strategy tests directly.
        var tests = new UpstreamSelectorTests();

        // Weighted random strategy
        tests.Select_WeightedRandom_RespectsWeights();
        tests.Select_WeightedRandom_WithZeroTotalWeight_FallsBackToUniform();
        tests.Select_WeightedRandom_WithSingleServer_ReturnsThatServer();
        tests.Select_WeightedRandom_WithEmptyList_ReturnsNull();

        // Least-connections strategy
        tests.Select_LeastConnections_SelectsServerWithFewestConnections();

        // Uniform random strategy
        tests.Select_Random_SelectsUniformly();

        // Round-robin strategy
        tests.Select_RoundRobin_CyclesThroughServers();

        // IP-hash (client affinity) strategy
        tests.Select_IpHash_PinsClientToSameServer();

        Console.WriteLine("All upstream selector strategies verified.");
    }
}
```

## MaintenanceWindow

`MaintenanceWindow` represents a recurring time frame during which maintenance operations may be performed on a service or server. It stores the daily start and end times (`StartTime`, `EndTime`), the days on which the window applies (`DaysOfWeek`), an optional human-readable `Description`, and an `IsEnabled` flag to activate or deactivate the schedule. Call `Validate()` to confirm the configuration is well-formed, and read `IsInWindow` to determine whether the current moment falls inside the active maintenance period.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Domain.Models;

class Program
{
    static void Main()
    {
        var window = new MaintenanceWindow
        {
            StartTime = "02:00",
            EndTime = "04:00",
            DaysOfWeek = "Saturday,Sunday",
            Description = "Weekly database maintenance",
            IsEnabled = true
        };

        // Ensure the configured window is valid before relying on it.
        window.Validate();

        Console.WriteLine($"Maintenance enabled: {window.IsEnabled}");
        Console.WriteLine($"Currently in window: {window.IsInWindow}");
        Console.WriteLine($"Schedule: {window.StartTime} - {window.EndTime} on {window.DaysOfWeek}");
        Console.WriteLine($"Description: {window.Description}");
    }
}
```

## IBackupService

`IBackupService` defines the contract for backing up services hosted on the VPS: it allows you to create new backups, enumerate the backups that exist, verify a backup's integrity before relying on it, and restore service state from a chosen backup. The built-in `BackupService` implementation exposes these operations asynchronously and reports outcomes through `BackupManifest` objects, whose nested `BackupIntegrityResult` type captures the outcome of an integrity check.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

class Program
{
    static async Task Main()
    {
        var backupService = new BackupService();

        // Create a new backup and remember its identifier
        string backupId = await backupService.CreateBackupAsync();
        Console.WriteLine($"Created backup: {backupId}");

        // List every backup that is currently available
        IReadOnlyList<string> backups = await backupService.ListBackupsAsync();
        Console.WriteLine($"Available backups: {backups.Count}");

        // Verify the integrity of the backup before restoring it
        BackupManifest.BackupIntegrityResult integrity =
            await backupService.VerifyBackupAsync(backupId);
        Console.WriteLine($"Integrity check result: {integrity}");

        // Restore service state from the verified backup
        BackupManifest manifest = await backupService.RestoreBackupAsync(backupId);
        Console.WriteLine($"Restore completed: {manifest}");
    }
}
```

## ILogAggregationService

`ILogAggregationService` defines the contract for aggregating and reading application logs across one or more log sources. The built-in `LogAggregationService` implementation discovers the available sources from the configured logs directory (by default `AppConstants.LogsDirectory`, with a constructor overload that accepts a custom directory), retrieves complete log listings asynchronously, and supports tailing the most recent entries. Results are returned as `LogEntry` objects, so callers can work with structured records instead of parsing raw log files themselves.

Example usage:
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Services;

class Program
{
    static async Task Main()
    {
        // Uses the default logs directory (AppConstants.LogsDirectory)
        var logService = new LogAggregationService();

        // Discover the available log sources
        IReadOnlyList<string> sources = logService.GetLogSources();
        Console.WriteLine($"Discovered {sources.Count} log sources");

        if (sources.Count > 0)
        {
            string source = sources[0];

            // Retrieve the log entries recorded for the source
            IReadOnlyList<LogEntry> logs = await logService.GetLogsAsync(source);
            Console.WriteLine($"Retrieved {logs.Count} log entries from {source}");

            // Tail the most recent entries
            IReadOnlyList<LogEntry> recent = await logService.TailAsync(source);
            foreach (LogEntry entry in recent)
            {
                Console.WriteLine(entry);
            }
        }
    }
}
```

## CollectionExtensionsUnitTests

`CollectionExtensionsUnitTests` is an xUnit test suite that validates the collection helper extension methods exposed by `CollectionExtensions`, covering safe element access (`SafeGet`), null/empty detection (`IsNullOrEmpty`), first-element retrieval (`FirstOrDefault`), chunking (`Batch`), and predicate-based splitting (`Partition`). The tests confirm correct results for typical inputs and verify the documented edge-case behavior—returning defaults for missing data and throwing `ArgumentNullException`/`ArgumentException` when invalid arguments such as null collections or non-positive batch sizes are supplied.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests;

class Program
{
    static void Main()
    {
        // Exercise the collection extension tests directly.
        var tests = new CollectionExtensionsUnitTests();

        // SafeGet returns the element at a valid index and the default value otherwise.
        tests.SafeGet_WithValidIndex_ReturnsCorrectElement();
        tests.SafeGet_WithNegativeIndex_ReturnsDefault();
        tests.SafeGet_WithOutOfRangeIndex_ReturnsDefault();
        tests.SafeGet_WithNullList_ReturnsDefault();

        // IsNullOrEmpty distinguishes populated, empty, and null collections.
        tests.IsNullOrEmpty_WithNonEmptyCollection_ReturnsFalse();
        tests.IsNullOrEmpty_WithEmptyCollection_ReturnsTrue();
        tests.IsNullOrEmpty_WithNullCollection_ReturnsTrue();

        // FirstOrDefault returns the first element, the default for empty input,
        // and throws ArgumentNullException for a null collection.
        tests.FirstOrDefault_WithNonEmptyCollection_ReturnsFirstElement();
        tests.FirstOrDefault_WithEmptyCollection_ReturnsDefault();
        tests.FirstOrDefault_WithNullCollection_ThrowsArgumentNullException();

        // Batch splits a collection into fixed-size chunks and validates arguments.
        tests.Batch_WithValidBatchSize_CreatesCorrectNumberOfBatches();
        tests.Batch_WithBatchSizeOne_CreatesSingleElementBatches();
        tests.Batch_WithBatchSizeEqualToCount_CreatesSingleBatch();
        tests.Batch_WithBatchSizeLargerThanCount_CreatesSingleBatch();
        tests.Batch_WithNullCollection_ThrowsArgumentNullException();
        tests.Batch_WithNonPositiveBatchSize_ThrowsArgumentException();

        // Partition splits a collection into matching and non-matching lists.
        tests.Partition_WithMatchingPredicate_SplitsCorrectly();
        tests.Partition_WithAllMatchingPredicate_ReturnsAllInMatching();
        tests.Partition_WithNoMatchingPredicate_ReturnsAllInNotMatching();
        tests.Partition_WithNullCollection_ReturnsTwoEmptyLists();

        Console.WriteLine("All collection extension behaviors verified.");
    }
}
```

## StringExtensionsUnitTests

`StringExtensionsUnitTests` is an xUnit test suite that validates the string helper extension methods exposed by `StringExtensions`, covering whitespace detection (`IsNullOrWhiteSpace`), title casing (`ToTitleCase`), kebab-case conversion (`ToKebabCase`), camel-case conversion (`ToCamelCase`), and length-limited truncation (`Truncate`). The tests confirm correct results for typical inputs—such as converting camel-case text to kebab-case or truncating over-long strings—and verify the documented edge-case behavior of returning null or empty input unchanged.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests;

class Program
{
    static void Main()
    {
        // Exercise the string extension tests directly.
        var tests = new StringExtensionsUnitTests();

        // IsNullOrWhiteSpace distinguishes null, empty, whitespace-only,
        // and populated strings.
        tests.IsNullOrWhiteSpace_NullInput_ReturnsTrue();
        tests.IsNullOrWhiteSpace_EmptyString_ReturnsTrue();
        tests.IsNullOrWhiteSpace_WhitespaceOnly_ReturnsTrue();
        tests.IsNullOrWhiteSpace_NonWhitespaceString_ReturnsFalse();

        // ToTitleCase converts words to title case and passes through
        // null/empty input unchanged.
        tests.ToTitleCase_NullInput_ReturnsNull();
        tests.ToTitleCase_EmptyString_ReturnsEmpty();
        tests.ToTitleCase_SingleCharacter_ReturnsUppercase();
        tests.ToTitleCase_MultipleWords_ReturnsTitleCased();

        // ToKebabCase converts identifiers to kebab-case.
        tests.ToKebabCase_NullInput_ReturnsNull();
        tests.ToKebabCase_EmptyString_ReturnsEmpty();
        tests.ToKebabCase_SingleWord_ReturnsLowercase();
        tests.ToKebabCase_CamelCase_ReturnsKebabCase();
        tests.ToKebabCase_AlreadyKebabCase_ReturnsSame();

        // ToCamelCase converts kebab-case identifiers to camelCase.
        tests.ToCamelCase_NullInput_ReturnsNull();
        tests.ToCamelCase_EmptyString_ReturnsEmpty();
        tests.ToCamelCase_SingleWord_ReturnsLowercase();
        tests.ToCamelCase_KebabCase_ReturnsCamelCase();

        // Truncate shortens long strings while leaving shorter ones intact.
        tests.Truncate_NullInput_ReturnsNull();
        tests.Truncate_EmptyString_ReturnsEmpty();
        tests.Truncate_StringShorterThanMaxLength_ReturnsOriginal();

        Console.WriteLine("All string extension behaviors verified.");
    }
}
```

## ProcessUtilitiesTests

`ProcessUtilitiesTests` is an xUnit test suite that validates the process-execution helpers provided by `ProcessUtilities`. It confirms that `ExecuteAsync` runs commands successfully while capturing their output, returns an error result with `IsSuccess` set to false when a command fails, and produces a timeout result when a command exceeds its allowed duration. It also verifies that `ProcessResult.GetOutput()` prefers captured error output when one is present and falls back to standard output otherwise.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Tests;

class Program
{
    static async Task Main()
    {
        // Exercise the process utilities tests directly.
        var tests = new ProcessUtilitiesTests();

        // ExecuteAsync runs a command and captures its output on success.
        await tests.ExecuteAsync_ShouldReturnSuccessAndCaptureOutput();

        // A failing command produces an error result with IsSuccess == false.
        await tests.ExecuteAsync_WhenCommandFails_ShouldReturnErrorAndIsSuccessFalse();

        // A command that exceeds its timeout yields a timeout result.
        await tests.ExecuteAsync_WhenTimeoutOccurs_ShouldReturnTimeoutResult();

        // GetOutput prefers error output when present, otherwise standard output.
        tests.ProcessResult_GetOutput_ReturnsErrorWhenErrorIsPresent();
        tests.ProcessResult_GetOutput_ReturnsOutputWhenNoError();

        Console.WriteLine("All process utilities behaviors verified.");
    }
}
```

## PerformanceMonitorUnitTests

`PerformanceMonitorUnitTests` is an xUnit test suite that validates the performance monitoring capabilities, ensuring accurate timer initialization, milestone tracking, report generation, and proper resource disposal. The tests verify that elapsed times increase correctly over time, null or empty milestone names are handled gracefully, and large numbers of milestones produce complete reports. It also confirms that the monitor integrates correctly with C# `using` statements for automatic disposal.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests;

class Program
{
    static void Main()
    {
        // Exercise the performance monitor tests directly.
        var tests = new PerformanceMonitorUnitTests();

        // Verify that the constructor initializes the operation name and starts the timer.
        tests.Constructor_ShouldInitializeOperationNameAndStartTimer();

        // Check that milestones record correct elapsed times.
        tests.MarkMilestone_ShouldRecordCorrectElapsedTimes();

        // Ensure the generated report contains the expected header and milestone formatting.
        tests.GetReport_ShouldContainHeaderAndMilestonesFormatting();

        // Confirm that disposal stops the timer without throwing exceptions.
        tests.Dispose_ShouldStopTimerWithoutThrowing();

        // Verify graceful handling of null or empty milestone names.
        tests.MarkMilestone_WithNullOrEmptyName_ShouldHandleGracefully();

        // Check that elapsed milliseconds increase over time.
        tests.GetElapsedMs_ShouldIncreaseOverTime();

        // Ensure the monitor works correctly within a using statement for automatic disposal.
        tests.UsingStatement_ShouldDisposeAutomatically();

        // Validate that a large number of milestones produces a report with all entries.
        tests.LargeNumberOfMilestones_ShouldProduceReportWithAllEntries();

        Console.WriteLine("All performance monitor behaviors verified.");
    }
}
```

## BatchProcessorTests

`BatchProcessorTests` is an xUnit test suite that validates the batch processing logic, ensuring items are correctly divided into complete batches, partial final batches, single-item batches, and empty collections are handled gracefully. It also verifies that custom object types are processed correctly and that the constructor properly enforces validation rules for batch size and process function arguments.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Tests.Processing;

class Program
{
    static async Task Main()
    {
        // Exercise the batch processor tests directly.
        var tests = new BatchProcessorTests();

        // Verify batch size validation
        tests.Constructor_WithZeroBatchSize_ThrowsArgumentException();
        tests.Constructor_WithNegativeBatchSize_ThrowsArgumentException();
        tests.Constructor_WithNullProcessFunction_ThrowsArgumentNullException();

        // Test processing scenarios
        await tests.ProcessAsync_WithExactBatchSize_ProcessesAllItemsInCompleteBatches();
        await tests.ProcessAsync_WithPartialFinalBatch_ProcessesRemainingItemsInFinalBatch();
        await tests.ProcessAsync_WithSingleItem_CreatesSingleItemBatch();
        await tests.ProcessAsync_WithEmptyCollection_DoesNotThrow();
        await tests.ProcessAsync_WithCustomObjectType_ProcessesCorrectly();

        // Access test metadata
        Console.WriteLine($"Test Suite ID: {tests.Id}");
        Console.WriteLine($"Test Suite Name: {tests.Name}");

        Console.WriteLine("All batch processor behaviors verified.");
    }
}
```

## AdaptiveLoadBalancerTests

`AdaptiveLoadBalancerTests` is an xUnit test suite that validates the adaptive load balancing evaluation logic, ensuring pools are scored and ranked according to server health, enabled state, latency, and effective weights. The tests confirm that empty or fully unhealthy pools yield empty evaluations or null selections, that unhealthy and disabled servers are marked ineligible, and that recording successful or failed request outcome keeps per-server metrics up to date. They also verify that recalibrating a pool resets metrics for all servers, that lower-latency servers are prioritized, and that equal scores fall back to effective weight when selecting a server.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Tests.LoadBalancing;

class Program
{
    static async Task Main()
    {
        // Exercise the adaptive load balancer tests directly.
        var tests = new AdaptiveLoadBalancerTests();

        // Empty pools yield an empty evaluation.
        await tests.EvaluatePoolAsync_WithEmptyPool_ReturnsEmptyEvaluation();
        await tests.EvaluatePoolAsync_WithNoServers_ReturnsEmptyEvaluation();

        // A single-server pool selects that server; multiple servers get ranked scores.
        await tests.EvaluatePoolAsync_WithSingleServer_ReturnsThatServerAsSelected();
        await tests.EvaluatePoolAsync_WithMultipleServers_ReturnsRankedScores();

        // Unhealthy and disabled servers are marked ineligible.
        await tests.EvaluatePoolAsync_WithUnhealthyServer_MarksAsIneligible();
        await tests.EvaluatePoolAsync_WithDisabledServer_MarksAsIneligible();

        // A pool where every server is unhealthy produces a null selection.
        await tests.EvaluatePoolAsync_WithAllUnhealthyServers_ReturnsNullSelection();

        // Recording outcomes updates per-server metrics.
        tests.RecordOutcomeAsync_WithSuccessfulRequest_UpdatesMetrics();
        tests.RecordOutcomeAsync_WithFailedRequest_UpdatesMetrics();

        // Effective weight falls back to the base weight when none is adaptive.
        await tests.GetEffectiveWeightAsync_WithNoAdaptiveWeight_ReturnsBaseWeight();

        // Recalibration resets metrics for every server in the pool.
        await tests.RecalibratePoolAsync_ResetsMetricsForAllServers();

        // Lower-latency servers are prioritized; equal scores fall back to weight.
        await tests.EvaluatePoolAsync_PrioritizesLowerLatencyServers();
        await tests.EvaluatePoolAsync_WithEqualScores_FallsBackToEffectiveWeight();

        Console.WriteLine("All adaptive load balancer behaviors verified.");
    }
}
```

## UpstreamHealthTrackerUnitTests

`UpstreamHealthTrackerUnitTests` is an xUnit test suite that validates the behavior of the `UpstreamHealthTracker` class, which manages the health state of upstream servers in a load balancing pool. The tests cover recording probe results (success/failure), updating counters and thresholds, marking servers as unhealthy or healthy, updating average response times, retrieving snapshots, and draining connections.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Tests.LoadBalancing;

class Program
{
    static async Task Main()
    {
        // Exercise the upstream health tracker tests directly.
        var tests = new UpstreamHealthTrackerUnitTests();

        // Test recording a successful probe increments the success counter.
        await tests.RecordProbeResultAsync_WithSuccessfulProbe_IncrementsSuccessCounter();

        // Test recording a failed probe increments the failure counter.
        await tests.RecordProbeResultAsync_WithFailedProbe_IncrementsFailureCounter();

        // Test getting a snapshot for an existing upstream returns the correct data.
        await tests.GetSnapshotAsync_WithExistingUpstream_ReturnsCorrectSnapshot();

        Console.WriteLine("All upstream health tracker tests verified.");
    }
}
```

## ArgumentParserTests

`ArgumentParserTests` is an xUnit test suite that validates the argument parsing logic, covering boolean flag detection, value extraction for both `=` and space-separated formats, case-insensitive matching, handling of repeated arguments, and edge cases like unknown flags or missing values. The tests confirm correct behavior for typical command-line inputs as well as documented edge-case scenarios.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests.Cli;

class Program
{
    static void Main()
    {
        // Exercise the argument parser tests directly.
        var tests = new ArgumentParserTests();

        // Boolean flag detection
        tests.HasFlag_SingleBooleanFlag_ReturnsTrue();
        tests.HasFlag_MultipleBooleanFlags_ReturnsTrueForEach();
        tests.HasFlag_FlagNotPresent_ReturnsFalse();
        tests.HasFlag_CaseInsensitiveMatching_ReturnsTrue();
        tests.HasFlag_RepeatedBooleanFlags_ReturnsTrue();
        tests.HasFlag_UnknownFlag_ReturnsFalse();

        // Value extraction with equals format
        tests.GetFlagValue_BooleanFlag_ReturnsEmptyString();
        tests.GetFlagValue_EqualsFormat_SingleValue_ReturnsValue();
        tests.GetFlagValue_EqualsFormat_MultipleValues_ReturnsCorrectValues();
        tests.GetFlagValue_EqualsFormat_WithSpecialCharacters_ReturnsValue();
        tests.GetFlagValue_EqualsFormat_FlagNotPresent_ReturnsNull();
        tests.GetFlagValue_EqualsFormat_CaseInsensitiveMatching_ReturnsValue();
        tests.GetFlagValue_RepeatedEqualsFormat_LastValueWins();

        // Value extraction with space format
        tests.GetFlagValue_SpaceFormat_SingleValue_ReturnsValue();
        tests.GetFlagValue_SpaceFormat_MultipleValues_ReturnsCorrectValues();
        tests.GetFlagValue_SpaceFormat_ValueFollowedByFlag_ReturnsEmptyString();
        tests.GetFlagValue_SpaceFormat_ValueAtEndOfArgs_ReturnsEmptyString();
        tests.GetFlagValue_RepeatedArgs_LastValueWins();
        tests.GetFlagValue_MixedFormats_LastValueWins();

        // Unknown flag handling
        tests.GetFlagValue_UnknownFlag_ReturnsNull();

        Console.WriteLine("All argument parser behaviors verified.");
    }
}
```

## DateTimeExtensionsUnitTests

`DateTimeExtensionsUnitTests` is an xUnit test suite that validates the date and time helper extension methods, covering relative time formatting, ISO 8601 serialization, day calculations, and duration string generation. The tests verify correct behavior for typical inputs and edge cases such as dates in the past, present, and future, as well as accurate working day calculations.

Example usage:
```csharp
using System;

class Program
{
    static void Main()
    {
        var now = DateTime.UtcNow;
        var pastDate = now.AddDays(-2);
        var futureDate = now.AddDays(5);

        Console.WriteLine($"Relative time: {pastDate.ToRelativeTime()}");
        Console.WriteLine($"ISO 8601 format: {now.ToIso8601()}");
        Console.WriteLine($"Start of day: {now.StartOfDay()}");
        Console.WriteLine($"Is past: {pastDate.IsPast()}");
        Console.WriteLine($"Is today: {now.IsToday()}");
        Console.WriteLine($"Is future: {futureDate.IsFuture()}");
        Console.WriteLine($"Working days between: {pastDate.WorkingDaysBetween(futureDate)}");
        Console.WriteLine($"Duration string: {now.Subtract(pastDate).ToDurationString()}");
    }
}
```

## TemplateEngineEdgeTests

`TemplateEngineEdgeTests` is an xUnit test suite that validates the edge-case behavior of the template engine's placeholder rendering logic. It ensures that templates containing missing, nested, empty, or whitespace-only placeholders are handled gracefully, while correctly substituting known variables, preserving special characters, and supporting various data types like numbers and booleans.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests.Utilities;

class Program
{
    static void Main()
    {
        // Exercise the template engine edge-case tests directly.
        var tests = new TemplateEngineEdgeTests();

        tests.Render_OnlyMissingPlaceholders_ReturnsTemplateUnchanged();
        tests.Render_MixedKnownAndUnknownPlaceholders_SubstitutesKnownOnly();
        tests.Render_NestedBraces_LeavesNestedBracesIntact();
        tests.Render_EmptyBraces_LeavesEmptyBracesIntact();
        tests.Render_BracesWithWhitespace_LeavesWhitespaceBracesIntact();
        tests.Render_RepeatedTokens_AllSubstituted();
        tests.Render_SpecialCharactersInValues_Preserved();
        tests.Render_NumericValues_ConvertedToString();
        tests.Render_BooleanValues_ConvertedToString();
        tests.Render_VeryLargeTemplate_AllPlaceholdersSubstituted();
        tests.Render_WhitespaceAroundPlaceholders_Preserved();
        tests.Render_PlaceholdersAtStartAndEnd_HandledCorrectly();
        tests.Render_ConsecutivePlaceholders_HandledCorrectly();
        tests.Render_PlaceholdersWithUnderscoresAndNumbers_WorkCorrectly();
        tests.Render_StaticMethod_MissingPlaceholdersHandled();
        tests.Render_StaticMethod_NullVariables_CreatesEmptyDictionary();
        tests.Render_WhitespaceOnlyTemplate_ReturnsWhitespace();
        tests.Render_OnlyNewlines_ReturnsNewlines();

        Console.WriteLine("All template engine edge-case behaviors verified.");
    }
}
```

## PathUtilitiesTests

`PathUtilitiesTests` is an xUnit test suite that validates the path utility helpers provided by `PathUtilities`. It covers relative path resolution, safe path combination, path normalization, directory size calculation, human-readable file size formatting, executability checks, unique file path generation, directory creation, and file name sanitization. Filesystem tests run inside a temporary root directory that is removed when the instance is disposed.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests;

class Program
{
    static void Main()
    {
        // Exercise the path utilities tests directly.
        var tests = new PathUtilitiesTests();

        // GetRelativePath returns expected relative path for valid inputs.
        tests.GetRelativePath_ValidInputs_ReturnsExpected("C:\\Folder\\Sub", "C:\\Folder\\Sub\\File.txt", "File.txt");
        tests.GetRelativePath_ValidInputs_ReturnsExpected("/usr/local/bin", "/usr/local/bin/script.sh", "script.sh");

        // SafeCombine combines parts safely and throws on path traversal.
        tests.SafeCombine_ValidParts_ReturnsCombinedPath();
        tests.SafeCombine_PathTraversal_Throws();

        // NormalizePath normalizes mixed separators.
        tests.NormalizePath_ValidInputs_ReturnsNormalized("folder\\subfolder/file.txt", "folder\\subfolder\\file.txt");

        // GetDirectorySize returns zero for non-existent directory and sum for existing files.
        tests.GetDirectorySize_NonExistent_ReturnsZero();
        tests.GetDirectorySize_WithFiles_ReturnsSum();

        // FormatFileSize formats bytes to human-readable string.
        tests.FormatFileSize_Values_ReturnsHumanReadable(1024, "1 KB");
        tests.FormatFileSize_Values_ReturnsHumanReadable(1536, "1.5 KB");

        // IsExecutable checks for .exe on Windows.
        tests.IsExecutable_NonExistent_ReturnsFalse();
        // Note: Windows-specific tests are skipped on non-Windows platforms.

        // GetUniqueFilePath returns a different name if file exists, same if not.
        tests.GetUniqueFilePath_FileExists_ReturnsDifferentName();
        tests.GetUniqueFilePath_FileDoesNotExist_ReturnsSamePath();

        // EnsureDirectoryExists creates a directory if it doesn't exist.
        tests.EnsureDirectoryExists_NewDirectory_CreatesIt();

        // SanitizeFileName removes invalid characters.
        tests.SanitizeFileName_RemovesInvalidChars("invalid<name>.txt", "invalidname.txt");

        Console.WriteLine("All path utilities behaviors verified.");
    }
}
```

## NotificationServiceTests

`NotificationServiceTests` is an xUnit test suite that validates the duplicate suppression functionality of the `NotificationService` class. It tests constructor validation, duplicate detection and suppression logic, behavior with multiple providers, concurrent scenarios, and notification extension methods.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Tests.Notifications;

class Program
{
    static async Task Main()
    {
        // Exercise the notification service tests directly.
        var tests = new NotificationServiceTests();

        // Test constructor validation
        tests.Constructor_WithNullLogger_ThrowsArgumentNullException();
        tests.Constructor_WithNullOptions_UsesDefaultOptions();

        // Test duplicate suppression behavior
        await tests.SendAsync_WithNullNotification_ThrowsArgumentNullException();
        await tests.SendAsync_WithDuplicateNotificationWithinWindow_SuppressesDuplicate();
        await tests.SendAsync_WithDifferentNotifications_DoesNotSuppress();
        await tests.SendAsync_WithSameTitleDifferentMessage_DoesNotSuppress();
        await tests.SendAsync_WithSuppressionDisabled_DoesNotSuppress();
        await tests.SendAsync_AfterSuppressionWindow_AllowsNotification();
        await tests.SendAsync_WithStringConstructor_DoesNotSuppress();

        // Test multiple providers and concurrent scenarios
        await tests.SendAsync_WithMultipleProviders_SuppressesAcrossAllProviders();
        await tests.SendAsync_WithConcurrentNotifications_NoRaceConditions();
        await tests.SendAsync_WithMaxNotifications_RemovesOldEntries();

        // Test notification suppression options
        tests.NotificationSuppressionOptions_DefaultValues();
        tests.NotificationSuppressionOptions_CustomValues();

        Console.WriteLine("All notification service tests passed.");
    }
}
```

## SerializationHelperUnitTests

`SerializationHelperUnitTests` is an xUnit test suite that validates the serialization and deserialization functionality of the `SerializationHelper` class. It tests JSON and XML serialization, null handling, dictionary conversion, and deep cloning operations.

Example usage:
```csharp
using System;
using CaddyVpsToolkit.Tests;

class Program
{
    static void Main()
    {
        // Exercise the serialization helper tests directly.
        var tests = new SerializationHelperUnitTests();

        // Test JSON serialization with indentation
        tests.ToJson_ShouldSerializeObject_WithIndentation();

        // Test deserialization of valid JSON
        tests.FromJson_ShouldDeserializeValidJson();

        // Test that invalid JSON throws an exception
        tests.FromJson_InvalidJson_ShouldThrowInvalidOperationException();

        // Test that invalid JSON in TryFromJson returns the default
        tests.TryFromJson_InvalidJson_ShouldReturnDefault();

        // Test XML serialization
        tests.ToXml_ShouldSerializeObject();

        // Test deserialization of valid XML
        tests.FromXml_ShouldDeserializeValidXml();

        // Test that invalid XML throws an exception
        tests.FromXml_InvalidXml_ShouldThrowInvalidOperationException();

        // Test dictionary conversion for null object
        tests.ToDictionary_NullObject_ShouldReturnEmptyDictionary();

        // Test dictionary conversion contains all public properties
        tests.ToDictionary_ShouldContainAllPublicProperties();

        // Test deep cloning creates an equal but distinct instance
        tests.DeepClone_ShouldCreateEqualButDistinctInstance();

        // Test deep cloning of null object returns null
        tests.DeepClone_NullObject_ShouldReturnNull();

        // Test JSON serialization of null object returns literal null
        tests.ToJson_NullObject_ShouldReturnLiteralNull();

        // Test XML serialization of null object throws an exception
        tests.ToXml_NullObject_ShouldThrowInvalidOperationException();

        Console.WriteLine("All serialization helper tests passed.");
    }
}
```