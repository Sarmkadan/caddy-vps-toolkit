using Xunit;
using CaddyVpsToolkit.Cli;

namespace CaddyVpsToolkit.Tests.Cli
{
    public class ArgumentValidatorTests
    {
        [Fact]
        public void Validate_RequiredArguments_Present_ReturnsValidResult()
        {
            // Arrange
            var parser = new ArgumentParser();
            parser.AddPositionals("arg1", "arg2");
            var descriptor = new CommandDescriptor();
            descriptor.RequiredArguments.Add("arg1");
            descriptor.RequiredArguments.Add("arg2");

            // Act
            var result = new ArgumentValidator().Validate(parser, descriptor);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_RequiredArguments_Missing_ReturnsInvalidResult()
        {
            // Arrange
            var parser = new ArgumentParser();
            parser.AddPositionals("arg1");
            var descriptor = new CommandDescriptor();
            descriptor.RequiredArguments.Add("arg1");
            descriptor.RequiredArguments.Add("arg2");

            // Act
            var result = new ArgumentValidator().Validate(parser, descriptor);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Missing required argument: arg2", result.Errors[0]);
        }

        [Fact]
        public void Validate_RequiredArguments_Count_Mismatch_ReturnsInvalidResult()
        {
            // Arrange
            var parser = new ArgumentParser();
            parser.AddPositionals("arg1");
            var descriptor = new CommandDescriptor();
            descriptor.RequiredArguments.Add("arg1");
            descriptor.RequiredArguments.Add("arg2");
            descriptor.RequiredArguments.Add("arg3");

            // Act
            var result = new ArgumentValidator().Validate(parser, descriptor);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
            Assert.Equal("Missing required argument: arg2", result.Errors[0]);
            Assert.Equal("Missing required argument: arg3", result.Errors[1]);
        }

        [Fact]
        public void Validate_OptionalFlags_Present_ReturnsValidResult()
        {
            // Arrange
            var parser = new ArgumentParser();
            parser.AddFlags("--flag1", "--flag2");
            var descriptor = new CommandDescriptor();
            descriptor.OptionalFlags.Add("--flag1");
            descriptor.OptionalFlags.Add("--flag2");

            // Act
            var result = new ArgumentValidator().Validate(parser, descriptor);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_OptionalFlags_Unknown_ReturnsInvalidResult()
        {
            // Arrange
            var parser = new ArgumentParser();
            parser.AddFlags("--flag1", "--flag2", "--unknown-flag");
            var descriptor = new CommandDescriptor();
            descriptor.OptionalFlags.Add("--flag1");
            descriptor.OptionalFlags.Add("--flag2");

            // Act
            var result = new ArgumentValidator().Validate(parser, descriptor);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Unknown flag: --unknown-flag", result.Errors[0]);
        }
    }
}
