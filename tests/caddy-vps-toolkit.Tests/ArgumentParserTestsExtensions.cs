using System;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="ArgumentParserTests"/> to facilitate test execution and validation.
    /// </summary>
    public static class ArgumentParserTestsExtensions
    {
        /// <summary>
        /// Executes all synchronous test methods on the provided test instance.
        /// </summary>
        /// <param name="tests">The test instance to execute. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static void RunAllTests(this ArgumentParserTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            tests.GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand();
            tests.GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly();
            tests.GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly();
            tests.ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService().Wait();
        }

        /// <summary>
        /// Determines whether all synchronous tests pass without throwing exceptions.
        /// </summary>
        /// <param name="tests">The test instance to validate. Cannot be null.</param>
        /// <returns>True if all tests pass; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static bool AllTestsPassed(this ArgumentParserTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            try
            {
                tests.RunAllTests();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Executes all test methods asynchronously on the provided test instance.
        /// </summary>
        /// <param name="tests">The test instance to execute. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static async Task RunAllTestsAsync(this ArgumentParserTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            await tests.ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService();
            tests.GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand();
            tests.GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly();
            tests.GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly();
        }

        /// <summary>
        /// Determines whether all tests pass asynchronously without throwing exceptions.
        /// </summary>
        /// <param name="tests">The test instance to validate. Cannot be null.</param>
        /// <returns>A task with a boolean result: true if all tests pass; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
        public static async Task<bool> AllTestsPassedAsync(this ArgumentParserTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            try
            {
                await tests.RunAllTestsAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}