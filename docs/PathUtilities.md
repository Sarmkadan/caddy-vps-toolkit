# PathUtilities
Provides a collection of static helper methods for common file‑system path manipulations, size reporting, and safety checks. The class is intended to centralize reliable, cross‑platform path handling logic used throughout the Caddy VPS Toolkit.

## API
### GetRelativePath
```csharp
public static string GetRelativePath(string basePath, string targetPath)
```
**Purpose** – Returns a relative path that, when combined with `basePath`, yields `targetPath`.  
**Parameters**  
- `basePath`: The directory that serves as the origin. Must be an absolute path.  
- `targetPath`: The destination file or directory. Must be an absolute path.  
**Return value** – A string containing the relative path from `basePath` to `targetPath`. If the paths are identical, an empty string is returned.  
**Exceptions**  
- `ArgumentNullException` – Either `basePath` or `targetPath` is `null`.  
- `ArgumentException` – One of the paths is not absolute, or the paths are on different drives (Windows) or different root components (Unix).  

### SafeCombine
```csharp
public static string SafeCombine(params string[] paths)
```
**Purpose** – Combines a sequence of path components while preventing traversal outside the intended root.  
**Parameters**  
- `paths`: One or more path fragments to be combined.  
**Return value** – A single string representing the combined path, with directory separators normalized for the current platform.  
**Exceptions**  
- `ArgumentNullException` – `paths` is `null` or any element is `null`.  
- `ArgumentException` – The combined result is empty, contains invalid path characters, or attempts to ascend beyond the first component (e.g., leading `..` that would escape the root).  

### NormalizePath
```csharp
public static string NormalizePath(string path)
```
**Purpose** – Normalizes a file system path by removing redundant separators, resolving `.` and `..` segments, and ensuring a consistent separator character.  
**Parameters**  
- `path`: The path to normalize. May be relative or absolute.  
**Return value** – The normalized path string. Trailing directory separator is preserved only if the input path explicitly ended with one and the path represents a directory.  
**Exceptions**  
- `ArgumentNullException` – `path` is `null`.  
- `NotSupportedException` – `path` contains characters that are invalid for the current platform.  

### GetDirectorySize
```csharp
public static long GetDirectorySize(string directoryPath)
```
**Purpose** – Calculates the total size, in bytes, of all files contained in the specified directory and its sub‑directories.  
**Parameters**  
- `directoryPath`: The directory to evaluate. Must exist.  
**Return value** – The cumulative size of all files, as a 64‑bit signed integer.  
**Exceptions**  
- `ArgumentNullException` – `directoryPath` is `null`.  
- `DirectoryNotFoundException` – The directory does not exist.  
- `UnauthorizedAccessException` – Access to a file or sub‑directory is denied.  
- `IOException` – An I/O error occurs while enumerating files.  

### FormatFileSize
```csharp
public static string FormatFileSize(long bytes, int decimalPlaces = 2)
```
**Purpose** – Converts a byte count into a human‑readable string with appropriate size units (B, KB, MB, GB, TB, PB).  
**Parameters**  
- `bytes`: The size in bytes. Must be non‑negative.  
- `decimalPlaces`: Number of digits to display after the decimal point (default 2).  
**Return value** – A formatted string such as `"1.23 KB"` or `"42 B"`.  
**Exceptions**  
- `ArgumentOutOfRangeException` – `bytes` is negative or `decimalPlaces` is less than 0.  

### IsExecutable
```csharp
public static bool IsExecutable(string filePath)
```
**Purpose** – Determines whether a file is considered executable based on its extension and, on Unix‑like platforms, its permission bits.  
**Parameters**  
- `filePath`: The path to the file to test.  
**Return value** – `true` if the file has an executable extension (`.exe`, `.cmd`, `.bat`, `.sh`, `.pl`, `.py`, etc.) **or** if the file exists and possesses the execute permission for the current user; otherwise `false`.  
**Exceptions**  
- `ArgumentNullException` – `filePath` is `null`.  
- `FileNotFoundException` – The file does not exist (the method returns `false` in this case; the exception is thrown only when the caller explicitly requests validation).  

### GetUniqueFilePath
```csharp
public static string GetUniqueFilePath(string directoryPath, string fileName, string extension = null)
```
**Purpose** – Generates a file path that does not already exist by appending a numeric suffix if necessary.  
**Parameters**  
- `directoryPath`: The directory in which the file should reside. Must exist.  
- `fileName`: The base name of the file (without extension).  
- `extension`: Optional file extension including the leading dot (e.g., `".txt"`). If `null`, no extension is added.  
**Return value** – A full path guaranteed to be unique within the specified directory.  
**Exceptions**  
- `ArgumentNullException` – `directoryPath` or `fileName` is `null`.  
- `DirectoryNotFoundException` – `directoryPath` does not exist.  
- `IOException` – An I/O error occurs while checking for file existence.  

### EnsureDirectoryExists
```csharp
public static void EnsureDirectoryExists(string directoryPath)
```
**Purpose** – Creates the directory and any necessary parent directories if they do not already exist.  
**Parameters**  
- `directoryPath`: The path of the directory to ensure.  
**Return value** – None.  
**Exceptions**  
- `ArgumentNullException` – `directoryPath` is `null`.  
- `UnauthorizedAccessException` – The caller lacks permission to create the directory.  
- `IOException` – An I/O error prevents directory creation (e.g., invalid path, device not ready).  

### SanitizeFileName
```csharp
public static string SanitizeFileName(string fileName)
```
**Purpose** – Removes or replaces characters that are illegal in file names on the current platform, producing a safe name.  
**Parameters**  
- `fileName`: The original file name to sanitize.  
**Return value** – A string containing only legal file‑name characters. Leading and trailing spaces are trimmed; if the result is empty, a single underscore (`"_"` ) is returned.  
**Exceptions**  
- `ArgumentNullException` – `fileName` is `null`.  

## Usage
```csharp
using CaddyVpsToolkit.IO;

// Get a relative path from the project root to a configuration file.
string relative = PathUtilities.GetRelativePath(
    @"C:\Projects\MyApp",
    @"C:\Projects\MyApp\config\settings.json");
// relative => "config\settings.json"

// Safely build a path for a log file, ensuring no directory traversal.
string logPath = PathUtilities.SafeCombine(
    @"C:\Logs",
    DateTime.Now.ToString("yyyy-MM-dd"),
    "app.log");
// logPath => "C:\Logs\2025-11-02\app.log"
```

```csharp
using CaddyVpsToolkit.IO;

// Ensure a directory exists before writing a file.
string targetDir = PathUtilities.SafeCombine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp",
    "cache");
PathUtilities.EnsureDirectoryExists(targetDir);

// Obtain a unique file name for a temporary export.
string uniqueExport = PathUtilities.GetUniqueFilePath(
    targetDir,
    "export",
    ".csv");
// If export.csv already exists, returns export_1.csv, export_2.csv, etc.

// Report the size of the cache directory.
long size = PathUtilities.GetDirectorySize(targetDir);
Console.WriteLine($"Cache size: {PathUtilities.FormatFileSize(size)}");
```

## Notes
- All methods are **static** and contain no mutable state; therefore they are thread‑safe for concurrent invocation.  
- Methods that accept paths do **not** modify the supplied strings; they return new instances.  
- `GetRelativePath` and `SafeCombine` assume the inputs are well‑formed; malformed inputs (e.g., containing invalid characters) will raise `ArgumentException`.  
- `GetDirectorySize` enumerates files recursively; very deep directory trees may cause a `StackOverflowException` if the underlying recursion in `Directory.EnumerateFiles` exceeds stack limits—consider using an iterative approach for extreme cases.  
- `IsExecutable` relies on the file’s extension list and, on non‑Windows platforms, the POSIX execute bit. It does **not** inspect the file’s contents to determine if it is a valid executable binary.  
- `SanitizeFileName` replaces illegal characters with an underscore (`_`). If the resulting string consists solely of spaces or becomes empty after trimming, a single underscore is returned to avoid producing an invalid file name.  
- When using `EnsureDirectoryExists`, be aware that a race condition may occur if another process deletes the directory immediately after the method returns; subsequent file operations should be prepared to handle `DirectoryNotFoundException`.  
- `FormatFileSize` uses base‑1024 units (KiB, MiB, …) internally but displays the conventional “KB”, “MB”, etc., labels for readability.  
- None of the methods allocate unmanaged resources; all memory usage is managed by the .NET runtime.
