// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Helper for serialization/deserialization of different formats.
    /// Supports JSON, XML, and provides safe conversion with error handling.
    /// </summary>
    public static class SerializationHelper
    {
        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public static string ToJson<T>(T obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize to JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize from JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Try deserialize JSON with default value on failure
        /// </summary>
        public static T TryFromJson<T>(string json, T defaultValue = default)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Serialize object to XML string
        /// </summary>
        public static string ToXml<T>(T obj)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, obj);
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize to XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserialize XML string to object
        /// </summary>
        public static T FromXml<T>(string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StringReader(xml))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize from XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convert object to dictionary
        /// </summary>
        public static Dictionary<string, object> ToDictionary<T>(T obj) where T : class
        {
            var dict = new Dictionary<string, object>();
            if (obj == null)
                return dict;

            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                dict[prop.Name] = prop.GetValue(obj);
            }

            return dict;
        }

        /// <summary>
        /// Clone object through serialization/deserialization
        /// </summary>
        public static T DeepClone<T>(T obj)
        {
            var json = ToJson(obj);
            return FromJson<T>(json);
        }
    }
}
