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
    /// Registry of available CLI commands with metadata and handlers.
    /// Supports command registration, lookup, and help text generation.
    /// This design allows dynamic command registration and extensibility.
    /// </summary>
    public class CommandRegistry
    {
        private readonly Dictionary<string, CommandDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);

        public void Register(CommandDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            _commands[descriptor.Name] = descriptor;
        }

        public CommandDescriptor Get(string name)
        {
            return _commands.TryGetValue(name, out var cmd) ? cmd : null;
        }

        public bool Exists(string name)
        {
            return _commands.ContainsKey(name);
        }

        public List<CommandDescriptor> GetAll()
        {
            return _commands.Values.ToList();
        }

        public string GenerateHelpText()
        {
            var lines = new List<string>
            {
                "Caddy VPS Toolkit - Available Commands",
                new string('=', 50),
                ""
            };

            foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
            {
                lines.Add($"{cmd.Name,-20} {cmd.Description}");
                if (!string.IsNullOrEmpty(cmd.Usage))
                    lines.Add($"  Usage: {cmd.Usage}");
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// Describes a CLI command with its metadata and validation rules.
    /// </summary>
    public class CommandDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
        public List<string> RequiredArguments { get; set; } = new();
        public List<string> OptionalFlags { get; set; } = new();

        public CommandDescriptor(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public CommandDescriptor WithUsage(string usage)
        {
            Usage = usage;
            return this;
        }

        public CommandDescriptor RequireArgument(string argName)
        {
            RequiredArguments.Add(argName);
            return this;
        }

        public CommandDescriptor AllowFlag(string flagName)
        {
            OptionalFlags.Add(flagName);
            return this;
        }
    }
}
