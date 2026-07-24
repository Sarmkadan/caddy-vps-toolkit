using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Comprehensive tests for PathUtilities.SafeCombine security checks
    /// </summary>
    public class SafeCombineTests : IDisposable
    {
        private readonly string _tempRoot;

        public SafeCombineTests()
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

        #region Path Traversal Tests

        /// <summary>
        /// Test that relative segment with '../../etc/passwd' style traversal is rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RelativePathTraversal_DoubleDot_Throws()
        {
            // Use a simple relative path as basePath
            var basePath = "base";
            var traversalPart = ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "outside.txt";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, traversalPart));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that relative segment with '../' style traversal is rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RelativePathTraversal_SingleDotDot_Throws()
        {
            // Use a simple relative path as basePath
            var basePath = "base";
            var traversalPart = ".." + Path.DirectorySeparatorChar + "outside.txt";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, traversalPart));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that multiple '../' segments are rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RelativePathTraversal_MultipleSegments_Throws()
        {
            var basePath = "base";
            var traversalPart = ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "etc" + Path.DirectorySeparatorChar + "passwd";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, traversalPart));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that URL-encoded traversal sequences are rejected
        /// </summary>
        [Fact]
        public void SafeCombine_UrlEncodedTraversal_Throws()
        {
            var basePath = "base";
            var encodedTraversal = "%2e%2e%2ffile.txt"; // "../file.txt"

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, encodedTraversal));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that URL-encoded traversal sequences with uppercase are rejected
        /// </summary>
        [Fact]
        public void SafeCombine_UrlEncodedTraversalUppercase_Throws()
        {
            var basePath = "base";
            var encodedTraversal = "%2E%2E%2Ffile.txt"; // "../file.txt"

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, encodedTraversal));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that mixed case URL encoding is rejected
        /// </summary>
        [Fact]
        public void SafeCombine_UrlEncodedTraversalMixedCase_Throws()
        {
            var basePath = "base";
            var encodedTraversal = "%2e%2E%2f" + "file.txt"; // "../file.txt"

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, encodedTraversal));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that encoded dot segments are rejected
        /// </summary>
        [Fact]
        public void SafeCombine_EncodedDotSegments_Throws()
        {
            var basePath = "base";
            var encodedDot = ".%2e" + Path.DirectorySeparatorChar + "file.txt"; // "./../file.txt"

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, encodedDot));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        #endregion

        #region Rooted Path Tests

        /// <summary>
        /// Test that an absolute/rooted path passed as the second segment does not escape the base directory
        /// </summary>
        [Fact]
        public void SafeCombine_RootedPath_AbsolutePath_Throws()
        {
            var basePath = "base";
            var rootedPart = "/etc/passwd";

            var exception = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, rootedPart));

            Assert.Contains("is rooted and cannot be safely combined", exception.Message);
        }

        /// <summary>
        /// Test that Windows absolute path is rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RootedPath_WindowsAbsolutePath_Throws()
        {
            var basePath = "base";
            var rootedPart = "C:\\Windows\\System32";

            var exception = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, rootedPart));

            Assert.Contains("is rooted and cannot be safely combined", exception.Message);
        }

        /// <summary>
        /// Test that Unix absolute path is rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RootedPath_UnixAbsolutePath_Throws()
        {
            var basePath = "base";
            var rootedPart = "/usr/local/bin";

            var exception = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, rootedPart));

            Assert.Contains("is rooted and cannot be safely combined", exception.Message);
        }

        /// <summary>
        /// Test that UNC paths on Windows are rejected
        /// </summary>
        [Fact]
        public void SafeCombine_RootedPath_UncPath_Throws()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = "base";
            var uncPath = "\\\\server\\share\\file.txt";

            var exception = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, uncPath));

            Assert.Contains("is rooted and cannot be safely combined", exception.Message);
        }

        #endregion

        #region Symlink Tests

        /// <summary>
        /// Test that symlink-adjacent normal paths that are valid still combine correctly (no false positives)
        /// </summary>
        [Fact]
        public void SafeCombine_Symlink_NormalPath_CombinesCorrectly()
        {
            var basePath = "base";
            Directory.CreateDirectory(basePath);

            var subDir = Path.Combine(basePath, "subdir");
            Directory.CreateDirectory(subDir);

            var filePath = PathUtilities.SafeCombine(basePath, "subdir", "file.txt");
            var expected = Path.Combine(basePath, "subdir", "file.txt");

            Assert.Equal(expected, filePath);
        }

        #endregion

        #region Argument Validation Tests

        /// <summary>
        /// Test that null base path throws ArgumentNullException
        /// </summary>
        [Fact]
        public void SafeCombine_NullBasePath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PathUtilities.SafeCombine(null!, "part"));
        }

        /// <summary>
        /// Test that empty base path throws ArgumentException
        /// </summary>
        [Fact]
        public void SafeCombine_EmptyBasePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(string.Empty, "part"));
        }

        /// <summary>
        /// Test that whitespace-only base path throws ArgumentException
        /// </summary>
        [Fact]
        public void SafeCombine_WhitespaceBasePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine("   ", "part"));
        }

        /// <summary>
        /// Test that null part throws ArgumentException
        /// </summary>
        [Fact]
        public void SafeCombine_NullPart_ThrowsArgumentException()
        {
            var basePath = "base";
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, null!));
        }

        /// <summary>
        /// Test that empty part throws ArgumentException
        /// </summary>
        [Fact]
        public void SafeCombine_EmptyPart_ThrowsArgumentException()
        {
            var basePath = "base";
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, string.Empty));
        }

        /// <summary>
        /// Test that whitespace-only part throws ArgumentException
        /// </summary>
        [Fact]
        public void SafeCombine_WhitespacePart_ThrowsArgumentException()
        {
            var basePath = "base";
            Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, "   "));
        }

        #endregion

        #region Path Separator Normalization Tests

        /// <summary>
        /// Test that mixed path separators are normalized correctly
        /// </summary>
        [Fact]
        public void SafeCombine_MixedPathSeparators_NormalizesCorrectly()
        {
            var basePath = "base";
            var mixedPath = "subdir\\subsubdir/file.txt"; // Mixed separators

            var result = PathUtilities.SafeCombine(basePath, mixedPath);
            var expected = Path.Combine(basePath, "subdir", "subsubdir", "file.txt");

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test that case-insensitive drive letter handling works correctly
        /// </summary>
        [Fact]
        public void SafeCombine_CaseInsensitiveDriveLetter_NormalizesCorrectly()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return; // Skip on non-Windows

            var basePath = "base";
            var upperDrivePath = "C:" + Path.DirectorySeparatorChar + "Windows";
            var lowerDrivePath = "c:" + Path.DirectorySeparatorChar + "windows";

            var exception1 = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, upperDrivePath));
            var exception2 = Assert.Throws<ArgumentException>(() =>
                PathUtilities.SafeCombine(basePath, lowerDrivePath));

            Assert.Contains("is rooted and cannot be safely combined", exception1.Message);
            Assert.Contains("is rooted and cannot be safely combined", exception2.Message);
        }

        #endregion

        #region Combined Path Traversal Tests

        /// <summary>
        /// Test that path traversal through multiple segments is detected
        /// </summary>
        [Fact]
        public void SafeCombine_CombinedPathTraversal_DetectsEscape()
        {
            var basePath = "base";
            var safePart = "subdir";
            var traversalPart = ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "outside.txt";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, safePart, traversalPart));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        /// <summary>
        /// Test that deep nesting with traversal is detected
        /// </summary>
        [Fact]
        public void SafeCombine_DeepNestingWithTraversal_DetectsEscape()
        {
            var basePath = "base";
            var parts = new[] { "level1", "level2", "level3", ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "outside" };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                PathUtilities.SafeCombine(basePath, parts));

            Assert.Contains("Path traversal attempt detected", exception.Message);
        }

        #endregion

        #region Valid Path Tests

        /// <summary>
        /// Test that valid relative paths combine correctly
        /// </summary>
        [Fact]
        public void SafeCombine_ValidRelativePath_CombinesCorrectly()
        {
            var basePath = "base";
            var result = PathUtilities.SafeCombine(basePath, "subdir", "file.txt");
            var expected = Path.Combine(basePath, "subdir", "file.txt");

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test that valid paths with special characters (not traversal) combine correctly
        /// </summary>
        [Fact]
        public void SafeCombine_ValidPathWithSpecialChars_CombinesCorrectly()
        {
            var basePath = "base";
            var specialPath = "file-with-dashes_and_underscores.txt";

            var result = PathUtilities.SafeCombine(basePath, specialPath);
            var expected = Path.Combine(basePath, specialPath);

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Test that empty parts array works correctly
        /// </summary>
        [Fact]
        public void SafeCombine_EmptyPartsArray_ReturnsBasePath()
        {
            var basePath = "base";
            var result = PathUtilities.SafeCombine(basePath);

            Assert.Equal(basePath, result);
        }

        #endregion
    }
}