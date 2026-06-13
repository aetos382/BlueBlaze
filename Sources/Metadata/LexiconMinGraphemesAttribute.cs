using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconMinGraphemesAttribute(int minGraphemes) : LexiconMetadataAttribute
{
    public int MinGraphemes { get; } = minGraphemes;
}
