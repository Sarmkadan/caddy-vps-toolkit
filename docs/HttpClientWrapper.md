# HttpClientWrapper

`HttpClientWrapper` is the sealed default implementation of the `IHttpClient` abstraction in the `caddy-vps-toolkit` project. It wraps a single `System.Net.Http.HttpClient` and adds a configurable request timeout, an optional retry policy, and JSON (de)serialization. Every verb method returns an `HttpResponse<T>` envelope that carries the status code, success flag, deserialized data, error message, and raw body in one object.

The supporting types live in the same namespace (`CaddyVpsToolkit.Integration`):
- `IHttpClient` - the interface that consumers should depend on.
- `HttpClientWrapperOptions` - a configuration class with `TimeoutMs` and `RetryPolicy` properties (defined in `Integration/HttpClientWrapperOptions.cs`).
- `HttpResponse<T>` - the response envelope returned by every request method.

## API

### HttpClientWrapper(HttpClientWrapperOptions? options = null)
- **Signature:** `public HttpClientWrapper(HttpClientWrapperOptions? options = null)`
- **Purpose:** Primary constructor. Builds the wrapper from a strongly typed options object, which can be bound from configuration.
- **Parameters:**
  - `options` - the configuration. If `null`, a default `HttpClientWrapperOptions` is used (30 000 ms timeout, no retries).
- **Remarks:** The constructor creates a new `HttpClient` with `Timeout` set to `options.TimeoutMs`. If `RetryPolicy` is `null`, a `NoRetryPolicy` is used.

### HttpClientWrapper(int timeoutMs = 30000, IRetryPolicy? retryPolicy = null)
- **Signature:** `public HttpClientWrapper(int timeoutMs = 30000, IRetryPolicy? retryPolicy = null)`
- **Purpose:** Backward-compatible convenience constructor. It forwards to the options-based constructor.
- **Parameters:**
  - `timeoutMs` - request timeout in milliseconds. Defaults to `30000` (the `DefaultTimeoutMs` constant).
  - `retryPolicy` - the retry policy to apply to each request. If `null`, a `NoRetryPolicy` is used.

### GetAsync\<T\>
- **Signature:** `public async Task<HttpResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null)`
- **Purpose:** Sends an HTTP `GET` request and deserializes the response body into `T`.
- **Parameters:** `url` - the request URL. `headers` - optional request headers, added to the request as-is.
- **Returns:** `Task<HttpResponse<T>>`. `Data` holds the deserialized body when the request succeeds.
- **Throws:** `ArgumentNullException` if `url` is `null`. `ArgumentException` if `url` is empty or whitespace, or if any header value is `null`. All other failures are returned in the envelope (see [Notes](#notes)).

### PostAsync\<T\>
- **Signature:** `public async Task<HttpResponse<T>> PostAsync<T>(string url, object data, Dictionary<string, string>? headers = null)`
- **Purpose:** Serializes `data` to JSON, sends it as an HTTP `POST` body with `Content-Type: application/json`, and deserializes the response body into `T`.
- **Parameters:** `url` - the request URL. `data` - the payload, serialized with `JsonSerializer.Serialize`. `headers` - optional request headers.
- **Returns:** `Task<HttpResponse<T>>`.
- **Throws:** `ArgumentNullException` if `url` is `null`. `ArgumentException` if `url` is empty or whitespace. Serialization and transport failures are returned in the envelope.

### PutAsync\<T\>
- **Signature:** `public async Task<HttpResponse<T>> PutAsync<T>(string url, object data, Dictionary<string, string>? headers = null)`
- **Purpose:** Same as `PostAsync<T>`, but sends an HTTP `PUT`.
- **Parameters:** Same as `PostAsync<T>`.
- **Returns:** `Task<HttpResponse<T>>`.
- **Throws:** Same as `PostAsync<T>`.

### DeleteAsync
- **Signature:** `public async Task<HttpResponse<string>> DeleteAsync(string url, Dictionary<string, string>? headers = null)`
- **Purpose:** Sends an HTTP `DELETE` request. The response body is returned as a raw string, so no deserialization takes place.
- **Parameters:** `url` - the request URL. `headers` - optional request headers.
- **Returns:** `Task<HttpResponse<string>>` where `Data` and `RawContent` both hold the response body.
- **Throws:** `ArgumentNullException` if `url` is `null`. `ArgumentException` if `url` is empty or whitespace.

### HttpResponse\<T\>
A `sealed` class with settable properties. Every property is populated as described below, depending on the outcome of the request.

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `int` | The HTTP status code of the response. `0` if no response was received. |
| `IsSuccess` | `bool` | `true` only when the status code is 2xx **and** the body was deserialized successfully. |
| `Data` | `T?` | The deserialized body, or the body as a string when `T` is `string`. `default` when deserialization fails or no body was read. |
| `Error` | `string?` | The exception message for transport, timeout, retry, serialization, or deserialization failures. `null` for a non-2xx response that was read successfully. |
| `RawContent` | `string?` | The response body as read from the wire, regardless of status or deserialization outcome. |

## Usage

### Example 1: Configuring the wrapper from options
```csharp
var options = new HttpClientWrapperOptions
{
    TimeoutMs = 10_000,
    RetryPolicy = new ExponentialBackoffRetryPolicy(maxRetries: 2)
};

IHttpClient client = new HttpClientWrapper(options);
```

### Example 2: Using the backward-compatible constructor
```csharp
// 5-second timeout, no retries (NoRetryPolicy is used when retryPolicy is null)
IHttpClient client = new HttpClientWrapper(timeoutMs: 5_000);
```

### Example 3: GET with custom headers and error handling
```csharp
var headers = new Dictionary<string, string>
{
    ["Authorization"] = $"Bearer {apiToken}",
    ["Accept"] = "application/json"
};

HttpResponse<ServiceStatus> response =
    await client.GetAsync<ServiceStatus>("https://127.0.0.1:2019/status", headers);

if (response.IsSuccess)
{
    Console.WriteLine($"{response.Data.Name}: healthy={response.Data.Healthy}");
}
else if (response.Error is not null)
{
    // Transport failure, timeout, or deserialization failure
    Console.WriteLine($"Request could not complete: {response.Error}");
}
else
{
    // Server responded with a non-2xx status code
    Console.WriteLine($"HTTP {response.StatusCode}: {response.RawContent}");
}
```

### Example 4: POST with a body and inspecting the raw response
```csharp
var request = new ServiceStatus("api", Healthy: false);

HttpResponse<ServiceStatus> response =
    await client.PostAsync<ServiceStatus>("https://127.0.0.1:2019/status", request);

if (response.StatusCode == 409)
{
    // Conflict: the server's error payload is in RawContent
    Console.WriteLine($"Conflict: {response.RawContent}");
}
```

### Example 5: DELETE returns the raw body
```csharp
HttpResponse<string> response = await client.DeleteAsync("https://127.0.0.1:2019/config/routes/old");

Console.WriteLine(response.IsSuccess
    ? $"Deleted (HTTP {response.StatusCode})"
    : $"Delete failed: {response.Error ?? response.RawContent}");
```

### Example 6: Registering the wrapper in DI
`AddHttpClientServices` registers a singleton `IHttpClient` backed by an `ExponentialBackoffRetryPolicy`:
```csharp
services.AddHttpClientServices(timeoutMs: 15_000, maxRetries: 3);

// Later, in a consumer:
public sealed class CaddyAdminClient(IHttpClient http)
{
    public Task<HttpResponse<string>> GetConfigAsync() =>
        http.GetAsync<string>("http://127.0.0.1:2019/config/");
}
```

## Notes

- **Argument validation throws; runtime failures do not.** `GetAsync`, `PostAsync`, `PutAsync`, and `DeleteAsync` validate `url` before anything else runs, so a `null` or blank URL throws. Anything that happens during the request (network errors, timeouts, serialization, deserialization, retries exhausted) is caught and returned with `IsSuccess = false` and `Error` set.
- **Header validation is only on `GetAsync`.** `GetAsync` rejects headers whose value is `null` with an `ArgumentException`. The other verbs add headers without that check, so avoid `null` values when calling them.
- **Non-2xx responses are not errors.** For a 4xx or 5xx response, `IsSuccess` is `false`, `StatusCode` and `RawContent` are set, and `Error` stays `null`. `Data` still holds the body if it deserializes into `T`, so an error payload may appear there. Check `StatusCode` or `IsSuccess` rather than `Error` to tell these apart from transport failures.
- **Empty bodies and `T = string`.** The body is passed to `JsonSerializer.Deserialize<T>` unless `T` is `string`. Empty input is not valid JSON, so an empty body for a non-`string` `T` is reported as a deserialization failure (`IsSuccess = false`, `Error` set) even when the status is 2xx. Use `T = string` for endpoints that return no content.
- **What counts as a failure for retries.** The retry policy retries when an attempt throws an exception, such as `HttpRequestException` or a timeout. It does not retry on HTTP status codes, because a non-2xx response does not throw. Each attempt builds a new `HttpRequestMessage`, so a `POST` or `PUT` body is resent in full on every retry.
- **Timeouts apply per attempt.** `TimeoutMs` is the `HttpClient.Timeout` for each attempt, so the total time for a request can be roughly `(retries + 1) × TimeoutMs` plus any backoff delay.
- **No cancellation token.** None of the methods accept a `CancellationToken`. The configured timeout is the only time limit on a request.
- **Lifetime.** `HttpClientWrapper` does not implement `IDisposable`, and the internal `HttpClient` is never disposed. Create one instance and reuse it for the life of the application, as the singleton registration in `AddHttpClientServices` does, rather than creating a new wrapper per request.
- **Thread safety.** The wrapper holds no per-request state, and `HttpClient` is safe for concurrent use. A single instance can be shared across threads. Each `HttpResponse<T>` is a new object per call.
- **Default retry behaviour.** When no retry policy is supplied, `NoRetryPolicy` runs each operation exactly once.
