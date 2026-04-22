using DependencyExplorer.Cli;

namespace DependencyExplorer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await CommandLineApplication.RunAsync(args);
    }
}
