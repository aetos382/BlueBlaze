using System;

namespace BlueBlaze.LexiconMetadata;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LexiconEncodingAttribute(string encoding) : LexiconMetadataAttribute
{
    public string Encoding { get; } = encoding;
}
