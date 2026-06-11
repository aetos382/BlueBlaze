using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace BlueBlaze.ResxSourceGenerator.Tests;

[TestClass]
public sealed class ResxGeneratorTests
{
    private const string SimpleResxContent = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="MyKey" xml:space="preserve">
            <value>Hello</value>
          </data>
        </root>
        """;

    private const string FormatResxContent = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="MyMessage" xml:space="preserve">
            <value>Hello, {0}!</value>
          </data>
        </root>
        """;

    [TestMethod]
    public void GeneratesStringProperty_WithRootNamespace()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", SimpleResxContent);
        var result = RunGenerator(additionalText, globalOptions: new() { ["build_property.RootNamespace"] = "MyNamespace" });

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        StringAssert.Contains(source, "namespace MyNamespace;", StringComparison.Ordinal);
        StringAssert.Contains(source, "internal static partial class Resources", StringComparison.Ordinal);
        StringAssert.Contains(source, "internal static string MyKey =>", StringComparison.Ordinal);
        StringAssert.Contains(source, "ResourceManager.GetString(\"MyKey\", null)!", StringComparison.Ordinal);
    }

    [TestMethod]
    public void GeneratesStringProperty_WithoutRootNamespace()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", SimpleResxContent);
        var result = RunGenerator(additionalText);

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        Assert.IsFalse(source.Contains("namespace ", StringComparison.Ordinal), "Should not emit namespace declaration");
        StringAssert.Contains(source, "internal static partial class Resources", StringComparison.Ordinal);
    }

    [TestMethod]
    public void GeneratesFormatStringMethod()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", FormatResxContent);
        var result = RunGenerator(additionalText);

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        StringAssert.Contains(source, "private static string MyMessage =>", StringComparison.Ordinal);
        StringAssert.Contains(source, "internal static string FormatMyMessage(object? arg0)", StringComparison.Ordinal);
        StringAssert.Contains(source, "string.Format(", StringComparison.Ordinal);
    }

    [TestMethod]
    public void GenerateSource_False_SkipsFile()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", SimpleResxContent);
        var result = RunGenerator(
            additionalText,
            fileOptions: new() { ["build_metadata.EmbeddedResource.GenerateSource"] = "false" });

        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    [TestMethod]
    public void AccessModifier_Public_GeneratesPublicMembers()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", SimpleResxContent);
        var result = RunGenerator(
            additionalText,
            fileOptions: new() { ["build_metadata.EmbeddedResource.AccessModifier"] = "public" });

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        StringAssert.Contains(source, "public static partial class Resources", StringComparison.Ordinal);
        StringAssert.Contains(source, "public static string MyKey =>", StringComparison.Ordinal);
    }

    [TestMethod]
    public void NonResxFile_IsNotGenerated()
    {
        var additionalText = new InMemoryAdditionalText("Resources.txt", "not a resx");
        var result = RunGenerator(additionalText);

        Assert.AreEqual(0, result.GeneratedTrees.Length);
    }

    [TestMethod]
    public void GenerateLocalizableResourceString_Simple()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", SimpleResxContent);
        var result = RunGenerator(
            additionalText,
            fileOptions: new() { ["build_metadata.EmbeddedResource.GenerateRoslynLocalizableResourceString"] = "true" });

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        StringAssert.Contains(source, "global::Microsoft.CodeAnalysis.LocalizableResourceString MyKey =>", StringComparison.Ordinal);
        StringAssert.Contains(source, "new global::Microsoft.CodeAnalysis.LocalizableResourceString(", StringComparison.Ordinal);
        Assert.IsFalse(source.Contains("GetString(", StringComparison.Ordinal), "Should not call GetString");
    }

    [TestMethod]
    public void GenerateLocalizableResourceString_Format()
    {
        var additionalText = new InMemoryAdditionalText("Resources.resx", FormatResxContent);
        var result = RunGenerator(
            additionalText,
            fileOptions: new() { ["build_metadata.EmbeddedResource.GenerateRoslynLocalizableResourceString"] = "true" });

        Assert.AreEqual(1, result.GeneratedTrees.Length);
        var source = result.GeneratedTrees[0].GetText().ToString();

        StringAssert.Contains(source, "global::Microsoft.CodeAnalysis.LocalizableResourceString MyMessage =>", StringComparison.Ordinal);
        StringAssert.Contains(
            source,
            "global::Microsoft.CodeAnalysis.LocalizableResourceString FormatMyMessage(string arg0)",
            StringComparison.Ordinal);
    }

    private static GeneratorDriverRunResult RunGenerator(
        AdditionalText additionalText,
        Dictionary<string, string>? globalOptions = null,
        Dictionary<string, string>? fileOptions = null)
    {
        var optionsProvider = new TestOptionsProvider(
            globalOptions ?? new(),
            new Dictionary<AdditionalText, Dictionary<string, string>>
            {
                [additionalText] = fileOptions ?? new()
            });

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var driver = CSharpGeneratorDriver
            .Create(new ResxGenerator())
            .AddAdditionalTexts([additionalText])
            .WithUpdatedAnalyzerConfigOptions(optionsProvider)
            .RunGenerators(compilation);

        return driver.GetRunResult();
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            this.Path = path;
            this._text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            return this._text;
        }
    }

    private sealed class DictConfigOptions : AnalyzerConfigOptions
    {
        public static readonly DictConfigOptions Empty = new(new());

        private readonly Dictionary<string, string> _values;

        public DictConfigOptions(Dictionary<string, string> values)
        {
            this._values = values;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return this._values.TryGetValue(key, out value);
        }
    }

    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly DictConfigOptions _global;
        private readonly Dictionary<AdditionalText, DictConfigOptions> _perFile;

        public TestOptionsProvider(
            Dictionary<string, string> global,
            Dictionary<AdditionalText, Dictionary<string, string>> perFile)
        {
            this._global = new DictConfigOptions(global);
            this._perFile = perFile.ToDictionary(kvp => kvp.Key, kvp => new DictConfigOptions(kvp.Value));
        }

        public override AnalyzerConfigOptions GlobalOptions => this._global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return DictConfigOptions.Empty;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return this._perFile.TryGetValue(textFile, out var opts) ? opts : DictConfigOptions.Empty;
        }
    }
}
