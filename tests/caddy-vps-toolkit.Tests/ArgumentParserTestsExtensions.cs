using System;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Tests
{
    public static class ArgumentParserTestsExtensions
    {
        public static void RunAllTests(this ArgumentParserTests tests)
        {
            tests.GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand();
            tests.GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly();
            tests.GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly();
            tests.ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService().Wait();
        }

        public static bool AllTestsPassed(this ArgumentParserTests tests)
        {
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

        public static async Task RunAllTestsAsync(this ArgumentParserTests tests)
        {
            await tests.ServiceRepository_GetByIdAsync_WithMockedRepository_ReturnsExpectedService();
            tests.GetCommand_CommandWithMixedCase_ReturnsLowercaseCommand();
            tests.GetFlagValue_EqualsSignFormat_ExtractsValueCorrectly();
            tests.GetFlagValue_SpaceSeparatedFormat_ExtractsValueCorrectly();
        }

        public static async Task<bool> AllTestsPassedAsync(this ArgumentParserTests tests)
        {
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
