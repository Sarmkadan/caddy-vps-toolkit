using System.Text.Json;

namespace CaddyVpsToolkit.Events
{
    public static class DomainEventJsonExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string ToJson(this DomainEvent value, bool indented = false)
        {
            if (indented)
            {
                var indentedOptions = new JsonSerializerOptions(_options)
                {
                    WriteIndented = true
                };
                return JsonSerializer.Serialize(value, indentedOptions);
            }

            return JsonSerializer.Serialize(value, _options);
        }

        public static DomainEvent? FromJson(string json)
        {
            return JsonSerializer.Deserialize<DomainEvent>(json, _options);
        }

        public static bool TryFromJson(string json, out DomainEvent? value)
        {
            try
            {
                value = JsonSerializer.Deserialize<DomainEvent>(json, _options);
                return true;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }
    }
}
