using System.Text;

namespace BlueBlaze.CommandGenerator.Generation;

/// <summary>
/// NSID 文字列からの命名規則導出ヘルパー。
/// <c>LexiconGenerator.Core.Generation.LexiconNameHelper</c> と同じ規則だが、
/// <c>CommandGenerator</c> は lexicon JSON を読まず NSID 文字列のみから導出するため独立実装する。
/// </summary>
internal static class NameHelper
{
    // "com.atproto.repo.createRecord" -> ["Com", "Atproto", "Repo", "CreateRecord"]
    internal static string[] NsidToSegments(string nsid)
    {
        var parts = nsid.Split('.');
        var segments = new string[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            segments[i] = ToPascalCase(parts[i]);
        }

        return segments;
    }

    // "camelCase" -> "PascalCase"
    internal static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        var sb = new StringBuilder(s.Length);
        var upperNext = true;
        foreach (var c in s)
        {
            if (c is '-' or '_')
            {
                upperNext = true;
            }
            else if (upperNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                upperNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    // "createRecord" -> "create-record" (CLI オプション/サブコマンド名用)
    internal static string ToKebabCase(string camelCase)
    {
        if (string.IsNullOrEmpty(camelCase))
        {
            return camelCase;
        }

        var sb = new StringBuilder(camelCase.Length + 4);
        for (var i = 0; i < camelCase.Length; i++)
        {
            var c = camelCase[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
