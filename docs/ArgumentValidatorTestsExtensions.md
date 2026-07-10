# ArgumentValidatorTestsExtensions
The `ArgumentValidatorTestsExtensions` class provides a set of extension methods for testing argument validation in the `caddy-vps-toolkit` project. It offers helper methods to create validators with required arguments or optional flags, as well as assertions to verify the detection of missing required arguments and unknown flags. These extensions simplify the process of writing unit tests for argument validation, making it easier to ensure the correctness and robustness of the toolkit's argument handling.

## API
* `public static ArgumentValidator CreateValidatorWithRequiredArgs`: Creates an `ArgumentValidator` instance with required arguments. This method returns a new `ArgumentValidator` object configured to validate the presence of required arguments. It does not throw any exceptions.
* `public static ArgumentValidator CreateValidatorWithOptionalFlags`: Creates an `ArgumentValidator` instance with optional flags. This method returns a new `ArgumentValidator` object configured to validate the presence of optional flags. It does not throw any exceptions.
* `public static void ShouldDetectMissingRequiredArguments`: Verifies that the `ArgumentValidator` detects missing required arguments. This method does not take any parameters and does not return a value. It throws an exception if the validator does not detect the missing required arguments.
* `public static void ShouldDetectUnknownFlags`: Verifies that the `ArgumentValidator` detects unknown flags. This method does not take any parameters and does not return a value. It throws an exception if the validator does not detect the unknown flags.

## Usage
The following examples demonstrate how to use the `ArgumentValidatorTestsExtensions` class:
```csharp
// Example 1: Creating a validator with required arguments
var validator = ArgumentValidatorTestsExtensions.CreateValidatorWithRequiredArgs();
validator.Validate(new[] { "arg1", "arg2" }); // Validates the presence of required arguments

// Example 2: Verifying the detection of unknown flags
ArgumentValidatorTestsExtensions.ShouldDetectUnknownFlags(); // Throws an exception if unknown flags are not detected
```

## Notes
When using the `ArgumentValidatorTestsExtensions` class, consider the following edge cases and thread-safety remarks:
* The `CreateValidatorWithRequiredArgs` and `CreateValidatorWithOptionalFlags` methods return new instances of `ArgumentValidator`, which can be safely used in a multithreaded environment.
* The `ShouldDetectMissingRequiredArguments` and `ShouldDetectUnknownFlags` methods are designed to throw exceptions when the validator does not detect the expected conditions. These exceptions can be caught and handled by the test framework to report test failures.
* The `ArgumentValidatorTestsExtensions` class does not maintain any internal state, making it thread-safe for use in concurrent test execution scenarios.
