using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconMaximumAttribute(Type type, object value) : LexiconMetadataAttribute
{
    public Type Type { get; } = type;

    public object Value { get; } = value;
}
