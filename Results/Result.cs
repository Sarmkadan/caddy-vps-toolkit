#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Results
{
    /// <summary>
    /// Generic result wrapper for operation outcomes.
    /// Supports success/failure states with data and error information.
    /// Useful for API responses and operation results.
    /// </summary>
    public sealed class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }

        public static Result<T> Success(T data = default)
        {
            return new Result<T> { IsSuccess = true, Data = data };
        }

        public static Result<T> Failure(string errorMessage, string errorCode = null)
        {
            return new Result<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode ?? "UNKNOWN_ERROR"
            };
        }
    }

    /// <summary>
    /// Non-generic result wrapper
    /// </summary>
    public sealed class Result
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }

        public static Result Success()
        {
            return new Result { IsSuccess = true };
        }

        public static Result Failure(string errorMessage, string errorCode = null)
        {
            return new Result
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode ?? "UNKNOWN_ERROR"
            };
        }
    }

    /// <summary>
    /// Paginated result for list operations
    /// </summary>
    public sealed class PaginatedResult<T>
    {
        public System.Collections.Generic.List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
