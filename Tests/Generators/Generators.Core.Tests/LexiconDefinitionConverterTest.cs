using System;
using System.Text.Json;

namespace BlueBlaze.Generators.Core.Tests;

[TestClass]
public sealed class LexiconDefinitionConverterTest
{
    [TestMethod]
    public void ざっくり全体をデシリアライズ()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "app.blueblaze.test",
            "defs": {
                "main": {
                    "type": "object",
                    "properties": {
                        "foo": { "type": "string" }
                    }
                }
            }
        }
        """;

        var deserialized = JsonSerializer.Deserialize(json, LexiconSerializerContext.Default.LexiconDocument);

        Assert.IsNotNull(deserialized);
    }

    [TestMethod]
    public void type属性が先頭にない要素のデシリアライズ()
    {
        ReadOnlySpan<byte> json = /*lang=json,strict*/ """
                                                       {
                                                           "maxLength": 1,
                                                           "type": "string"
                                                       }
                                                       """u8;

        var reader = new Utf8JsonReader(json);
        reader.Read();

        var converter = new LexiconDefinitionConverter();

        var definition = converter.Read(ref reader, typeof(LexiconDefinition), new JsonSerializerOptions());

        Assert.IsInstanceOfType<StringDefinition>(definition);
    }

    [TestMethod]
    public void StringDefinitionのデシリアライズ()
    {
        ReadOnlySpan<byte> json = /*lang=json,strict*/ """
        {
            "type": "string"
        }
        """u8;

        var reader = new Utf8JsonReader(json);
        reader.Read();

        var converter = new LexiconDefinitionConverter();

        var definition = converter.Read(ref reader, typeof(LexiconDefinition), new JsonSerializerOptions());

        Assert.IsInstanceOfType<StringDefinition>(definition);
    }
}
