using System.Text.Json;
using System.Text.Json.Serialization;

namespace AveManiaBot;

public class TextFieldConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // Handle 'text' as a plain string
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            JsonDocument.ParseValue(ref reader);
            return string.Empty;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            JsonDocument.ParseValue(ref reader);
            return string.Empty;
        }

        return string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value is string stringValue)
        {
            writer.WriteStringValue(stringValue);
        }
    }
}