#if !NET7_0_OR_GREATER

using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices;

[Embedded]
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Field |
    AttributeTargets.Property |
    AttributeTargets.Struct)]
internal sealed class RequiredMemberAttribute : Attribute;

#endif
