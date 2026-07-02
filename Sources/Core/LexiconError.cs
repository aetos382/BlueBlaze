using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueBlaze.Core;

public sealed class LexiconError
{
    public required string Error { get; set; }

    public string? Description { get; set; }

#pragma warning disable CA2227
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; set; }
#pragma warning restore
}

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(LexiconError))]
internal partial class ErrorSerializerContext : JsonSerializerContext;
