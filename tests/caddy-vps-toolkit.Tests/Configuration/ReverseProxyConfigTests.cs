#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CaddyVpsToolkit.Configuration;
using CaddyVpsToolkit.Core.Exceptions;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Configuration
{
    /// <summary>
    /// Tests for <see cref="ReverseProxyConfig"/> startup validation.
    /// </summary>
    [Trait("Category", "Unit")]
    public sealed class ReverseProxyConfigTests
    {
        private static ReverseProxyConfig CreateValidConfig()
        {
            var config = new ReverseProxyConfig { Domain = "app.example.com" };
            config.AddUpstream("localhost:8080");
            return config;
        }

        /// <summary>
        /// Verifies that a fully populated, valid configuration passes validation without errors.
        /// </summary>
        [Fact]
        public void Validate_WithValidConfig_ShouldNotThrow()
        {
            // Arrange
            var config = CreateValidConfig();
            config.AddUpstream("http://127.0.0.1:3000");
            config.AddUpstream("https://api.internal:8443/");
            config.AddUpstream("[::1]:9000");
            config.LoadBalancingPolicy = "least_conn";
            config.DialTimeoutSeconds = 5;
            config.ResponseHeaderTimeoutSeconds = 60;

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().NotThrow();
            config.GetValidationErrors().Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that a configuration with no upstreams is rejected.
        /// </summary>
        [Fact]
        public void Validate_WithNoUpstreams_ShouldReportAtLeastOneUpstreamRequired()
        {
            // Arrange
            var config = new ReverseProxyConfig { Domain = "app.example.com" };

            // Act
            var act = () => config.Validate();

            // Assert
            act.Should().Throw<ReverseProxyConfigValidationException>()
                .Which.Errors.Should().ContainSingle()
                .Which.Should().Contain("At least one upstream is required");
        }

        /// <summary>
        /// Verifies that an empty domain is rejected.
        /// </summary>
        [Fact]
        public void Validate_WithEmptyDomain_ShouldReportDomainRequired()
        {
            // Arrange
            var config = CreateValidConfig();
            config.Domain = "   ";

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle().Which.Should().Be("Domain must be set.");
        }

        /// <summary>
        /// Verifies that malformed upstreams are rejected, covering host:port and URL forms.
        /// </summary>
        [Theory]
        [InlineData("localhost")]
        [InlineData("localhost:")]
        [InlineData(":8080")]
        [InlineData("localhost:0")]
        [InlineData("localhost:65536")]
        [InlineData("localhost:99999")]
        [InlineData("localhost:abc")]
        [InlineData("user@localhost:8080")]
        [InlineData("bad host:8080")]
        [InlineData("http://localhost")]
        [InlineData("http://localhost/")]
        [InlineData("http://localhost:0")]
        [InlineData("http://localhost:70000")]
        [InlineData("http://localhost:8080/api")]
        [InlineData("http://localhost:8080?x=1")]
        [InlineData("ftp://localhost:21")]
        [InlineData("tcp://localhost:8080")]
        public void Validate_WithInvalidUpstream_ShouldReportUpstreamFormat(string upstream)
        {
            // Arrange
            var config = new ReverseProxyConfig { Domain = "app.example.com" };
            config.AddUpstream(upstream);

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("must be host:port or http(s)://host:port");
        }

        /// <summary>
        /// Verifies that the boundary ports 1 and 65535 are accepted.
        /// </summary>
        [Theory]
        [InlineData("localhost:1")]
        [InlineData("localhost:65535")]
        [InlineData("http://localhost:65535")]
        public void Validate_WithBoundaryPort_ShouldAccept(string upstream)
        {
            // Arrange
            var config = new ReverseProxyConfig { Domain = "app.example.com" };
            config.AddUpstream(upstream);

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that the same upstream listed twice (ignoring case) is reported once.
        /// </summary>
        [Fact]
        public void Validate_WithDuplicateUpstreams_ShouldReportEachDuplicateOnce()
        {
            // Arrange
            var config = new ReverseProxyConfig { Domain = "app.example.com" };
            config.AddUpstream("LocalHost:8080");
            config.AddUpstream("localhost:8080");
            config.AddUpstream("localhost:8080");

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("is defined more than once");
        }

        /// <summary>
        /// Verifies that every Caddy load-balancing policy listed in the spec is accepted.
        /// </summary>
        [Theory]
        [InlineData("random")]
        [InlineData("round_robin")]
        [InlineData("least_conn")]
        [InlineData("first")]
        [InlineData("ip_hash")]
        [InlineData("uri_hash")]
        [InlineData("header")]
        [InlineData("cookie")]
        public void Validate_WithSupportedLoadBalancingPolicy_ShouldAccept(string policy)
        {
            // Arrange
            var config = CreateValidConfig();
            config.LoadBalancingPolicy = policy;

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that an unsupported or malformed load-balancing policy is rejected.
        /// </summary>
        [Theory]
        [InlineData("weighted_round_robin")]
        [InlineData("RANDOM")]
        [InlineData("least-conn")]
        [InlineData("")]
        public void Validate_WithUnsupportedLoadBalancingPolicy_ShouldReportPolicy(string policy)
        {
            // Arrange
            var config = CreateValidConfig();
            config.LoadBalancingPolicy = policy;

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain($"Load-balancing policy '{policy}' is not supported");
        }

        /// <summary>
        /// Verifies that a null policy (unset) is accepted and leaves Caddy's default in place.
        /// </summary>
        [Fact]
        public void Validate_WithNullLoadBalancingPolicy_ShouldAccept()
        {
            // Arrange
            var config = CreateValidConfig();
            config.LoadBalancingPolicy = null;

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that zero or negative dial and response-header timeouts are rejected.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithNonPositiveDialTimeout_ShouldReportDialTimeout(int seconds)
        {
            // Arrange
            var config = CreateValidConfig();
            config.DialTimeoutSeconds = seconds;

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("Dial timeout must be a positive number of seconds");
        }

        /// <summary>
        /// Verifies that zero or negative response-header timeouts are rejected.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-30)]
        public void Validate_WithNonPositiveResponseHeaderTimeout_ShouldReportResponseHeaderTimeout(int seconds)
        {
            // Arrange
            var config = CreateValidConfig();
            config.ResponseHeaderTimeoutSeconds = seconds;

            // Act
            var errors = config.GetValidationErrors();

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("Response header timeout must be a positive number of seconds");
        }

        /// <summary>
        /// Verifies that all violations are aggregated into one exception rather than stopping at the first.
        /// </summary>
        [Fact]
        public void Validate_WithMultipleProblems_ShouldThrowSingleExceptionWithAllMessages()
        {
            // Arrange
            var config = new ReverseProxyConfig { Domain = "", LoadBalancingPolicy = "bogus", DialTimeoutSeconds = 0, ResponseHeaderTimeoutSeconds = -5 };
            config.AddUpstream("not-a-target");
            config.AddUpstream("also:bad");

            // Act
            var act = () => config.Validate();

            // Assert
            var exception = act.Should().Throw<ReverseProxyConfigValidationException>().Which;
            exception.Errors.Should().HaveCount(6);
            exception.Errors.Should().Contain(e => e.Contains("Domain must be set"));
            exception.Errors.Should().Contain(e => e.Contains("'not-a-target' must be host:port"));
            exception.Errors.Should().Contain(e => e.Contains("'also:bad' must be host:port"));
            exception.Errors.Should().Contain(e => e.Contains("Load-balancing policy 'bogus'"));
            exception.Errors.Should().Contain(e => e.Contains("Dial timeout"));
            exception.Errors.Should().Contain(e => e.Contains("Response header timeout"));
            exception.Message.Should().Contain("(6 problem(s))")
                .And.Contain("Domain must be set.")
                .And.Contain("Dial timeout must be a positive number of seconds, but was 0.");
        }

        /// <summary>
        /// Verifies that rendering refuses to produce a Caddyfile snippet for an invalid configuration.
        /// </summary>
        [Fact]
        public void ToCaddyfileSnippet_WithInvalidConfig_ShouldThrowBeforeRendering()
        {
            // Arrange
            var config = CreateValidConfig();
            config.DialTimeoutSeconds = 0;

            // Act
            var act = () => config.ToCaddyfileSnippet();

            // Assert
            act.Should().Throw<ReverseProxyConfigValidationException>();
        }

        /// <summary>
        /// Verifies that a valid configuration renders the policy and timeouts into the snippet.
        /// </summary>
        [Fact]
        public void ToCaddyfileSnippet_WithValidConfig_ShouldRenderPolicyAndTimeouts()
        {
            // Arrange
            var config = CreateValidConfig();
            config.LoadBalancingPolicy = "round_robin";
            config.DialTimeoutSeconds = 3;
            config.ResponseHeaderTimeoutSeconds = 45;

            // Act
            var snippet = config.ToCaddyfileSnippet();

            // Assert
            snippet.Should().Contain("lb_policy round_robin")
                .And.Contain("dial_timeout 3s")
                .And.Contain("response_header_timeout 45s");
        }
    }
}
