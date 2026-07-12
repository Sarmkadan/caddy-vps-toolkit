#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the TemplateEngine class.
/// </summary>
namespace CaddyVpsToolkit.Tests.Utilities
{
    public sealed class TemplateEngineTests
    {
        /// <summary>
        /// Tests that a single variable is substituted correctly.
        /// </summary>
        [Fact]
        public void Render_SingleVariable_SubstitutesCorrectly()
        {
            var engine = new TemplateEngine();
            engine.Set("name", "api-service");

            var result = engine.Render("Service: {{name}}");

            result.Should().Be("Service: api-service");
        }

        /// <summary>
        /// Tests that multiple variables are substituted correctly.
        /// </summary>
        [Fact]
        public void Render_MultipleVariables_SubstitutesAllPlaceholders()
        {
            var engine = new TemplateEngine();
            engine.Set("host", "localhost");
            engine.Set("port", 8080);

            var result = engine.Render("http://{{host}}:{{port}}/health");

            result.Should().Be("http://localhost:8080/health");
        }

        /// <summary>
        /// Tests that an unknown variable leaves the placeholder intact.
        /// </summary>
        [Fact]
        public void Render_UnknownVariable_LeavesPlaceholderIntact()
        {
            var engine = new TemplateEngine();

            var result = engine.Render("Hello {{unknown}}!");

            result.Should().Be("Hello {{unknown}}!");
        }

        /// <summary>
        /// Tests that a null template returns null.
        /// </summary>
        [Fact]
        public void Render_NullTemplate_ReturnsNull()
        {
            var engine = new TemplateEngine();

            var result = engine.Render(null!);

            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that an empty template returns an empty string.
        /// </summary>
        [Fact]
        public void Render_EmptyTemplate_ReturnsEmptyString()
        {
            var engine = new TemplateEngine();

            var result = engine.Render(string.Empty);

            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that a template with no placeholders returns unchanged.
        /// </summary>
        [Fact]
        public void Render_NoPlaceholders_ReturnsTemplateUnchanged()
        {
            var engine = new TemplateEngine();
            engine.Set("name", "test");

            var result = engine.Render("no placeholders here");

            result.Should().Be("no placeholders here");
        }

        /// <summary>
        /// Tests that a static overload substitutes variables from a dictionary.
        /// </summary>
        [Fact]
        public void Render_StaticOverload_SubstitutesFromDictionary()
        {
            var vars = new Dictionary<string, object>
            {
                ["service"] = "caddy",
                ["version"] = "2.0"
            };

            var result = TemplateEngine.Render("{{service}} v{{version}}", vars);

            result.Should().Be("caddy v2.0");
        }

        /// <summary>
        /// Tests that setting an empty key throws an ArgumentException.
        /// </summary>
        /// <param name="act">The action to test.</param>
        [Fact]
        public void Set_EmptyKey_ThrowsArgumentException()
        {
            var engine = new TemplateEngine();

            Action act = () => engine.Set(string.Empty, "value");

            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that setting a null key throws an ArgumentException.
        /// </summary>
        /// <param name="act">The action to test.</param>
        [Fact]
        public void Set_NullKey_ThrowsArgumentException()
        {
            var engine = new TemplateEngine();

            Action act = () => engine.Set(null!, "value");

            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that getting an existing key returns the value.
        /// </summary>
        [Fact]
        public void Get_ExistingKey_ReturnsValue()
        {
            var engine = new TemplateEngine();
            engine.Set("key", "value");

            engine.Get("key").Should().Be("value");
        }

        /// <summary>
        /// Tests that getting a missing key returns null.
        /// </summary>
        [Fact]
        public void Get_MissingKey_ReturnsNull()
        {
            var engine = new TemplateEngine();

            engine.Get("missing").Should().BeNull();
        }

        /// <summary>
        /// Tests that rendering a variable with a null value substitutes an empty string.
        /// </summary>
        [Fact]
        public void Render_VariableWithNullValue_SubstitutesEmptyString()
        {
            var engine = new TemplateEngine();
            engine.Set("val", null!);

            var result = engine.Render("result: {{val}}");

            result.Should().Be("result: ");
        }

        /// <summary>
        /// Tests that the constructor with a dictionary uses the provided variables.
        /// </summary>
        [Fact]
        public void Constructor_WithDictionary_UsesProvidedVariables()
        {
            var vars = new Dictionary<string, object> { ["x"] = 42 };
            var engine = new TemplateEngine(vars);

            var result = engine.Render("{{x}}");

            result.Should().Be("42");
        }
    }
}
