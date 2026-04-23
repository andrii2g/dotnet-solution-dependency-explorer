namespace MixedLegacySample.Shared.Abstractions;

public interface IMessageBus
{
    void Publish(string message);
}
