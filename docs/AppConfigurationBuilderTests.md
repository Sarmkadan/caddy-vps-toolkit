# AppConfigurationBuilderTests

`AppConfigurationBuilderTests` is a unit test suite that validates the behaviour of the `AppConfigurationBuilder` class. It ensures that configuration settings are correctly added, default values are applied only for missing keys, file-based configuration loading fails gracefully for non-existent files, and typed value retrieval methods (`GetInt`, `GetBool`) handle both valid and invalid inputs as expected.

## API

### public void WithSetting_WithValidKey_ShouldAddSetting
Verifies that calling `WithSetting` using a valid, non-null key correctly registers the setting in the builder.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test fails if the setting is not present after the call.

### public void WithSetting_WithNullKey_ShouldThrowArgumentException
Ensures that passing a `null` key to `WithSetting` immediately throws an `ArgumentException`.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test expects `ArgumentException` from the production code; the test itself fails if the exception is not thrown.

### public void WithDefaults_ShouldAddOnlyMissingKeys
Confirms that `WithDefaults` adds default entries exclusively for keys that do not already exist in the configuration, leaving existing values untouched.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test fails if defaults overwrite existing keys or if missing keys remain absent.

### public void WithJsonFile_WithNonExistentFile_ShouldThrowFileNotFoundException
Validates that attempting to load configuration from a JSON file path that does not exist results in a `FileNotFoundException`.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test expects `FileNotFoundException` from the production code; the test itself fails if the exception is not thrown or a different exception type is raised.

### public void GetInt_WithValidNumber_ShouldReturnParsedInt
Checks that `GetInt` returns the correct integer when the stored value is a string representing a valid integer.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test fails if the parsed integer does not match the expected value.

### public void GetInt_WithInvalidNumber_ShouldReturnDefault
Verifies that `GetInt` returns the provided default value when the stored string cannot be parsed as an integer (e.g., non-numeric text).  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test fails if an exception is thrown or a value other than the supplied default is returned.

### public void GetBool_WithValidBool_ShouldReturnParsedBool
Ensures that `GetBool` correctly interprets a string representing a valid boolean (`true`/`false`) and returns the corresponding `bool` value.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Test fails if the parsed boolean does not match the expected value.

## Usage

```csharp
// Example 1: Building configuration with explicit settings and defaults
var builder = new AppConfigurationBuilder();
builder.WithSetting("host", "localhost");
builder.WithSetting("port", "8080");
builder.WithDefaults(new Dictionary<string, string>
{
    { "host", "0.0.0.0" },
    { "port", "3000" },
    { "logLevel", "info" }
});

// 'host' and 'port' retain their explicit values; only 'logLevel' is added.
string host = builder.Get("host");       // "localhost"
string logLevel = builder.Get("logLevel"); // "info"
```

```csharp
// Example 2: Retrieving typed values with fallback defaults
var builder = new AppConfigurationBuilder();
builder.WithSetting("maxRetries", "5");
builder.WithSetting("enableCache", "true");
builder.WithSetting("timeout", "invalid");

int retries = builder.GetInt("maxRetries", 3);      // 5
bool cache = builder.GetBool("enableCache", false); // true
int timeout = builder.GetInt("timeout", 30);        // 30 (fallback, because "invalid" is not a number)
```

## Notes

- **Edge cases:** `GetInt` and `GetBool` must tolerate leading/trailing whitespace, empty strings, and completely non-numeric/non-boolean content, consistently returning the caller-supplied default without throwing exceptions. The `WithJsonFile` method is expected to throw only `FileNotFoundException` for missing files; other I/O errors (e.g., permission issues) should be covered by separate tests if applicable.
- **Thread safety:** These test methods operate on isolated builder instances and do not share mutable state. The underlying `AppConfigurationBuilder` is not guaranteed to be thread-safe based on these signatures alone; concurrent calls to `WithSetting` or `WithDefaults` from multiple threads should be avoided unless explicitly documented as safe by the production class.
