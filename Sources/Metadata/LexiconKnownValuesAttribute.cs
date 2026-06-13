using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconKnownValuesAttribute(params string[] values) : LexiconMetadataAttribute
{
    public string[] Values { get; } = values;
}
