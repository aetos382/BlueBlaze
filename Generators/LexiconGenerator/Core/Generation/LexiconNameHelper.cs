using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.LexiconGenerator.Core.Generation;

internal static class LexiconNameHelper
{
    // "com.atproto.repo.strongRef" -> ["Com", "Atproto", "Repo", "StrongRef"]
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

    // "camelCase" or "kebab-case" -> "PascalCase"
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

    // Returns the fully qualified static class path (dot-separated) for the namespace container.
    // For case 1/2 (record/object/defs-only): all segments
    // For case 3 (query/procedure/subscription): all segments (last segment IS the container)
    internal static string GetContainerPath(string nsid)
    {
        return string.Join(".", NsidToSegments(nsid));
    }

    // Returns the C# type name for a #main def of a record/object.
    // "com.atproto.repo.strongRef" -> "StrongRef"  (class inside Com.Atproto.Repo)
    internal static string GetMainClassName(string nsid)
    {
        var dot = nsid.LastIndexOf('.');
        return ToPascalCase(dot < 0 ? nsid : nsid[(dot + 1)..]);
    }

    // Returns the C# type name for a non-main def in a defs-only file.
    // "com.atproto.repo.defs" + "commitMeta" -> "CommitMeta" (inside Defs container)
    internal static string GetDefClassName(string defKey)
    {
        return ToPascalCase(defKey);
    }

    // Resolves a lexicon ref string to a fully-qualified C# type path usable in generated code.
    // currentNsid: the NSID of the document being processed
    // nsidIndex: map of nsid -> main def type (null=defs-only)
    // refStr: e.g. "#localDef", "com.atproto.repo.defs#commitMeta", "com.atproto.repo.strongRef"
    internal static string ResolveRef(
        string currentNsid,
        string refStr,
        IReadOnlyDictionary<string, LexiconType?> nsidIndex)
    {
        string targetNsid;
        string defKey;

        if (refStr.StartsWith('#'))
        {
            targetNsid = currentNsid;
            defKey = refStr[1..];
        }
        else
        {
#pragma warning disable CA1307
            var hash = refStr.IndexOf('#');
#pragma warning restore CA1307
            if (hash < 0)
            {
                targetNsid = refStr;
                defKey = "main";
            }
            else
            {
                targetNsid = refStr[..hash];
                defKey = refStr[(hash + 1)..];
            }
        }

        var containerPath = GetContainerPath(targetNsid);
        nsidIndex.TryGetValue(targetNsid, out var mainType);

        if (defKey == "main")
        {
            // Case 1: record/object -> last segment is the class name, parent is the container
            if (mainType is LexiconType.Record or LexiconType.Object)
            {
                return containerPath; // e.g. "Com.Atproto.Repo.StrongRef" (last segment is the class)
            }

            // Case 3: query/procedure/subscription -> no "main" class, container is the static class
            // Should not normally ref the container itself, but return the container path
            return containerPath;
        }

        // Non-main def
        if (mainType is LexiconType.Record or LexiconType.Object)
        {
            // Case 1: nested inside main class
            // containerPath already includes last segment as class name
            var targetSegments = NsidToSegments(targetNsid);
            return containerPath + "." + GetNestedDefClassName(defKey, targetSegments[^1]);
        }

        if (mainType is LexiconType.Query or LexiconType.Procedure or LexiconType.Subscription)
        {
            // Case 3: Output/Message プレフィックスを付けた兄弟クラスとして解決
            var outputOrMessage = mainType == LexiconType.Subscription ? "Message" : "Output";
            return containerPath + "." + outputOrMessage + ToPascalCase(defKey);
        }

        // Case 2: defs-only -> nested inside container class
        var defsOnlySegments = NsidToSegments(targetNsid);
        return containerPath + "." + GetNestedDefClassName(defKey, defsOnlySegments[^1]);
    }

    // 非main defのクラス名が、同じ NSID のコンテナ末尾セグメント名(main クラス名、
    // または defs-only の場合はコンテナ自身の名前)と衝突する場合に回避する。
    // C# はネスト型の名前を直接のコンテナ型と同じにできない(CS0542)ため。
    internal static string GetNestedDefClassName(string defKey, string containerClassName)
    {
        var className = ToPascalCase(defKey);
        return className == containerClassName ? className + "Def" : className;
    }

    // Returns the hint name prefix for a generated file.
    // e.g. "BlueBlaze.Generated.Com.Atproto.Repo.StrongRef"
    internal static string GetHintNameBase(string? generatedCodeNamespace, string classPath)
    {
        return string.IsNullOrEmpty(generatedCodeNamespace)
            ? classPath
            : generatedCodeNamespace + "." + classPath;
    }

    // 相対型パスを global:: 付き完全修飾パスに変換する
    internal static string GlobalizeTypePath(string resolvedPath, string? generatedCodeNamespace)
    {
        if (string.IsNullOrEmpty(generatedCodeNamespace))
        {
            return "global::" + resolvedPath;
        }

        return "global::" + generatedCodeNamespace + "." + resolvedPath;
    }

    // Parses a ref string into (targetNsid, defKey).
    internal static (string Nsid, string DefKey) ParseRef(string refStr, string currentNsid)
    {
        if (refStr.StartsWith('#'))
        {
            return (currentNsid, refStr[1..]);
        }

#pragma warning disable CA1307
        var hash = refStr.IndexOf('#');
#pragma warning restore CA1307
        if (hash < 0)
        {
            return (refStr, "main");
        }

        return (refStr[..hash], refStr[(hash + 1)..]);
    }

    // Builds the $type discriminator value for a ref (NSID form without #main).
    internal static string GetTypeDiscriminator(string refStr)
    {
        if (refStr.EndsWith("#main", StringComparison.Ordinal))
        {
            return refStr[..^5];
        }

        return refStr;
    }
}
