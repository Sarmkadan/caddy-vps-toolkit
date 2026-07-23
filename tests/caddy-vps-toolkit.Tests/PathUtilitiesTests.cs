using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests
{
    public class PathUtilitiesTests : IDisposable
    {
        private readonly string _tempRoot;

        public PathUtilitiesTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);
        }

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

        [Theory]
        [InlineData("C:\\Folder\\Sub", "C:\\Folder\\Sub\\File.txt", "File.txt")]
        [InlineData("/usr/local/bin", "/usr/local/bin/script.sh", "script.sh")]
        [InlineData("", "/some/path", "/some/path")]
        [InlineData("C:\\Folder", "D:\\Other\\File.txt", "D:\\Other\\File.txt")]
        public void GetRelativePath_ValidInputs_ReturnsExpected(string from, string to, string expected)
        {
            var result = PathUtilities.GetRelativePath(from, to);
            Assert.Equal(expected, result);
        }

        #endregion

        #region SafeCombine

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

        [Fact]
        public void SafeCombine_PathTraversal_Throws()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            var traversalPart = "..\\..\\outside.txt";

            Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, traversalPart));
        }

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

        [Fact]
        public void SafeCombine_NullBasePath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PathUtilities.SafeCombine(null!, "part"));
        }

        [Fact]
        public void SafeCombine_EmptyBasePath_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(string.Empty, "part"));
        }

        [Fact]
        public void SafeCombine_NullPart_Throws()
        {
            var basePath = Path.Combine(_tempRoot, "base");
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, null!));
        }

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

        [Theory]
        [InlineData("folder\\subfolder/file.txt", "folder\\subfolder\\file.txt")]
        [InlineData("folder/subfolder\\file.txt", "folder\\subfolder\\file.txt")]
        [InlineData("", "")]
        public void NormalizePath_ValidInputs_ReturnsNormalized(string input, string expected)
        {
            var result = PathUtilities.NormalizePath(input);
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDirectorySize

        [Fact]
        public void GetDirectorySize_NonExistent_ReturnsZero()
        {
            var nonExistent = Path.Combine(_tempRoot, "doesnotexist");
            var size = PathUtilities.GetDirectorySize(nonExistent);
            Assert.Equal(0L, size);
        }

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

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(512, "512 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1 MB")]
        [InlineData(1073741824, "1 GB")]
        public void FormatFileSize_Values_ReturnsHumanReadable(long bytes, string expected)
        {
            var result = PathUtilities.FormatFileSize(bytes);
            Assert.Equal(expected, result);
        }

        #endregion

        #region IsExecutable

        [Fact]
        public void IsExecutable_NonExistent_ReturnsFalse()
        {
            var path = Path.Combine(_tempRoot, "nonexistent.exe");
            Assert.False(PathUtilities.IsExecutable(path));
        }

        [Fact]
        public void IsExecutable_WindowsExtensionCheck_ReturnsTrue()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // skip on non-Windows

            var exePath = Path.Combine(_tempRoot, "app.exe");
            File.WriteAllText(exePath, "dummy");
            Assert.True(PathUtilities.IsExecutable(exePath));
        }

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

        [Fact]
        public void GetUniqueFilePath_FileExists_ReturnsDifferentName()
        {
            var filePath = Path.Combine(_tempRoot, "dup.txt");
            File.WriteAllText(filePath, "content");

            var unique = PathUtilities.GetUniqueFilePath(filePath);
            Assert.NotEqual(filePath, unique);
            Assert.False(File.Exists(unique));
        }

        [Fact]
        public void GetUniqueFilePath_FileDoesNotExist_ReturnsSamePath()
        {
            var filePath = Path.Combine(_tempRoot, "new.txt");
            var unique = PathUtilities.GetUniqueFilePath(filePath);
            Assert.Equal(filePath, unique);
        }

        #endregion

        #region EnsureDirectoryExists

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

        [Theory]
        [InlineData("invalid<name>.txt", "invalidname.txt")]
        [InlineData("con<>:|?*?.txt", "con.txt")]
        [InlineData("valid_name.txt", "valid_name.txt")]
        public void SanitizeFileName_RemovesInvalidChars(string input, string expected)
        {
            var result = PathUtilities.SanitizeFileName(input);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
