using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconStringFormatAttribute(string format) : LexiconMetadataAttribute
{
    public string Format { get; } = format;
}
