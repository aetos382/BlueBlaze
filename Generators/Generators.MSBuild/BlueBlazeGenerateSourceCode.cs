using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlueBlaze.Generators.MSBuild;

public sealed class BlueBlazeGenerateSourceCode : Task
{
#pragma warning disable CA1819

    [Output]
    public ITaskItem[] GeneratedSourceFiles { get; set; } = [];

#pragma warning restore CA1819

    public override bool Execute()
    {
        return true;
    }
}
