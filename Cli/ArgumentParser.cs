#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaddyVpsToolkit.Cli
{
    /// <summary>
    /// Parses command-line arguments into structured command objects.
    /// Uses a simple key-value pattern for flags and supports positional arguments.
    /// </summary>
    public sealed class ArgumentParser
    {
        // FrozenSet provides O(1) lookup with minimal overhead; constructed once at
        // startup so there is zero cost on the hot path that calls HasFlag/GetFlagValue.
        private static readonly FrozenSet<string> _booleanFlags = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "verbose", "quiet", "debug", "force", "dry-run", "yes", "confirm",
            "json", "no-color", "version", "help", "ssl", "no-ssl", "https",
            "include-comments", "watch", "daemon", "validate"
        );

        private readonly string[] _args;

        // Maximum allowed argument length to prevent memory exhaustion attacks
        private const int MaxArgumentLength = 1024 * 1024; // 1MB per argument

        // Maximum allowed argument count to prevent memory exhaustion attacks
        private const int MaxArgumentCount = 100000; // 100k arguments

        public ArgumentParser(string[] args)
        {
            _args = ValidateAndSanitizeArguments(args ?? []);
        }

        /// <summary>
        /// Validates and sanitizes command-line arguments to prevent memory exhaustion and injection attacks.
        /// </summary>
        /// <param name="args">Raw command-line arguments</param>
        /// <returns>Validated and sanitized arguments</returns>
        /// <exception cref="ArgumentException">Thrown if arguments exceed safe limits</exception>
        private static string[] ValidateAndSanitizeArguments(string[] args)
        {
            if (args.Length > MaxArgumentCount)
            {
                throw new ArgumentException(
                    $"Argument count ({args.Length}) exceeds maximum allowed ({MaxArgumentCount}). Possible denial-of-service attempt.");
            }

            var validatedArgs = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.Length > MaxArgumentLength)
                {
                    throw new ArgumentException(
                        $"Argument at index {i} exceeds maximum allowed length ({MaxArgumentLength} characters). Possible memory exhaustion attempt. Length: {arg.Length}");
                }

                // Sanitize argument to prevent shell metacharacter injection
                // This parser should never execute shell commands, only tokenize arguments
                validatedArgs[i] = SanitizeArgument(arg);
            }

            return validatedArgs;
        }

        /// <summary>
        /// Sanitizes a single argument to prevent shell metacharacter injection and directory traversal.
        /// The ArgumentParser should only tokenize arguments, never interpret or execute them.
        /// </summary>
        /// <param name="argument">The raw argument to sanitize</param>
        /// <returns>Sanitized argument safe for use in file paths and process arguments</returns>
        private static string SanitizeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return argument;
            }

            // Check for shell metacharacters that could be used for command injection
            // These should never be interpreted by the parser itself
            if (argument.IndexOfAny([';', '|', '&', '$', '`', '>', '<', '!']) >= 0)
            {
                // Replace shell metacharacters with underscores to neutralize them
                var sb = new StringBuilder(argument.Length);
                foreach (char c in argument)
                {
                    if (" ;|&$`><!".IndexOf(c) >= 0)
                    {
                        sb.Append('_');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

            // Also sanitize directory traversal attempts (../)
            if (argument.Contains(".."))
            {
                // Replace .. with underscores to prevent directory traversal
                return argument.Replace("..", "__");
            }

            return argument;
        }

        /// <summary>
        /// Get the command name (first argument)
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if internal state is corrupted</exception>
        public string GetCommand()
        {
            return _args.Length > 0 ? _args[0].ToLowerInvariant() : string.Empty;
        }

        /// <summary>
        /// Get positional argument at index (0-based after command)
        /// </summary>
        /// <param name="index">Zero-based index of the positional argument</param>
        /// <returns>The positional argument value, or null if index is out of range</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is negative</exception>
        public string GetPositional(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            }

            int argIndex = index + 1; // Skip command
            return argIndex < _args.Length ? _args[argIndex] : null;
        }

        /// <summary>
        /// Get flag value (--flag value or --flag=value).
        /// Boolean flags (--verbose, --force, etc.) return empty string when present, null when absent.
        /// </summary>
        /// <param name="flagName">Name of the flag to get the value for</param>
        /// <returns>The flag value if present, empty string for boolean flags, null if flag is not present</returns>
        /// <exception cref="ArgumentNullException">Thrown if flagName is null</exception>
        /// <exception cref="ArgumentException">Thrown if flagName is empty or whitespace</exception>
        public string GetFlagValue(string flagName)
        {
            ArgumentNullException.ThrowIfNull(flagName);

            if (string.IsNullOrWhiteSpace(flagName))
            {
                throw new ArgumentException("Flag name cannot be empty or whitespace.", nameof(flagName));
            }

            // Known boolean flags never carry a value — avoid scanning for a trailing argument.
            if (_booleanFlags.Contains(flagName))
                return HasFlag(flagName) ? string.Empty : null;

            var fnSpan = flagName.AsSpan();
            string result = null;
            for (int i = 1; i < _args.Length; i++)
            {
                var argSpan = _args[i].AsSpan();
                if (argSpan.Length < 4 || argSpan[0] != '-' || argSpan[1] != '-') continue;
                var rest = argSpan[2..];

                // --flag=value format: no extra string allocation for prefix construction.
                if (rest.Length > fnSpan.Length + 1
                    && rest[fnSpan.Length] == '='
                    && rest.StartsWith(fnSpan, StringComparison.OrdinalIgnoreCase))
                {
                    result = rest[(fnSpan.Length + 1)..].ToString();
                    continue;
                }

                // --flag value format
                if (rest.Equals(fnSpan, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < _args.Length && !_args[i + 1].StartsWith("--"))
                        result = _args[i + 1];
                    else
                        result = string.Empty;
                    continue;
                }
            }
            return result;
        }

        /// <summary>
        /// Check if flag is present.
        /// Uses span comparisons to avoid allocating "--flagName" and "--flagName=" strings on each call.
        /// </summary>
        /// <param name="flagName">Name of the flag to check</param>
        /// <returns>True if flag is present, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown if flagName is null</exception>
        /// <exception cref="ArgumentException">Thrown if flagName is empty or whitespace</exception>
        public bool HasFlag(string flagName)
        {
            ArgumentNullException.ThrowIfNull(flagName);

            if (string.IsNullOrWhiteSpace(flagName))
            {
                throw new ArgumentException("Flag name cannot be empty or whitespace.", nameof(flagName));
            }
            var fnSpan = flagName.AsSpan();

            foreach (var arg in _args)
            {
                var argSpan = arg.AsSpan();
                if (argSpan.Length < 4 || argSpan[0] != '-' || argSpan[1] != '-') continue;
                var rest = argSpan[2..];

                if (rest.Equals(fnSpan, StringComparison.OrdinalIgnoreCase)) return true;
                if (rest.Length > fnSpan.Length
                    && rest[fnSpan.Length] == '='
                    && rest.StartsWith(fnSpan, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get all positional arguments after command
        /// </summary>
        /// <returns>List of positional arguments (non-flag arguments)</returns>
        public List<string> GetAllPositional()
        {
            var positionals = new List<string>();
            for (int i = 1; i < _args.Length; i++)
            {
                if (!_args[i].StartsWith("--"))
                    positionals.Add(_args[i]);
            }
            return positionals;
        }

        /// <summary>
        /// Get all flag names provided
        /// </summary>
        /// <returns>List of all flag names found in arguments</returns>
        public List<string> GetAllFlags()
        {
            var flags = new List<string>();
            foreach (var arg in _args.AsSpan(1))
            {
                var span = arg.AsSpan();
                if (span.Length >= 3 && span[0] == '-' && span[1] == '-')
                {
                    var rest = span[2..];
                    var eqIdx = rest.IndexOf('=');
                    flags.Add(eqIdx >= 0 ? rest[..eqIdx].ToString() : rest.ToString());
                }
            }
            return flags;
        }
    }
}
