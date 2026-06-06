using System.Diagnostics;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlueBlaze.Generators.MSBuild;

public sealed class BlueBlazeGenerateSourceCode : Task
{
#pragma warning disable CA1819

    [Required]
    public ITaskItem[] LexiconDocuments { get; set; } = [];

    public string? OutputPath { get; set; }

    public bool DebugBreakOnExecute { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = [];

#pragma warning restore CA1819

    public override bool Execute()
    {
        if (this.DebugBreakOnExecute)
        {
            Debugger.Launch();
        }

        foreach (var lexiconDocument in this.LexiconDocuments)
        {
            if (!bool.TryParse(lexiconDocument.GetMetadata("IsLexiconDocument"), out var isLexiconDocument) ||
                !isLexiconDocument)
            {
                continue;
            }

            var fullPath = lexiconDocument.GetMetadata("FullPath");
            this.Log.LogMessage(fullPath);
        }

        return true;
    }
}
