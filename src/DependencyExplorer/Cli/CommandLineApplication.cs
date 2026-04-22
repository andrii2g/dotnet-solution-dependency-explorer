using DependencyExplorer.Utils;

namespace DependencyExplorer.Cli;

internal static class CommandLineApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelpRequest(args))
        {
            CommandLineHelp.WriteGeneralHelp(Console.Out);
            return ExitCodes.Success;
        }

        var command = args[0];
        if (!string.Equals(command, "analyze", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown command '{command}'.");
            Console.Error.WriteLine();
            CommandLineHelp.WriteGeneralHelp(Console.Error);
            return ExitCodes.InvalidArguments;
        }

        var parseResult = AnalyzeCommandParser.Parse(args[1..]);
        if (!parseResult.IsSuccess)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine($"error: {error}");
            }

            Console.Error.WriteLine();
            CommandLineHelp.WriteAnalyzeHelp(Console.Error);
            return ExitCodes.InvalidArguments;
        }

        var options = parseResult.Options!;
        var logger = new ConsoleLogger(options.Verbose);
        var commandRunner = new AnalyzeCommand(logger);
        return await commandRunner.RunAsync(options);
    }

    private static bool IsHelpRequest(IReadOnlyList<string> args)
    {
        return args.Count == 1 && (args[0] is "-h" or "--help" or "help");
    }
}
