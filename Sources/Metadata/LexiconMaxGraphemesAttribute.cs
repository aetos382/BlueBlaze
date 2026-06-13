using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconMaxGraphemesAttribute(int maxGraphemes) : LexiconMetadataAttribute
{
    public int MaxGraphemes { get; } = maxGraphemes;
}
