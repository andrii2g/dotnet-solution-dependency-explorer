using A2G.DependencyExplorer.Cli;

namespace A2G.DependencyExplorer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await CommandLineApplication.RunAsync(args);
    }
}
