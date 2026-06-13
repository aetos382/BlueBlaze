using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconFormatAttribute(string format) : LexiconMetadataAttribute
{
    public string Format { get; } = format;
}
