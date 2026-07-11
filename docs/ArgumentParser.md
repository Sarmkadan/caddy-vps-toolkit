# ArgumentParser
The `ArgumentParser` type provides a simple, immutable way to parse a string array of command‑line arguments into a command, positional parameters, and flags. It is intended for lightweight CLI tools that need to retrieve individual values or enumerate all parsed elements without external dependencies.

## API
### ArgumentParser
```csharp
public ArgumentParser(string[] args)
```
**Purpose** – Initializes a new parser with the supplied command‑line arguments.  
**Parameters**  
- `args`: The raw argument tokens as they would appear in `Main(string[] args)`.  
**Return value** – None (constructor)  
**Exceptions** – Throws `ArgumentNull.  
**Exceptions** – Throws `ArgumentNullException` if `args` is `null`.

### GetCommand
```csharp
public string GetCommand()
```
**Purpose** – Retrieves the first non‑flag token that is not assigned as a value to a flag, typically representing the command or sub‑command.  
**Parameters** – None.  
**Return value** – The command string, or `null` if no command is present.  
**Exceptions** – None.

### GetPositional
```csharp
public string GetPositional(int index)
```
**Purpose** – Returns the positional argument at the specified zero‑based index, ignoring flags and their values.  
**Parameters**  
- `index`: Zero‑based position of the desired argument.  
**Return value** – The positional argument string, or `null` if the index is out of range.  
**Exceptions** – Throws `ArgumentOutOfRangeException` if `index` is negative.

### GetFlagValue
```csharp
public string GetFlagValue(string flagName)
```
**Purpose** – Obtains the value associated with a flag, supporting both `--flag=value` and `--flag value` forms.  
**Parameters**  
- `flagName`: The flag.  
**Parameters**  
- `flagName`: The flag name without leading dashes (e.g., `"output"` for `--output`).  
**Return value** – The flag’s value string, or `null` if the flag is absent or has no value.  
**Exceptions** – Throws `ArgumentNullException` if `flagName` is `null`.

### HasFlag
```csharp
public bool HasFlag(string flagName)
```
**Purpose** – Determines whether a flag is present in the argument list, regardless of whether it carries a value.  
**Parameters**  
- `flagName`: The flag name without leading dashes.  
**Return value** – `true` if the flag appears at least once; otherwise `false`.  
**Exceptions** – Throws `ArgumentNullException` if `flagName` is `null`.

### GetAllPositional
```csharp
public List<string> GetAllPositional()
```
**Purpose** – Returns a list containing every positional argument in the order they were encountered.  
**Parameters** – None.  
**Return value** – A `List<string>` of positional arguments; empty list if none exist.  
**Exceptions** – None.

### GetAllFlags
```csharp
public List<string> GetAllFlags()
```
**Purpose** – Returns a list of the distinct flag names that were present in the argument list.  
**Parameters** – None.  
**Return value** – A `List<string>` of flag names (without dashes); empty list if no flags were found.  
**Exceptions** – None.

## Usage
```csharp
using System;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args)
    {
        var parser = new ArgumentParser(args);

        string command = parser.GetCommand();          // e.g., "deploy"
        bool verbose   = parser.HasFlag("verbose");
        string output  = parser.GetFlagValue("output"); // e.g., "./dist"

        List<string> positional = parser.GetAllPositional();
        // positional might contain ["app.zip", "env=prod"]

        Console.WriteLine($"Command: {command ?? "<none>"}");
        Console.WriteLine($"Verbose: {verbose}");
        Console.WriteLine($"Output:  {output ?? "<none>"}");
        Console.WriteLine($"Positionals: {string.Join(", ", positional)}");

        return 0;
    }
}
```

```csharp
using System;
using System.Linq;

class Demo
{
    static void ShowHelp(string[] args)
    {
        var parser = new ArgumentParser(args);

        if (parser.HasFlag("help") || !parser.GetAllFlags().Any())
        {
            Console.WriteLine("Usage: mytool <command> [options] <args...>");
            Console.WriteLine("Options:");
            Console.WriteLine("  --help        Show this help message");
            Console.WriteLine("  --verbose     Enable verbose logging");
            Console.WriteLine("  --out <path>  Set output directory");
            return;
        }

        // Normal processing...
    }
}
```

## Notes
- The parser treats any token that starts with `-` or `--` as a flag; the following token is considered its value unless the flag uses the `=` syntax or the next token also starts with `-`.  
- Duplicate flags are allowed; `HasFlag` returns `true` if the flag appears at least once, `GetFlagValue` returns the value from the **last** occurrence, and `GetAllFlags` contains each flag name only once.  
- Positional arguments are collected **after** removing flags and their associated values; therefore a flag’s value is never mistakenly treated as a positional argument.  
- Passing `null` for the constructor’s `args` argument or for any `flagName` parameter results in an `ArgumentNullException`.  
- The instance is immutable after construction; consequently, multiple threads may safely call any of the getter methods concurrently without additional synchronization. However, the constructor itself must not be invoked concurrently on the same `args` reference if that reference is being modified elsewhere.
