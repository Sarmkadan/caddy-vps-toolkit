// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Linq;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Utilities for file system path operations and validation.
    /// Provides cross-platform path handling and safety checks.
    /// </summary>
    public static class PathUtilities
    {
        /// <summary>
        /// Get the relative path from one path to another
        /// </summary>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
                return toPath;

            var fromUri = new Uri(Path.GetFullPath(fromPath));
            var toUri = new Uri(Path.GetFullPath(toPath));

            return Uri.UnescapeDataString(
                fromUri.MakeRelativeUri(toUri).ToString().Replace('/', Path.DirectorySeparatorChar)
            );
        }

        /// <summary>
        /// Safely combine path parts, preventing path traversal attacks
        /// </summary>
        public static string SafeCombine(string basePath, params string[] parts)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentException("Base path required", nameof(basePath));

            string combined = basePath;
            foreach (var part in parts)
            {
                combined = Path.Combine(combined, part);
                // Ensure the combined path doesn't escape basePath (security check)
                if (!Path.GetFullPath(combined).StartsWith(Path.GetFullPath(basePath)))
                    throw new InvalidOperationException("Path traversal attempt detected");
            }

            return combined;
        }

        /// <summary>
        /// Normalize path separators for current OS
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Get the size of a directory including all subdirectories
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            long totalSize = 0;
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                    totalSize += file.Length;
            }
            catch
            {
                return 0;
            }

            return totalSize;
        }

        /// <summary>
        /// Format bytes as human-readable size (e.g., "1.5 MB")
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Check if file path is executable
        /// </summary>
        public static bool IsExecutable(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                // On Unix-like systems, check execute bit
                if (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    var attr = File.GetAttributes(filePath);
                    return (attr & FileAttributes.System) == FileAttributes.System;
                }

                // On Windows, check extension
                var ext = Path.GetExtension(filePath).ToLower();
                return ext == ".exe" || ext == ".bat" || ext == ".cmd" || ext == ".com";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get unique filename if file exists
        /// </summary>
        public static string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);

            int counter = 1;
            while (true)
            {
                var newPath = Path.Combine(directory, $"{fileName}_{counter}{extension}");
                if (!File.Exists(newPath))
                    return newPath;
                counter++;
            }
        }

        /// <summary>
        /// Ensure directory exists, creating if needed
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Clean up invalid path characters
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
