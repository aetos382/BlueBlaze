using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconKnownValuesAttribute(params string[] values) : LexiconMetadataAttribute
{
    public string[] Values { get; } = values;
}
