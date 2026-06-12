using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueBlaze.LexiconGenerator.Core.Tests;

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
        StringAssert.Contains(file.SourceText, "public sealed partial class Defs", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public sealed partial class CommitMeta", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Cid { get; init; }", StringComparison.Ordinal);
        StringAssert.Contains(file.SourceText, "public required string Rev { get; init; }", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Query型_OutputクラスとSubDefが兄弟クラスとして生成される()
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

        var outputFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.DoQuery.Output.g.cs");
        StringAssert.Contains(outputFile.SourceText, "public sealed partial class Output", StringComparison.Ordinal);
        // OutputItem は Output にネストされず兄弟クラスとして global:: 修飾で参照される
        StringAssert.Contains(outputFile.SourceText, $"global::{Namespace}.Com.Example.DoQuery.OutputItem", StringComparison.Ordinal);

        // OutputItem は Output にネストされず DoQuery の直下に配置される (ファイル名がフラット構造を示す)
        var itemFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.DoQuery.OutputItem.g.cs");
        StringAssert.Contains(itemFile.SourceText, "public sealed partial class OutputItem", StringComparison.Ordinal);
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
        StringAssert.Contains(postFile.SourceText, $"[global::System.Text.Json.Serialization.JsonDerivedType(typeof(global::{Namespace}.App.Test.Embed), \"app.test.embed\")]", StringComparison.Ordinal);
        StringAssert.Contains(postFile.SourceText, "public interface IEmbed { }", StringComparison.Ordinal);

        var implFile = result.Files.Single(f => f.HintName == $"{Namespace}.App.Test.Embed.App_Test_Post_IEmbed.g.cs");
        StringAssert.Contains(implFile.SourceText, $"public sealed partial class Embed : global::{Namespace}.App.Test.Post.IEmbed {{ }}", StringComparison.Ordinal);
    }

    [TestMethod]
    public void Subscription型_MessageCommitが兄弟クラスとして属性なしで生成される()
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

        // MessageCommit は Message にネストされず Subscribe の直下に配置される (ファイル名がフラット構造を示す)
        var commitFile = result.Files.Single(f => f.HintName == $"{Namespace}.Com.Example.Subscribe.MessageCommit.g.cs");
        StringAssert.Contains(commitFile.SourceText, "public sealed partial class MessageCommit", StringComparison.Ordinal);
        // Subscription sub-defs は CBOR エンコードのため JsonPropertyName を付与しない
        Assert.IsFalse(commitFile.SourceText.Contains("JsonPropertyName", StringComparison.Ordinal),
            "Subscription のサブクラスに JsonPropertyName があってはならない");
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
            .Select((json, i) => LexiconCodeGenerator.Parse(json, $"test{i}.json"))
            .ToList();
        return LexiconCodeGenerator.Generate(docs, Namespace);
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
