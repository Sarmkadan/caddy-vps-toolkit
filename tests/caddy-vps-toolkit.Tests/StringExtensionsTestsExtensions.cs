using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CaddyVpsToolkit.Tests
{
    /// <summary>
    /// Extension methods that operate on <see cref="StringExtensionsTests"/> instances.
    /// </summary>
    public static class StringExtensionsTestsExtensions
    {
        /// <summary>
        /// Executes all parameter‑less public test methods on the supplied <see cref="StringExtensionsTests"/> instance.
        /// </summary>
        /// <param name="tests">The test class instance whose methods should be run.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> containing the names of the test methods that completed without throwing an exception.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> RunAll(this StringExtensionsTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var passed = new List<string>();
            var methods = typeof(StringExtensionsTests)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void));

            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(tests, null);
                    passed.Add(method.Name);
                }
                catch
                {
                    // Swallow exceptions – a failing test is simply not added to the result list.
                }
            }

            return passed;
        }

        /// <summary>
        /// Retrieves the names of all public, parameter‑less test methods declared on <see cref="StringExtensionsTests"/>.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static IEnumerable<string> GetTestMethodNames(this StringExtensionsTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return typeof(StringExtensionsTests)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void))
                .Select(m => m.Name);
        }

        /// <summary>
        /// Invokes a single test method by its name on the supplied <see cref="StringExtensionsTests"/> instance.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <param name="methodName">The exact name of the test method to invoke.</param>
        /// <returns>
        /// <c>true</c> if the method was found and executed without throwing; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> or <paramref name="methodName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="methodName"/> is an empty string.</exception>
        public static bool InvokeTestByName(this StringExtensionsTests tests, string methodName)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrEmpty(methodName);

            var method = typeof(StringExtensionsTests)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (method is null || method.GetParameters().Length != 0 || method.ReturnType != typeof(void))
                return false;

            try
            {
                method.Invoke(tests, null);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
