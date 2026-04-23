using MixedLegacySample.Host.Runtime;
using MixedLegacySample.Shared.Time;

Console.WriteLine(new LegacyHost(new ConsoleMessageBus(), new SystemClock()).Run());
