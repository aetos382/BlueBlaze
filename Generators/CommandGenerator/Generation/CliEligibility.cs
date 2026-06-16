using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace BlueBlaze.CommandGenerator.Generation;

internal sealed record CliEligibilityResult(
    bool IsEligible,
    string? WarningMessage,
    List<LexiconPropertyInfo> ParameterProperties,
    List<LexiconPropertyInfo>? InputProperties,
    bool NeedsInputJsonFallback);

/// <summary>
/// NSID 単位で CLI コマンドを自動生成できるかどうかを判定する。
///
/// - Parameters は lexicon 仕様上常にプリミティブのみなので、対象外になることはない。
/// - Input が存在しない(procedure で input なし、または input.schema が無い uploadBlob 形)場合は対象内。
/// - Input の required プロパティに非プリミティブが1つでもあれば対象外 + Warning(手書き実装に回す)。
/// - Input の optional プロパティにのみ非プリミティブがある場合は対象内だが、
///   <c>--input-json</c> での全体上書きフォールバックが必要。
/// </summary>
internal static class CliEligibility
{
    internal static CliEligibilityResult Evaluate(LexiconRequestInfo request, Compilation compilation)
    {
        var parameterProperties = request.ParametersType is not null
            ? LexiconSymbolReader.ReadProperties(request.ParametersType, compilation)
            : [];

        if (request.InputType is null)
        {
            return new CliEligibilityResult(true, null, parameterProperties, null, false);
        }

        var inputProperties = LexiconSymbolReader.ReadProperties(request.InputType, compilation);

        var requiredNonPrimitive = inputProperties
            .Where(p => p.IsRequired && !p.IsPrimitive)
            .ToList();

        if (requiredNonPrimitive.Count > 0)
        {
            var names = string.Join(", ", requiredNonPrimitive.Select(p => p.JsonName));
            var message =
                $"NSID '{request.Nsid}' の Input に必須の非プリミティブプロパティ({names})が含まれるため、" +
                "CLI コマンドの自動生成をスキップしました。手書き実装が必要です。";
            return new CliEligibilityResult(false, message, parameterProperties, inputProperties, false);
        }

        var needsInputJsonFallback = inputProperties.Any(p => !p.IsRequired && !p.IsPrimitive);

        return new CliEligibilityResult(true, null, parameterProperties, inputProperties, needsInputJsonFallback);
    }
}
