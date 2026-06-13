using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconFormatAttribute(string format) : LexiconMetadataAttribute
{
    public string Format { get; } = format;
}
