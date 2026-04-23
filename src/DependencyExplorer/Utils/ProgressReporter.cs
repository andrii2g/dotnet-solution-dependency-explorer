namespace A2G.DependencyExplorer.Utils;

internal sealed class ProgressReporter
{
    private readonly ConsoleLogger _logger;
    private int _lastPercent = -1;

    public ProgressReporter(ConsoleLogger logger)
    {
        _logger = logger;
    }

    public void Report(int percent, string message)
    {
        var boundedPercent = Math.Clamp(percent, 0, 100);
        if (boundedPercent <= _lastPercent)
        {
            return;
        }

        _lastPercent = boundedPercent;
        _logger.Progress(boundedPercent, message);
    }
}
