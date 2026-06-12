using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class ClientMethodEmitter
{
    internal static void Emit(
        LexiconDocumentWithInfo docInfo,
        string clientCodeNamespace,
        string modelCodeNamespace,
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
            EmitQueryMethod(nsid, queryDef, clientCodeNamespace, modelCodeNamespace, files);
        }
        else if (mainDef is ProcedureDefinition procDef)
        {
            EmitProcedureMethod(nsid, procDef, clientCodeNamespace, modelCodeNamespace, files);
        }
    }

    private static void EmitQueryMethod(
        string nsid,
        QueryDefinition queryDef,
        string clientCodeNamespace,
        string modelCodeNamespace,
        List<GeneratedSourceFile> files)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);
        var containerSegments = new string[segments.Length - 1];
        Array.Copy(segments, containerSegments, segments.Length - 1);

        var methodName = segments[segments.Length - 1] + "Async";
        var modelPath = string.Join(".", segments);

        var hasParameters = queryDef.Parameters?.Properties != null &&
                            queryDef.Parameters.Properties.Count > 0;
        var hasResponse = queryDef.Output?.Schema is ObjectDefinition;

        if (!hasResponse)
        {
            return;
        }

        var sb = new StringBuilder();
        EmitFileHeader(sb, clientCodeNamespace);

        sb.AppendLine("public static partial class AtProtocolClientExtensions");
        sb.AppendLine("{");

        OpenPartialStructContainers(sb, containerSegments);

        var methodIndent = new string(' ', (containerSegments.Length + 1) * 4);
        var responseType = $"global::{modelCodeNamespace}.{modelPath}.Response";

        sb.Append($"{methodIndent}public global::System.Threading.Tasks.ValueTask<{responseType}> {methodName}(");
        if (hasParameters)
        {
            sb.AppendLine($"global::{modelCodeNamespace}.{modelPath}.Parameters parameters,");
            sb.Append($"{methodIndent}    ");
        }
        sb.AppendLine($"global::System.Threading.CancellationToken cancellationToken = default) =>");
        sb.AppendLine($"{methodIndent}    throw new global::System.NotImplementedException();");

        ClosePartialStructContainers(sb, containerSegments);
        sb.AppendLine("}");

        var hintName = LexiconNameHelper.GetHintNameBase(clientCodeNamespace, modelPath + ".Client") + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));
    }

    private static void EmitProcedureMethod(
        string nsid,
        ProcedureDefinition procDef,
        string clientCodeNamespace,
        string modelCodeNamespace,
        List<GeneratedSourceFile> files)
    {
        var segments = LexiconNameHelper.NsidToSegments(nsid);
        var containerSegments = new string[segments.Length - 1];
        Array.Copy(segments, containerSegments, segments.Length - 1);

        var methodName = segments[segments.Length - 1] + "Async";
        var modelPath = string.Join(".", segments);

        var hasInput = procDef.Input?.Schema is ObjectDefinition;
        var hasOutput = procDef.Output?.Schema is ObjectDefinition;

        if (!hasInput && !hasOutput)
        {
            return;
        }

        var sb = new StringBuilder();
        EmitFileHeader(sb, clientCodeNamespace);

        sb.AppendLine("public static partial class AtProtocolClientExtensions");
        sb.AppendLine("{");

        OpenPartialStructContainers(sb, containerSegments);

        var methodIndent = new string(' ', (containerSegments.Length + 1) * 4);

        if (hasOutput)
        {
            var responseType = $"global::{modelCodeNamespace}.{modelPath}.Response";
            sb.Append($"{methodIndent}public global::System.Threading.Tasks.ValueTask<{responseType}> {methodName}(");
            if (hasInput)
            {
                sb.AppendLine($"global::{modelCodeNamespace}.{modelPath}.Request request,");
                sb.Append($"{methodIndent}    ");
            }
            sb.AppendLine($"global::System.Threading.CancellationToken cancellationToken = default) =>");
            sb.AppendLine($"{methodIndent}    throw new global::System.NotImplementedException();");
        }
        else
        {
            var requestType = $"global::{modelCodeNamespace}.{modelPath}.Request";
            sb.AppendLine($"{methodIndent}public global::System.Threading.Tasks.ValueTask {methodName}(");
            sb.AppendLine($"{methodIndent}    {requestType} request,");
            sb.AppendLine($"{methodIndent}    global::System.Threading.CancellationToken cancellationToken = default) =>");
            sb.AppendLine($"{methodIndent}    throw new global::System.NotImplementedException();");
        }

        ClosePartialStructContainers(sb, containerSegments);
        sb.AppendLine("}");

        var hintName = LexiconNameHelper.GetHintNameBase(clientCodeNamespace, modelPath + ".Client") + ".g.cs";
        files.Add(new GeneratedSourceFile(hintName, sb.ToString()));
    }

    private static void EmitFileHeader(StringBuilder sb, string clientCodeNamespace)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS1591");
        sb.AppendLine();
        sb.AppendLine($"namespace {clientCodeNamespace};");
        sb.AppendLine();
    }

    private static void OpenPartialStructContainers(StringBuilder sb, string[] containerSegments)
    {
        for (var i = 0; i < containerSegments.Length; i++)
        {
            var indent = new string(' ', (i + 1) * 4);
            var structName = ClientPrefixEmitter.ConcatSegments(containerSegments, i + 1);
            sb.AppendLine($"{indent}public readonly partial struct {structName}");
            sb.AppendLine($"{indent}{{");
        }
    }

    private static void ClosePartialStructContainers(StringBuilder sb, string[] containerSegments)
    {
        for (var i = containerSegments.Length - 1; i >= 0; i--)
        {
            var indent = new string(' ', (i + 1) * 4);
            sb.AppendLine($"{indent}}}");
        }
    }
}
