using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlueBlaze.Generators.MSBuild;

public sealed class BlueBlazeGenerateSourceCode : Task
{
#pragma warning disable CA1819

    [Required]
    public ITaskItem[] LexiconDocuments { get; set; } = [];

    public string? OutputPath { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; } = [];

#pragma warning restore CA1819

    public override bool Execute()
    {
        foreach (var lexiconDocument in this.LexiconDocuments)
        {
            this.Log.LogMessage(lexiconDocument.ItemSpec);
        }

        return true;
    }
}
