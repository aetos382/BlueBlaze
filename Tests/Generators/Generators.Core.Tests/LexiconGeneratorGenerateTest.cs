using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueBlaze.Generators.Core.Tests;

[TestClass]
public sealed class LexiconGeneratorGenerateTest
{
    private const string Namespace = "BlueBlaze.Generated";

    [TestMethod]
    public void ObjectMain_sealed_partial_classが生成される()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "com.atproto.repo.strongRef",
            "defs": {
                "main": {
                    "type": "object",
                    "description": "A URI with a content-hash fingerprint.",
                    "required": ["uri", "cid"],
                    "properties": {
                        "uri": { "type": "string", "format": "at-uri" },
                        "cid": { "type": "string", "format": "cid" }
                    }
                }
            }
        }
        """;

        var result = Generate(json);

        AssertNoErrors(result);
        Assert.AreEqual(1, result.Files.Count);

        var file = result.Files[0];
        Assert.AreEqual($"{Namespace}.Com.Atproto.Repo.StrongRef.g.cs", file.HintName);
        StringAssert.Contains(file.SourceText, "/// <summary>A URI with a content-hash fingerprint.</summary>", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public sealed partial class StrongRef", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "[global::System.Text.Json.Serialization.JsonPropertyName(\"uri\")]", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Uri { get; init; }", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "[global::System.Text.Json.Serialization.JsonPropertyName(\"cid\")]", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Cid { get; init; }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void DefsOnly_sealed_classが生成される()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "com.atproto.repo.defs",
            "defs": {
                "commitMeta": {
                    "type": "object",
                    "required": ["cid", "rev"],
                    "properties": {
                        "cid": { "type": "string", "format": "cid" },
                        "rev": { "type": "string", "format": "tid" }
                    }
                }
            }
        }
        """;

        var result = Generate(json);

        AssertNoErrors(result);
        Assert.AreEqual(1, result.Files.Count);

        var file = result.Files[0];
        Assert.AreEqual($"{Namespace}.Com.Atproto.Repo.Defs.CommitMeta.g.cs", file.HintName);
        StringAssert.Contains(file.SourceText, "public static partial class Defs", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public sealed class CommitMeta", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Cid { get; init; }", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Rev { get; init; }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Query型_ResponseクラスとSubDefがResponseにネストされる()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "com.example.doQuery",
            "defs": {
                "main": {
                    "type": "query",
                    "output": {
                        "encoding": "application/json",
                        "schema": {
                            "type": "object",
                            "required": ["items"],
                            "properties": {
                                "items": {
                                    "type": "array",
                                    "items": { "type": "ref", "ref": "#item" }
                                }
                            }
                        }
                    }
                },
                "item": {
                    "type": "object",
                    "required": ["name"],
                    "properties": {
                        "name": { "type": "string" }
                    }
                }
            }
        }
        """;

        var result = Generate(json);

        AssertNoErrors(result);
        Assert.AreEqual(2, result.Files.Count);

        var responseFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.DoQuery.Response.g.cs");
        StringAssert.Contains(responseFile.SourceText, "public sealed partial class Response", StringComparison.Ordinal);
        StringAssert.Contains(responseFile.SourceText, "Com.Example.DoQuery.Response.Item", StringComparison.Ordinal);

        var itemFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.DoQuery.Response.Item.g.cs");
        StringAssert.Contains(itemFile.SourceText, "public sealed partial class Response", StringComparison.Ordinal);
        StringAssert.Contains(itemFile.SourceText, "public sealed class Item", StringComparison.Ordinal);
        StringAssert.Contains(itemFile.SourceText, "public required string Name { get; init; }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Union型_JsonPolymorphicインターフェースが生成される()
    {
        var embedJson = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "app.test.embed",
            "defs": {
                "main": {
                    "type": "object",
                    "properties": {
                        "alt": { "type": "string" }
                    }
                }
            }
        }
        """;

        var postJson = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "app.test.post",
            "defs": {
                "main": {
                    "type": "object",
                    "properties": {
                        "embed": {
                            "type": "union",
                            "refs": ["app.test.embed"]
                        }
                    }
                }
            }
        }
        """;

        var result = Generate([postJson, embedJson]);

        AssertNoErrors(result);
        Assert.AreEqual(3, result.Files.Count); // Post, Embed, Embed:IEmbed impl

        var postFile = result.Files.Single(f => f.HintName == $"{Namespace}.App.Test.Post.g.cs");
        StringAssert.Contains(postFile.SourceText, "public IEmbed? Embed { get; init; }", StringComparison.Ordinal);
        StringAssert.Contains(postFile.SourceText, "[global::System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"$type\")]", StringComparison.Ordinal);
        StringAssert.Contains(postFile.SourceText, "[global::System.Text.Json.Serialization.JsonDerivedType(typeof(App.Test.Embed), \"app.test.embed\")]", StringComparison.Ordinal);
        StringAssert.Contains(postFile.SourceText, "public interface IEmbed { }", StringComparison.Ordinal);

        var implFile = result.Files.Single(f => f.HintName == $"{Namespace}.App.Test.Embed.App_Test_Post_IEmbed.g.cs");
        StringAssert.Contains(implFile.SourceText, "public sealed partial class Embed : App.Test.Post.IEmbed { }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Subscription型_Messageクラスに属性なしで生成される()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "com.example.subscribe",
            "defs": {
                "main": {
                    "type": "subscription",
                    "message": {
                        "schema": {
                            "type": "union",
                            "refs": ["#commit"]
                        }
                    }
                },
                "commit": {
                    "type": "object",
                    "required": ["seq"],
                    "properties": {
                        "seq": { "type": "integer" }
                    }
                }
            }
        }
        """;

        var result = Generate(json);

        AssertNoErrors(result);

        var commitFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.Subscribe.Message.Commit.g.cs");
        // Subscription sub-defs are nested in Message partial class
        StringAssert.Contains(commitFile.SourceText, "public sealed partial class Message", StringComparison.Ordinal);
        StringAssert.Contains(commitFile.SourceText, "public sealed class Commit", StringComparison.Ordinal);
        // No [JsonPropertyName] on Message or its nested classes (CBOR encoding)
        Assert.IsFalse(commitFile.SourceText.Contains("JsonPropertyName", StringComparison.Ordinal),
            "Subscription の Message ネストクラスに JsonPropertyName があってはならない");
        StringAssert.Contains(commitFile.SourceText, "public required int Seq { get; init; }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Parameters_JsonPropertyNameなし()
    {
        var json = /*lang=json*/ """
        {
            "lexicon": 1,
            "id": "com.example.search",
            "defs": {
                "main": {
                    "type": "query",
                    "parameters": {
                        "type": "params",
                        "required": ["q"],
                        "properties": {
                            "q": { "type": "string" }
                        }
                    },
                    "output": {
                        "encoding": "application/json",
                        "schema": {
                            "type": "object",
                            "properties": {}
                        }
                    }
                }
            }
        }
        """;

        var result = Generate(json);

        AssertNoErrors(result);

        var paramsFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.Search.Parameters.g.cs");
        StringAssert.Contains(paramsFile.SourceText, "public required string Q { get; init; }", StringComparison.Ordinal);
        Assert.IsFalse(paramsFile.SourceText.Contains("JsonPropertyName", StringComparison.Ordinal),
            "Parameters クラスに JsonPropertyName があってはならない");
    }

    private static GenerateResult Generate(string json)
    {
        return Generate([json]);
    }

    private static GenerateResult Generate(IEnumerable<string> jsons)
    {
        var docs = jsons
            .Select((json, i) => LexiconGenerator.Parse(json, $"test{i}.json"))
            .ToList();
        return LexiconGenerator.Generate(docs, Namespace);
    }

    private static void AssertNoErrors(GenerateResult result)
    {
        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Assert.Fail($"生成エラー:\n{string.Join("\n", errors.Select(d => d.Message))}");
        }
    }
}
