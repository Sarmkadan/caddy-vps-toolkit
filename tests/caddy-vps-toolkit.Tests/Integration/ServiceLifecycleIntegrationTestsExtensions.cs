namespace CaddyVpsToolkit.Tests.Integration;

/// <summary>
/// Provides extension methods for <see cref="ServiceLifecycleIntegrationTests"/> to facilitate common test scenarios
/// and assertions related to service lifecycle management and Caddy configuration generation.
/// </summary>
public static class ServiceLifecycleIntegrationTestsExtensions
{
    /// <summary>
    /// Verifies that multiple consecutive calls to <see cref="ServiceLifecycleIntegrationTests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput"/>
    /// do not interfere with each other and produce valid output consistently.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    public static async Task FullWorkflow_CreateService_ThenGenerateCaddyConfig_MultipleCallsProduceValidOutput(
        this ServiceLifecycleIntegrationTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        await tests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput();
        await tests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput();
    }

    /// <summary>
    /// Tests <see cref="ServiceLifecycleIntegrationTests.ServiceLifecycle_CreateAndDelete_WorksEndToEnd"/> with a specified number of iterations.
    /// Useful for stress testing service creation and deletion operations.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="iterations">The number of iterations to perform. Must be non-negative.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is negative.</exception>
    public static async Task ServiceLifecycle_CreateAndDelete_LargerNumberOfIterations_WorksEndToEnd(
        this ServiceLifecycleIntegrationTests tests, int iterations)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentOutOfRangeException.ThrowIfNegative(iterations);

        for (int i = 0; i < iterations; i++)
        {
            await tests.ServiceLifecycle_CreateAndDelete_WorksEndToEnd();
        }
    }

    /// <summary>
    /// Executes the full service lifecycle workflow multiple times in sequence with the same service definition.
    /// Verifies that service creation, status transitions, and deletion operations are idempotent.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="repeatCount">The number of times to repeat the workflow. Must be positive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="repeatCount"/> is not positive.</exception>
    public static async Task FullServiceLifecycle_RepeatWorkflow_Idempotent(
        this ServiceLifecycleIntegrationTests tests, int repeatCount)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(repeatCount, 0);

        for (int i = 0; i < repeatCount; i++)
        {
            await tests.ServiceLifecycle_CreateAndDelete_WorksEndToEnd();
        }
    }

    /// <summary>
    /// Executes the full workflow and verifies that Caddy configuration generation produces consistent results
    /// across multiple invocations with the same input.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="invocationCount">The number of times to invoke the workflow. Must be positive.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="invocationCount"/> is not positive.</exception>
    public static async Task FullWorkflow_CreateService_ThenGenerateCaddyConfig_ConsistentOutput(
        this ServiceLifecycleIntegrationTests tests, int invocationCount)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(invocationCount, 0);

        for (int i = 0; i < invocationCount; i++)
        {
            await tests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput();
        }
    }
}
