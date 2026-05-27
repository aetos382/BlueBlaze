#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
[Embedded]
internal sealed class DoesNotReturnIfAttribute(bool parameterValue) : Attribute
{
    public bool ParameterValue => parameterValue;
}

#endif
