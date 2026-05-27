#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method)]
[Embedded]
internal sealed class DoesNotReturnAttribute : Attribute;

#endif
