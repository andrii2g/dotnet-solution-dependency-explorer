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

        var json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(analysisJsonPath, json, cancellationToken);
        await File.WriteAllTextAsync(summaryPath, BuildSummary(result), cancellationToken);
    }

    private static string BuildSummary(AnalysisResult result)
    {
        var projectCount = result.Projects.Count;
        var packageCount = result.Projects.Sum(project => project.PackageReferences.Count);
        var documentCount = result.Projects.Sum(project => project.DocumentCount);
        var typeCount = result.Types.Count;

        var lines = new List<string>
        {
            "# Dependency Explorer Summary",
            string.Empty,
            $"Input path: `{result.Metadata.InputPath}`",
            string.Empty,
            "## Counts",
            string.Empty,
            $"- Projects: {projectCount}",
            $"- Package references: {packageCount}",
            $"- Documents: {documentCount}",
            $"- Named types: {typeCount}",
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

        return string.Join(Environment.NewLine, lines);
    }
}
