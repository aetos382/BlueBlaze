using System;
using System.Collections.Generic;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class ClientMethodEmitter
{
    private const string CoreNs = "global::BlueBlaze.Client.Core";

    internal static void Emit(
        LexiconDocumentWithInfo docInfo,
        bool generateTypeInfo,
        string generatedCodeNamespace,
        List<GeneratedSourceFile> files)
    {
        var doc = docInfo.Document;
        var nsid = doc.Id;

        if (!doc.Definitions.TryGetValue("main", out var mainDef))
        {
            return;
        }

        if (mainDef is QueryDefinition queryDef)
        {
            EmitMethod(
                nsid,
                hasParameters: queryDef.Parameters?.Properties?.Count > 0,
                hasInput: false,
                hasOutput: queryDef.Output?.Schema is ObjectDefinition,
                generateTypeInfo,
                generatedCodeNamespace,
                files);
        }
        else if (mainDef is ProcedureDefinition procDef)
        {
            EmitMethod(
                nsid,
                hasParameters: false,
                hasInput: procDef.Input?.Schema is ObjectDefinition,
                hasOutput: procDef.Output?.Schema is ObjectDefinition,
                generateTypeInfo,
                generatedCodeNamespace,
                files);
        }
    }

    private static void EmitMethod(
        string nsid,
        bool hasParameters,
        bool hasInput,
        bool hasOutput,
        bool generateTypeInfo,
        string generatedCodeNamespace,
        List<GeneratedSourceFile> files)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);
        var containerSegments = new string[segments.Length - 1];
        Array.Copy(segments, containerSegments, segments.Length - 1);

        var methodName = segments[^1] + "Async";
        var modelPath = string.Join(".", segments);
        var modelType = $"global::{generatedCodeNamespace}.{modelPath}";

        var outputType = hasOutput
            ? $"{modelType}.Output"
            : $"{CoreNs}.VoidOutput";

        var returnType = $"global::System.Threading.Tasks.ValueTask<{CoreNs}.LexiconResponse<{outputType}>>";

        var requestExpr = hasInput
            ? $"new {modelType}.Request(input)"
            : hasParameters
                ? $"new {modelType}.Request(parameters)"
                : $"{modelType}.Request.Instance";

        var deserializerExpr = hasOutput
            ? $"{modelType}.Deserializer.Instance"
            : $"{CoreNs}.VoidOutputDeserializer.Instance";

        var isb = new IndentedStringBuilder();
        EmitFileHeader(isb, generatedCodeNamespace);

        isb.AppendLine("public static partial class AtProtocolClientExtensions");
        isb.AppendLine("{");
        using (isb.Indent())
        {
            OpenPartialStructContainers(isb, containerSegments);

            if (!generateTypeInfo)
            {
                isb.AppendLine("[global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.\")]");
                isb.AppendLine("[global::System.Diagnostics.CodeAnalysis.RequiresDynamicCode(\"JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext.\")]");
            }
            if (hasInput)
            {
                isb.AppendLine($"public {returnType} {methodName}(");
                using (isb.Indent())
                {
                    isb.AppendLine($"{modelType}.Input input,");
                    isb.AppendLine($"global::System.Threading.CancellationToken cancellationToken = default) =>");
                    isb.AppendLine("client.SendAsync(");
                    using (isb.Indent())
                    {
                        isb.AppendLine($"{requestExpr},");
                        isb.AppendLine($"{deserializerExpr},");
                        isb.AppendLine("cancellationToken);");
                    }
                }
            }
            else if (hasParameters)
            {
                isb.AppendLine($"public {returnType} {methodName}(");
                using (isb.Indent())
                {
                    isb.AppendLine($"{modelType}.Parameters? parameters = null,");
                    isb.AppendLine($"global::System.Threading.CancellationToken cancellationToken = default) =>");
                    isb.AppendLine("client.SendAsync(");
                    using (isb.Indent())
                    {
                        isb.AppendLine($"{requestExpr},");
                        isb.AppendLine($"{deserializerExpr},");
                        isb.AppendLine("cancellationToken);");
                    }
                }
            }
            else
            {
                isb.AppendLine($"public {returnType} {methodName}(global::System.Threading.CancellationToken cancellationToken = default) =>");
                using (isb.Indent())
                {
                    isb.AppendLine("client.SendAsync(");
                    using (isb.Indent())
                    {
                        isb.AppendLine($"{requestExpr},");
                        isb.AppendLine($"{deserializerExpr},");
                        isb.AppendLine("cancellationToken);");
                    }
                }
            }

            ClosePartialStructContainers(isb, containerSegments);
        }
        isb.AppendLine("}");

        var hintName = LexiconNameHelper.GetHintNameBase(generatedCodeNamespace, modelPath + ".Client") + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, isb.ToString()));
    }

    private static void EmitFileHeader(IndentedStringBuilder isb, string generatedCodeNamespace)
    {
        isb.AppendLine("// <auto-generated/>");
        isb.AppendLine("#nullable enable");
        isb.AppendLine("#pragma warning disable CS1591");
        isb.AppendLine();
        isb.AppendLine($"namespace {generatedCodeNamespace};");
        isb.AppendLine();
    }

    private static void OpenPartialStructContainers(IndentedStringBuilder isb, string[] containerSegments)
    {
        for (var i = 0; i < containerSegments.Length; i++)
        {
            var structName = ClientPrefixEmitter.ConcatSegments(containerSegments, i + 1);
            isb.AppendLine($"public readonly partial struct {structName}");
            isb.AppendLine("{");
            isb.Indent();
        }
    }

    private static void ClosePartialStructContainers(IndentedStringBuilder isb, string[] containerSegments)
    {
        for (var i = containerSegments.Length - 1; i >= 0; i--)
        {
            isb.Dedent();
            isb.AppendLine("}");
        }
    }
}
