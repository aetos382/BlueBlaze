using System;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class RepoOp
{
    public RepoOp(string action, string path, ReadOnlyMemory<byte>? cid, ReadOnlyMemory<byte>? prev = null)
    {
        this.Action = action;
        this.Path = path;
        this.Cid = cid;
        this.Prev = prev;
    }

    public string Action { get; }
    public string Path { get; }

    /// <summary>nullable cid-link</summary>
    public ReadOnlyMemory<byte>? Cid { get; }

    /// <summary>optional cid-link</summary>
    public ReadOnlyMemory<byte>? Prev { get; }
}
