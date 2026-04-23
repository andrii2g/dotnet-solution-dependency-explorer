using System.Text.Json;
using A2G.DependencyExplorer.Models;

namespace A2G.DependencyExplorer.Export;

internal sealed class AnalysisResultWriter
{
    private const int MermaidEdgeWarningThreshold = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public async Task WriteAsync(AnalysisResult result, string outputDirectory, CancellationToken cancellationToken)
    {
        var analysisJsonPath = Path.Combine(outputDirectory, "analysis.json");
        var reportPath = Path.Combine(outputDirectory, "report.md");
        var summaryPath = Path.Combine(outputDirectory, "summary.md");
        var inventoryPath = Path.Combine(outputDirectory, "inventory.md");
        var violationsPath = Path.Combine(outputDirectory, "violations.md");
        var projectGraphPath = Path.Combine(outputDirectory, "graph-projects.mmd");
        var namespaceGraphPath = Path.Combine(outputDirectory, "graph-namespaces.mmd");
        var globalClassGraphPath = Path.Combine(outputDirectory, "graph-classes-global.mmd");
        var focusedClassGraphPath = Path.Combine(outputDirectory, "graph-classes-focused.mmd");
        var globalDiGraphPath = Path.Combine(outputDirectory, "graph-di-global.mmd");
        var focusedDiGraphPath = Path.Combine(outputDirectory, "graph-di-focused.mmd");

        var json = JsonSerializer.Serialize(result, JsonOptions);
        var summary = BuildSummary(result);
        var inventory = BuildInventory(result);
        var violations = BuildViolations(result);
        var projectGraph = ShouldEmitProjectGraph(result) ? BuildProjectMermaid(result) : null;
        var namespaceGraph = ShouldEmitNamespaceGraph(result) ? BuildNamespaceMermaid(result) : null;
        var globalClassGraph = BuildGlobalClassMermaid(result);
        var globalDiGraph = !result.Options.SkipDiGraph ? BuildDiMermaid(result, focused: false) : null;
        var focusedClassGraph = HasFocus(result) ? BuildFocusedClassMermaid(result) : null;
        var focusedDiGraph = HasFocus(result) && !result.Options.SkipDiGraph ? BuildDiMermaid(result, focused: true) : null;

        await File.WriteAllTextAsync(analysisJsonPath, json, cancellationToken);
        await File.WriteAllTextAsync(summaryPath, summary, cancellationToken);
        await File.WriteAllTextAsync(inventoryPath, inventory, cancellationToken);
        await File.WriteAllTextAsync(violationsPath, violations, cancellationToken);
        await File.WriteAllTextAsync(
            reportPath,
            BuildCombinedReport(
                result,
                summary,
                inventory,
                violations,
                projectGraph,
                namespaceGraph,
                globalClassGraph,
                globalDiGraph,
                focusedClassGraph,
                focusedDiGraph),
            cancellationToken);
        if (projectGraph is not null)
        {
            await File.WriteAllTextAsync(projectGraphPath, projectGraph, cancellationToken);
        }

        if (namespaceGraph is not null)
        {
            await File.WriteAllTextAsync(namespaceGraphPath, namespaceGraph, cancellationToken);
        }

        await File.WriteAllTextAsync(globalClassGraphPath, globalClassGraph, cancellationToken);
        if (globalDiGraph is not null)
        {
            await File.WriteAllTextAsync(globalDiGraphPath, globalDiGraph, cancellationToken);
        }

        if (focusedClassGraph is not null)
        {
            await File.WriteAllTextAsync(focusedClassGraphPath, focusedClassGraph, cancellationToken);
            if (focusedDiGraph is not null)
            {
                await File.WriteAllTextAsync(focusedDiGraphPath, focusedDiGraph, cancellationToken);
            }
        }

        await WriteRunnableProjectReportsAsync(result, outputDirectory, cancellationToken);
    }

    private static async Task WriteRunnableProjectReportsAsync(AnalysisResult result, string outputDirectory, CancellationToken cancellationToken)
    {
        var runnableProjects = result.Projects
            .Where(project => project.IsRunnable)
            .OrderBy(project => project.Name, StringComparer.Ordinal)
            .ToArray();

        if (runnableProjects.Length == 0)
        {
            return;
        }

        var reportsRoot = Path.Combine(outputDirectory, "reports");
        Directory.CreateDirectory(reportsRoot);

        foreach (var project in runnableProjects)
        {
            var projectDirectory = Path.Combine(reportsRoot, SanitizePathSegment(project.Name));
            Directory.CreateDirectory(projectDirectory);

            var focusedTypeIds = result.Types
                .Where(type => string.Equals(type.ProjectId, project.Id, StringComparison.Ordinal))
                .Select(type => type.Id)
                .ToHashSet(StringComparer.Ordinal);

            var classGraph = BuildTypeMermaid(result, focusedTypeIds);
            var diGraph = result.Options.SkipDiGraph ? null : BuildDiMermaid(result, focusedTypeIds);
            var neighborhoodGraph = BuildProjectNeighborhoodMermaid(result, project);
            var report = BuildRunnableProjectReport(result, project, focusedTypeIds, classGraph, diGraph, neighborhoodGraph);

            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "report.md"), report, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "graph-classes-focused.mmd"), classGraph, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(projectDirectory, "graph-project-neighborhood.mmd"), neighborhoodGraph, cancellationToken);

            if (diGraph is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(projectDirectory, "graph-di-focused.mmd"), diGraph, cancellationToken);
            }
        }
    }

    private static string BuildCombinedReport(
        AnalysisResult result,
        string summary,
        string inventory,
        string violations,
        string? projectGraph,
        string? namespaceGraph,
        string globalClassGraph,
        string? globalDiGraph,
        string? focusedClassGraph,
        string? focusedDiGraph)
    {
        var sections = new List<string>
        {
            "# Dependency Explorer Report",
            string.Empty,
            $"Generated from `{result.Metadata.InputPath}`.",
            string.Empty,
        };

        AppendRenderWarnings(sections, result, "##");

        sections.AddRange(
        [
            "## Summary",
            string.Empty,
            TrimMarkdownHeading(summary),
            string.Empty,
            "## Inventory",
            string.Empty,
            TrimMarkdownHeading(inventory),
            string.Empty,
            "## Findings",
            string.Empty,
            TrimMarkdownHeading(violations),
            string.Empty,
        ]);

        if (projectGraph is not null)
        {
            AppendMermaidSection(sections, "Project Graph", projectGraph);
        }

        if (namespaceGraph is not null)
        {
            AppendMermaidSection(sections, "Namespace Graph", namespaceGraph);
        }

        AppendMermaidSection(sections, "Global Class Graph", globalClassGraph);

        if (globalDiGraph is not null)
        {
            AppendMermaidSection(sections, "Global DI Graph", globalDiGraph);
        }

        if (focusedClassGraph is not null)
        {
            AppendMermaidSection(sections, "Focused Class Graph", focusedClassGraph);
        }

        if (focusedDiGraph is not null)
        {
            AppendMermaidSection(sections, "Focused DI Graph", focusedDiGraph);
        }

        return string.Join(Environment.NewLine, sections);
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
            "## Scope",
            string.Empty,
            $"- Level: {result.Options.Level}",
            $"- Focus project: {result.Options.FocusProject ?? "none"}",
            $"- Focus namespace: {result.Options.FocusNamespace ?? "none"}",
            $"- Focus class: {result.Options.FocusClass ?? "none"}",
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

        AppendRenderWarnings(lines, result, "##");

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

    private static string BuildRunnableProjectReport(
        AnalysisResult result,
        ProjectInfoModel project,
        ISet<string> focusedTypeIds,
        string classGraph,
        string? diGraph,
        string neighborhoodGraph)
    {
        var internalTypeEdgeCount = result.TypeDependencies
            .Where(edge => !edge.IsExternal &&
                           string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                           string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal) &&
                           focusedTypeIds.Contains(edge.SourceId) &&
                           focusedTypeIds.Contains(edge.TargetId) &&
                           !string.Equals(edge.SourceId, edge.TargetId, StringComparison.Ordinal))
            .Select(edge => (edge.SourceId, edge.TargetId))
            .Distinct()
            .Count();

        var diEdgeCount = result.Options.SkipDiGraph
            ? 0
            : result.DiDependencies
                .Where(edge => !edge.IsExternal &&
                               string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                               string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal) &&
                               focusedTypeIds.Contains(edge.SourceId) &&
                               focusedTypeIds.Contains(edge.TargetId))
                .Select(edge => (edge.SourceId, edge.TargetId))
                .Distinct()
                .Count();

        var lines = new List<string>
        {
            $"# {project.Name} Report",
            string.Empty,
            $"Generated from `{result.Metadata.InputPath}`.",
            string.Empty,
            "## Project",
            string.Empty,
            $"- Name: `{project.Name}`",
            $"- Path: `{project.FilePath}`",
            $"- SDK: {project.Sdk ?? "unknown"}",
            $"- Output type: {project.OutputType ?? "unknown"}",
            $"- Runnable: {(project.IsRunnable ? "yes" : "no")}",
            $"- Frameworks: {(project.TargetFrameworks.Count == 0 ? "unknown" : string.Join(", ", project.TargetFrameworks))}",
            $"- Documents: {project.DocumentCount}",
            $"- Package references: {(project.PackageReferences.Count == 0 ? "none" : string.Join(", ", project.PackageReferences.Select(package => package.Version is null ? package.Name : $"{package.Name}@{package.Version}")))}",
            $"- Project references: {(project.ProjectReferences.Count == 0 ? "none" : string.Join(", ", project.ProjectReferences))}",
            string.Empty,
            "## Focused Counts",
            string.Empty,
            $"- Project types: {focusedTypeIds.Count}",
            $"- Internal class dependency edges: {internalTypeEdgeCount}",
            $"- Internal constructor DI edges: {diEdgeCount}",
            string.Empty,
            "This report is a runnable-project slice of the global analysis. Use the root `report.md` for the full system view.",
            string.Empty,
        };

        var warnings = BuildProjectRenderWarnings(project.Name, internalTypeEdgeCount, diEdgeCount, result.Options.SkipDiGraph);
        if (warnings.Count > 0)
        {
            lines.Add("## Graph Rendering Warnings");
            lines.Add(string.Empty);
            foreach (var warning in warnings)
            {
                lines.Add($"- {warning}");
            }

            lines.Add(string.Empty);
        }

        AppendMermaidSection(lines, "Project Neighborhood Graph", neighborhoodGraph);
        AppendMermaidSection(lines, "Focused Class Graph", classGraph);
        if (diGraph is not null)
        {
            AppendMermaidSection(lines, "Focused DI Graph", diGraph);
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
        return BuildTypeMermaid(result, GetFocusedTypeIds(result, focused: false));
    }

    private static string BuildFocusedClassMermaid(AnalysisResult result)
    {
        return BuildTypeMermaid(result, GetFocusedTypeIds(result, focused: true));
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

    private static string BuildTypeMermaid(AnalysisResult result, ISet<string>? focusTypeIds)
    {
        var typeById = result.Types
            .GroupBy(type => type.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var internalEdges = result.TypeDependencies
            .Where(edge => !edge.IsExternal &&
                           string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                           string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal))
            .Where(edge => !string.Equals(edge.SourceId, edge.TargetId, StringComparison.Ordinal))
            .Where(edge => focusTypeIds is null || (focusTypeIds.Contains(edge.SourceId) && focusTypeIds.Contains(edge.TargetId)))
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
            nodeIds.UnionWith((focusTypeIds ?? result.Types.Select(type => type.Id).ToHashSet(StringComparer.Ordinal)));
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

    private static string BuildDiMermaid(AnalysisResult result, bool focused)
    {
        var typeById = result.Types
            .GroupBy(type => type.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var focusTypeIds = GetFocusedTypeIds(result, focused);
        return BuildDiMermaid(result, focusTypeIds, typeById);
    }

    private static string BuildDiMermaid(AnalysisResult result, ISet<string>? focusTypeIds)
    {
        var typeById = result.Types
            .GroupBy(type => type.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        return BuildDiMermaid(result, focusTypeIds, typeById);
    }

    private static string BuildDiMermaid(
        AnalysisResult result,
        ISet<string>? focusTypeIds,
        IReadOnlyDictionary<string, TypeInfoModel> typeById)
    {
        var visibleEdges = result.DiDependencies
            .Where(edge => !edge.IsExternal &&
                           string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                           string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal))
            .Where(edge => focusTypeIds is null || (focusTypeIds.Contains(edge.SourceId) && focusTypeIds.Contains(edge.TargetId)))
            .Select(edge => (edge.SourceId, edge.TargetId))
            .Distinct()
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ToArray();

        var nodeIds = new HashSet<string>(visibleEdges.Select(edge => edge.SourceId), StringComparer.Ordinal);
        nodeIds.UnionWith(visibleEdges.Select(edge => edge.TargetId));
        if (nodeIds.Count == 0 && focusTypeIds is not null)
        {
            nodeIds.UnionWith(focusTypeIds);
        }

        var lines = new List<string>
        {
            "graph TD",
        };

        foreach (var nodeId in nodeIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var label = typeById.TryGetValue(nodeId, out var type)
                ? BuildTypeLabel(type)
                : nodeId;
            lines.Add($"    {MakeMermaidNodeId(nodeId)}[{EscapeMermaidLabel(label)}]");
        }

        foreach (var edge in visibleEdges)
        {
            lines.Add($"    {MakeMermaidNodeId(edge.SourceId)} --> {MakeMermaidNodeId(edge.TargetId)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildTypeLabel(TypeInfoModel type)
    {
        return string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    private static string BuildProjectNeighborhoodMermaid(AnalysisResult result, ProjectInfoModel project)
    {
        var projectById = result.Projects.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var connectedProjectIds = new HashSet<string>(StringComparer.Ordinal)
        {
            project.Id,
        };

        foreach (var edge in result.ProjectDependencies.Where(edge => !edge.IsExternal))
        {
            if (string.Equals(edge.SourceId, project.Id, StringComparison.Ordinal) ||
                string.Equals(edge.TargetId, project.Id, StringComparison.Ordinal))
            {
                connectedProjectIds.Add(edge.SourceId);
                connectedProjectIds.Add(edge.TargetId);
            }
        }

        var edges = result.ProjectDependencies
            .Where(edge => !edge.IsExternal &&
                           connectedProjectIds.Contains(edge.SourceId) &&
                           connectedProjectIds.Contains(edge.TargetId))
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string>
        {
            "graph TD",
        };

        foreach (var projectId in connectedProjectIds
            .OrderBy(id => ToStableProjectGraphNodeId(id, projectById), StringComparer.Ordinal))
        {
            var projectLabel = projectById.TryGetValue(projectId, out var item) ? item.Name : projectId;
            lines.Add($"    {MakeMermaidNodeId(ToStableProjectGraphNodeId(projectId, projectById))}[{EscapeMermaidLabel(projectLabel)}]");
        }

        foreach (var edge in edges)
        {
            lines.Add($"    {MakeMermaidNodeId(ToStableProjectGraphNodeId(edge.SourceId, projectById))} --> {MakeMermaidNodeId(ToStableProjectGraphNodeId(edge.TargetId, projectById))}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> BuildProjectRenderWarnings(string projectName, int classEdgeCount, int diEdgeCount, bool skipDiGraph)
    {
        var warnings = new List<string>();
        if (classEdgeCount >= MermaidEdgeWarningThreshold)
        {
            warnings.Add($"The focused class graph for `{projectName}` was generated with {classEdgeCount} edges and may exceed Mermaid render limits in some UIs.");
        }

        if (!skipDiGraph && diEdgeCount >= MermaidEdgeWarningThreshold)
        {
            warnings.Add($"The focused DI graph for `{projectName}` was generated with {diEdgeCount} edges and may exceed Mermaid render limits in some UIs.");
        }

        return warnings;
    }

    private static void AppendRenderWarnings(List<string> lines, AnalysisResult result, string headingLevel)
    {
        var warnings = BuildRenderWarnings(result);
        if (warnings.Count == 0)
        {
            return;
        }

        lines.Add($"{headingLevel} Graph Rendering Warnings");
        lines.Add(string.Empty);

        foreach (var warning in warnings)
        {
            lines.Add($"- {warning}");
        }

        lines.Add(string.Empty);
    }

    private static IReadOnlyList<string> BuildRenderWarnings(AnalysisResult result)
    {
        var warnings = new List<string>();
        var globalClassEdgeCount = result.TypeDependencies
            .Where(edge => !edge.IsExternal &&
                           string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                           string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal) &&
                           !string.Equals(edge.SourceId, edge.TargetId, StringComparison.Ordinal))
            .Select(edge => (edge.SourceId, edge.TargetId))
            .Distinct()
            .Count();

        if (globalClassEdgeCount >= MermaidEdgeWarningThreshold)
        {
            warnings.Add($"The full global class graph was generated with {globalClassEdgeCount} edges and may exceed Mermaid render limits in some UIs.");
        }

        if (!result.Options.SkipDiGraph)
        {
            var globalDiEdgeCount = result.DiDependencies
                .Where(edge => !edge.IsExternal &&
                               string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                               string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal))
                .Select(edge => (edge.SourceId, edge.TargetId))
                .Distinct()
                .Count();

            if (globalDiEdgeCount >= MermaidEdgeWarningThreshold)
            {
                warnings.Add($"The full global DI graph was generated with {globalDiEdgeCount} edges and may exceed Mermaid render limits in some UIs.");
            }
        }

        return warnings;
    }

    private static void AppendMermaidSection(List<string> sections, string title, string graph)
    {
        sections.Add($"## {title}");
        sections.Add(string.Empty);
        sections.Add("```mermaid");
        sections.AddRange(graph.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        sections.Add("```");
        sections.Add(string.Empty);
    }

    private static string TrimMarkdownHeading(string markdown)
    {
        var lines = markdown
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .ToList();

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        if (lines.Count > 0 && lines[0].StartsWith("# ", StringComparison.Ordinal))
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        return string.Join(Environment.NewLine, lines);
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

    private static string SanitizePathSegment(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-');
        }

        return builder.ToString().Trim('-');
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

    private static bool ShouldEmitProjectGraph(AnalysisResult result)
    {
        return result.Options.Level is nameof(Cli.AnalysisLevel.Project) or nameof(Cli.AnalysisLevel.All);
    }

    private static bool ShouldEmitNamespaceGraph(AnalysisResult result)
    {
        return result.Options.Level is nameof(Cli.AnalysisLevel.Namespace) or nameof(Cli.AnalysisLevel.All);
    }

    private static bool HasFocus(AnalysisResult result)
    {
        return !string.IsNullOrWhiteSpace(result.Options.FocusProject) ||
               !string.IsNullOrWhiteSpace(result.Options.FocusNamespace) ||
               !string.IsNullOrWhiteSpace(result.Options.FocusClass);
    }

    private static ISet<string>? GetFocusedTypeIds(AnalysisResult result, bool focused)
    {
        if (!focused || !HasFocus(result))
        {
            return null;
        }

        var matchingTypes = result.Types.Where(type => MatchesFocus(type, result)).Select(type => type.Id).ToHashSet(StringComparer.Ordinal);
        return matchingTypes;
    }

    private static bool MatchesFocus(TypeInfoModel type, AnalysisResult result)
    {
        var projectMatches = string.IsNullOrWhiteSpace(result.Options.FocusProject) ||
            result.Projects.Any(project =>
                string.Equals(project.Id, type.ProjectId, StringComparison.Ordinal) &&
                string.Equals(project.Name, result.Options.FocusProject, StringComparison.OrdinalIgnoreCase));

        var namespaceMatches = string.IsNullOrWhiteSpace(result.Options.FocusNamespace) ||
            type.Namespace.StartsWith(result.Options.FocusNamespace!, StringComparison.OrdinalIgnoreCase);

        var typeLabel = BuildTypeLabel(type);
        var focusClass = result.Options.FocusClass;
        var classMatches = string.IsNullOrWhiteSpace(focusClass) ||
            string.Equals(type.Id, focusClass, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type.Id, $"global::{focusClass}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(typeLabel, focusClass, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type.Name, focusClass, StringComparison.OrdinalIgnoreCase);

        return projectMatches && namespaceMatches && classMatches;
    }
}
