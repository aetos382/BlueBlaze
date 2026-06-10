using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueBlaze.LexiconGenerator.Core;

public sealed class LexiconDefinitionConverter :
    JsonConverter<LexiconDefinition>
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

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                break;
            }

            var isTypeProperty = reader.HasValueSequence
                ? reader.GetString() == "type"
                : reader.ValueSpan.SequenceEqual("type"u8);

            if (!isTypeProperty)
            {
                reader.Skip();
                continue;
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Invalid schema.");
            }

            break;
        }

        var type = JsonSerializer.Deserialize(ref reader, LexiconSerializerContext.Default.LexiconType);
        LexiconDefinition result = type switch
        {
            LexiconType.Record => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.RecordDefinition)!,
            LexiconType.Query => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.QueryDefinition)!,
            LexiconType.Procedure => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ProcedureDefinition)!,
            LexiconType.Subscription => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.SubscriptionDefinition)!,
            LexiconType.PermissionSet => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.PermissionSetDefinition)!,
            LexiconType.Boolean => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.BooleanDefinition)!,
            LexiconType.Integer => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.IntegerDefinition)!,
            LexiconType.String => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.StringDefinition)!,
            LexiconType.Bytes => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.BytesDefinition)!,
            LexiconType.CidLink => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.CidLinkDefinition)!,
            LexiconType.Blob => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.BlobDefinition)!,
            LexiconType.Array => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ArrayDefinition)!,
            LexiconType.Object => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ObjectDefinition)!,
            LexiconType.Parameters => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ParametersDefinition)!,
            LexiconType.Permission => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.PermissionDefinition)!,
            LexiconType.Token => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.TokenDefinition)!,
            LexiconType.Reference => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.ReferenceDefinition)!,
            LexiconType.Union => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.UnionDefinition)!,
            LexiconType.Unknown => JsonSerializer.Deserialize(ref originalReader, LexiconSerializerContext.Default.UnknownDefinition)!,
            _ => throw new JsonException($"Unsupported LexiconType: {type}")
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
