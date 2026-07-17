# ServiceRepositoryJsonExtensions

Provides extension methods for JSON serialization and deserialization of `ServiceRepository` and `ManagedService` objects, enabling easy conversion to and from JSON strings within the caddy‑vps‑toolkit.

## API

### `public static string ToJson(this ServiceRepository repository)`
Serializes a `ServiceRepository` instance to a JSON string.  
- **Parameters**  
  - `repository`: The repository to serialize. Must not be `null`.  
- **Return value**  
  - A JSON‑encoded string representing the repository.  
- **Exceptions**  
  - `ArgumentNullException` if `repository` is `null`.  
  - `JsonException` if serialization fails for any reason.

### `public static ManagedService? FromJson(this string json)`
Deserializes a JSON string into a single `ManagedService` instance.  
- **Parameters**  
  - `json`: The JSON string to parse. May be `null`.  
- **Return value**  
  - The deserialized `ManagedService`, or `null` if `json` is `null` or does not represent a valid `ManagedService`.  
- **Exceptions**  
  - `JsonException` if `json` is non‑null but contains malformed JSON that cannot be parsed into a `ManagedService`.

### `public static bool TryFromJson(this string json, out ManagedService? service)`
Attempts to deserialize a JSON string into a `ManagedService`, indicating success via the return value.  
- **Parameters**  
  - `json`: The JSON string to parse. May be `null`.  
  - `service`: When the method returns `true`, contains the deserialized `ManagedService`; otherwise `null`.  
- **Return value**  
  - `true` if `json` was successfully parsed into a `ManagedService`; otherwise `false`.  
- **Exceptions**  
  - None. This method does not throw; all error conditions are reported via the return value.

### `public static string ToJson(this IEnumerable<ManagedService> services)`
Serializes a collection of `ManagedService` objects to a JSON array string.  
- **Parameters**  
  - `services`: The sequence of services to serialize. Must not be `null`.  
- **Return value**  
  - A JSON array string containing the serialized services.  
- **Exceptions**  
  - `ArgumentNullException` if `services` is `null`.  
  - `JsonException` if serialization fails.

### `public static IReadOnlyList<ManagedService> FromJsonToList(this string json)`
Deserializes a JSON string into a read‑only list of `ManagedService` objects.  
- **Parameters**  
  - `json`: The JSON string to parse. May be `null`.  
- **Return value**  
  - A read‑only list of `ManagedService` instances. Returns an empty list if `json` is `null` or does not represent a valid JSON array of services.  
- **Exceptions**  
  - `JsonException` if `json` is non‑null but contains malformed JSON that cannot be parsed into a list of `ManagedService`.

### `public static bool TryFromJsonToList(this string json, out IReadOnlyList<ManagedService>? services)`
Attempts to deserialize a JSON string into a read‑only list of `ManagedService` objects, indicating success via the return value.  
- **Parameters**  
  - `json`: The JSON string to parse. May be `null`.  
  - `services`: When the method returns `true`, contains the deserialized list; otherwise `null`.  
- **Return value**  
  - `true` if `json` was successfully parsed into a list of `ManagedService`; otherwise `false`.  
- **Exceptions**  
  - None. This method does not throw; all error conditions are reported via the return value.

## Usage

```csharp
using CaddyVpsToolkit.ServiceRepository;

// Assume we have a populated ServiceRepository instance.
ServiceRepository repo = GetRepository();

// Serialize the repository to JSON for storage or transmission.
string json = repo.ToJson();
File.WriteAllText("repository.json", json);

// Later, deserialize the JSON back into a ManagedService object.
string serviceJson = GetJsonFromSomewhere();
ManagedService? service = serviceJson.FromJson();
if (service != null)
{
    ProcessService(service);
}
```

```csharp
using System.Collections.Generic;
using CaddyVpsToolkit.ServiceRepository;

// Serialize a collection of services.
IEnumerable<ManagedService> services = LoadServices();
string servicesJson = services.ToJson();

// Safely attempt to parse a JSON array of services.
if (servicesJson.TryFromJsonToList(out IReadOnlyList<ManagedService>? list) && list != null)
{
    foreach (var svc in list)
    {
        Console.WriteLine(svc.Name);
    }
}
else
{
    Console.WriteLine("Failed to parse service list.");
}
```

## Notes

- All extension methods are **static** and contain no internal state; therefore they are thread‑safe and can be invoked concurrently from multiple threads without synchronization.  
- Passing `null` for input parameters that are not explicitly allowed results in an `ArgumentNullException` for the `ToJson` overloads, while the `FromJson`/`TryFromJson` family treats `null` as a valid input and returns a default value (`null` or `false`) rather than throwing.  
- The JSON serialization uses the default settings of the underlying JSON serializer; custom formatting or handling of special types is not supported by these methods.  
- If the supplied JSON is syntactically correct but does not match the expected shape (e.g., missing required properties), the `FromJson` and `FromJsonToList` methods will throw a `JsonException`, whereas the `Try*` variants will return `false` and set the output argument to `null`.  
- Empty collections are serialized as an empty JSON array (`[]`) and deserializing an empty array yields an empty read‑only list.  
- These methods do not perform any validation beyond JSON parsing; callers should validate the resulting objects according to domain‑specific rules if necessary.
