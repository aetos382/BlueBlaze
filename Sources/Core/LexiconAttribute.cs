using System;

namespace BlueBlaze.Core;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LexiconAttribute(string nsid, LexiconOperationKind kind) : LexiconMetadataAttribute
{
    public string Nsid { get; } = nsid;

    public LexiconOperationKind Kind { get; } = kind;
}
