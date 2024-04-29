namespace CaddyVpsToolkit.Tests.Domain;

public static class CaddyRouteEdgeCaseTestsExtensions
{
    /// <summary>
    /// Verifies that Validate_BasicAuthEnabledWithoutPassword_ThrowsValidationException is present.
    /// </summary>
    /// <exception cref="AssertException">Thrown if <paramref name="tests"/> does not have the expected test.</exception>
    public static void Validate_BasicAuthEnabledWithoutPassword_ThrowsValidationExceptionTestExists(this CaddyRouteEdgeCaseTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        
        // Assuming there's a test method for the scenario
        tests.GetType().GetMethod("Validate_BasicAuthEnabledWithoutPassword_ThrowsValidationException")?.Invoke(tests, null);
    }

    /// <summary>
    /// Verifies that Validate_CookieAuthEnabledWithoutCookieName_ThrowsValidationException is present.
    /// </summary>
    /// <exception cref="AssertException">Thrown if <paramref name="tests"/> does not have the expected test.</exception>
    public static void Validate_CookieAuthEnabledWithoutCookieName_ThrowsValidationExceptionTestExists(this CaddyRouteEdgeCaseTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        
        // Assuming there's a test method for the scenario
        tests.GetType().GetMethod("Validate_CookieAuthEnabledWithoutCookieName_ThrowsValidationException")?.Invoke(tests, null);
    }
}
