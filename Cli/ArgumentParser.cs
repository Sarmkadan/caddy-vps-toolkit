#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

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

        public ArgumentParser(string[] args)
        {
            _args = args ?? [];
        }

        /// <summary>
        /// Get the command name (first argument)
        /// </summary>
        public string GetCommand()
        {
            return _args.Length > 0 ? _args[0].ToLower() : string.Empty;
        }

        /// <summary>
        /// Get positional argument at index (0-based after command)
        /// </summary>
        public string GetPositional(int index)
        {
            int argIndex = index + 1; // Skip command
            return argIndex < _args.Length ? _args[argIndex] : null;
        }

        /// <summary>
        /// Get flag value (--flag value or --flag=value).
        /// Boolean flags (--verbose, --force, etc.) return empty string when present, null when absent.
        /// </summary>
        public string GetFlagValue(string flagName)
        {
            if (flagName is null) return null;

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
        public bool HasFlag(string flagName)
        {
            if (flagName is null) return false;
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
