using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LexiconConstAttribute(Type type, object value) : LexiconMetadataAttribute
{
    public Type Type { get; } = type;

    public object Value { get; } = value;
}
