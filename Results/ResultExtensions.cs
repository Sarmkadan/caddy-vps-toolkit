#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CaddyVpsToolkit.Results
{
    /// <summary>
    /// Extension methods for <see cref="Result"/> and <see cref="Result{T}"/> types.
    /// Provides common operations like mapping, chaining, and conversion.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Maps a successful result to a new value using the provided function.
        /// If the result is a failure, returns the failure unchanged.
        /// </summary>
        /// <typeparam name="T">The source result data type</typeparam>
        /// <typeparam name="TResult">The result data type after mapping</typeparam>
        /// <param name="result">The source result</param>
        /// <param name="mapper">Function to transform the data on success</param>
        /// <returns>A new result with transformed data, or the original failure</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is null</exception>
        public static Result<TResult> Map<T, TResult>(
            this Result<T> result,
            Func<T, TResult> mapper)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(mapper);

            return result.IsSuccess
                ? Result<TResult>.Success(mapper(result.Data))
                : Result<TResult>.Failure(result.ErrorMessage, result.ErrorCode);
        }

        /// <summary>
        /// Binds a successful result to another result using the provided function.
        /// Enables fluent chaining of operations that return results.
        /// </summary>
        /// <typeparam name="T">The source result data type</typeparam>
        /// <typeparam name="TResult">The result data type after binding</typeparam>
        /// <param name="result">The source result</param>
        /// <param name="binder">Function to transform the data and return a new result</param>
        /// <returns>The bound result, or the original failure</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null</exception>
        public static Result<TResult> Bind<T, TResult>(
            this Result<T> result,
            Func<T, Result<TResult>> binder)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(binder);

            return result.IsSuccess
                ? binder(result.Data)
                : Result<TResult>.Failure(result.ErrorMessage, result.ErrorCode);
        }

        /// <summary>
        /// Converts a result to a boolean indicating success.
        /// Returns true if the result is successful, false otherwise.
        /// </summary>
        /// <typeparam name="T">The result data type</typeparam>
        /// <param name="result">The result to check</param>
        /// <returns>True if successful, false if failed</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static bool ToBoolean<T>(this Result<T> result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return result.IsSuccess;
        }

        /// <summary>
        /// Converts a non-generic result to a boolean indicating success.
        /// </summary>
        /// <param name="result">The result to check</param>
        /// <returns>True if successful, false if failed</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static bool ToBoolean(this Result result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return result.IsSuccess;
        }

        /// <summary>
        /// Gets the error message if the result failed, or null if successful.
        /// </summary>
        /// <typeparam name="T">The result data type</typeparam>
        /// <param name="result">The result to check</param>
        /// <returns>The error message if failed, otherwise null</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static string? GetErrorOrNull<T>(this Result<T> result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return result.IsSuccess ? null : result.ErrorMessage;
        }

        /// <summary>
        /// Gets the error message if the result failed, or null if successful.
        /// </summary>
        /// <param name="result">The result to check</param>
        /// <returns>The error message if failed, otherwise null</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static string? GetErrorOrNull(this Result result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return result.IsSuccess ? null : result.ErrorMessage;
        }

        /// <summary>
        /// Safely executes an action if the result is successful.
        /// The action is only executed if the result succeeded.
        /// </summary>
        /// <typeparam name="T">The result data type</typeparam>
        /// <param name="result">The source result</param>
        /// <param name="action">Action to execute on success</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        public static void OnSuccess<T>(
            this Result<T> result,
            Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(action);

            if (result.IsSuccess)
            {
                action(result.Data);
            }
        }

        /// <summary>
        /// Safely executes an action if the result is successful.
        /// The action is only executed if the result succeeded.
        /// </summary>
        /// <param name="result">The source result</param>
        /// <param name="action">Action to execute on success</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        public static void OnSuccess(
            this Result result,
            Action action)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(action);

            if (result.IsSuccess)
            {
                action();
            }
        }

        /// <summary>
        /// Safely executes an action if the result failed.
        /// The action is only executed if the result failed.
        /// </summary>
        /// <typeparam name="T">The result data type</typeparam>
        /// <param name="result">The source result</param>
        /// <param name="action">Action to execute on failure</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        public static void OnFailure<T>(
            this Result<T> result,
            Action<string, string> action)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(action);

            if (!result.IsSuccess)
            {
                action(result.ErrorMessage, result.ErrorCode);
            }
        }

        /// <summary>
        /// Safely executes an action if the result failed.
        /// The action is only executed if the result failed.
        /// </summary>
        /// <param name="result">The source result</param>
        /// <param name="action">Action to execute on failure</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null</exception>
        public static void OnFailure(
            this Result result,
            Action<string, string> action)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(action);

            if (!result.IsSuccess)
            {
                action(result.ErrorMessage, result.ErrorCode);
            }
        }

        /// <summary>
        /// Converts a paginated result to a read-only list.
        /// Returns an empty list if the result is null or failed.
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="result">The paginated result</param>
        /// <returns>Read-only list of items, or empty list on failure</returns>
        public static IReadOnlyList<T> ToReadOnlyList<T>(this PaginatedResult<T>? result)
        {
            return result?.Items.AsReadOnly() ?? Array.Empty<T>().AsReadOnly();
        }

        /// <summary>
        /// Converts a result to a tuple containing success status and data/error.
        /// </summary>
        /// <typeparam name="T">The result data type</typeparam>
        /// <param name="result">The source result</param>
        /// <returns>
        /// A tuple where Item1 is true for success/false for failure,
        /// Item2 is the data if successful or error message if failed
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static (bool IsSuccess, T Data) ToTuple<T>(this Result<T> result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return (result.IsSuccess, result.IsSuccess ? result.Data : default!);
        }

        /// <summary>
        /// Converts a non-generic result to a tuple containing success status.
        /// </summary>
        /// <param name="result">The source result</param>
        /// <returns>A tuple where Item1 is true for success/false for failure</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null</exception>
        public static (bool Success, string ErrorMessage) ToTuple(this Result result)
        {
            ArgumentNullException.ThrowIfNull(result);
            return result.IsSuccess
                ? (true, string.Empty)
                : (false, result.ErrorMessage);
        }
    }
}