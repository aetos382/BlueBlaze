namespace BlueBlaze.Client.AtProtocol.Manual.SubscribeRepos;

public sealed class MessageInfo : IMessage
{
    public MessageInfo(string name, string? message = null)
    {
        this.Name = name;
        this.Message = message;
    }

    public string Name { get; }
    public string? Message { get; }
}
