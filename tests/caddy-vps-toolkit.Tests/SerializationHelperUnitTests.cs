#nullable enable
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CaddyVpsToolkit.Utilities;
using Xunit;

namespace CaddyVpsToolkit.Tests.Utilities
{
    /// <summary>
    /// Unit tests for <see cref="SerializationHelper"/>.
    /// Covers JSON, XML, dictionary conversion and deep‑clone functionality,
    /// including happy‑path, edge‑cases and error‑paths.
    /// </summary>
    public sealed class SerializationHelperUnitTests
    {
        private sealed class SampleDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public void ToJson_ShouldSerializeObject_WithIndentation()
        {
            var dto = new SampleDto { Id = 42, Name = "Answer" };
            var json = SerializationHelper.ToJson(dto);

            Assert.Contains("\"Id\":42", json);
            Assert.Contains("\"Name\":\"Answer\"", json);
            // Indentation check – at least one newline should be present
            Assert.Contains(Environment.NewLine, json);
        }

        [Fact]
        public void FromJson_ShouldDeserializeValidJson()
        {
            const string json = "{\"Id\":1,\"Name\":\"Test\"}";
            var dto = SerializationHelper.FromJson<SampleDto>(json);

            Assert.NotNull(dto);
            Assert.Equal(1, dto.Id);
            Assert.Equal("Test", dto.Name);
        }

        [Fact]
        public void FromJson_InvalidJson_ShouldThrowInvalidOperationException()
        {
            const string invalidJson = "{ this is not json }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SerializationHelper.FromJson<SampleDto>(invalidJson));

            Assert.Contains("Failed to deserialize from JSON", ex.Message);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ShouldReturnDefault()
        {
            const string invalidJson = "not a json";

            var result = SerializationHelper.TryFromJson<SampleDto>(invalidJson, defaultValue: null);

            Assert.Null(result);
        }

        [Fact]
        public void ToXml_ShouldSerializeObject()
        {
            var dto = new SampleDto { Id = 7, Name = "XmlTest" };
            var xml = SerializationHelper.ToXml(dto);

            // Basic sanity checks – XML should contain element names and values
            Assert.Contains("<Id>7</Id>", xml);
            Assert.Contains("<Name>XmlTest</Name>", xml);
            Assert.StartsWith("<?xml", xml.TrimStart());
        }

        [Fact]
        public void FromXml_ShouldDeserializeValidXml()
        {
            const string xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<SampleDto xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Id>99</Id>
  <Name>FromXml</Name>
</SampleDto>";
            var dto = SerializationHelper.FromXml<SampleDto>(xml);

            Assert.NotNull(dto);
            Assert.Equal(99, dto.Id);
            Assert.Equal("FromXml", dto.Name);
        }

        [Fact]
        public void FromXml_InvalidXml_ShouldThrowInvalidOperationException()
        {
            const string invalidXml = "<not><valid></xml>";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SerializationHelper.FromXml<SampleDto>(invalidXml));

            Assert.Contains("Failed to deserialize from XML", ex.Message);
        }

        [Fact]
        public void ToDictionary_NullObject_ShouldReturnEmptyDictionary()
        {
            SampleDto? dto = null;
            var dict = SerializationHelper.ToDictionary(dto!);

            Assert.NotNull(dict);
            Assert.Empty(dict);
        }

        [Fact]
        public void ToDictionary_ShouldContainAllPublicProperties()
        {
            var dto = new SampleDto { Id = 123, Name = "DictTest" };
            var dict = SerializationHelper.ToDictionary(dto);

            Assert.Equal(2, dict.Count);
            Assert.Equal(123, dict["Id"]);
            Assert.Equal("DictTest", dict["Name"]);
        }

        [Fact]
        public void DeepClone_ShouldCreateEqualButDistinctInstance()
        {
            var original = new SampleDto { Id = 5, Name = "CloneMe" };
            var clone = SerializationHelper.DeepClone(original);

            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
        }

        [Fact]
        public void DeepClone_NullObject_ShouldReturnNull()
        {
            SampleDto? original = null;
            var clone = SerializationHelper.DeepClone(original);

            Assert.Null(clone);
        }

        [Fact]
        public void ToJson_NullObject_ShouldReturnLiteralNull()
        {
            SampleDto? dto = null;
            var json = SerializationHelper.ToJson(dto);

            Assert.Equal("null", json.Trim());
        }

        [Fact]
        public void ToXml_NullObject_ShouldThrowInvalidOperationException()
        {
            SampleDto? dto = null;
            var ex = Assert.Throws<InvalidOperationException>(() => SerializationHelper.ToXml(dto!));
            Assert.Contains("Failed to serialize to XML", ex.Message);
        }
    }
}
