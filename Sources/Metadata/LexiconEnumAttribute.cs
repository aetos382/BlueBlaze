using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconEnumAttribute(Type type, params object[] values) : LexiconMetadataAttribute
{
    public Type Type { get; } = type;

    public object[] Values { get; } = values;
}
