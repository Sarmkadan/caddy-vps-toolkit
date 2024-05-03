# IRetryPolicy
The `IRetryPolicy` type is designed to handle retry logic for asynchronous operations, providing a way to execute tasks with a retry mechanism in case of failures. This allows for more robust and resilient code, enabling developers to define policies for retrying failed operations.

## API
* `ExponentialBackoffRetryPolicy`: This property returns an instance of `ExponentialBackoffRetryPolicy`, which is a specific implementation of a retry policy that increases the backoff time exponentially between retries.
* `LinearBackoffRetryPolicy`: This property returns an instance of `LinearBackoffRetryPolicy`, which is a specific implementation of a retry policy that increases the backoff time linearly between retries.
* `ExecuteAsync<T>`: This method executes an asynchronous operation with the retry policy. It takes no parameters and returns a `Task` of type `T`. The method will retry the operation according to the defined policy if it fails.
* `ExecuteAsync`: This method executes an asynchronous operation with the retry policy. It takes no parameters and returns a `Task`. The method will retry the operation according to the defined policy if it fails.

## Usage
The following examples demonstrate how to use the `IRetryPolicy` type:
```csharp
// Example 1: Using ExponentialBackoffRetryPolicy
var retryPolicy = new ExponentialBackoffRetryPolicy();
var result = await retryPolicy.ExecuteAsync<string>(() => FetchDataFromApi());

// Example 2: Using LinearBackoffRetryPolicy
var retryPolicy = new LinearBackoffRetryPolicy();
await retryPolicy.ExecuteAsync(() => SendDataToApi());
```

## Notes
When using `IRetryPolicy`, consider the following edge cases and thread-safety remarks:
* The retry policy will only retry the operation if it fails, and the number of retries is determined by the specific policy implementation.
* The `ExponentialBackoffRetryPolicy` and `LinearBackoffRetryPolicy` properties return new instances each time they are accessed, so they can be safely used from multiple threads.
* The `ExecuteAsync` and `ExecuteAsync<T>` methods are asynchronous and non-blocking, allowing the calling thread to continue executing while the operation is being retried.
* If the operation being retried throws an exception, the retry policy will catch the exception and retry the operation according to the defined policy. If all retries fail, the exception will be re-thrown.
