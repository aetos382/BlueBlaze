using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueBlaze.Generators.Core;

public sealed class LexiconDefinitionConverter : JsonConverter<LexiconDefinition>
{
    public override LexiconDefinition? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (typeToConvert != typeof(LexiconDefinition))
        {
            throw new ArgumentException($"Unexpected type to convert: {typeToConvert.FullName}", nameof(typeToConvert));
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected start of object, but got {reader.TokenType}.");
        }

        var originalReader = reader;

        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException("Unexpected end of JSON.");
        }

        var propertyName = reader.GetString();
        if (propertyName != "type")
        {
            throw new JsonException($"Expected property 'type', but got '{propertyName}'.");
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Unexpected end of JSON.");
        }

        var type = JsonSerializer.Deserialize(ref reader, LexiconSerializerContext.Default.LexiconType);
        LexiconDefinition result = type switch
        {
            LexiconType.String => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.StringDefinition)!,
            LexiconType.Object => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ObjectDefinition)!,
            _ => throw new NotSupportedException($"Unsupported LexiconType: {type}")
        };

        reader = originalReader;

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        LexiconDefinition value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
