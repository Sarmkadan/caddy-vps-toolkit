#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Middleware
{
    /// <summary>
    /// Pipeline for handling errors across the application.
    /// Catches exceptions, logs them, and converts to user-friendly messages.
    /// This approach centralizes error handling and ensures consistent error responses.
    /// </summary>
    public interface IErrorHandler
    {
        Task<ErrorResponse> HandleAsync(Exception ex);
    }

    public sealed class ErrorHandlingPipeline : IErrorHandler
    {
        private readonly ILogger _logger;
        private readonly List<Func<Exception, Task<ErrorResponse>>> _handlers;

        public ErrorHandlingPipeline(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = new List<Func<Exception, Task<ErrorResponse>>>();
            SetupDefaultHandlers();
        }

        private void SetupDefaultHandlers()
        {
            // Handle specific exception types
            AddHandler(ex => ex is ServiceNotFoundException, async ex =>
            {
                await _logger.LogErrorAsync($"Service not found: {ex.Message}");
                return new ErrorResponse
                {
                    ExitCode = 1,
                    Message = ex.Message,
                    Code = "SERVICE_NOT_FOUND"
                };
            });

            AddHandler(ex => ex is ServiceConfigurationException, async ex =>
            {
                await _logger.LogErrorAsync($"Configuration error: {ex.Message}");
                return new ErrorResponse
                {
                    ExitCode = 2,
                    Message = ex.Message,
                    Code = "CONFIGURATION_ERROR"
                };
            });

            AddHandler(ex => ex is ArgumentException, async ex =>
            {
                await _logger.LogErrorAsync($"Invalid argument: {ex.Message}");
                return new ErrorResponse
                {
                    ExitCode = 3,
                    Message = ex.Message,
                    Code = "INVALID_ARGUMENT"
                };
            });
        }

        public void AddHandler(Func<Exception, bool> predicate, Func<Exception, Task<ErrorResponse>> handler)
        {
            _handlers.Add(async ex =>
            {
                if (predicate(ex))
                    return await handler(ex);
                return null;
            });
        }

        public async Task<ErrorResponse> HandleAsync(Exception ex)
        {
            foreach (var handler in _handlers)
            {
                var result = await handler(ex);
                if (result is not null)
                    return result;
            }

            // Default handling for unknown exceptions
            await _logger.LogErrorAsync($"Unexpected error: {ex.Message}");
            return new ErrorResponse
            {
                ExitCode = 255,
                Message = "An unexpected error occurred",
                Code = "UNEXPECTED_ERROR",
                Details = ex.ToString()
            };
        }
    }

    /// <summary>
    /// Structured error response
    /// </summary>
    public sealed class ErrorResponse
    {
        public int ExitCode { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
