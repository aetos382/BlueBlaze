using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconMaxLengthAttribute(int maxLength) : LexiconMetadataAttribute
{
    public int MaxLength { get; } = maxLength;
}
