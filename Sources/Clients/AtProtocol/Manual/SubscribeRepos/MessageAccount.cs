namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class MessageAccount : IMessage
{
    public MessageAccount(long seq, string did, string time, bool active, string? status = null)
    {
        this.Seq = seq;
        this.Did = did;
        this.Time = time;
        this.Active = active;
        this.Status = status;
    }

    public long Seq { get; }
    public string Did { get; }
    public string Time { get; }
    public bool Active { get; }
    public string? Status { get; }
}
