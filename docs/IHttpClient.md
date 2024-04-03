# IHttpClient

`IHttpClient` is a lightweight HTTP client abstraction designed for typed request/response interactions in the `caddy-vps-toolkit` project. It wraps an underlying `HttpClientWrapper` and provides generic methods for common HTTP verbs (`GET`, `POST`, `PUT`, `DELETE`), returning structured `HttpResponse<T>` objects that carry deserialized data, status metadata, error information, and raw response content in a single envelope.

## API

### HttpClientWrapper
- **Type:** `HttpClientWrapper` (property, getter)
- **Purpose:** Exposes the underlying HTTP client wrapper instance. This can be used for advanced scenarios requiring direct access to the configured client, such as custom request manipulation or inspection of the underlying handler.
- **Remarks:** The returned object is the same instance used internally by all verb methods. Modifying its configuration (e.g., base address, timeouts, default headers) will affect subsequent calls made through this interface.

### GetAsync\<T\>
- **Signature:** `async Task<HttpResponse<T>> GetAsync<T>(...)`
- **Purpose:** Sends an HTTP `GET` request and deserializes a successful response body into an instance of `T`.
- **Parameters:** Accepts a resource URI (string or `Uri`) and an optional cancellation token.
- **Returns:** `Task<HttpResponse<T>>` whose `Data` property holds the deserialized payload on success.
- **Throws:** `ArgumentNullException` if the URI is null. `HttpRequestException` for network-level failures. `TaskCanceledException` on cancellation or timeout. Deserialization failures are captured in the response envelope rather than thrown.

### PostAsync\<T\>
- **Signature:** `async Task<HttpResponse<T>> PostAsync<T>(...)`
- **Purpose:** Sends an HTTP `POST` request with a serialized body and deserializes the response into `T`.
- **Parameters:** A URI, the request payload object (serialized internally, typically as JSON), and an optional cancellation token.
- **Returns:** `Task<HttpResponse<T>>` with the deserialized response body in `Data`.
- **Throws:** `ArgumentNullException` if the URI is null. `HttpRequestException` for transport errors. `TaskCanceledException` on cancellation/timeout. Serialization or deserialization errors are surfaced through the `Error` property of the returned envelope.

### PutAsync\<T\>
- **Signature:** `async Task<HttpResponse<T>> PutAsync<T>(...)`
- **Purpose:** Sends an HTTP `PUT` request with a serialized body and deserializes the response into `T`.
- **Parameters:** A URI, the request payload object, and an optional cancellation token.
- **Returns:** `Task<HttpResponse<T>>` containing the deserialized response.
- **Throws:** Same conditions as `PostAsync<T>`.

### DeleteAsync
- **Signature:** `async Task<HttpResponse<string>> DeleteAsync(...)`
- **Purpose:** Sends an HTTP `DELETE` request and returns the response body as a raw string.
- **Parameters:** A URI and an optional cancellation token.
- **Returns:** `Task<HttpResponse<string>>` where `Data` is the raw response content string.
- **Throws:** `ArgumentNullException` for a null URI. `HttpRequestException` for network failures. `TaskCanceledException` on cancellation/timeout.

### StatusCode
- **Type:** `int` (property, getter, belongs to `HttpResponse<T>`)
- **Purpose:** The HTTP status code returned by the server (e.g., 200, 404, 500).

### IsSuccess
- **Type:** `bool` (property, getter, belongs to `HttpResponse<T>`)
- **Purpose:** Indicates whether the HTTP response status code falls within the successful 2xx range. `true` for success; `false` otherwise.

### Data
- **Type:** `T` (property, getter, belongs to `HttpResponse<T>`)
- **Purpose:** The deserialized response body when `IsSuccess` is `true` and deserialization succeeds. When `IsSuccess` is `false` or deserialization fails, this property holds the default value of `T`.

### Error
- **Type:** `string` (property, getter, belongs to `HttpResponse<T>`)
- **Purpose:** An error message describing what went wrong. Populated when the request fails at the transport level, the server returns a non-success status code, or deserialization of the response body fails. `null` or empty when the entire request/response cycle completes successfully.

### RawContent
- **Type:** `string` (property, getter, belongs to `HttpResponse<T>`)
- **Purpose:** The raw response body as a string, regardless of success or failure. Useful for logging, debugging, or inspecting error payloads from the server when deserialization is not possible.

## Usage

### Example 1: Fetching a configuration object with error handling

```csharp
IHttpClient client = /* obtained via DI or factory */;
HttpResponse<CaddyConfig> response = await client.GetAsync<CaddyConfig>("api/v1/config");

if (response.IsSuccess)
{
    CaddyConfig config = response.Data;
    Console.WriteLine($"Loaded config revision: {config.Revision}");
}
else
{
    Console.WriteLine($"Request failed with status {response.StatusCode}: {response.Error}");
    Console.WriteLine($"Raw response: {response.RawContent}");
}
```

### Example 2: Creating a resource with POST and inspecting the raw response

```csharp
IHttpClient client = /* ... */;
var newRecord = new DnsRecord { Name = "www", Type = "A", Value = "10.0.0.1" };

HttpResponse<DnsRecord> response = await client.PostAsync<DnsRecord>("api/v1/dns/records", newRecord);

if (response.IsSuccess)
{
    Console.WriteLine($"Created record with ID: {response.Data.Id}");
}
else if (response.StatusCode == 409)
{
    // Conflict – inspect the server's error payload
    Console.WriteLine($"Conflict: {response.RawContent}");
}
else
{
    Console.WriteLine($"Unexpected error: {response.Error}");
}
```

## Notes

- **Error capture vs. exceptions:** Network-level failures (DNS resolution, connection refused, TLS errors) and timeouts are thrown as exceptions (`HttpRequestException`, `TaskCanceledException`). Application-level failures (4xx, 5xx status codes) and deserialization errors are captured in the `HttpResponse<T>` envelope and do not throw.
- **Default value on failure:** When `IsSuccess` is `false`, the `Data` property returns `default(T)` (e.g., `null` for reference types). Always check `IsSuccess` before accessing `Data`.
- **RawContent availability:** `RawContent` is populated even when deserialization fails or the status code indicates an error, provided the server returned a response body. It may be `null` or empty if no body was sent.
- **Thread safety:** The interface itself is stateless beyond the shared `HttpClientWrapper`. Concurrent calls through the same `IHttpClient` instance are safe as long as the underlying `HttpClientWrapper` is used in a thread-safe manner (the standard `HttpClient` is thread-safe for concurrent requests). Instance properties like `StatusCode`, `IsSuccess`, `Data`, `Error`, and `RawContent` belong to individual `HttpResponse<T>` objects returned from each call and do not share mutable state across invocations.
- **Cancellation:** All async methods accept an optional `CancellationToken`. If omitted, the underlying `HttpClient` timeout still applies. Passing a token allows cooperative cancellation from the caller side.
