#if NETSTANDARD || !NETCOREAPP3_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Parameter)]
[Embedded]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
    public string ParameterName => parameterName;
}

#endif
