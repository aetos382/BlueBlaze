using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using BlueBlaze.Generators.Core;

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

        var items = new List<LexiconDocumentWithInfo>();

        foreach (var lexiconDocument in this.LexiconDocuments)
        {
            if (!IsLexiconDocument(lexiconDocument))
            {
                continue;
            }

            var fullPath = lexiconDocument.GetMetadata("FullPath");
            var text = File.ReadAllText(fullPath);

            var item = LexiconGenerator.Parse(text, fullPath);
            items.Add(item);
        }

        return true;
    }

    private static bool IsLexiconDocument(ITaskItem item)
    {
        return bool.TryParse(item.GetMetadata("IsLexiconDocument"), out var value) && value;
    }
}
