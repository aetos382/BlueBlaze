#if !NET8_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class ArgumentOutOfRangeExceptionExtensions
{
    extension(ArgumentOutOfRangeException)
    {
        public static void ThrowIfEqual<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) == 0, paramName);
        }

        public static void ThrowIfGreaterThan<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) > 0, paramName);
        }

        public static void ThrowIfGreaterThanOrEqual<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) >= 0, paramName);
        }

        public static void ThrowIfLessThan<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) < 0, paramName);
        }

        public static void ThrowIfLessThanOrEqual<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) <= 0, paramName);
        }

        public static void ThrowIfNotEqual<T>(
            T value,
            T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            ThrowIf(value.CompareTo(other) != 0, paramName);
        }

        public static void ThrowIfNegative(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ThrowIf(value < 0, paramName);
        }

        public static void ThrowIfNegativeOrZero(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ThrowIf(value <= 0, paramName);
        }

        public static void ThrowIfZero(
            int value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ThrowIf(value == 0, paramName);
        }
    }

    private static void ThrowIf(
        [DoesNotReturnIf(true)] bool condition,
        string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }
}

#endif
