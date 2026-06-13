using System;

namespace BlueBlaze.LexiconGenerator.Core;

public sealed record GeneratorOptions
{
    public static readonly GeneratorOptions Default = new();

    public bool GenerateTypeInfo { get; init; }

    public string? TargetFramework { get; init; }

    public bool ForceEmitAotAttributes { get; init; }

    internal bool ShouldEmitAotAttributes
    {
        get
        {
            if (this.GenerateTypeInfo)
            {
                return false;
            }

            // NET7+ は BCL が両属性をサポートするため常に出力
            if (this.TargetFramework is not null && GetNetMajorVersion(this.TargetFramework) >= 7)
            {
                return true;
            }

            // NET7 未満の場合は明示的に有効化されている場合のみ出力
            return this.ForceEmitAotAttributes;
        }
    }

    private static int GetNetMajorVersion(string targetFramework)
    {
        // net5.0, net6.0, net7.0, ..., net10.0 → メジャーバージョンを返す
        // netstandard2.0, netstandard2.1, netcoreapp3.1 → 0 を返す
        if (!targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var rest = targetFramework[3..];

        if (rest.StartsWith("standard", StringComparison.OrdinalIgnoreCase) ||
            rest.StartsWith("coreapp", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

#if NET
        var dotIndex = rest.IndexOf('.', StringComparison.Ordinal);
#else
        var dotIndex = rest.IndexOf('.');
#endif
        var majorStr = dotIndex >= 0 ? rest[..dotIndex] : rest;

        return int.TryParse(majorStr, out var major) ? major : 0;
    }
}
