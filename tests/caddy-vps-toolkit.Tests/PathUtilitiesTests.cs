using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Unit tests for <see cref="PathUtilities"/> covering relative path resolution, safe path
    /// combination, path normalization, directory size calculation, human-readable file size
    /// formatting, executability checks, unique file path generation, directory creation, and
    /// file name sanitization. Filesystem tests run inside a temporary root directory that is
    /// removed when the instance is disposed.
    /// </summary>
    public class PathUtilitiesTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Creates a unique temporary root directory under the system temp path that serves as
        /// an isolated sandbox for the filesystem-based tests in this class.
        /// </summary>
        public PathUtilitiesTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);
        }

        /// <summary>
        /// Recursively deletes the temporary root directory created in the constructor,
        /// silently ignoring cleanup failures so they can never fail a test.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempRoot))
                    Directory.Delete(_tempRoot, true);
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        #region GetRelativePath

        /// <summary>
        /// Verifies that <see cref="PathUtilities.GetRelativePath"/> returns the file name for
        /// targets inside the base directory (Windows and Unix), returns the target unchanged
        /// when the base path is empty, and returns the absolute target for cross-drive
        /// Windows paths where no relative path exists.
        /// </summary>
        /// <param name="from">The base path the relative path is computed from.</param>
        /// <param name="to">The target path to express relative to <paramref name="from"/>.</param>
        /// <param name="expected">The expected relative path result.</param>
        [Theory]
        [InlineData("C:\\Folder\\Sub", "C:\\Folder\\Sub\\File.txt", "File.txt")]
        [InlineData("/usr/local/bin", "/usr/local/bin/script.sh", "script.sh")]
        [InlineData("", "/some/path", "/some/path")]
        [InlineData("C:\\Folder", "D:\\Other\\File.txt", "D:\\Other\\File.txt")]
        public void GetRelativePath_ValidInputs_ReturnsExpected(string from, string to, string expected)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentException.ThrowIfNullOrEmpty(to);
            var result = PathUtilities.GetRelativePath(from, to);
            Assert.Equal(expected, result);
        }

        #endregion

        #region SafeCombine

        /// <summary>
        /// Verifies that combining a base path with simple relative parts produces exactly
        /// the same result as a plain System.IO.Path.Combine call.
        /// </summary>
        [Fact]
        public void SafeCombine_ValidParts_ReturnsCombinedPath()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            var part1 = "sub";
            var part2 = "file.txt";

            var result = PathUtilities.SafeCombine(basePath, part1, part2);
            var expected = Path.Combine(basePath, part1, part2);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that a part containing ".." segments escaping the base directory causes
        /// <see cref="PathUtilities.SafeCombine"/> to throw an <see cref="InvalidOperationException"/>.
        /// </summary>
        [Fact]
        public void SafeCombine_PathTraversal_Throws()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            var traversalPart = "..\\..\\outside.txt";

            Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, traversalPart));
        }

        /// <summary>
        /// Verifies that the reserved Windows device name "CON" is rejected with an
        /// <see cref="ArgumentException"/>; skipped on non-Windows platforms.
        /// </summary>
        [Fact]
        public void SafeCombine_ReservedWindowsDeviceName_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = Path.Combine(_tempRoot, "base");
            var reservedName = "CON";

            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, reservedName));
        }

        /// <summary>
        /// Verifies that the reserved Windows serial port name "COM1" is rejected with an
        /// <see cref="ArgumentException"/>; skipped on non-Windows platforms.
        /// </summary>
        [Fact]
        public void SafeCombine_ReservedWindowsPortName_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = Path.Combine(_tempRoot, "base");
            var portName = "COM1";

            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, portName));
        }

        /// <summary>
        /// Verifies that a file name ending in a trailing dot ("file.") is rejected with an
        /// <see cref="ArgumentException"/>; skipped on non-Windows platforms.
        /// </summary>
        [Fact]
        public void SafeCombine_TrailingDot_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = Path.Combine(_tempRoot, "base");
            var trailingDot = "file.";

            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, trailingDot));
        }

        /// <summary>
        /// Verifies that a file name ending in a trailing space ("file ") is rejected with an
        /// <see cref="ArgumentException"/>; skipped on non-Windows platforms.
        /// </summary>
        [Fact]
        public void SafeCombine_TrailingSpace_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = Path.Combine(_tempRoot, "base");
            var trailingSpace = "file ";

            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, trailingSpace));
        }

        /// <summary>
        /// Creates a real symlink inside the base directory that points to a directory outside
        /// of it, then verifies that combining through the symlink is detected as a path escape
        /// and throws an <see cref="InvalidOperationException"/>; skipped on non-Unix systems.
        /// The symlink and the outside target are cleaned up afterwards.
        /// </summary>
        [Fact]
        public void SafeCombine_SymlinkEscape_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                return; // Skip on non-Unix systems

            var basePath = Path.Combine(_tempRoot, "base");
            Directory.CreateDirectory(basePath);

            // Create a symlink outside the base directory
            var outsidePath = Path.Combine(Path.GetTempPath(), "outside_symlink_target_" + Guid.NewGuid());
            Directory.CreateDirectory(outsidePath);

            var symlinkPath = Path.Combine(basePath, "outside_link");
            try
            {
                // Create symlink pointing outside
                File.CreateSymbolicLink(symlinkPath, outsidePath);

                // Try to combine through the symlink - should detect escape
                Assert.Throws<InvalidOperationException>(() =>
                    PathUtilities.SafeCombine(basePath, "outside_link", "file.txt"));
            }
            finally
            {
                // Cleanup
                try { File.Delete(symlinkPath); } catch { }
                try { Directory.Delete(outsidePath, true); } catch { }
            }
        }

        /// <summary>
        /// Verifies that passing a <c>null</c> base path to <see cref="PathUtilities.SafeCombine"/>
        /// throws an <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void SafeCombine_NullBasePath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PathUtilities.SafeCombine(null!, "part"));
        }

        /// <summary>
        /// Verifies that passing an empty base path to <see cref="PathUtilities.SafeCombine"/>
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void SafeCombine_EmptyBasePath_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(string.Empty, "part"));
        }

        /// <summary>
        /// Verifies that passing a <c>null</c> part to <see cref="PathUtilities.SafeCombine"/>
        /// throws an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void SafeCombine_NullPart_Throws()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, null!));
        }

        /// <summary>
        /// Verifies that an absolute (rooted) part such as "/etc/passwd" is rejected with an
        /// <see cref="ArgumentException"/> because it would escape the base directory.
        /// </summary>
        [Fact]
        public void SafeCombine_RootedPart_Throws()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            var rootedPart = "/etc/passwd";

            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, rootedPart));
        }

        #endregion

        #region NormalizePath

        /// <summary>
        /// Verifies that mixed forward and backward slashes are normalized to the platform
        /// separator and that an empty input produces an empty result.
        /// </summary>
        /// <param name="input">The path containing mixed directory separators.</param>
        /// <param name="expected">The expected path with normalized separators.</param>
        [Theory]
        [InlineData("folder\\subfolder/file.txt", "folder\\subfolder\\file.txt")]
        [InlineData("folder/subfolder\\file.txt", "folder\\subfolder\\file.txt")]
        [InlineData("", "")]
        public void NormalizePath_ValidInputs_ReturnsNormalized(string input, string expected)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentException.ThrowIfNullOrEmpty(expected);
            var result = PathUtilities.NormalizePath(input);
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDirectorySize

        /// <summary>
        /// Verifies that querying the size of a directory that does not exist returns zero bytes.
        /// </summary>
        [Fact]
        public void GetDirectorySize_NonExistent_ReturnsZero()
        {
            var nonExistent = Path.Combine(_tempRoot, "doesnotexist");
            var size = PathUtilities.GetDirectorySize(nonExistent);
            Assert.Equal(0L, size);
        }

        /// <summary>
        /// Writes two files of 100 and 200 bytes into a fresh directory and verifies that
        /// <see cref="PathUtilities.GetDirectorySize"/> reports their combined size of 300 bytes.
        /// </summary>
        [Fact]
        public void GetDirectorySize_WithFiles_ReturnsSum()
        {
            var dir = Path.Combine(_tempRoot, "sizeTest");
            Directory.CreateDirectory(dir);

            var file1 = Path.Combine(dir, "a.txt");
            var file2 = Path.Combine(dir, "b.txt");

            File.WriteAllText(file1, new string('x', 100));
            File.WriteAllText(file2, new string('y', 200));

            var size = PathUtilities.GetDirectorySize(dir);
            Assert.Equal(300L, size);
        }

        #endregion

        #region FormatFileSize

        /// <summary>
        /// Verifies that byte counts are rendered as human-readable sizes in B, KB, MB and GB,
        /// including fractional values such as 1536 bytes formatted as "1.5 KB".
        /// </summary>
        /// <param name="bytes">The number of bytes to format.</param>
        /// <param name="expected">The expected human-readable representation.</param>
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(512, "512 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1 MB")]
        [InlineData(1073741824, "1 GB")]
        public void FormatFileSize_Values_ReturnsHumanReadable(long bytes, string expected)
        {
            ArgumentException.ThrowIfNullOrEmpty(expected);
            var result = PathUtilities.FormatFileSize(bytes);
            Assert.Equal(expected, result);
        }

        #endregion

        #region IsExecutable

        /// <summary>
        /// Verifies that a path pointing to a non-existent file is reported as not executable.
        /// </summary>
        [Fact]
        public void IsExecutable_NonExistent_ReturnsFalse()
        {
            var path = Path.Combine(_tempRoot, "nonexistent.exe");
            Assert.False(PathUtilities.IsExecutable(path));
        }

        /// <summary>
        /// Creates an existing ".exe" file and verifies it is treated as executable on Windows;
        /// skipped on other operating systems.
        /// </summary>
        [Fact]
        public void IsExecutable_WindowsExtensionCheck_ReturnsTrue()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // skip on non-Windows

            var exePath = Path.Combine(_tempRoot, "app.exe");
            File.WriteAllText(exePath, "dummy");
            Assert.True(PathUtilities.IsExecutable(exePath));
        }

        /// <summary>
        /// Creates an existing ".txt" file and verifies it is not treated as executable on
        /// Windows; skipped on other operating systems.
        /// </summary>
        [Fact]
        public void IsExecutable_WindowsExtensionCheck_ReturnsFalse()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // skip on non-Windows

            var txtPath = Path.Combine(_tempRoot, "file.txt");
            File.WriteAllText(txtPath, "dummy");
            Assert.False(PathUtilities.IsExecutable(txtPath));
        }

        #endregion

        #region GetUniqueFilePath

        /// <summary>
        /// Creates "dup.txt" and verifies that <see cref="PathUtilities.GetUniqueFilePath"/>
        /// returns a different path that does not yet exist on disk.
        /// </summary>
        [Fact]
        public void GetUniqueFilePath_FileExists_ReturnsDifferentName()
        {
            var filePath = Path.Combine(_tempRoot, "dup.txt");
            File.WriteAllText(filePath, "content");

            var unique = PathUtilities.GetUniqueFilePath(filePath);
            Assert.NotEqual(filePath, unique);
            Assert.False(File.Exists(unique));
        }

        /// <summary>
        /// Verifies that when the requested file does not exist, the original path is
        /// returned unchanged.
        /// </summary>
        [Fact]
        public void GetUniqueFilePath_FileDoesNotExist_ReturnsSamePath()
        {
            var filePath = Path.Combine(_tempRoot, "new.txt");
            var unique = PathUtilities.GetUniqueFilePath(filePath);
            Assert.Equal(filePath, unique);
        }

        #endregion

        #region EnsureDirectoryExists

        /// <summary>
        /// Verifies that a directory that does not exist yet is created by
        /// <see cref="PathUtilities.EnsureDirectoryExists"/> and present afterwards.
        /// </summary>
        [Fact]
        public void EnsureDirectoryExists_NewDirectory_CreatesIt()
        {
            var dir = Path.Combine(_tempRoot, "newDir");
            Assert.False(Directory.Exists(dir));

            PathUtilities.EnsureDirectoryExists(dir);
            Assert.True(Directory.Exists(dir));
        }

        #endregion

        #region SanitizeFileName

        /// <summary>
        /// Verifies that characters invalid in file names (&lt; &gt; : | ? *) are removed while
        /// names without invalid characters are returned unchanged.
        /// </summary>
        /// <param name="input">The raw file name that may contain invalid characters.</param>
        /// <param name="expected">The sanitized file name expected afterwards.</param>
        [Theory]
        [InlineData("invalid<name>.txt", "invalidname.txt")]
        [InlineData("con<>:|?*?.txt", "con.txt")]
        [InlineData("valid_name.txt", "valid_name.txt")]
        public void SanitizeFileName_RemovesInvalidChars(string input, string expected)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentException.ThrowIfNullOrEmpty(expected);
            var result = PathUtilities.SanitizeFileName(input);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
