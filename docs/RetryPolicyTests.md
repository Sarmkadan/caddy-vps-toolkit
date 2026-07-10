# RetryPolicyTests

`RetryPolicyTests` is a test class within the `caddy-vps-toolkit` project that validates the behaviour of retry policy implementations. It ensures that synchronous and asynchronous retry strategies handle success, transient failure followed by recovery, exhaustion of retry attempts, and invalid arguments according to their documented contracts.

## API

### ExecuteAsync_SuccessOnFirstAttempt_ReturnsResultWithoutRetry
```csharp
public async Task ExecuteAsync_SuccessOnFirstAttempt_ReturnsResultWithoutRetry()
```
Verifies that when an operation succeeds on the first invocation, the retry policy returns the expected result immediately and does not perform any retry attempts.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy retries unnecessarily or returns an incorrect value.

### ExecuteAsync_FailsThenSucceeds_ReturnsResultAfterRetry
```csharp
public async Task ExecuteAsync_FailsThenSucceeds_ReturnsResultAfterRetry()
```
Confirms that when an operation throws on its first call but succeeds on a subsequent attempt, the policy retries the operation and ultimately returns the successful result.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy does not retry, retries an incorrect number of times, or fails to return the eventual success value.

### ExecuteAsync_ExceedsMaxRetries_RethrowsLastException
```csharp
public async Task ExecuteAsync_ExceedsMaxRetries_RethrowsLastException()
```
Ensures that when an operation consistently fails and the maximum retry count is exhausted, the policy rethrows the last observed exception rather than swallowing it or returning a default value.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy suppresses the exception, throws an earlier exception instead of the last one, or exceeds the configured maximum attempts.

### ExecuteAsync_NullOperation_ThrowsArgumentNullException
```csharp
public async Task ExecuteAsync_NullOperation_ThrowsArgumentNullException()
```
Validates that passing a `null` operation delegate to the asynchronous `ExecuteAsync` method immediately throws an `ArgumentNullException`, providing fail-fast behaviour for incorrect usage.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy attempts to invoke the null delegate or throws an exception of a different type.

### ExecuteAsync_VoidOverload_NullOperation_ThrowsArgumentNullException
```csharp
public async Task ExecuteAsync_VoidOverload_NullOperation_ThrowsArgumentNullException()
```
Validates that the void-returning overload of `ExecuteAsync` also throws an `ArgumentNullException` when supplied with a `null` operation delegate, ensuring consistent argument validation across overloads.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the void overload does not perform null-checking or throws an unexpected exception type.

### LinearBackoffRetryPolicy_SuccessOnFirstAttempt_ReturnsResult
```csharp
public async Task LinearBackoffRetryPolicy_SuccessOnFirstAttempt_ReturnsResult()
```
Tests the linear backoff variant of the retry policy, confirming that a successful first attempt returns the result without applying any delay or retry logic.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the linear backoff policy introduces unnecessary delays or retries on initial success.

### LinearBackoffRetryPolicy_FailsThenSucceeds_RetriesAndReturns
```csharp
public async Task LinearBackoffRetryPolicy_FailsThenSucceeds_RetriesAndReturns()
```
Verifies that the linear backoff policy retries after a failure, applies the expected linear delay between attempts, and returns the result once the operation succeeds.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy does not respect the linear backoff timing, fails to retry, or returns an incorrect result.

### LinearBackoffRetryPolicy_ExceedsMaxRetries_Throws
```csharp
public async Task LinearBackoffRetryPolicy_ExceedsMaxRetries_Throws()
```
Ensures that the linear backoff policy rethrows the last exception when the maximum number of retries is exceeded, and that the total number of attempts matches the configured limit.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy suppresses the final exception or attempts more than the allowed number of retries.

### NoRetryPolicy_SuccessOnFirstCall_ReturnsResult
```csharp
public async Task NoRetryPolicy_SuccessOnFirstCall_ReturnsResult()
```
Tests the no-retry policy variant, confirming that a successful operation returns its result directly without any retry infrastructure being invoked.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the no-retry policy introduces any retry behaviour or alters the return value.

### NoRetryPolicy_OperationThrows_PropagatesImmediately
```csharp
public async Task NoRetryPolicy_OperationThrows_PropagatesImmediately()
```
Validates that the no-retry policy propagates an exception thrown by the operation immediately, without attempting any retries or wrapping the exception.

**Parameters:** None (test method).  
**Returns:** A completed `Task`.  
**Throws:** Fails the test if the policy retries, delays, or wraps the original exception.

## Usage

### Testing a custom retry policy against the established contract
```csharp
[TestClass]
public class MyRetryPolicyTests : RetryPolicyTests
{
    protected override IRetryPolicy CreatePolicy()
    {
        // Provide the concrete implementation under test
        return new MyCustomRetryPolicy(maxRetries: 3, baseDelay: TimeSpan.FromMilliseconds(50));
    }

    // Inherited tests automatically validate MyCustomRetryPolicy
    // against all RetryPolicyTests scenarios.
}
```

### Direct invocation of a specific test for debugging a failure scenario
```csharp
var test = new RetryPolicyTests
{
    // Assume policy factory is injected or overridden in a derived context
};

await test.ExecuteAsync_FailsThenSucceeds_ReturnsResultAfterRetry();
// If the assertion fails, the exception will describe whether the policy
// failed to retry, retried too many times, or returned an unexpected value.
```

## Notes

- All test methods are asynchronous and should be awaited to ensure proper exception propagation and test completion.
- The tests assume that retry policies are stateless with respect to concurrent invocations; each test method operates on an independent policy instance and operation delegate.
- Argument validation tests (`NullOperation` variants) expect immediate throwing before any delay or retry logic is applied. Implementations that defer delegate invocation risk failing these tests.
- The linear backoff tests may rely on timing tolerances rather than exact delay measurements. Policies that introduce jitter or use wall-clock time should ensure their effective delays fall within the test’s acceptable bounds.
- The no-retry policy tests serve as a baseline: any policy configured with zero retries must behave identically to a direct invocation of the operation delegate.
