using System;
using System.Collections.Generic;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class MessageCommit : IMessage
{
    public MessageCommit(
        long seq,
        bool rebase,
        bool tooBig,
        string repo,
        ReadOnlyMemory<byte> commit,
        string rev,
        string? since,
        ReadOnlyMemory<byte> blocks,
        IReadOnlyList<RepoOp> ops,
        IReadOnlyList<byte[]> blobs,
        string time,
        ReadOnlyMemory<byte>? prevData = null)
    {
        this.Seq = seq;
        this.Rebase = rebase;
        this.TooBig = tooBig;
        this.Repo = repo;
        this.Commit = commit;
        this.Rev = rev;
        this.Since = since;
        this.Blocks = blocks;
        this.Ops = ops;
        this.Blobs = blobs;
        this.Time = time;
        this.PrevData = prevData;
    }

    public long Seq { get; }
    public bool Rebase { get; }
    public bool TooBig { get; }
    public string Repo { get; }
    public ReadOnlyMemory<byte> Commit { get; }
    public string Rev { get; }
    public string? Since { get; }
    public ReadOnlyMemory<byte> Blocks { get; }
    public IReadOnlyList<RepoOp> Ops { get; }
    public IReadOnlyList<byte[]> Blobs { get; }
    public string Time { get; }
    public ReadOnlyMemory<byte>? PrevData { get; }
}
