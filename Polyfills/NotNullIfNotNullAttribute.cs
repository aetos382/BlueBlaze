#if !NETSTANDARD_2_1_OR_GREATER && !NETCOREAPP_3_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.ReturnValue)]
[Embedded]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    public NotNullIfNotNullAttribute(
        string parameterName)
    {
        this.ParameterName = parameterName;
    }

    public string ParameterName { get; }
}

#endif
