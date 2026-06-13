using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconMinLengthAttribute(int minLength) : LexiconMetadataAttribute
{
    public int MinLength { get; } = minLength;
}
