using MixedLegacySample.Shared.Abstractions;
using MixedLegacySample.Shared.Time;

namespace MixedLegacySample.Core.Billing;

public sealed class LegacyInvoiceRepositoryService
{
    private readonly IMessageBus _messageBus;
    private readonly SystemClock _clock;

    public LegacyInvoiceRepositoryService(IMessageBus messageBus, SystemClock clock)
    {
        _messageBus = messageBus;
        _clock = clock;
    }

    public string SaveInvoice(string customerName)
    {
        string message = $"Saved invoice for {customerName} at {_clock.UtcNow():O}.";
        _messageBus.Publish(message);
        return message;
    }
}
