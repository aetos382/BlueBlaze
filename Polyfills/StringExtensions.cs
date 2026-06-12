#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER

namespace System;

internal static class StringExtensions
{
    extension(string s)
    {
        public bool StartsWith(char c)
        {
            return s.Length > 0 && s[0] == c;
        }
    }
}

#endif
