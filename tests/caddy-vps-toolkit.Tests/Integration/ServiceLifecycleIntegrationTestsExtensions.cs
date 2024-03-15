namespace CaddyVpsToolkit.Tests.Integration;

public static class ServiceLifecycleIntegrationTestsExtensions
{
    /// <summary>
    /// Verifies that multiple consecutive calls to FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput 
    /// do not interfere with each other.
    /// </summary>
    public static async Task FullWorkflow_CreateService_ThenGenerateCaddyConfig_MultipleCallsProduceValidOutput(
        this ServiceLifecycleIntegrationTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        await tests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput();
        await tests.FullWorkflow_CreateService_ThenGenerateCaddyConfig_ProducesValidOutput();
    }

    /// <summary>
    /// Tests ServiceLifecycle_CreateAndDelete_WorksEndToEnd with a larger number of iterations.
    /// </summary>
    /// <param name="tests">The instance of ServiceLifecycleIntegrationTests.</param>
    /// <param name="iterations">The number of iterations to perform.</param>
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
}
