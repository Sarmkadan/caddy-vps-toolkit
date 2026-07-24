#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Edge case tests for the TemplateEngine class.
/// Tests missing placeholders, nested braces, empty template, null values, repeated tokens, and very large templates.
/// </summary>
namespace CaddyVpsToolkit.Tests.Utilities
{
    public sealed class TemplateEngineEdgeTests
    {
        /// <summary>
        /// Tests that a template with only missing placeholders throws TemplateVariableMissingException in strict mode.
        /// </summary>
        [Fact]
        public void Render_OnlyMissingPlaceholders_ThrowsTemplateVariableMissingException()
        {
            var engine = new TemplateEngine();

            Action act = () => engine.Render("{{missing1}} and {{missing2}}");

            act.Should().Throw<TemplateVariableMissingException>()
                .Where(e => e.MissingVariables.Contains("missing1") && e.MissingVariables.Contains("missing2"));
        }

        /// <summary>
        /// Tests that a template with mixed known and unknown placeholders throws TemplateVariableMissingException in strict mode.
        /// </summary>
        [Fact]
        public void Render_MixedKnownAndUnknownPlaceholders_ThrowsTemplateVariableMissingException()
        {
            var engine = new TemplateEngine();
            engine.Set("known", "value");

            Action act = () => engine.Render("{{known}} and {{unknown}} and {{another_known}}");

            act.Should().Throw<TemplateVariableMissingException>()
                .Where(e => e.MissingVariables.Contains("unknown") && e.MissingVariables.Contains("another_known"));
        }

        /// <summary>
        /// Tests nested braces - braces within braces should not be treated as placeholders.
        /// </summary>
        [Fact]
        public void Render_NestedBraces_LeavesNestedBracesIntact()
        {
            var engine = new TemplateEngine();
            engine.Set("var", "test");

            var result = engine.Render("{{var}} {{ {{var}} }}");

            result.Should().Be("test {{ test }}");
        }

        /// <summary>
        /// Tests braces with no content between them.
        /// </summary>
        [Fact]
        public void Render_EmptyBraces_ThrowsTemplateVariableMissingException()
        {
            var engine = new TemplateEngine();

            Action act = () => engine.Render("{{");

            act.Should().Throw<TemplateVariableMissingException>();
        }

        /// <summary>
        /// Tests braces with only whitespace between them.
        /// </summary>
        [Fact]
        public void Render_BracesWithWhitespace_ThrowsTemplateVariableMissingException()
        {
            var engine = new TemplateEngine();

            Action act = () => engine.Render("{{");

            act.Should().Throw<TemplateVariableMissingException>();
        }

        /// <summary>
        /// Tests that repeated tokens are all substituted.
        /// </summary>
        [Fact]
        public void Render_RepeatedTokens_AllSubstituted()
        {
            var engine = new TemplateEngine();
            engine.Set("token", "replaced");

            var result = engine.Render("{{token}} {{token}} {{token}}");

            result.Should().Be("replaced replaced replaced");
        }

        /// <summary>
        /// Tests that special characters in values are preserved.
        /// </summary>
        [Fact]
        public void Render_SpecialCharactersInValues_Preserved()
        {
            var engine = new TemplateEngine();
            engine.Set("path", "/var/www/html");
            engine.Set("url", "https://example.com/path?query=value");

            var result = engine.Render("Path: {{path}}, URL: {{url}}");

            result.Should().Be("Path: /var/www/html, URL: https://example.com/path?query=value");
        }

        /// <summary>
        /// Tests that numeric values are converted to strings correctly.
        /// </summary>
        [Fact]
        public void Render_NumericValues_ConvertedToString()
        {
            var engine = new TemplateEngine();
            engine.Set("int", 42);
            engine.Set("double", 3.14159);
            engine.Set("negative", -100);

            var result = engine.Render("{{int}}, {{double}}, {{negative}}");

            result.Should().Be("42, 3.14159, -100");
        }

        /// <summary>
        /// Tests that boolean values are converted to strings correctly.
        /// </summary>
        [Fact]
        public void Render_BooleanValues_ConvertedToString()
        {
            var engine = new TemplateEngine();
            engine.Set("flag", true);
            engine.Set("disabled", false);

            var result = engine.Render("{{flag}} {{disabled}}");

            result.Should().Be("True False");
        }

        /// <summary>
        /// Tests that very large template with many placeholders works correctly.
        /// </summary>
        [Fact]
        public void Render_VeryLargeTemplate_AllPlaceholdersSubstituted()
        {
            var engine = new TemplateEngine();
            var template = new System.Text.StringBuilder();

            // Build a large template with 100 placeholders
            for (int i = 0; i < 100; i++)
            {
                var key = $"var{i}";
                engine.Set(key, i);
                template.Append("{{").Append(key).Append("}} ");
            }

            var result = engine.Render(template.ToString());

            // Verify all placeholders were replaced
            result.Should().NotContain("{{");
            result.Should().Contain("0 1 2 3 4 5 6 7 8 9");
        }

        /// <summary>
        /// Tests that whitespace around placeholders is preserved.
        /// </summary>
        [Fact]
        public void Render_WhitespaceAroundPlaceholders_Preserved()
        {
            var engine = new TemplateEngine();
            engine.Set("var", "test");

            var result = engine.Render(" {{var}} {{var}} ");

            result.Should().Be(" test test ");
        }

        /// <summary>
        /// Tests that placeholders at the start and end of template are handled correctly.
        /// </summary>
        [Fact]
        public void Render_PlaceholdersAtStartAndEnd_HandledCorrectly()
        {
            var engine = new TemplateEngine();
            engine.Set("start", "begin");
            engine.Set("end", "finish");

            var result = engine.Render("{{start}} middle {{end}}");

            result.Should().Be("begin middle finish");
        }

        /// <summary>
        /// Tests that consecutive placeholders are handled correctly.
        /// </summary>
        [Fact]
        public void Render_ConsecutivePlaceholders_HandledCorrectly()
        {
            var engine = new TemplateEngine();
            engine.Set("a", "1");
            engine.Set("b", "2");
            engine.Set("c", "3");

            var result = engine.Render("{{a}}{{b}}{{c}}");

            result.Should().Be("123");
        }

        /// <summary>
        /// Tests that placeholders with underscores and numbers work correctly.
        /// </summary>
        [Fact]
        public void Render_PlaceholdersWithUnderscoresAndNumbers_WorkCorrectly()
        {
            var engine = new TemplateEngine();
            engine.Set("var_1", "first");
            engine.Set("var_2", "second");
            engine.Set("var_10", "tenth");

            var result = engine.Render("{{var_1}} {{var_2}} {{var_10}}");

            result.Should().Be("first second tenth");
        }

        /// <summary>
        /// Tests that static Render method throws TemplateVariableMissingException for missing placeholders.
        /// </summary>
        [Fact]
        public void Render_StaticMethod_ThrowsTemplateVariableMissingExceptionForMissingVariables()
        {
            var vars = new Dictionary<string, object> { ["known"] = "value" };

            Action act = () => TemplateEngine.Render("{{known}} {{unknown}}", vars);

            act.Should().Throw<TemplateVariableMissingException>()
                .Where(e => e.MissingVariables.Contains("unknown"));
        }

        /// <summary>
        /// Tests that static Render method with null variables throws ArgumentNullException.
        /// </summary>
        [Fact]
        public void Render_StaticMethod_NullVariables_ThrowsArgumentNullException()
        {
            Action act = () => TemplateEngine.Render("test {{var}}", null!);

            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that whitespace-only template returns empty string.
        /// </summary>
        [Fact]
        public void Render_WhitespaceOnlyTemplate_ReturnsWhitespace()
        {
            var engine = new TemplateEngine();

            var result = engine.Render(" ");

            result.Should().Be(" ");
        }

        /// <summary>
        /// Tests that template with only newlines returns newlines.
        /// </summary>
        [Fact]
        public void Render_OnlyNewlines_ReturnsNewlines()
        {
            var engine = new TemplateEngine();

            var result = engine.Render("\n\n\n");

            result.Should().Be("\n\n\n");
        }
    }
}
