using System.Text.Json;
using DependencyExplorer.Models;

namespace DependencyExplorer.Export;

internal sealed class AnalysisResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public async Task WriteAsync(AnalysisResult result, string outputDirectory, CancellationToken cancellationToken)
    {
        var analysisJsonPath = Path.Combine(outputDirectory, "analysis.json");
        var summaryPath = Path.Combine(outputDirectory, "summary.md");
        var globalClassGraphPath = Path.Combine(outputDirectory, "graph-classes-global.mmd");

        var json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(analysisJsonPath, json, cancellationToken);
        await File.WriteAllTextAsync(summaryPath, BuildSummary(result), cancellationToken);
        if (string.Equals(result.Options.GraphFormat, "Mermaid", StringComparison.OrdinalIgnoreCase))
        {
            await File.WriteAllTextAsync(globalClassGraphPath, BuildGlobalClassMermaid(result), cancellationToken);
        }
    }

    private static string BuildSummary(AnalysisResult result)
    {
        var metrics = result.Metrics;

        var lines = new List<string>
        {
            "# Dependency Explorer Summary",
            string.Empty,
            $"Input path: `{result.Metadata.InputPath}`",
            string.Empty,
            "## Counts",
            string.Empty,
            $"- Projects: {metrics.ProjectCount}",
            $"- Package references: {metrics.PackageReferenceCount}",
            $"- Documents: {metrics.DocumentCount}",
            $"- Named types: {metrics.TypeCount}",
            $"- Project dependency edges: {metrics.ProjectDependencyCount}",
            $"- Namespace dependency edges: {metrics.NamespaceDependencyCount}",
            $"- Type dependency edges: {metrics.TypeDependencyCount}",
            $"- Internal type dependency edges: {metrics.InternalTypeDependencyCount}",
            $"- External type dependency edges: {metrics.ExternalTypeDependencyCount}",
            $"- Constructor DI edges: {metrics.DiDependencyCount}",
            string.Empty,
            "## Workspace Diagnostics",
            string.Empty,
        };

        if (result.Diagnostics.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                lines.Add($"- `{diagnostic.Kind}` {diagnostic.Message}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("## Projects");
        lines.Add(string.Empty);

        foreach (var project in result.Projects)
        {
            lines.Add($"- `{project.Name}`");
            lines.Add($"  Path: `{project.FilePath}`");
            lines.Add($"  Frameworks: {(project.TargetFrameworks.Count == 0 ? "unknown" : string.Join(", ", project.TargetFrameworks))}");
            lines.Add($"  Documents: {project.DocumentCount}");
            lines.Add($"  Project references: {(project.ProjectReferences.Count == 0 ? "none" : string.Join(", ", project.ProjectReferences))}");
            lines.Add($"  Package references: {(project.PackageReferences.Count == 0 ? "none" : string.Join(", ", project.PackageReferences.Select(package => package.Version is null ? package.Name : $"{package.Name}@{package.Version}")))}");
            lines.Add(string.Empty);
        }

        lines.Add("## Top Type Fan-Out");
        lines.Add(string.Empty);
        if (metrics.TopTypeFanOut.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (var metric in metrics.TopTypeFanOut)
            {
                lines.Add($"- `{metric.Label}`: {metric.Value}");
            }
        }

        lines.Add(string.Empty);
        lines.Add("## Top Type Fan-In");
        lines.Add(string.Empty);
        if (metrics.TopTypeFanIn.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (var metric in metrics.TopTypeFanIn)
            {
                lines.Add($"- `{metric.Label}`: {metric.Value}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildGlobalClassMermaid(AnalysisResult result)
    {
        var typeById = result.Types.ToDictionary(type => type.Id, StringComparer.Ordinal);
        var internalEdges = result.TypeDependencies
            .Where(edge => !edge.IsExternal &&
                           string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                           string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal))
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();

        var nodeIds = new HashSet<string>(internalEdges.Select(edge => edge.SourceId), StringComparer.Ordinal);
        nodeIds.UnionWith(internalEdges.Select(edge => edge.TargetId));
        if (nodeIds.Count == 0)
        {
            nodeIds.UnionWith(result.Types.Select(type => type.Id));
        }

        var lines = new List<string>
        {
            "graph TD",
        };

        foreach (var nodeId in nodeIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var nodeName = MakeMermaidNodeId(nodeId);
            var label = typeById.TryGetValue(nodeId, out var type)
                ? BuildTypeLabel(type)
                : nodeId;
            lines.Add($"    {nodeName}[{EscapeMermaidLabel(label)}]");
        }

        foreach (var edge in internalEdges)
        {
            lines.Add($"    {MakeMermaidNodeId(edge.SourceId)} --> {MakeMermaidNodeId(edge.TargetId)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildTypeLabel(TypeInfoModel type)
    {
        return string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    private static string MakeMermaidNodeId(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static string EscapeMermaidLabel(string value)
    {
        return value.Replace("[", "(").Replace("]", ")").Replace("\"", "'");
    }
}
