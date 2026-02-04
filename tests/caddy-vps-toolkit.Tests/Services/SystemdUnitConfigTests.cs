#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="SystemdUnitConfig"/> unit file generation and validation.
    /// </summary>
    public sealed class SystemdUnitConfigTests
    {
        private static SystemdUnitConfig MinimalValidConfig() => new()
        {
            ServiceId = "svc-1",
            UnitName  = "my-app.service",
            ExecStart = "/usr/bin/my-app --serve",
            User      = "www-data",
            Group     = "www-data",
        };

        // ─── Validate ─────────────────────────────────────────────────────────

        [Fact]
        public void Validate_MinimalValidConfig_DoesNotThrow()
        {
            var config = MinimalValidConfig();
            config.Invoking(c => c.Validate()).Should().NotThrow();
        }

        [Fact]
        public void Validate_MissingUnitName_ThrowsValidationException()
        {
            var config = MinimalValidConfig();
            config.UnitName = "";

            config.Invoking(c => c.Validate())
                  .Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>()
                  .WithMessage("*Unit name*");
        }

        [Fact]
        public void Validate_MissingExecStart_ThrowsValidationException()
        {
            var config = MinimalValidConfig();
            config.ExecStart = "  ";

            config.Invoking(c => c.Validate())
                  .Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>()
                  .WithMessage("*ExecStart*");
        }

        [Fact]
        public void Validate_NegativeRestartDelay_ThrowsValidationException()
        {
            var config = MinimalValidConfig();
            config.RestartDelaySeconds = -1;

            config.Invoking(c => c.Validate())
                  .Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>()
                  .WithMessage("*Restart delay*");
        }

        // ─── GenerateSystemdContent — [Unit] section ───────────────────────────

        [Fact]
        public void GenerateSystemdContent_ContainsUnitSection()
        {
            var content = MinimalValidConfig().GenerateSystemdContent();

            content.Should().Contain("[Unit]");
            content.Should().Contain("Description=Service unit for my-app.service");
            content.Should().Contain("After=network.target");
        }

        [Fact]
        public void GenerateSystemdContent_WithWants_EmitsWantsDirective()
        {
            var config = MinimalValidConfig();
            config.Wants = new List<string> { "postgresql.service" };

            var content = config.GenerateSystemdContent();

            content.Should().Contain("Wants=postgresql.service");
        }

        [Fact]
        public void GenerateSystemdContent_WithoutWants_DoesNotEmitWantsDirective()
        {
            var content = MinimalValidConfig().GenerateSystemdContent();

            content.Should().NotContain("Wants=");
        }

        // ─── GenerateSystemdContent — [Service] section ────────────────────────

        [Fact]
        public void GenerateSystemdContent_ContainsServiceSection()
        {
            var content = MinimalValidConfig().GenerateSystemdContent();

            content.Should().Contain("[Service]");
            content.Should().Contain("ExecStart=/usr/bin/my-app --serve");
            content.Should().Contain("User=www-data");
            content.Should().Contain("Group=www-data");
        }

        [Fact]
        public void GenerateSystemdContent_RestartPolicyIsLowercase()
        {
            var config = MinimalValidConfig();
            config.RestartPolicy = RestartPolicy.OnFailure;

            var content = config.GenerateSystemdContent();

            content.Should().Contain("Restart=onfailure");
        }

        [Fact]
        public void GenerateSystemdContent_WithWorkingDirectory_EmitsDirective()
        {
            var config = MinimalValidConfig();
            config.WorkingDirectory = "/opt/my-app";

            var content = config.GenerateSystemdContent();

            content.Should().Contain("WorkingDirectory=/opt/my-app");
        }

        [Fact]
        public void GenerateSystemdContent_WithoutWorkingDirectory_OmitsDirective()
        {
            var config = MinimalValidConfig();
            config.WorkingDirectory = null;

            var content = config.GenerateSystemdContent();

            content.Should().NotContain("WorkingDirectory=");
        }

        [Fact]
        public void GenerateSystemdContent_WithExecReloadAndStop_EmitsDirectives()
        {
            var config = MinimalValidConfig();
            config.ExecReload = "/bin/kill -HUP $MAINPID";
            config.ExecStop   = "/bin/kill -TERM $MAINPID";

            var content = config.GenerateSystemdContent();

            content.Should().Contain("ExecReload=/bin/kill -HUP $MAINPID");
            content.Should().Contain("ExecStop=/bin/kill -TERM $MAINPID");
        }

        [Fact]
        public void GenerateSystemdContent_WithEnvironmentVariables_EmitsEnvironmentDirectives()
        {
            var config = MinimalValidConfig();
            config.Environment = new Dictionary<string, string>
            {
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "PORT",                   "5000" },
            };

            var content = config.GenerateSystemdContent();

            content.Should().Contain("Environment=\"ASPNETCORE_ENVIRONMENT=Production\"");
            content.Should().Contain("Environment=\"PORT=5000\"");
        }

        [Fact]
        public void GenerateSystemdContent_WithEnvironmentFile_EmitsEnvironmentFileDirective()
        {
            var config = MinimalValidConfig();
            config.EnvironmentFiles = new List<string> { "/etc/my-app/env" };

            var content = config.GenerateSystemdContent();

            content.Should().Contain("EnvironmentFile=/etc/my-app/env");
        }

        [Fact]
        public void GenerateSystemdContent_WithLimitNoFile_EmitsLimitNOFILE()
        {
            var config = MinimalValidConfig();
            config.LimitNoFile      = true;
            config.LimitNoFileValue = 65535;

            var content = config.GenerateSystemdContent();

            content.Should().Contain("LimitNOFILE=65535");
        }

        [Fact]
        public void GenerateSystemdContent_WithoutLimitNoFile_OmitsLimitNOFILE()
        {
            var config = MinimalValidConfig();
            config.LimitNoFile = false;

            var content = config.GenerateSystemdContent();

            content.Should().NotContain("LimitNOFILE");
        }

        // ─── GenerateSystemdContent — [Install] section ────────────────────────

        [Fact]
        public void GenerateSystemdContent_ContainsInstallSection()
        {
            var content = MinimalValidConfig().GenerateSystemdContent();

            content.Should().Contain("[Install]");
            content.Should().Contain("WantedBy=multi-user.target");
        }

        // ─── GenerateSystemdContent — braces / structure ──────────────────────

        [Fact]
        public void GenerateSystemdContent_ContainsAllThreeSectionsInOrder()
        {
            var content = MinimalValidConfig().GenerateSystemdContent();

            var unitIdx    = content.IndexOf("[Unit]",    StringComparison.Ordinal);
            var serviceIdx = content.IndexOf("[Service]", StringComparison.Ordinal);
            var installIdx = content.IndexOf("[Install]", StringComparison.Ordinal);

            unitIdx.Should().BeLessThan(serviceIdx);
            serviceIdx.Should().BeLessThan(installIdx);
        }
    }
}
