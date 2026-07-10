#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Results;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Results
{
    /// <summary>
    /// Tests for the generic Result class.
    /// </summary>
    public sealed class ResultGenericTests
    {
        /// <summary>
        /// Verifies that a successful result with data sets IsSuccess to true and Data to the provided value.
        /// </summary>
        [Fact]
        public void Success_WithData_SetsIsSuccessTrueAndData()
        {
            var result = Result<string>.Success("hello");

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("hello");
            result.ErrorMessage.Should().BeNull();
        }

        /// <summary>
        /// Verifies that a successful result without an argument sets IsSuccess to true and Data to the default value.
        /// </summary>
        [Fact]
        public void Success_WithoutArgument_SetsIsSuccessTrueAndDefaultData()
        {
            var result = Result<int>.Success();

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(default(int));
        }

        /// <summary>
        /// Verifies that a failed result with a message sets IsSuccess to false and ErrorMessage to the provided value.
        /// </summary>
        [Fact]
        public void Failure_WithMessage_SetsIsSuccessFalseAndMessage()
        {
            var result = Result<string>.Failure("something went wrong");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("something went wrong");
            result.Data.Should().BeNull();
        }

        /// <summary>
        /// Verifies that a failed result with a message and code sets ErrorCode to the provided value.
        /// </summary>
        [Fact]
        public void Failure_WithMessageAndCode_SetsErrorCode()
        {
            var result = Result<int>.Failure("bad request", "BAD_REQUEST");

            result.ErrorCode.Should().Be("BAD_REQUEST");
            result.IsSuccess.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that a failed result without a code defaults to UNKNOWN_ERROR.
        /// </summary>
        [Fact]
        public void Failure_WithoutCode_DefaultsToUnknownError()
        {
            var result = Result<int>.Failure("oops");

            result.ErrorCode.Should().Be("UNKNOWN_ERROR");
        }
    }

    /// <summary>
    /// Tests for the non-generic Result class.
    /// </summary>
    public sealed class ResultNonGenericTests
    {
        /// <summary>
        /// Verifies that a successful result sets IsSuccess to true.
        /// </summary>
        [Fact]
        public void Success_SetsIsSuccessTrue()
        {
            var result = Result.Success();

            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        /// <summary>
        /// Verifies that a failed result sets IsSuccess to false and ErrorMessage to the provided value.
        /// </summary>
        [Fact]
        public void Failure_SetsIsSuccessFalseAndMessage()
        {
            var result = Result.Failure("failed");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("failed");
        }

        /// <summary>
        /// Verifies that a failed result without a code defaults to UNKNOWN_ERROR.
        /// </summary>
        [Fact]
        public void Failure_WithoutCode_DefaultsToUnknownError()
        {
            var result = Result.Failure("error");

            result.ErrorCode.Should().Be("UNKNOWN_ERROR");
        }
    }

    /// <summary>
    /// Tests for the PaginatedResult class.
    /// </summary>
    public sealed class PaginatedResultTests
    {
        /// <summary>
        /// Verifies that TotalPages calculates correctly for even division.
        /// </summary>
        [Fact]
        public void TotalPages_CalculatesCorrectlyForEvenDivision()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 20,
                PageSize = 5,
                Page = 1
            };

            paged.TotalPages.Should().Be(4);
        }

        /// <summary>
        /// Verifies that TotalPages rounds up for remainder.
        /// </summary>
        [Fact]
        public void TotalPages_RoundsUpForRemainder()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 21,
                PageSize = 5,
                Page = 1
            };

            paged.TotalPages.Should().Be(5);
        }

        /// <summary>
        /// Verifies that HasNextPage returns true when not on the last page.
        /// </summary>
        [Fact]
        public void HasNextPage_WhenNotOnLastPage_ReturnsTrue()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 20,
                PageSize = 5,
                Page = 3
            };

            paged.HasNextPage.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that HasNextPage returns false when on the last page.
        /// </summary>
        [Fact]
        public void HasNextPage_WhenOnLastPage_ReturnsFalse()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 20,
                PageSize = 5,
                Page = 4
            };

            paged.HasNextPage.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that HasPreviousPage returns false when on the first page.
        /// </summary>
        [Fact]
        public void HasPreviousPage_WhenOnFirstPage_ReturnsFalse()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 20,
                PageSize = 5,
                Page = 1
            };

            paged.HasPreviousPage.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that HasPreviousPage returns true when past the first page.
        /// </summary>
        [Fact]
        public void HasPreviousPage_WhenPastFirstPage_ReturnsTrue()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 20,
                PageSize = 5,
                Page = 2
            };

            paged.HasPreviousPage.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that TotalPages returns 0 when TotalCount is 0.
        /// </summary>
        [Fact]
        public void TotalPages_WhenTotalCountIsZero_ReturnsZero()
        {
            var paged = new PaginatedResult<int>
            {
                TotalCount = 0,
                PageSize = 10,
                Page = 1
            };

            paged.TotalPages.Should().Be(0);
            paged.HasNextPage.Should().BeFalse();
        }
    }
}
