using MixedLegacySample.Shared.Abstractions;

namespace MixedLegacySample.Host.Runtime;

public sealed class ConsoleMessageBus : IMessageBus
{
    public void Publish(string message)
    {
        Console.WriteLine(message);
    }
}
