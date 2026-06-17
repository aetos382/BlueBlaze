using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using BlueBlaze.CommandGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlueBlaze.CommandGenerator.Tests;

internal static class GeneratorTestHelper
{
    private static readonly MetadataReference[] References = BuildReferences();

    internal static (ImmutableArray<GeneratedSourceResult> GeneratedSources, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        string source,
        IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            new TestAnalyzerConfigOptions(globalOptions ?? new Dictionary<string, string>()));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CommandSourceGenerator().AsSourceGenerator()],
            optionsProvider: optionsProvider);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var runResult = driver.GetRunResult();
        var generatorResult = runResult.Results[0];

        return (generatorResult.GeneratedSources, generatorResult.Diagnostics);
    }

    // テストプロセス自体はシングルファイル化されないため、Assembly.Location は安全に使える。
    [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "Test process is never published as a single file.")]
    private static MetadataReference[] BuildReferences()
    {
        var anchorAssemblies = new[]
        {
            typeof(object).Assembly,
            typeof(IReadOnlyList<int>).Assembly,
            typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly,
            typeof(BlueBlaze.Core.LexiconAttribute).Assembly,
            typeof(System.CommandLine.Command).Assembly,
        };

        var locations = new HashSet<string>(StringComparer.Ordinal);
        var references = new List<MetadataReference>();

        foreach (var assembly in anchorAssemblies)
        {
            if (locations.Add(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // ランタイムが持つ全アセンブリ(System.Runtime 等の暗黙参照)も含める。
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                if (path.Length > 0 && locations.Add(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        return references.ToArray();
    }

    private sealed class TestAnalyzerConfigOptionsProvider(TestAnalyzerConfigOptions globalOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return this.GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return this.GlobalOptions;
        }
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            return options.TryGetValue(key, out value!);
        }
    }
}
