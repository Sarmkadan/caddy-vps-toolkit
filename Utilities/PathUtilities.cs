#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
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
            if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
                return toPath;

            var fromUri = new Uri(Path.GetFullPath(fromPath));
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
        /// <exception cref="InvalidOperationException">Thrown if path traversal is detected</exception>
        public static string SafeCombine(string basePath, params string[] parts)
        {
            ArgumentException.ThrowIfNullOrEmpty(basePath, nameof(basePath));

            // Normalize base path - resolve any symlinks to get the real physical path
            basePath = Path.GetFullPath(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            // Resolve symlinks in base path to prevent symlink-based escapes
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                try
                {
                    var baseDirInfo = new DirectoryInfo(basePath);
                    if (baseDirInfo.LinkTarget != null)
                    {
                        // Base path itself is a symlink - resolve it
                        basePath = baseDirInfo.ResolveLinkTarget(true)?.FullName ?? basePath;
                    }
                    else
                    {
                        // Check if base path contains symlinks that could be traversed
                        var parent = baseDirInfo.Parent;
                        while (parent != null)
                        {
                            if (parent.LinkTarget != null)
                            {
                                // Parent directory is a symlink - resolve to real path
                                var resolvedParent = parent.ResolveLinkTarget(true)?.FullName ?? parent.FullName;
                                basePath = basePath.Replace(parent.FullName, resolvedParent);
                                break;
                            }
                            parent = parent.Parent;
                        }
                    }
                }
                catch
                {
                    // If resolution fails, continue with the original path
                    // This maintains backward compatibility
                }
            }

            // Ensure base path is not rooted (should be relative to a known safe root)
            if (Path.IsPathRooted(basePath))
            {
                // For Windows-style rooted paths (e.g., "C:\\"), ensure they're within a safe root
                // This prevents absolute paths like "C:\\Windows\\System32" when basePath is "C:\\safe"
                throw new ArgumentException("Base path must be a relative path, absolute paths are not allowed", nameof(basePath));
            }

            string combined = basePath;
            foreach (var part in parts)
            {
                ArgumentException.ThrowIfNullOrEmpty(part, nameof(parts));

                // Reject any part that is rooted (starts with / or drive letter)
                if (Path.IsPathRooted(part))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' is rooted and cannot be safely combined. Use relative paths only.",
                        nameof(parts)
                    );
                }

                // Normalize path separators
                string normalizedPart = part.Replace('\\', Path.DirectorySeparatorChar);

                // Reject reserved Windows device names (case-insensitive)
                // These cannot be used as file or directory names on Windows
                var reservedNames = new[] { "CON", "PRN", "AUX", "NUL" };
                var nameWithoutExt = Path.GetFileNameWithoutExtension(normalizedPart);

                if (reservedNames.Contains(nameWithoutExt, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains reserved Windows device name '{nameWithoutExt}' and cannot be safely combined.",
                        nameof(parts)
                    );
                }

                // Reject COM/LPT ports (COM1-COM9, LPT1-LPT9)
                if ((nameWithoutExt.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                     nameWithoutExt.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                    int.TryParse(nameWithoutExt[3..], out _))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains reserved Windows port name '{nameWithoutExt}' and cannot be safely combined.",
                        nameof(parts)
                    );
                }

                // Reject parts with trailing dots or spaces (Windows limitation)
                if (normalizedPart.EndsWith(".", StringComparison.Ordinal) ||
                    normalizedPart.EndsWith(" ", StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains trailing dots or spaces which are not allowed on Windows.",
                        nameof(parts)
                    );
                }

                // Reject parts that contain path traversal sequences
                if (normalizedPart.Contains("..") ||
                    normalizedPart.Contains("~" + Path.DirectorySeparatorChar) ||
                    normalizedPart.Contains("~" + Path.AltDirectorySeparatorChar))
                {
                    throw new ArgumentException(
                        $"Path part '{part}' contains path traversal sequences and cannot be safely combined.",
                        nameof(parts)
                    );
                }

                combined = Path.Combine(combined, part);

                // Final security check: ensure the combined path doesn't escape basePath
                // Use GetFullPath to normalize, then resolve symlinks on Unix-like systems
                string combinedFullPath = Path.GetFullPath(combined);
                string baseFullPath = Path.GetFullPath(basePath);

                // Resolve symlinks in the combined path to detect symlink-based escapes
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                {
                    try
                    {
                        var combinedFileInfo = new FileInfo(combinedFullPath);
                        if (combinedFileInfo.LinkTarget != null)
                        {
                            // The combined path is a symlink - resolve it
                            var resolvedCombined = combinedFileInfo.ResolveLinkTarget(true)?.FullName ?? combinedFullPath;
                            combinedFullPath = resolvedCombined;
                        }

                        // Also resolve parent directories that might be symlinks
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
                    catch
                    {
                        // If resolution fails, continue with the original path
                    }
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
