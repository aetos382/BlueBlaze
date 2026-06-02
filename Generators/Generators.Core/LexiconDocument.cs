using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueBlaze.Generators.Core;

#pragma warning disable CA1819

public sealed class LexiconDocument
{
    public int Lexicon { get; set; }

    public required string Id { get; set; }

    public string? Description { get; set; }

    [JsonPropertyName("defs")]
    public required IReadOnlyDictionary<string, LexiconDefinition> Definitions { get; set; }
}

#pragma warning disable CA1720

[JsonConverter(typeof(JsonStringEnumConverter<LexiconType>))]
public enum LexiconType
{
    [JsonStringEnumMemberName("record")]
    Record,

    [JsonStringEnumMemberName("query")]
    Query,

    [JsonStringEnumMemberName("procedure")]
    Procedure,

    [JsonStringEnumMemberName("subscription")]
    Subscription,

    [JsonStringEnumMemberName("permissionSet")]
    PermissionSet,

    [JsonStringEnumMemberName("boolean")]
    Boolean,

    [JsonStringEnumMemberName("integer")]
    Integer,

    [JsonStringEnumMemberName("string")]
    String,

    [JsonStringEnumMemberName("bytes")]
    Bytes,

    [JsonStringEnumMemberName("cid-link")]
    CidLink,

    [JsonStringEnumMemberName("blob")]
    Blob,

    [JsonStringEnumMemberName("array")]
    Array,

    [JsonStringEnumMemberName("object")]
    Object,

    [JsonStringEnumMemberName("params")]
    Parameters,

    [JsonStringEnumMemberName("permission")]
    Permission,

    [JsonStringEnumMemberName("token")]
    Token,

    [JsonStringEnumMemberName("ref")]
    Reference,

    [JsonStringEnumMemberName("union")]
    Union,

    [JsonStringEnumMemberName("unknown")]
    Unknown
}

#pragma warning restore CA1720

[JsonConverter(typeof(LexiconDefinitionConverter))]
public abstract class LexiconDefinition
{
    [JsonConverter(typeof(JsonStringEnumConverter<LexiconType>))]
    public required LexiconType Type { get; set; }

    public string? Description { get; set; }
}

public sealed class RecordDefinition : LexiconDefinition
{
    public required string Key { get; set; }

    public required ObjectDefinition Record { get; set; }
}

public sealed class QueryDefinition : LexiconDefinition
{
    public ParametersDefinition? Parameters { get; set; }

    public OutputDefinition? Output { get; set; }

    public ErrorDefinition[]? Errors { get; set; }
}

public sealed class ProcedureDefinition : LexiconDefinition
{
    public ParametersDefinition? Parameters { get; set; }

    public ObjectDefinition? Input { get; set; }

    public OutputDefinition? Output { get; set; }

    public ErrorDefinition[]? Errors { get; set; }
}

public sealed class SubscriptionDefinition : LexiconDefinition
{
    public ParametersDefinition? Parameters { get; set; }

    public required MessageDefinition Message { get; set; }

    public ErrorDefinition[]? Errors { get; set; }
}

public sealed class PermissionSetDefinition : LexiconDefinition
{
    public string? Title { get; set; }

    [JsonPropertyName("title:lang")]
    public IReadOnlyDictionary<string, string>? InternationalizedTitle { get; set; }

    public string? Detail { get; set; }

    [JsonPropertyName("detail:lang")]
    public IReadOnlyDictionary<string, string>? InternationalizedDetail { get; set; }

    public required PermissionDefinition[] Permissions { get; set; }
}

public sealed class MessageDefinition
{
    public string? Description { get; set; }

    public required ObjectDefinition Schema { get; set; }
}

public sealed class OutputDefinition
{
    public string? Description { get; set; }

    public required string Encoding { get; set; }

    public LexiconDefinition? Schema { get; set; }
}

public sealed class ErrorDefinition
{
    public required string Name { get; set; }

    public string? Description { get; set; }
}

public sealed class BooleanDefinition : LexiconDefinition
{
    public bool? Default { get; set; }

    public bool? Const { get; set; }
}

public sealed class IntegerDefinition : LexiconDefinition
{
    public int? Minimum { get; set; }

    public int? Maximum { get; set; }

    public int[]? Enum { get; set; }

    public int? Default { get; set; }

    public int? Const { get; set; }
}

public sealed class StringDefinition : LexiconDefinition
{
    [JsonConverter(typeof(JsonStringEnumConverter<StringFormat>))]
    public StringFormat? Format { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public int? MinGraphemes { get; set; }

    public int? MaxGraphemes { get; set; }

    public string[]? KnownValues { get; set; }

    public string[]? Enum { get; set; }

    public string? Default { get; set; }

    public string? Const { get; set; }
}

public sealed class BytesDefinition : LexiconDefinition
{
    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }
}

public sealed class CidLinkDefinition : LexiconDefinition;

public sealed class ArrayDefinition : LexiconDefinition
{
    public required LexiconDefinition Items { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }
}

public sealed class ObjectDefinition : LexiconDefinition
{
    public required IReadOnlyDictionary<string, LexiconDefinition> Properties { get; set; }

    public string[]? Required { get; set; }

    public string[]? Nullable { get; set; }
}

public sealed class BlobDefinition : LexiconDefinition
{
    public string? Accept { get; set; }

    public int? MaxSize { get; set; }
}

public sealed class ParametersDefinition : LexiconDefinition
{
    public IReadOnlyDictionary<string, LexiconDefinition>? Properties { get; set; }

    public string[]? Required { get; set; }
}

public sealed class PermissionDefinition : LexiconDefinition
{
    public required string Resource { get; set; }
}

public sealed class TokenDefinition : LexiconDefinition;

public sealed class ReferenceDefinition : LexiconDefinition
{
    public required string Ref { get; set; }
}

public sealed class UnionDefinition : LexiconDefinition
{
    public required string[] Refs { get; set; }

    public bool? Closed { get; set; }
}

public sealed class UnknownDefinition : LexiconDefinition;

[JsonConverter(typeof(JsonStringEnumConverter<StringFormat>))]
public enum StringFormat
{
    [JsonStringEnumMemberName("at-identifier")]
    AtIdentifier,

    [JsonStringEnumMemberName("at-uri")]
    AtUri,

    [JsonStringEnumMemberName("cid")]
    Cid,

    [JsonStringEnumMemberName("datetime")]
    DateTime,

    [JsonStringEnumMemberName("did")]
    Did,

    [JsonStringEnumMemberName("handle")]
    Handle,

    [JsonStringEnumMemberName("nsid")]
    Nsid,

    [JsonStringEnumMemberName("tid")]
    Tid,

    [JsonStringEnumMemberName("record-key")]
    RecordKey,

    [JsonStringEnumMemberName("uri")]
    Uri,

    [JsonStringEnumMemberName("language")]
    Language
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(LexiconDocument))]
[JsonSerializable(typeof(RecordDefinition))]
[JsonSerializable(typeof(QueryDefinition))]
[JsonSerializable(typeof(ProcedureDefinition))]
[JsonSerializable(typeof(SubscriptionDefinition))]
[JsonSerializable(typeof(PermissionSetDefinition))]
[JsonSerializable(typeof(BooleanDefinition))]
[JsonSerializable(typeof(IntegerDefinition))]
[JsonSerializable(typeof(StringDefinition))]
[JsonSerializable(typeof(BytesDefinition))]
[JsonSerializable(typeof(CidLinkDefinition))]
[JsonSerializable(typeof(BlobDefinition))]
[JsonSerializable(typeof(ArrayDefinition))]
[JsonSerializable(typeof(ObjectDefinition))]
[JsonSerializable(typeof(ParametersDefinition))]
[JsonSerializable(typeof(PermissionDefinition))]
[JsonSerializable(typeof(TokenDefinition))]
[JsonSerializable(typeof(ReferenceDefinition))]
[JsonSerializable(typeof(UnionDefinition))]
[JsonSerializable(typeof(UnknownDefinition))]
internal sealed partial class LexiconSerializerContext : JsonSerializerContext;
