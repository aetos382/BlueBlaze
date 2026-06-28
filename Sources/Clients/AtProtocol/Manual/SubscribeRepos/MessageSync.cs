using System;

namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class MessageSync : IMessage
{
    public MessageSync(long seq, string did, ReadOnlyMemory<byte> blocks, string rev, string time)
    {
        this.Seq = seq;
        this.Did = did;
        this.Blocks = blocks;
        this.Rev = rev;
        this.Time = time;
    }

    public long Seq { get; }
    public string Did { get; }
    public ReadOnlyMemory<byte> Blocks { get; }
    public string Rev { get; }
    public string Time { get; }
}
