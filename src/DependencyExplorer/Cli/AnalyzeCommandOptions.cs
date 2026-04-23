namespace A2G.DependencyExplorer.Cli;

internal sealed record AnalyzeCommandOptions(
    string SolutionPath,
    string OutputDirectory,
    AnalysisLevel Level,
    bool Verbose,
    bool SkipClassification,
    bool SkipDiGraph,
    string? FocusProject,
    string? FocusNamespace,
    string? FocusClass);

internal enum AnalysisLevel
{
    Project,
    Namespace,
    Class,
    All,
}
