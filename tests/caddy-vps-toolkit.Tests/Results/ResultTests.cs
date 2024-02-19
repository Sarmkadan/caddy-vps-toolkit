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
    public sealed class ResultGenericTests
    {
        [Fact]
        public void Success_WithData_SetsIsSuccessTrueAndData()
        {
            var result = Result<string>.Success("hello");

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("hello");
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Success_WithoutArgument_SetsIsSuccessTrueAndDefaultData()
        {
            var result = Result<int>.Success();

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(default(int));
        }

        [Fact]
        public void Failure_WithMessage_SetsIsSuccessFalseAndMessage()
        {
            var result = Result<string>.Failure("something went wrong");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("something went wrong");
            result.Data.Should().BeNull();
        }

        [Fact]
        public void Failure_WithMessageAndCode_SetsErrorCode()
        {
            var result = Result<int>.Failure("bad request", "BAD_REQUEST");

            result.ErrorCode.Should().Be("BAD_REQUEST");
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void Failure_WithoutCode_DefaultsToUnknownError()
        {
            var result = Result<int>.Failure("oops");

            result.ErrorCode.Should().Be("UNKNOWN_ERROR");
        }
    }

    public sealed class ResultNonGenericTests
    {
        [Fact]
        public void Success_SetsIsSuccessTrue()
        {
            var result = Result.Success();

            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Failure_SetsIsSuccessFalseAndMessage()
        {
            var result = Result.Failure("failed");

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("failed");
        }

        [Fact]
        public void Failure_WithoutCode_DefaultsToUnknownError()
        {
            var result = Result.Failure("error");

            result.ErrorCode.Should().Be("UNKNOWN_ERROR");
        }
    }

    public sealed class PaginatedResultTests
    {
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
