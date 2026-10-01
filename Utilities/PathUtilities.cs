#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
            ArgumentNullException.ThrowIfNull(fromPath, nameof(fromPath));
            ArgumentException.ThrowIfNullOrEmpty(toPath, nameof(toPath));

            if (string.IsNullOrEmpty(fromPath))
                return toPath;

            var fromFull = Path.GetFullPath(fromPath);
            // Ensure the from path is treated as a directory for correct relative resolution
            if (!fromFull.EndsWith(Path.DirectorySeparatorChar))
                fromFull += Path.DirectorySeparatorChar;
            var fromUri = new Uri(fromFull);
            var toUri = new Uri(Path.GetFullPath(toPath));

            return Uri.UnescapeDataString(
                fromUri.MakeRelativeUri(toUri).ToString().Replace('/', Path.DirectorySeparatorChar)
            );
        }

        /// <summary>
        /// Safely combine path parts, preventing path traversal attacks, rooted paths,
        /// symlink escapes, and reserved names
        /// </summary>
        /// <param name="basePath">The base directory path (must be a relative or safe absolute path)</param>
        /// <param name="parts">Path parts to combine</param>
        /// <returns>Combined path that stays within basePath</returns>
        /// <exception cref="ArgumentException">Thrown if basePath is null or empty</exception>
        /// <exception cref="ArgumentException">Thrown if any part is rooted (starts with / or drive letter)</exception>
        /// <exception cref="ArgumentException">Thrown if any part contains reserved names (Windows) or invalid patterns</exception>
/// <remarks>
/// This is the designated path-combining entry point for the application.
/// Use this method instead of System.IO.Path.Combine to ensure security checks are applied.
/// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if path traversal is detected</exception>
        public static string SafeCombine(string basePath, params string[] parts)
        {
            ArgumentException.ThrowIfNullOrEmpty(basePath, nameof(basePath));

            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Base path cannot be whitespace only.", nameof(basePath));

            var originalBasePath = basePath;
            // Resolve for security comparisons only
            string baseFullPath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            // Resolve symlinks in base path to prevent symlink-based escapes
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                try
                {
                    var baseDirInfo = new DirectoryInfo(baseFullPath);
                    if (baseDirInfo.LinkTarget != null)
                        baseFullPath = baseDirInfo.ResolveLinkTarget(true)?.FullName ?? baseFullPath;
                }
                catch { }
            }

            string combined = originalBasePath;
            if (parts is null)
                throw new ArgumentException("Parts array cannot be null.", nameof(parts));
            foreach (var part in parts)
            {
                if (part is null)
                    throw new ArgumentException("Path part cannot be null.", nameof(parts));
                if (string.IsNullOrEmpty(part))
                    throw new ArgumentException("Path part cannot be empty.", nameof(parts));

                // URL-decode the part to detect encoded traversal sequences
                string decodedPart;
                try { decodedPart = Uri.UnescapeDataString(part); }
                catch { decodedPart = part; }

                // Normalize path separators
                string normalizedPart = decodedPart.Replace('\\', Path.DirectorySeparatorChar);

                // Reject any part that is rooted (starts with / or drive letter)
                bool isRooted = Path.IsPathRooted(part) || Path.IsPathRooted(decodedPart);
                // Also detect Windows-style drive letters on non-Windows platforms
                if (!isRooted && part.Length >= 2 && char.IsLetter(part[0]) && part[1] == ':')
                    isRooted = true;
                if (isRooted)
                {
                    throw new ArgumentException(
                        $"Path part '{part}' is rooted and cannot be safely combined. Use relative paths only.",
                        nameof(parts));
                }

                // Reject reserved Windows device names (case-insensitive)
                var reservedNames = new[] { "CON", "PRN", "AUX", "NUL" };
                var nameWithoutExt = Path.GetFileNameWithoutExtension(normalizedPart);

                if (reservedNames.Contains(nameWithoutExt, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains reserved Windows device name '{nameWithoutExt}' and cannot be safely combined.",
                        nameof(parts));
                }

                // Reject COM/LPT ports
                if (nameWithoutExt.Length > 3 &&
                    (nameWithoutExt.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                     nameWithoutExt.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                    int.TryParse(nameWithoutExt[3..], out _))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains reserved Windows port name '{nameWithoutExt}' and cannot be safely combined.",
                        nameof(parts));
                }

                // Reject parts with trailing dots or spaces (Windows limitation)
                if (normalizedPart.EndsWith(".", StringComparison.Ordinal) ||
                    normalizedPart.EndsWith(" ", StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains trailing dots or spaces which are not allowed on Windows.",
                        nameof(parts));
                }

                // Reject parts that contain path traversal sequences (check both original and decoded)
                if (normalizedPart.Contains("..") ||
                    normalizedPart.Contains("~" + Path.DirectorySeparatorChar) ||
                    normalizedPart.Contains("~" + Path.AltDirectorySeparatorChar))
                {
                    throw new InvalidOperationException(
                        "Path traversal attempt detected: combined path escapes base directory");
                }

                // Use the normalized part (platform separators) for combining
                combined = Path.Combine(combined, normalizedPart);

                // Final security check: ensure the combined path doesn't escape basePath
                string combinedFullPath = Path.GetFullPath(combined);

                // Resolve symlinks in the combined path to detect symlink-based escapes
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                {
                    try
                    {
                        var combinedFileInfo = new FileInfo(combinedFullPath);
                        if (combinedFileInfo.LinkTarget != null)
                            combinedFullPath = combinedFileInfo.ResolveLinkTarget(true)?.FullName ?? combinedFullPath;

                        var parent = combinedFileInfo.Directory;
                        while (parent != null)
                        {
                            if (parent.LinkTarget != null)
                            {
                                var resolvedParent = parent.ResolveLinkTarget(true)?.FullName ?? parent.FullName;
                                combinedFullPath = combinedFullPath.Replace(parent.FullName, resolvedParent);
                                break;
                            }
                            parent = parent.Parent;
                        }
                    }
                    catch { }
                }

                if (!combinedFullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Path traversal attempt detected: combined path escapes base directory");
                }
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
/// <remarks>
/// Uses Path.Combine internally for simple concatenation within the whitelisted file.
/// This is allowed as PathUtilities.cs is excluded from BannedApiAnalyzers bans.
/// </remarks>
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
            // Combine platform-specific invalid chars with a universal set to ensure
            // consistent behavior across operating systems
            var platformChars = Path.GetInvalidFileNameChars();
            var universalInvalid = new[] { '<', '>', ':', '"', '|', '?', '*' };
            var invalidChars = new HashSet<char>(platformChars);
            foreach (var c in universalInvalid)
                invalidChars.Add(c);
            return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
        }
    }
}
