using System.Text.Json;
using A2G.DependencyExplorer.Models;

namespace A2G.DependencyExplorer.Export;

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
        var inventoryPath = Path.Combine(outputDirectory, "inventory.md");
        var violationsPath = Path.Combine(outputDirectory, "violations.md");
        var projectGraphPath = Path.Combine(outputDirectory, "graph-projects.mmd");
        var namespaceGraphPath = Path.Combine(outputDirectory, "graph-namespaces.mmd");
        var globalClassGraphPath = Path.Combine(outputDirectory, "graph-classes-global.mmd");

        var json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(analysisJsonPath, json, cancellationToken);
        await File.WriteAllTextAsync(summaryPath, BuildSummary(result), cancellationToken);
        await File.WriteAllTextAsync(inventoryPath, BuildInventory(result), cancellationToken);
        await File.WriteAllTextAsync(violationsPath, BuildViolations(result), cancellationToken);
        if (string.Equals(result.Options.GraphFormat, "Mermaid", StringComparison.OrdinalIgnoreCase))
        {
            await File.WriteAllTextAsync(projectGraphPath, BuildProjectMermaid(result), cancellationToken);
            await File.WriteAllTextAsync(namespaceGraphPath, BuildNamespaceMermaid(result), cancellationToken);
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
            "## Analysis Options",
            string.Empty,
            $"- Classification: {(result.Options.SkipClassification ? "skipped by user request" : "enabled")}",
            $"- Constructor DI graph: {(result.Options.SkipDiGraph ? "skipped by user request" : "enabled")}",
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

        lines.Add(string.Empty);
        lines.Add("## Key Findings");
        lines.Add(string.Empty);
        if (result.Findings.Count == 0)
        {
            lines.Add("- None");
        }
        else
        {
            foreach (var finding in result.Findings.Take(10))
            {
                lines.Add($"- [{finding.Severity}] {finding.Message}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildInventory(AnalysisResult result)
    {
        var lines = new List<string>
        {
            "# Dependency Explorer Inventory",
            string.Empty,
            "| Project | Classification | Documents | Package refs | Project refs | Notes |",
            "| --- | --- | ---: | ---: | ---: | --- |",
        };

        foreach (var project in result.Projects.OrderBy(project => project.Name, StringComparer.Ordinal))
        {
            var classification = result.Options.SkipClassification
                ? "Skipped"
                : project.Classification is null
                    ? "Unknown (Low)"
                    : $"{project.Classification.Layer} ({project.Classification.Confidence})";
            var notes = result.Options.SkipClassification
                ? "classification skipped"
                : project.Classification?.Reasons.Count > 0
                ? string.Join("; ", project.Classification.Reasons.Take(2))
                : "none";
            lines.Add($"| {project.Name} | {classification} | {project.DocumentCount} | {project.PackageReferences.Count} | {project.ProjectReferences.Count} | {notes} |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildViolations(AnalysisResult result)
    {
        var lines = new List<string>
        {
            "# Dependency Explorer Findings",
            string.Empty,
        };

        if (result.Options.SkipClassification)
        {
            lines.Add("Classification-driven heuristic analysis was skipped by user request.");
            lines.Add(string.Empty);
        }

        if (result.Findings.Count == 0)
        {
            lines.Add(result.Options.SkipClassification
                ? "No non-classification violations or warnings were produced for this run."
                : "No violations or warnings were produced for this run.");
            return string.Join(Environment.NewLine, lines);
        }

        foreach (var finding in result.Findings)
        {
            lines.Add($"- [{finding.Severity}] {finding.Category}: {finding.Message}");
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
            .Where(edge => !string.Equals(edge.SourceId, edge.TargetId, StringComparison.Ordinal))
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var visibleEdges = internalEdges
            .Select(edge => (edge.SourceId, edge.TargetId))
            .Distinct()
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ToArray();

        var nodeIds = new HashSet<string>(visibleEdges.Select(edge => edge.SourceId), StringComparer.Ordinal);
        nodeIds.UnionWith(visibleEdges.Select(edge => edge.TargetId));
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

        foreach (var edge in visibleEdges)
        {
            lines.Add($"    {MakeMermaidNodeId(edge.SourceId)} --> {MakeMermaidNodeId(edge.TargetId)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildProjectMermaid(AnalysisResult result)
    {
        var projectById = result.Projects.ToDictionary(project => project.Id, StringComparer.Ordinal);
        var edges = result.ProjectDependencies
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();

        var nodeIds = new HashSet<string>(result.Projects.Select(project => project.Id), StringComparer.Ordinal);
        nodeIds.UnionWith(edges.Select(edge => edge.TargetId));

        var lines = new List<string>
        {
            "graph TD",
        };

        foreach (var nodeId in nodeIds
            .OrderBy(id => ToStableProjectGraphNodeId(id, projectById), StringComparer.Ordinal))
        {
            var label = projectById.TryGetValue(nodeId, out var project)
                ? project.Name
                : nodeId.StartsWith("package::", StringComparison.Ordinal) ? nodeId["package::".Length..] : nodeId;
            lines.Add($"    {MakeMermaidNodeId(ToStableProjectGraphNodeId(nodeId, projectById))}[{EscapeMermaidLabel(label)}]");
        }

        foreach (var edge in edges
            .OrderBy(edge => ToStableProjectGraphNodeId(edge.SourceId, projectById), StringComparer.Ordinal)
            .ThenBy(edge => ToStableProjectGraphNodeId(edge.TargetId, projectById), StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal))
        {
            lines.Add($"    {MakeMermaidNodeId(ToStableProjectGraphNodeId(edge.SourceId, projectById))} --> {MakeMermaidNodeId(ToStableProjectGraphNodeId(edge.TargetId, projectById))}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildNamespaceMermaid(AnalysisResult result)
    {
        var projectById = result.Projects.ToDictionary(project => project.Id, StringComparer.Ordinal);
        var edges = result.NamespaceDependencies
            .Where(edge => !edge.IsExternal)
            .Where(edge => !string.Equals(edge.SourceId, edge.TargetId, StringComparison.Ordinal))
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var visibleEdges = edges
            .Select(edge => (edge.SourceId, edge.TargetId))
            .Distinct()
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ToArray();

        var nodeIds = new HashSet<string>(visibleEdges.Select(edge => edge.SourceId), StringComparer.Ordinal);
        nodeIds.UnionWith(visibleEdges.Select(edge => edge.TargetId));
        if (nodeIds.Count == 0)
        {
            nodeIds.UnionWith(result.Types
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace))
                .Select(type => $"{type.ProjectId}::{type.Namespace}"));
        }

        var lines = new List<string>
        {
            "graph TD",
        };

        foreach (var nodeId in nodeIds
            .OrderBy(id => ToStableNamespaceGraphNodeId(id, projectById), StringComparer.Ordinal))
        {
            lines.Add($"    {MakeMermaidNodeId(ToStableNamespaceGraphNodeId(nodeId, projectById))}[{EscapeMermaidLabel(BuildNamespaceLabel(nodeId))}]");
        }

        foreach (var edge in visibleEdges
            .OrderBy(edge => ToStableNamespaceGraphNodeId(edge.SourceId, projectById), StringComparer.Ordinal)
            .ThenBy(edge => ToStableNamespaceGraphNodeId(edge.TargetId, projectById), StringComparer.Ordinal))
        {
            lines.Add($"    {MakeMermaidNodeId(ToStableNamespaceGraphNodeId(edge.SourceId, projectById))} --> {MakeMermaidNodeId(ToStableNamespaceGraphNodeId(edge.TargetId, projectById))}");
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

    private static string BuildNamespaceLabel(string namespaceId)
    {
        var separatorIndex = namespaceId.IndexOf("::", StringComparison.Ordinal);
        return separatorIndex >= 0 ? namespaceId[(separatorIndex + 2)..] : namespaceId;
    }

    private static string ToStableProjectGraphNodeId(string nodeId, IReadOnlyDictionary<string, ProjectInfoModel> projectById)
    {
        if (projectById.TryGetValue(nodeId, out var project))
        {
            return $"project::{project.Name}";
        }

        return nodeId.StartsWith("package::", StringComparison.Ordinal)
            ? nodeId
            : $"project::{nodeId}";
    }

    private static string ToStableNamespaceGraphNodeId(string namespaceId, IReadOnlyDictionary<string, ProjectInfoModel> projectById)
    {
        var separatorIndex = namespaceId.IndexOf("::", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return namespaceId;
        }

        var projectId = namespaceId[..separatorIndex];
        var namespaceName = namespaceId[(separatorIndex + 2)..];
        var projectName = projectById.TryGetValue(projectId, out var project) ? project.Name : projectId;
        return $"project::{projectName}::namespace::{namespaceName}";
    }
}
