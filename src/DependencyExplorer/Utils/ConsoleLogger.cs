namespace A2G.DependencyExplorer.Utils;

internal sealed class ConsoleLogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose)
    {
        _verbose = verbose;
    }

    public void Info(string message)
    {
        Console.WriteLine(message);
    }

    public void Error(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void Verbose(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"[verbose] {message}");
        }
    }

    public void Progress(int percent, string message)
    {
        Console.WriteLine($"[{percent,3}%] {message}");
    }
}
