namespace BlueBlaze.Core;

public interface ISubscriptionMessageDeserializer<TMessage>
{
    TMessage? Deserialize(string? messageType, System.ReadOnlyMemory<byte> payload);
}
