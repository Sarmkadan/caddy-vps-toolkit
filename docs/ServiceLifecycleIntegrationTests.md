# ServiceLifecycleIntegrationTests

A comprehensive suite of integration tests for the `caddy-vps-toolkit` project, verifying end-to-end functionality of service lifecycle management, Caddy configuration generation, caching, retry policies, event handling, and template rendering. These tests ensure that core components interact correctly under realistic conditions, including concurrency, edge cases, and system state transitions.

## API

### `public async Task FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput`
Verifies the complete workflow of creating a service, generating a Caddy configuration, and validating the output structure. Ensures the generated configuration adheres to expected syntax and includes all required route blocks.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the generated configuration is invalid or missing expected elements.

### `public async Task ServiceStatusTransition_StoppedToRunning_UpdatesCorrectly`
Tests the transition of a service's status from `Stopped` to `Running`, confirming that the internal state is updated correctly and persists across operations.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the status transition fails or the state is not updated as expected.

### `public async Task ServiceLifecycle_CreateAndDelete_WorksEndToEnd`
Validates the full lifecycle of a service, including creation, verification, and deletion. Ensures no orphaned resources remain after deletion.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if any step in the lifecycle fails (e.g., creation, deletion, or verification).

### `public async Task HealthHistory_GetLast24Hours_ReturnsResults`
Checks that the health history retrieval for the last 24 hours returns non-empty results, verifying that health monitoring data is correctly recorded and accessible.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if no health history results are returned.

### `public async Task MemoryCache_ConcurrentReadWrites_AllSucceed`
Tests concurrent read and write operations on the memory cache, ensuring thread safety and consistency under load. Validates that all operations complete without data corruption or race conditions.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if any operation fails or cache consistency is violated.

### `public async Task EventBus_ConcurrentPublishes_HandlersInvokedCorrectCount`
Verifies that concurrent event publications result in the correct number of handler invocations, ensuring the event bus processes messages reliably under load.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the handler invocation count does not match expectations.

### `public async Task RetryPolicy_ConcurrentOperations_AllComplete`
Tests the retry policy under concurrent operations, ensuring all operations eventually succeed despite transient failures.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if any operation fails to complete after retries.

### `public void StateMachine_IndependentInstances_DoNotInterfere`
Confirms that multiple independent instances of the state machine operate without interference, ensuring isolation between service instances.
**Parameters:** None.
**Returns:** `void`.
**Throws:**
- `AssertFailedException` if state changes in one instance affect another.

### `public async Task CaddyConfig_WithMultipleActiveRoutes_GeneratesAllRouteBlocks`
Ensures that a Caddy configuration with multiple active routes generates all expected route blocks, validating the correctness of the configuration generation logic.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if any route block is missing or malformed.

### `public async Task CaddyConfig_WithNoActiveRoutes_IncludesPlaceholderComment`
Tests that a Caddy configuration with no active routes includes a placeholder comment, ensuring graceful handling of edge cases.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the placeholder comment is missing.

### `public async Task CaddyConfig_WithNullRoutesList_TreatsAsEmpty`
Verifies that a `null` routes list is treated as an empty list during Caddy configuration generation, preventing null reference exceptions.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the configuration generation fails or behaves unexpectedly.

### `public void PaginationAndFiltering_CombinedQueryBuilder_ReturnsExpectedPage`
Tests the combined query builder for pagination and filtering, ensuring it returns the correct page of results based on the provided criteria.
**Parameters:** None.
**Returns:** `void`.
**Throws:**
- `AssertFailedException` if the returned page does not match expectations.

### `public async Task CacheAndRetryPolicy_CacheHit_SkipsRetryableOperation`
Validates that a cache hit bypasses the retryable operation, confirming that caching optimizes performance by avoiding unnecessary retries.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the retryable operation is not skipped on a cache hit.

### `public void TemplateEngine_SystemdUnitTemplate_RendersCorrectly`
Ensures the systemd unit template renders correctly, including all required placeholders and dynamic values.
**Parameters:** None.
**Returns:** `void`.
**Throws:**
- `AssertFailedException` if the rendered template is missing expected content.

### `public async Task ReadmeUseCase_AddServiceAndGenerateConfigs_ProducesExpectedOutput`
Tests the end-to-end use case described in the README, verifying that adding a service and generating configurations produces the expected output.
**Parameters:** None.
**Returns:** `Task` representing the asynchronous operation.
**Throws:**
- `AssertFailedException` if the output does not match the expected configuration.

### `public void ReadmeUseCase_PaginateServiceList_ReturnsCorrectPage`
Validates the pagination use case described in the README, ensuring the service list returns the correct page of results.
**Parameters:** None.
**Returns:** `void`.
**Throws:**
- `AssertFailedException` if the returned page does not match expectations.

## Usage

### Example 1: Testing Service Lifecycle and Caddy Configuration
