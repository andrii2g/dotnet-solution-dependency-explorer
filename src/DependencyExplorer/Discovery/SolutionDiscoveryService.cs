using System.Xml.Linq;
using DependencyExplorer.Models;
using DependencyExplorer.Utils;
using DependencyExplorer.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DependencyExplorer.Discovery;

internal sealed class SolutionDiscoveryService
{
    private readonly HashSet<string> _supportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        LanguageNames.CSharp,
    };

    public async Task<AnalysisResult> DiscoverAsync(
        WorkspaceLoadResult workspaceLoadResult,
        Cli.AnalyzeCommandOptions options,
        CancellationToken cancellationToken)
    {
        var projects = new List<ProjectInfoModel>();
        var types = new List<TypeInfoModel>();

        foreach (var project in workspaceLoadResult.Projects.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (!_supportedLanguages.Contains(project.Language))
            {
                continue;
            }

            var projectId = project.Id.Id.ToString();
            var packageReferences = await ReadPackageReferencesAsync(project.FilePath, cancellationToken);
            var targetFrameworks = await ReadTargetFrameworksAsync(project.FilePath, cancellationToken);

            projects.Add(new ProjectInfoModel
            {
                Id = projectId,
                Name = project.Name,
                FilePath = project.FilePath ?? string.Empty,
                Language = project.Language,
                TargetFrameworks = targetFrameworks,
                ProjectReferences = project.ProjectReferences
                    .Select(reference => workspaceLoadResult.GetProjectName(reference.ProjectId))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()!,
                PackageReferences = packageReferences,
                DocumentCount = project.Documents.Count(),
            });

            foreach (var document in project.Documents.OrderBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
                if (syntaxRoot is null)
                {
                    continue;
                }

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                if (semanticModel is null)
                {
                    continue;
                }

                foreach (var declaration in syntaxRoot.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    types.Add(CreateTypeModel(symbol, projectId, document.FilePath));
                }

                foreach (var declaration in syntaxRoot.DescendantNodes().OfType<EnumDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    types.Add(CreateTypeModel(symbol, projectId, document.FilePath));
                }
            }
        }

        return new AnalysisResult
        {
            Metadata = new AnalysisMetadata
            {
                ToolVersion = ToolVersion.Value,
                InputPath = options.SolutionPath,
                InputKind = "solution",
                GeneratedAtUtc = DateTimeOffset.UtcNow,
            },
            Options = new AnalysisOptionsSnapshot
            {
                OutputDirectory = options.OutputDirectory,
                Level = options.Level.ToString(),
                GraphFormat = options.GraphFormat.ToString(),
                Verbose = options.Verbose,
            },
            Diagnostics = workspaceLoadResult.Diagnostics
                .OrderBy(d => d.Kind, StringComparer.Ordinal)
                .ThenBy(d => d.Message, StringComparer.Ordinal)
                .ToArray(),
            Projects = projects
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToArray(),
            Types = types
                .OrderBy(t => t.Namespace, StringComparer.Ordinal)
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .ThenBy(t => t.ProjectId, StringComparer.Ordinal)
                .ToArray(),
        };
    }

    private static TypeInfoModel CreateTypeModel(INamedTypeSymbol symbol, string projectId, string? filePath)
    {
        return new TypeInfoModel
        {
            Id = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            ProjectId = projectId,
            Kind = symbol.TypeKind.ToString(),
            Accessibility = symbol.DeclaredAccessibility.ToString(),
            FilePath = filePath,
        };
    }

    private static async Task<IReadOnlyList<string>> ReadTargetFrameworksAsync(string? projectFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath) || !File.Exists(projectFilePath))
        {
            return Array.Empty<string>();
        }

        await using var stream = File.OpenRead(projectFilePath);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        var frameworks = document
            .Descendants()
            .Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .SelectMany(element => (element.Value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return frameworks;
    }

    private static async Task<IReadOnlyList<PackageReferenceModel>> ReadPackageReferencesAsync(string? projectFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath) || !File.Exists(projectFilePath))
        {
            return Array.Empty<PackageReferenceModel>();
        }

        await using var stream = File.OpenRead(projectFilePath);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => new PackageReferenceModel
            {
                Name = element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty,
                Version = element.Attribute("Version")?.Value
                    ?? element.Elements().FirstOrDefault(child => child.Name.LocalName == "Version")?.Value,
            })
            .Where(package => !string.IsNullOrWhiteSpace(package.Name))
            .OrderBy(package => package.Name, StringComparer.Ordinal)
            .ThenBy(package => package.Version, StringComparer.Ordinal)
            .ToArray();
    }
}
