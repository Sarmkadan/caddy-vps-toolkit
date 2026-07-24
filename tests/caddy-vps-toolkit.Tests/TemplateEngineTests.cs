using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Utilities;
using Xunit;

namespace CaddyVpsToolkit.Tests
{
    public class TemplateEngineTests
    {
        [Fact]
        public void Render_SimplePlaceholder_SubstitutesValue()
        {
            // Arrange
            var engine = new TemplateEngine();
            engine.Set("name", "World");
            var template = "Hello {{name}}!";

            // Act
            var result = engine.Render(template);

            // Assert
            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void Render_MissingPlaceholderValue_ThrowsTemplateVariableMissingException()
        {
            // Arrange
            var engine = new TemplateEngine();
            var template = "Hello {{name}}!";

            // Act & Assert
            Assert.Throws<TemplateVariableMissingException>(() => engine.Render(template));
        }

        [Fact]
        public void Render_RepeatedPlaceholders_SubstitutesAllOccurrences()
        {
            // Arrange
            var engine = new TemplateEngine();
            engine.Set("greet", "Hi");
            var template = "{{greet}}, {{greet}}!";

            // Act
            var result = engine.Render(template);

            // Assert
            Assert.Equal("Hi, Hi!", result);
        }

        [Fact]
        public void Render_EmptyTemplate_ReturnsEmptyString()
        {
            // Arrange
            var engine = new TemplateEngine();
            var template = string.Empty;

            // Act
            var result = engine.Render(template);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Render_StaticMethod_UsesProvidedDictionary()
        {
            // Arrange
            var variables = new Dictionary<string, object> { { "foo", "bar" } };
            var template = "Value: {{foo}}";

            // Act
            var result = TemplateEngine.Render(template, variables);

            // Assert
            Assert.Equal("Value: bar", result);
        }

        [Fact]
        public void Get_ReturnsNull_WhenKeyNotSet()
        {
            // Arrange
            var engine = new TemplateEngine();

            // Act
            var value = engine.Get("missing");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void Set_ThrowsArgumentNullException_WhenKeyIsNullOrEmpty()
        {
            // Arrange
            var engine = new TemplateEngine();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => engine.Set(null!, "value"));
            Assert.Throws<ArgumentException>(() => engine.Set(string.Empty, "value"));
        }
    }
}
