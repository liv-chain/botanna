using System.Text.Json;
using System.Text.Json.Serialization;

namespace AveManiaBot.Helper;

/// <summary>
/// Text field converter for telegram text field
/// </summary>
public class TextFieldConverter : JsonConverter<string>
{
    /// <summary>
    /// Reads and converts JSON data to a string using the specified Utf8JsonReader.
    /// </summary>
    /// <param name="reader">The Utf8JsonReader to read JSON data from.</param>
    /// <param name="typeToConvert">The type of the object to convert. This parameter is ignored as this method specifically handles strings.</param>
    /// <param name="options">Options to configure the JSON serialization or deserialization process.</param>
    /// <returns>A string parsed from the JSON data, or an empty string if the JSON data is an object or array.</returns>
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