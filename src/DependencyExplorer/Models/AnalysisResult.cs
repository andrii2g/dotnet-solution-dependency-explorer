namespace DependencyExplorer.Models;

internal sealed class AnalysisResult
{
    public required AnalysisMetadata Metadata { get; init; }

    public required AnalysisOptionsSnapshot Options { get; init; }

    public required IReadOnlyList<WorkspaceDiagnosticInfo> Diagnostics { get; init; }

    public required IReadOnlyList<ProjectInfoModel> Projects { get; init; }

    public required IReadOnlyList<TypeInfoModel> Types { get; init; }
}

internal sealed class AnalysisMetadata
{
    public required string ToolVersion { get; init; }

    public required string InputPath { get; init; }

    public required string InputKind { get; init; }

    public required DateTimeOffset GeneratedAtUtc { get; init; }
}

internal sealed class AnalysisOptionsSnapshot
{
    public required string OutputDirectory { get; init; }

    public required string Level { get; init; }

    public required string GraphFormat { get; init; }

    public required bool Verbose { get; init; }
}

internal sealed class WorkspaceDiagnosticInfo
{
    public required string Kind { get; init; }

    public required string Message { get; init; }
}

internal sealed class ProjectInfoModel
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string FilePath { get; init; }

    public required string Language { get; init; }

    public required IReadOnlyList<string> TargetFrameworks { get; init; }

    public required IReadOnlyList<string> ProjectReferences { get; init; }

    public required IReadOnlyList<PackageReferenceModel> PackageReferences { get; init; }

    public required int DocumentCount { get; init; }
}

internal sealed class PackageReferenceModel
{
    public required string Name { get; init; }

    public string? Version { get; init; }
}

internal sealed class TypeInfoModel
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Namespace { get; init; }

    public required string ProjectId { get; init; }

    public required string Kind { get; init; }

    public string? Accessibility { get; init; }

    public string? FilePath { get; init; }
}
