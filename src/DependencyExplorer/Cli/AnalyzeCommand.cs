using A2G.DependencyExplorer.Discovery;
using A2G.DependencyExplorer.Export;
using A2G.DependencyExplorer.Utils;
using A2G.DependencyExplorer.Workspace;

namespace A2G.DependencyExplorer.Cli;

internal sealed class AnalyzeCommand
{
    private readonly ConsoleLogger _logger;

    public AnalyzeCommand(ConsoleLogger logger)
    {
        _logger = logger;
    }

    public async Task<int> RunAsync(AnalyzeCommandOptions options)
    {
        if (!File.Exists(options.SolutionPath))
        {
            _logger.Error($"Solution path was not found: {options.SolutionPath}");
            return ExitCodes.InvalidArguments;
        }

        var extension = Path.GetExtension(options.SolutionPath);
        if (!string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Error($"The solution path must point to a .sln or .slnx file: {options.SolutionPath}");
            return ExitCodes.InvalidArguments;
        }

        try
        {
            Directory.CreateDirectory(options.OutputDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error($"The output directory could not be created: {options.OutputDirectory}");
            _logger.Error(ex.Message);
            return ExitCodes.InvalidArguments;
        }

        _logger.Info($"Solution: {options.SolutionPath}");
        _logger.Info($"Output: {options.OutputDirectory}");
        _logger.Info($"Level: {options.Level}");
        _logger.Info($"Graph format: {options.GraphFormat}");
        _logger.Info($"Skip classification: {options.SkipClassification}");
        _logger.Info($"Skip DI graph: {options.SkipDiGraph}");

        try
        {
            var workspaceLoader = new WorkspaceLoader();
            _logger.Info("Loading solution through MSBuildWorkspace...");
            var workspaceLoadResult = await workspaceLoader.LoadSolutionAsync(options.SolutionPath, CancellationToken.None);
            _logger.Verbose($"Loaded {workspaceLoadResult.Projects.Count} projects.");

            foreach (var diagnostic in workspaceLoadResult.Diagnostics)
            {
                _logger.Verbose($"workspace {diagnostic.Kind}: {diagnostic.Message}");
            }

            var discoveryService = new SolutionDiscoveryService();
            _logger.Info("Discovering projects, package references, and named types...");
            var analysisResult = await discoveryService.DiscoverAsync(workspaceLoadResult, options, CancellationToken.None);

            var writer = new AnalysisResultWriter();
            await writer.WriteAsync(analysisResult, options.OutputDirectory, CancellationToken.None);

            _logger.Info($"Projects discovered: {analysisResult.Projects.Count}");
            _logger.Info($"Named types discovered: {analysisResult.Types.Count}");
            _logger.Info($"Workspace diagnostics: {analysisResult.Diagnostics.Count}");
            _logger.Info($"Findings: {analysisResult.Findings.Count}");
            _logger.Info($"Wrote {Path.Combine(options.OutputDirectory, "analysis.json")}");
            _logger.Info($"Wrote {Path.Combine(options.OutputDirectory, "summary.md")}");
            _logger.Info($"Wrote {Path.Combine(options.OutputDirectory, "inventory.md")}");
            _logger.Info($"Wrote {Path.Combine(options.OutputDirectory, "violations.md")}");

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.Error("Analysis failed during workspace loading or discovery.");
            _logger.Error(ex.Message);
            _logger.Verbose(ex.ToString());
            return ExitCodes.ExecutionFailed;
        }
    }
}
