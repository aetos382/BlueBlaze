using System;
using System.Collections.Generic;
using System.Linq;

using BlueBlaze.CommandGenerator.Generation;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.CommandGenerator;

/// <summary>
/// lexicon JSON を再パースせず、<c>LexiconGenerator</c> が生成した <c>[Lexicon(nsid, kind)]</c> 付き
/// <c>Request</c> クラスを Compilation の Symbol 解析で発見し、System.CommandLine の CLI コマンド
/// 定義(<c>Command</c>/<c>Option&lt;T&gt;</c>)を生成する。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor SkippedDescriptor = new(
        id: "BBCMD001",
        title: "CLI コマンドの自動生成をスキップしました",
        messageFormat: "{0}",
        category: "CommandGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (opts, _) =>
            {
                opts.GlobalOptions.TryGetValue("build_property.BlueBlazeCommandGeneratorTargetSet", out var targetSetRaw);
                var targetSet = string.IsNullOrEmpty(targetSetRaw)
                    ? new HashSet<string>(StringComparer.Ordinal) { "*" }
                    : new HashSet<string>(targetSetRaw!.Split([';'], StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

                opts.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                var generatedNamespace = string.IsNullOrEmpty(rootNamespace)
                    ? "BlueBlaze.Cli.Generated"
                    : rootNamespace + ".Generated";

                return (TargetSet: targetSet, GeneratedNamespace: generatedNamespace);
            });

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(optionsProvider),
            static (spc, pair) =>
            {
                var (compilation, options) = pair;

                var requests = LexiconSymbolReader.FindRequests(compilation);
                if (requests.Count == 0)
                {
                    return;
                }

                var jsonSerializerContextType = compilation
                    .GetSymbolsWithName("LexiconJsonSerializerContext", SymbolFilter.Type)
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();

                var eligible = new List<(LexiconRequestInfo Request, string[] Segments)>();

                foreach (var request in requests)
                {
                    if (!IsInTargetSet(request.Nsid, options.TargetSet))
                    {
                        continue;
                    }

                    var eligibility = CliEligibility.Evaluate(request, compilation);
                    if (!eligibility.IsEligible)
                    {
                        if (eligibility.WarningMessage is not null)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(SkippedDescriptor, Location.None, eligibility.WarningMessage));
                        }

                        continue;
                    }

                    var segments = NameHelper.NsidToSegments(request.Nsid);
                    var source = CliCommandEmitter.Emit(request, eligibility, segments, options.GeneratedNamespace, jsonSerializerContextType);
                    var hintName = string.Join(".", segments) + ".CliCommand.g.cs";
                    spc.AddSource(hintName, source);

                    eligible.Add((request, segments));
                }

                if (eligible.Count > 0)
                {
                    var treeSource = CliCommandTreeEmitter.Emit(eligible, options.GeneratedNamespace);
                    spc.AddSource("CliCommandTree.g.cs", treeSource);
                }
            });
    }

    private static bool IsInTargetSet(string nsid, HashSet<string> targetSet)
    {
        return targetSet.Contains("*") || targetSet.Contains(nsid);
    }
}
