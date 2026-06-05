using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuditService.API.Converters
{
    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (DateOnly.TryParse(dateString, out var date))
                {
                    return date;
                }
            }
            throw new JsonException($"Unable to convert \"{reader.GetString()}\" to DateOnly.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }

    public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString))
                {
                    return null;
                }
                if (DateOnly.TryParse(dateString, out var date))
                {
                    return date;
                }
            }
            throw new JsonException($"Unable to convert \"{reader.GetString()}\" to DateOnly?.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(DateFormat));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
