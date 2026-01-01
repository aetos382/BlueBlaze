#if !NET7_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices;

[Embedded]
[AttributeUsage(AttributeTargets.All)]
internal sealed class CompilerFeatureRequiredAttribute :
    Attribute
{
    public CompilerFeatureRequiredAttribute(
        string featureName)
    {
        this.FeatureName = featureName;
    }

    public string FeatureName { get; }

    public const string RefStructs = nameof(RefStructs);
    public const string RequiredMembers = nameof(RequiredMembers);
}

#endif
