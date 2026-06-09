using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlaze.Generators.Core.Generation;

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
        return ToPascalCase(dot < 0 ? nsid : nsid.Substring(dot + 1));
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

        if (refStr.StartsWith("#", StringComparison.Ordinal))
        {
            targetNsid = currentNsid;
            defKey = refStr.Substring(1);
        }
        else
        {
            var hash = refStr.IndexOf('#');
            if (hash < 0)
            {
                targetNsid = refStr;
                defKey = "main";
            }
            else
            {
                targetNsid = refStr.Substring(0, hash);
                defKey = refStr.Substring(hash + 1);
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
            return containerPath + "." + ToPascalCase(defKey);
        }

        if (mainType is LexiconType.Query or LexiconType.Procedure or LexiconType.Subscription)
        {
            // Case 3: Response/Message プレフィックスを付けた兄弟クラスとして解決
            var responseOrMessage = mainType == LexiconType.Subscription ? "Message" : "Response";
            return containerPath + "." + responseOrMessage + ToPascalCase(defKey);
        }

        // Case 2: defs-only -> nested inside container class
        return containerPath + "." + ToPascalCase(defKey);
    }

    // Returns the hint name prefix for a generated file.
    // e.g. "BlueBlaze.Generated.Com.Atproto.Repo.StrongRef"
    internal static string GetHintNameBase(string? generatedModelNamespace, string classPath)
    {
        return string.IsNullOrEmpty(generatedModelNamespace)
            ? classPath
            : generatedModelNamespace + "." + classPath;
    }

    // 相対型パスを global:: 付き完全修飾パスに変換する
    internal static string GlobalizeTypePath(string resolvedPath, string? generatedModelNamespace)
    {
        if (string.IsNullOrEmpty(generatedModelNamespace))
        {
            return "global::" + resolvedPath;
        }

        return "global::" + generatedModelNamespace + "." + resolvedPath;
    }

    // Parses a ref string into (targetNsid, defKey).
    internal static (string Nsid, string DefKey) ParseRef(string refStr, string currentNsid)
    {
        if (refStr.StartsWith("#", StringComparison.Ordinal))
        {
            return (currentNsid, refStr.Substring(1));
        }

        var hash = refStr.IndexOf('#');
        if (hash < 0)
        {
            return (refStr, "main");
        }

        return (refStr.Substring(0, hash), refStr.Substring(hash + 1));
    }

    // Builds the $type discriminator value for a ref (NSID form without #main).
    internal static string GetTypeDiscriminator(string refStr)
    {
        if (refStr.EndsWith("#main", StringComparison.Ordinal))
        {
            return refStr.Substring(0, refStr.Length - 5);
        }

        return refStr;
    }
}
