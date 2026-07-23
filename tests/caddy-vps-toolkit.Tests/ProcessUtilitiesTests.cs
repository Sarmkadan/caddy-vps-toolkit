#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CaddyVpsToolkit.Utilities;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public class ProcessUtilitiesTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldReturnSuccessAndCaptureOutput()
        {
            // Use a simple command that exists on both Windows and Linux.
            // On Linux we use /bin/echo, on Windows we use cmd /c echo.
            string command;
            string arguments;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = "cmd";
                arguments = "/c echo HelloWorld";
            }
            else
            {
                command = "/bin/echo";
                arguments = "HelloWorld";
            }

            var result = await ProcessUtilities.ExecuteAsync(command, arguments, timeoutMs: 5000);

            Assert.True(result.IsSuccess, $"Process should succeed, but got error: {result.Error}");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("HelloWorld", result.Output.Trim());
            // GetOutput should return the standard output when there is no error.
            Assert.Equal(result.Output.Trim(), result.GetOutput().Trim());
        }

        [Fact]
        public async Task ExecuteAsync_WhenCommandFails_ShouldReturnErrorAndIsSuccessFalse()
        {
            // Invoke a command that will exit with a non‑zero code.
            // Using 'dotnet' with an invalid argument works on both platforms.
            var result = await ProcessUtilities.ExecuteAsync("dotnet", "invalidcommand", timeoutMs: 5000);

            Assert.False(result.IsSuccess);
            Assert.NotEqual(0, result.ExitCode);
            // The error stream should contain something informative.
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            // GetOutput should return the error string because Error is non‑empty.
            Assert.Equal(result.Error, result.GetOutput());
        }

        [Fact]
        public async Task ExecuteAsync_WhenTimeoutOccurs_ShouldReturnTimeoutResult()
        {
            // This test is only reliable on platforms that have the 'sleep' command.
            // Skip on Windows where 'sleep' is not a default executable.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return; // skip test on unsupported OS
            }

            // Run a command that sleeps longer than the timeout.
            var result = await ProcessUtilities.ExecuteAsync("sleep", "1", timeoutMs: 10);

            Assert.False(result.IsSuccess);
            Assert.Equal(-1, result.ExitCode);
            Assert.Equal("Process timeout", result.Error);
            // GetOutput should return the timeout error.
            Assert.Equal("Process timeout", result.GetOutput());
        }

        [Fact]
        public void ProcessResult_GetOutput_ReturnsErrorWhenErrorIsPresent()
        {
            var procResult = new ProcessUtilities.ProcessResult
            {
                ExitCode = 1,
                Output = "standard output",
                Error = "some error",
                IsSuccess = false
            };

            Assert.Equal(procResult.Error, procResult.GetOutput());
        }

        [Fact]
        public void ProcessResult_GetOutput_ReturnsOutputWhenNoError()
        {
            var procResult = new ProcessUtilities.ProcessResult
            {
                ExitCode = 0,
                Output = "standard output",
                Error = "",
                IsSuccess = true
            };

            Assert.Equal(procResult.Output, procResult.GetOutput());
        }
    }
}
