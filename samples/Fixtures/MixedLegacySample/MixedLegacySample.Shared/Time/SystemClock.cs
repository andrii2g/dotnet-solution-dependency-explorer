namespace MixedLegacySample.Shared.Time;

public sealed class SystemClock
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
