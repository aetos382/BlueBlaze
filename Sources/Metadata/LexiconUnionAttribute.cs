using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface)]
public sealed class LexiconUnionAttribute(params Type[] refs) : LexiconMetadataAttribute
{
    public Type[] Refs { get; } = refs;

    public bool Closed { get; init; }
}
