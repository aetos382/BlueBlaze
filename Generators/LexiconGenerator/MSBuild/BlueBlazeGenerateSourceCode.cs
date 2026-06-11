using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using BlueBlaze.LexiconGenerator.Core;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlueBlaze.LexiconGenerator.MSBuild;

public sealed class BlueBlazeGenerateSourceCode : Task
{
#pragma warning disable CA1819
    // ReSharper disable MemberCanBePrivate.Global

    [Required]
    public ITaskItem[] LexiconDocuments { get; set; } = [];

    [Required]
    public required string OutputPath { get; set; }

    [Required]
    public required string GeneratedCodeNamespace { get; set; }

    public bool DebugBreakOnExecute { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = [];

    // ReSharper restore MemberCanBePrivate.Global
#pragma warning restore CA1819

    public override bool Execute()
    {
        if (this.DebugBreakOnExecute)
        {
            Debugger.Launch();
        }

        var items = new List<ParseResult>();

        foreach (var lexiconDocument in this.LexiconDocuments)
        {
            if (!IsLexiconDocument(lexiconDocument))
            {
                continue;
            }

            var fullPath = lexiconDocument.GetMetadata("FullPath");
            var text = File.ReadAllText(fullPath);

            var item = LexiconCodeGenerator.Parse(text, fullPath);
            items.Add(item);
        }

        var result = LexiconCodeGenerator.Generate(items, this.GeneratedCodeNamespace);

        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                this.Log.LogError(diag.Message);
            }
            else
            {
                this.Log.LogWarning(diag.Message);
            }
        }

        var taskItems = new List<ITaskItem>(result.Files.Count);

        foreach (var file in result.Files)
        {
            var outputFile = Path.Combine(this.OutputPath, file.HintName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? this.OutputPath);

            File.WriteAllText(outputFile, file.SourceText, new System.Text.UTF8Encoding(false));
            taskItems.Add(new TaskItem(outputFile));
        }

        this.GeneratedFiles = taskItems.ToArray();

        return !this.Log.HasLoggedErrors;
    }

    private static bool IsLexiconDocument(ITaskItem item)
    {
        return bool.TryParse(item.GetMetadata("IsLexiconDocument"), out var value) && value;
    }
}
