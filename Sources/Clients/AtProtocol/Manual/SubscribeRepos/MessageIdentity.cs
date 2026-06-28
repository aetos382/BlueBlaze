namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class MessageIdentity : IMessage
{
    public MessageIdentity(long seq, string did, string time, string? handle = null)
    {
        this.Seq = seq;
        this.Did = did;
        this.Time = time;
        this.Handle = handle;
    }

    public long Seq { get; }
    public string Did { get; }
    public string Time { get; }
    public string? Handle { get; }
}
