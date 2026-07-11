# EnvironmentHelper
The `EnvironmentHelper` class provides a set of static methods for interacting with and retrieving information about the current environment and system. It offers a range of functionalities, including accessing environment variables, determining the current application root and directory, identifying the operating system, and retrieving system information such as processor count and application version.

## API
* `public static string GetEnvironmentVariable`: Retrieves the value of an environment variable. This method takes no parameters and returns a string representing the value of the variable. If the variable does not exist, it returns an empty string.
* `public static void SetEnvironmentVariable`: Sets the value of an environment variable. This method takes two parameters: the name of the variable and its new value. It does not return any value. If the variable does not exist, it is created.
* `public static bool IsDevelopment`: Checks if the current environment is a development environment. This method takes no parameters and returns a boolean indicating whether the environment is development.
* `public static bool IsProduction`: Checks if the current environment is a production environment. This method takes no parameters and returns a boolean indicating whether the environment is production.
* `public static string GetApplicationRoot`: Retrieves the root directory of the current application. This method takes no parameters and returns a string representing the application root.
* `public static string GetHomeDirectory`: Retrieves the home directory of the current user. This method takes no parameters and returns a string representing the home directory.
* `public static string GetTempDirectory`: Retrieves the temporary directory of the system. This method takes no parameters and returns a string representing the temporary directory.
* `public static string GetCurrentDirectory`: Retrieves the current working directory of the application. This method takes no parameters and returns a string representing the current directory.
* `public static bool IsWindows`: Checks if the current operating system is Windows. This method takes no parameters and returns a boolean indicating whether the OS is Windows.
* `public static bool IsUnix`: Checks if the current operating system is Unix-based (including Linux and macOS). This method takes no parameters and returns a boolean indicating whether the OS is Unix-based.
* `public static int GetProcessorCount`: Retrieves the number of processors available on the system. This method takes no parameters and returns an integer representing the processor count.
* `public static string GetApplicationVersion`: Retrieves the version of the current application. This method takes no parameters and returns a string representing the application version.

## Usage
The following examples demonstrate how to use the `EnvironmentHelper` class:
```csharp
// Example 1: Checking the environment and accessing directories
if (EnvironmentHelper.IsDevelopment)
{
    string appRoot = EnvironmentHelper.GetApplicationRoot;
    string homeDir = EnvironmentHelper.GetHomeDirectory;
    Console.WriteLine($"Application root: {appRoot}, Home directory: {homeDir}");
}

// Example 2: Setting an environment variable and checking the OS
EnvironmentHelper.SetEnvironmentVariable("MY_VAR", "my_value");
string myVarValue = EnvironmentHelper.GetEnvironmentVariable;
Console.WriteLine($"MY_VAR value: {myVarValue}");

if (EnvironmentHelper.IsWindows)
{
    Console.WriteLine("Running on Windows");
}
else if (EnvironmentHelper.IsUnix)
{
    Console.WriteLine("Running on Unix-based OS");
}
```

## Notes
When using the `EnvironmentHelper` class, consider the following:
- The `GetEnvironmentVariable` and `SetEnvironmentVariable` methods interact with the system's environment variables, which can be affected by the current process and user permissions.
- The `IsDevelopment` and `IsProduction` methods rely on internal logic to determine the environment type, which may not always accurately reflect the actual environment.
- The `GetApplicationRoot`, `GetHomeDirectory`, `GetTempDirectory`, and `GetCurrentDirectory` methods return directory paths as strings, which should be handled accordingly to avoid path manipulation vulnerabilities.
- The `IsWindows` and `IsUnix` methods provide a basic way to determine the operating system but do not account for other, less common operating systems.
- The `GetProcessorCount` method returns the number of logical processors available, which may not necessarily reflect the number of physical cores.
- The `GetApplicationVersion` method returns the version of the application as a string, which can be useful for logging, debugging, or display purposes.
- All methods in the `EnvironmentHelper` class are static, making them thread-safe as they do not rely on instance state. However, the methods that interact with environment variables and file system directories should be used with caution in multithreaded environments to avoid potential race conditions.
