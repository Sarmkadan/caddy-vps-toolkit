# BackupServiceTests

Unit test suite for the `BackupService` class, validating backup creation, restoration, and listing behaviours. The tests cover both nominal paths and error conditions, ensuring that JSON backup files are written correctly, missing or invalid inputs are handled with appropriate exceptions, and directory enumeration returns expected results.

## API

### BackupServiceTests

Constructor. Initialises a new instance of the test class. No parameters. Test infrastructure (e.g., temporary directories, mock services) is typically set up per test method via the test framework’s initialisation attributes.

### async Task CreateBackupAsync_WritesJsonFileToOutputPath

Verifies that calling `CreateBackupAsync` with a specific output path produces a JSON file at that exact location. The test confirms the file exists and contains valid serialised backup data.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if the file is missing or content is invalid.

### async Task CreateBackupAsync_WithNullOutputPath_GeneratesTimestampedFilename

Ensures that when `CreateBackupAsync` receives a null output path, it falls back to generating a timestamp-based filename in a default location. The test checks that a file with a name matching the expected pattern is created.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if no file is generated or the filename pattern does not match.

### async Task RestoreBackupAsync_WithMissingFile_ThrowsCaddyVpsException

Confirms that attempting to restore from a non-existent backup file path causes `RestoreBackupAsync` to throw a `CaddyVpsException`. The test supplies a path known to be absent and asserts the exception type and/or message.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if the expected exception is not thrown or a different exception type is raised.

### async Task RestoreBackupAsync_WithValidBackup_RestoresServicesAndConfig

Validates the happy path for restoration: given a valid backup JSON file, `RestoreBackupAsync` correctly reinstates service definitions and configuration state. The test inspects post-restore state to confirm fidelity with the backup contents.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if services or configuration do not match the expected restored state.

### async Task ListBackupsAsync_WithNonExistentDirectory_ReturnsEmptyList

Tests that `ListBackupsAsync` returns an empty collection when the target directory does not exist, rather than throwing an exception. The test points to a directory path that has not been created.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if the result is not an empty list or an exception is thrown.

### async Task ListBackupsAsync_WithBackupsPresent_ReturnsSortedPaths

Verifies that `ListBackupsAsync` enumerates backup files in the expected directory and returns them sorted (typically by name or timestamp). The test pre-seeds the directory with multiple backup files and checks ordering and completeness.

- **Parameters:** none (test method).
- **Returns:** a Task representing the asynchronous test operation.
- **Throws:** assertion failures if the list is unsorted, missing entries, or contains unexpected items.

## Usage

### Example 1: Running backup creation and listing tests together

```csharp
[TestFixture]
public class BackupServiceTestSuite
{
    private BackupServiceTests _tests;

    [SetUp]
    public void SetUp()
    {
        _tests = new BackupServiceTests();
        // Assume test framework initialises temporary paths, mocks, etc.
    }

    [Test]
    public async Task FullBackupCycle_ShouldCreateAndList()
    {
        // Create a backup with explicit path
        await _tests.CreateBackupAsync_WritesJsonFileToOutputPath();

        // Create a backup with auto-generated filename
        await _tests.CreateBackupAsync_WithNullOutputPath_GeneratesTimestampedFilename();

        // Verify both appear when listing
        await _tests.ListBackupsAsync_WithBackupsPresent_ReturnsSortedPaths();
    }
}
```

### Example 2: Testing restore failure and success scenarios

```csharp
[Test]
public async Task RestoreScenarios_ShouldHandleMissingAndValidFiles()
{
    var tests = new BackupServiceTests();

    // Confirm missing file throws
    await tests.RestoreBackupAsync_WithMissingFile_ThrowsCaddyVpsException();

    // After seeding a valid backup, restore should succeed
    await tests.RestoreBackupAsync_WithValidBackup_RestoresServicesAndConfig();
}
```

## Notes

- **Edge cases:** `ListBackupsAsync_WithNonExistentDirectory_ReturnsEmptyList` explicitly covers the scenario where the backup directory is absent; the implementation must not throw but return an empty list. `RestoreBackupAsync_WithMissingFile_ThrowsCaddyVpsException` ensures that a missing file is distinguished from other failure modes by throwing `CaddyVpsException` rather than a generic I/O exception.
- **Thread safety:** These are test methods intended for sequential execution within a test runner. They are not designed for concurrent invocation. Shared state (e.g., temporary directories) should be isolated per test to avoid cross-test interference; the test framework’s lifecycle attributes (SetUp/TearDown) are expected to manage this.
- **Sorting assumption:** `ListBackupsAsync_WithBackupsPresent_ReturnsSortedPaths` relies on a deterministic sort order. If the underlying `ListBackupsAsync` implementation changes its ordering contract, this test must be updated accordingly.
