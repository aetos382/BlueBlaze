using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class LexiconDescriptionAttribute(string description) : LexiconMetadataAttribute
{
    public string Description { get; } = description;
}
