// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Cli
{
    /// <summary>
    /// Parses command-line arguments into structured command objects.
    /// Uses a simple key-value pattern for flags and supports positional arguments.
    /// </summary>
    public class ArgumentParser
    {
        private readonly string[] _args;
        private int _position = 0;

        public ArgumentParser(string[] args)
        {
            _args = args ?? Array.Empty<string>();
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
        /// Get flag value (--flag value or --flag=value)
        /// </summary>
        public string GetFlagValue(string flagName)
        {
            for (int i = 1; i < _args.Length; i++)
            {
                string arg = _args[i];

                // Handle --flag=value format
                if (arg.StartsWith($"--{flagName}="))
                    return arg.Substring($"--{flagName}=".Length);

                // Handle --flag value format
                if (arg == $"--{flagName}")
                {
                    if (i + 1 < _args.Length && !_args[i + 1].StartsWith("--"))
                        return _args[i + 1];
                    return string.Empty;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if flag is present
        /// </summary>
        public bool HasFlag(string flagName)
        {
            return _args.Any(a => a == $"--{flagName}" || a.StartsWith($"--{flagName}="));
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
            foreach (var arg in _args.Skip(1))
            {
                if (arg.StartsWith("--"))
                {
                    string flagName = arg.Substring(2).Split('=')[0];
                    flags.Add(flagName);
                }
            }
            return flags;
        }
    }
}
