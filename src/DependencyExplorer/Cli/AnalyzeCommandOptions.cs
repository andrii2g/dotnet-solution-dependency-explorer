namespace A2G.DependencyExplorer.Cli;

internal sealed record AnalyzeCommandOptions(
    string SolutionPath,
    string OutputDirectory,
    AnalysisLevel Level,
    GraphFormat GraphFormat,
    bool Verbose,
    bool SkipClassification,
    bool SkipDiGraph);

internal enum AnalysisLevel
{
    Project,
    Namespace,
    Class,
    All,
}

internal enum GraphFormat
{
    Mermaid,
    None,
}
