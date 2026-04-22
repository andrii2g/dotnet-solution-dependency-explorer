using DependencyExplorer.Utils;

namespace DependencyExplorer.Cli;

internal sealed class AnalyzeCommand
{
    private readonly ConsoleLogger _logger;

    public AnalyzeCommand(ConsoleLogger logger)
    {
        _logger = logger;
    }

    public Task<int> RunAsync(AnalyzeCommandOptions options)
    {
        if (!File.Exists(options.SolutionPath))
        {
            _logger.Error($"Solution path was not found: {options.SolutionPath}");
            return Task.FromResult(ExitCodes.InvalidArguments);
        }

        var extension = Path.GetExtension(options.SolutionPath);
        if (!string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Error($"The solution path must point to a .sln or .slnx file: {options.SolutionPath}");
            return Task.FromResult(ExitCodes.InvalidArguments);
        }

        try
        {
            Directory.CreateDirectory(options.OutputDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error($"The output directory could not be created: {options.OutputDirectory}");
            _logger.Error(ex.Message);
            return Task.FromResult(ExitCodes.InvalidArguments);
        }

        _logger.Info("Phase 1 CLI shell initialized.");
        _logger.Info($"Solution: {options.SolutionPath}");
        _logger.Info($"Output: {options.OutputDirectory}");
        _logger.Info($"Level: {options.Level}");
        _logger.Info($"Graph format: {options.GraphFormat}");
        _logger.Verbose("Verbose logging enabled.");
        _logger.Verbose("Roslyn workspace loading and analysis are not implemented until later phases.");

        return Task.FromResult(ExitCodes.Success);
    }
}
