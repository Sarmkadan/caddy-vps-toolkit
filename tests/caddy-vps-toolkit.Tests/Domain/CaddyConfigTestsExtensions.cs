#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CaddyVpsToolkit.Tests.Domain
{
    /// <summary>
    /// Extension methods that provide additional introspection and execution helpers for <see cref="CaddyConfigTests"/>.
    /// </summary>
    public static class CaddyConfigTestsExtensions
    {
        /// <summary>
        /// Returns the names of all public instance methods declared on <see cref="CaddyConfigTests"/>
        /// whose name starts with <c>Validate_</c>.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> of method names.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> GetValidationMethodNames(this CaddyConfigTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return tests.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => m.Name.StartsWith("Validate_", StringComparison.Ordinal))
                        .Select(m => m.Name)
                        .ToArray();
        }

        /// <summary>
        /// Executes all validation methods (those whose name starts with <c>Validate_</c>) on the supplied
        /// <see cref="CaddyConfigTests"/> instance and returns a collection that contains the exception
        /// thrown by each method, or <c>null</c> if the method completed without throwing.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> where each element corresponds to a validation method.
        /// The element is <c>null</c> when the method succeeded, otherwise it holds the caught exception.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<Exception?> RunValidationMethods(this CaddyConfigTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var methods = tests.GetValidationMethodNames();
            var results = new List<Exception?>(methods.Count);

            foreach (var name in methods)
            {
                var method = tests.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
                if (method is null)
                {
                    results.Add(new MissingMethodException($"Method '{name}' could not be found."));
                    continue;
                }

                try
                {
                    method.Invoke(tests, null);
                    results.Add(null);
                }
                catch (TargetInvocationException tie) when (tie.InnerException is not null)
                {
                    results.Add(tie.InnerException);
                }
                catch (Exception ex)
                {
                    results.Add(ex);
                }
            }

            return results;
        }

        /// <summary>
        /// Retrieves the names of all public instance methods declared on <see cref="CaddyConfigTests"/>.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the method names.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> GetAllTestMethodNames(this CaddyConfigTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return tests.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Select(m => m.Name)
                        .ToArray();
        }
    }
}
