using System.Linq;

namespace BlueBlaze.CommandGenerator.Tests;

[TestClass]
public sealed class CommandSourceGeneratorTest
{
    [TestMethod]
    public void Query_全プリミティブParametersはOption化される()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class Search
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.search", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public Request(global::TestNs.Com.Example.Search.Parameters? parameters = null)
                        {
                            this.Parameters = parameters;
                        }

                        public string Nsid => "com.example.search";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters { get; }
                    }

                    public sealed partial class Parameters : global::BlueBlaze.Core.ILexiconParameters
                    {
                        public string Q { get; set; }
                        public int? Limit { get; set; }

                        public Parameters(string q)
                        {
                            this.Q = q;
                        }

                        public global::System.Collections.Generic.IReadOnlyDictionary<string, string[]> ToDictionary()
                        {
                            return new global::System.Collections.Generic.Dictionary<string, string[]>();
                        }
                    }
                }
            }
        }
        """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsTrue(diagnostics.IsEmpty, string.Join("\n", diagnostics));

        var commandFile = generatedSources.Single(f => f.HintName.Contains("Com.Example.Search", System.StringComparison.Ordinal));
        var text = commandFile.SourceText.ToString();

        StringAssert.Contains(text, "new global::System.CommandLine.Option<string>(\"--q\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "Required = true", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "new global::System.CommandLine.Option<int?>(\"--limit\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "new global::TestNs.Com.Example.Search.Request(parameters)", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "global::BlueBlaze.Core.RawJsonDeserializer.Instance", System.StringComparison.Ordinal);
    }

    [TestMethod]
    public void Procedure_requiredが全プリミティブのInputはOption化される()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class CreateThing
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.createThing", global::BlueBlaze.Core.LexiconOperationKind.Procedure)]
                    public sealed class Request : global::BlueBlaze.Core.IProcedureRequest
                    {
                        public Request(global::TestNs.Com.Example.CreateThing.Input input)
                        {
                            this.Input = input;
                        }

                        public string Nsid => "com.example.createThing";
                        public global::BlueBlaze.Core.ILexiconInput? Input { get; }
                    }

                    public sealed partial class Input : global::BlueBlaze.Core.ILexiconInput
                    {
                        [global::System.Text.Json.Serialization.JsonPropertyName("name")]
                        public string Name { get; set; }

                        [global::System.Text.Json.Serialization.JsonPropertyName("count")]
                        public int Count { get; set; }

                        [global::System.Text.Json.Serialization.JsonConstructor]
                        public Input(string name)
                        {
                            this.Name = name;
                        }

                        public global::System.Net.Http.HttpContent ToHttpContent()
                        {
                            return global::System.Net.Http.Json.JsonContent.Create(this, this.GetType());
                        }
                    }
                }
            }
        }
        """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsTrue(diagnostics.IsEmpty, string.Join("\n", diagnostics));

        var commandFile = generatedSources.Single(f => f.HintName.Contains("Com.Example.CreateThing", System.StringComparison.Ordinal));
        var text = commandFile.SourceText.ToString();

        StringAssert.Contains(text, "new global::System.CommandLine.Option<string>(\"--name\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "Required = true", System.StringComparison.Ordinal);
        // count は required ではない値型(int)なので、コンストラクタの実パラメータには無い ⇒ Option<int?> で optional
        StringAssert.Contains(text, "new global::System.CommandLine.Option<int?>(\"--count\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "new global::TestNs.Com.Example.CreateThing.Request(input)", System.StringComparison.Ordinal);
    }

    [TestMethod]
    public void Procedure_requiredに非プリミティブがあると生成をスキップしてWarningを出す()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class CreateRecord
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.createRecord", global::BlueBlaze.Core.LexiconOperationKind.Procedure)]
                    public sealed class Request : global::BlueBlaze.Core.IProcedureRequest
                    {
                        public Request(global::TestNs.Com.Example.CreateRecord.Input input)
                        {
                            this.Input = input;
                        }

                        public string Nsid => "com.example.createRecord";
                        public global::BlueBlaze.Core.ILexiconInput? Input { get; }
                    }

                    public sealed partial class Input : global::BlueBlaze.Core.ILexiconInput
                    {
                        [global::System.Text.Json.Serialization.JsonPropertyName("record")]
                        public object Record { get; set; }

                        [global::System.Text.Json.Serialization.JsonConstructor]
                        public Input(object record)
                        {
                            this.Record = record;
                        }

                        public global::System.Net.Http.HttpContent ToHttpContent()
                        {
                            return global::System.Net.Http.Json.JsonContent.Create(this, this.GetType());
                        }
                    }
                }
            }
        }
        """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsFalse(generatedSources.Any(f => f.HintName.Contains("CreateRecord", System.StringComparison.Ordinal)));
        Assert.IsTrue(diagnostics.Any(d => d.Id == "BBCMD001"));
    }

    [TestMethod]
    public void Procedure_optionalな非プリミティブがあればinput_json併設になる()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class UpdateThing
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.updateThing", global::BlueBlaze.Core.LexiconOperationKind.Procedure)]
                    public sealed class Request : global::BlueBlaze.Core.IProcedureRequest
                    {
                        public Request(global::TestNs.Com.Example.UpdateThing.Input input)
                        {
                            this.Input = input;
                        }

                        public string Nsid => "com.example.updateThing";
                        public global::BlueBlaze.Core.ILexiconInput? Input { get; }
                    }

                    public sealed partial class Input : global::BlueBlaze.Core.ILexiconInput
                    {
                        [global::System.Text.Json.Serialization.JsonPropertyName("id")]
                        public string Id { get; set; }

                        [global::System.Text.Json.Serialization.JsonPropertyName("metadata")]
                        public object? Metadata { get; set; }

                        [global::System.Text.Json.Serialization.JsonConstructor]
                        public Input(string id)
                        {
                            this.Id = id;
                        }

                        public global::System.Net.Http.HttpContent ToHttpContent()
                        {
                            return global::System.Net.Http.Json.JsonContent.Create(this, this.GetType());
                        }
                    }
                }
            }
        }
        """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsTrue(diagnostics.IsEmpty, string.Join("\n", diagnostics));

        var commandFile = generatedSources.Single(f => f.HintName.Contains("Com.Example.UpdateThing", System.StringComparison.Ordinal));
        var text = commandFile.SourceText.ToString();

        StringAssert.Contains(text, "new global::System.CommandLine.Option<string>(\"--id\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "\"--input-json\"", System.StringComparison.Ordinal);
        // metadata 自体は非プリミティブなので個別 Option 化されない
        Assert.IsFalse(text.Contains("\"--metadata\"", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void Lexicon属性が無い型は生成対象にならない()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class NotALexicon
                {
                    public int Value { get; set; }
                }
            }
        }
        """;

        var (generatedSources, _) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsTrue(generatedSources.IsEmpty);
    }

    [TestMethod]
    public void LexiconCliCommandのRemoveで対象NSIDを除外できる()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class Search
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.search", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public static readonly Request Instance = new();
                        private Request() { }

                        public string Nsid => "com.example.search";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters => null;
                    }
                }
            }
        }
        """;

        var options = new System.Collections.Generic.Dictionary<string, string>
        {
            ["build_property.BlueBlazeCommandGeneratorTargetSet"] = "com.example.other",
        };

        var (generatedSources, _) = GeneratorTestHelper.RunGenerator(source, options);

        Assert.IsTrue(generatedSources.IsEmpty);
    }

    [TestMethod]
    public void LexiconCliCommandで明示的に列挙したNSIDのみ生成される_ホワイトリスト()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class Search
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.search", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public static readonly Request Instance = new();
                        private Request() { }

                        public string Nsid => "com.example.search";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters => null;
                    }
                }

                public sealed partial class Other
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.other", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public static readonly Request Instance = new();
                        private Request() { }

                        public string Nsid => "com.example.other";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters => null;
                    }
                }
            }
        }
        """;

        var options = new System.Collections.Generic.Dictionary<string, string>
        {
            ["build_property.BlueBlazeCommandGeneratorTargetSet"] = "com.example.search",
        };

        var (generatedSources, _) = GeneratorTestHelper.RunGenerator(source, options);

        Assert.IsTrue(generatedSources.Any(f => f.HintName.Contains("Com.Example.Search", System.StringComparison.Ordinal)));
        Assert.IsFalse(generatedSources.Any(f => f.HintName.Contains("Com.Example.Other", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void 複数NSIDがprefixを共有する場合CliCommandTreeが正しく構築される()
    {
        var source = /*lang=csharp*/ """
        namespace TestNs;

        public sealed partial class Com
        {
            public sealed partial class Example
            {
                public sealed partial class Search
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.search", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public static readonly Request Instance = new();
                        private Request() { }

                        public string Nsid => "com.example.search";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters => null;
                    }
                }

                public sealed partial class List
                {
                    [global::BlueBlaze.Core.Lexicon("com.example.list", global::BlueBlaze.Core.LexiconOperationKind.Query)]
                    public sealed class Request : global::BlueBlaze.Core.IQueryRequest
                    {
                        public static readonly Request Instance = new();
                        private Request() { }

                        public string Nsid => "com.example.list";
                        public global::BlueBlaze.Core.ILexiconParameters? Parameters => null;
                    }
                }
            }
        }
        """;

        var (generatedSources, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.IsTrue(diagnostics.IsEmpty, string.Join("\n", diagnostics));

        var treeFile = generatedSources.Single(f => f.HintName.Contains("CliCommandTree", System.StringComparison.Ordinal));
        var text = treeFile.SourceText.ToString();

        // リーフコマンド自体(Command("search", ...) 等)は各 NSID 専用ファイル側で生成され、
        // CliCommandTree からは BuildCliCommand(client) の呼び出しのみが参照される。
        StringAssert.Contains(text, "ComExampleListCliCommand.BuildCliCommand(client)", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "ComExampleSearchCliCommand.BuildCliCommand(client)", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "new global::System.CommandLine.Command(\"com\", \"Com\")", System.StringComparison.Ordinal);
        StringAssert.Contains(text, "new global::System.CommandLine.Command(\"example\", \"Com.Example\")", System.StringComparison.Ordinal);
    }
}
