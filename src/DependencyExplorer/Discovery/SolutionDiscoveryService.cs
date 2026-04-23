using A2G.DependencyExplorer.Classification;
using System.Xml.Linq;
using A2G.DependencyExplorer.Models;
using A2G.DependencyExplorer.Utils;
using A2G.DependencyExplorer.Workspace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace A2G.DependencyExplorer.Discovery;

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
        var analysisRoot = Path.GetDirectoryName(options.SolutionPath) ?? Environment.CurrentDirectory;
        var projects = new List<ProjectInfoModel>();
        var types = new List<TypeInfoModel>();
        var declaredTypes = new List<DeclaredTypeContext>();
        var discoveredTypeKeys = new HashSet<string>(StringComparer.Ordinal);

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
                FilePath = MakeRelativePath(analysisRoot, project.FilePath),
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

                    var typeModel = CreateTypeModel(symbol, projectId, analysisRoot, document.FilePath);
                    if (discoveredTypeKeys.Add($"{typeModel.ProjectId}|{typeModel.Id}"))
                    {
                        types.Add(typeModel);
                        declaredTypes.Add(new DeclaredTypeContext(symbol, project, document, typeModel));
                    }
                }
            }
        }

        var typeById = types.ToDictionary(type => type.Id, StringComparer.Ordinal);
        var projectById = projects.ToDictionary(project => project.Id, StringComparer.Ordinal);
        var projectDependencies = BuildProjectDependencies(projects);
        var namespaceDependencies = new HashSet<DependencyEdgeModel>(DependencyEdgeComparer.Instance);
        var typeDependencies = new HashSet<DependencyEdgeModel>(DependencyEdgeComparer.Instance);
        var diDependencies = new HashSet<DependencyEdgeModel>(DependencyEdgeComparer.Instance);

        foreach (var declaredType in declaredTypes.OrderBy(context => context.Type.Id, StringComparer.Ordinal))
        {
            AddTypeDependenciesForDeclaredType(
                declaredType,
                namespaceDependencies,
                typeDependencies,
                diDependencies,
                typeById);
        }

        var orderedProjectDependencies = projectDependencies
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var orderedNamespaceDependencies = namespaceDependencies
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var orderedTypeDependencies = typeDependencies
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var orderedDiDependencies = diDependencies
            .OrderBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ThenBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ThenBy(edge => edge.DependencyKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.Label, StringComparer.Ordinal)
            .ToArray();
        var metrics = BuildMetrics(projects, types, orderedProjectDependencies, orderedNamespaceDependencies, orderedTypeDependencies, orderedDiDependencies, typeById);

        if (options.SkipDiGraph)
        {
            orderedDiDependencies = [];
            metrics = BuildMetrics(projects, types, orderedProjectDependencies, orderedNamespaceDependencies, orderedTypeDependencies, orderedDiDependencies, typeById);
        }

        var classificationService = new ClassificationService();
        if (!options.SkipClassification)
        {
            classificationService.Apply(projects, types, orderedTypeDependencies, orderedDiDependencies);
        }

        var findings = classificationService.BuildFindings(
            projects,
            types,
            orderedTypeDependencies,
            metrics,
            workspaceLoadResult.Diagnostics,
            includeClassificationFindings: !options.SkipClassification);

        return new AnalysisResult
        {
            Metadata = new AnalysisMetadata
            {
                ToolVersion = ToolVersion.Value,
                InputPath = MakeRelativePath(analysisRoot, options.SolutionPath),
                InputKind = "solution",
                GeneratedAtUtc = DateTimeOffset.UtcNow,
            },
            Options = new AnalysisOptionsSnapshot
            {
                OutputDirectory = options.OutputDirectory,
                Level = options.Level.ToString(),
                GraphFormat = options.GraphFormat.ToString(),
                Verbose = options.Verbose,
                SkipClassification = options.SkipClassification,
                SkipDiGraph = options.SkipDiGraph,
                FocusProject = options.FocusProject,
                FocusNamespace = options.FocusNamespace,
                FocusClass = options.FocusClass,
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
            ProjectDependencies = orderedProjectDependencies,
            NamespaceDependencies = orderedNamespaceDependencies,
            TypeDependencies = orderedTypeDependencies,
            DiDependencies = orderedDiDependencies,
            Metrics = metrics,
            Findings = findings,
        };
    }

    private static AnalysisMetrics BuildMetrics(
        IReadOnlyList<ProjectInfoModel> projects,
        IReadOnlyList<TypeInfoModel> types,
        IReadOnlyList<DependencyEdgeModel> projectDependencies,
        IReadOnlyList<DependencyEdgeModel> namespaceDependencies,
        IReadOnlyList<DependencyEdgeModel> typeDependencies,
        IReadOnlyList<DependencyEdgeModel> diDependencies,
        IReadOnlyDictionary<string, TypeInfoModel> typeById)
    {
        var internalTypeDependencies = typeDependencies.Where(edge => !edge.IsExternal).ToArray();
        var externalTypeDependencies = typeDependencies.Where(edge => edge.IsExternal).ToArray();

        var fanOut = internalTypeDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .Select(group => new NodeMetric
            {
                Id = group.Key,
                Label = typeById.TryGetValue(group.Key, out var type) ? BuildTypeLabel(type) : group.Key,
                Value = group.Select(edge => edge.TargetId).Distinct(StringComparer.Ordinal).Count(),
            })
            .OrderByDescending(metric => metric.Value)
            .ThenBy(metric => metric.Label, StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        var fanIn = internalTypeDependencies
            .GroupBy(edge => edge.TargetId, StringComparer.Ordinal)
            .Select(group => new NodeMetric
            {
                Id = group.Key,
                Label = typeById.TryGetValue(group.Key, out var type) ? BuildTypeLabel(type) : group.Key,
                Value = group.Select(edge => edge.SourceId).Distinct(StringComparer.Ordinal).Count(),
            })
            .OrderByDescending(metric => metric.Value)
            .ThenBy(metric => metric.Label, StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return new AnalysisMetrics
        {
            ProjectCount = projects.Count,
            PackageReferenceCount = projects.Sum(project => project.PackageReferences.Count),
            DocumentCount = projects.Sum(project => project.DocumentCount),
            TypeCount = types.Count,
            ProjectDependencyCount = projectDependencies.Count,
            NamespaceDependencyCount = namespaceDependencies.Count,
            TypeDependencyCount = typeDependencies.Count,
            InternalTypeDependencyCount = internalTypeDependencies.Length,
            ExternalTypeDependencyCount = externalTypeDependencies.Length,
            DiDependencyCount = diDependencies.Count,
            TopTypeFanOut = fanOut,
            TopTypeFanIn = fanIn,
        };
    }

    private static IReadOnlyList<DependencyEdgeModel> BuildProjectDependencies(IReadOnlyList<ProjectInfoModel> projects)
    {
        var projectByName = projects.ToDictionary(project => project.Name, StringComparer.Ordinal);
        var edges = new HashSet<DependencyEdgeModel>(DependencyEdgeComparer.Instance);

        foreach (var project in projects)
        {
            foreach (var projectReference in project.ProjectReferences)
            {
                if (!projectByName.TryGetValue(projectReference, out var targetProject))
                {
                    continue;
                }

                edges.Add(new DependencyEdgeModel
                {
                    SourceId = project.Id,
                    TargetId = targetProject.Id,
                    SourceKind = "Project",
                    TargetKind = "Project",
                    DependencyKind = "ProjectReference",
                    IsExternal = false,
                    Label = projectReference,
                });
            }

            foreach (var packageReference in project.PackageReferences)
            {
                edges.Add(new DependencyEdgeModel
                {
                    SourceId = project.Id,
                    TargetId = $"package::{packageReference.Name}",
                    SourceKind = "Project",
                    TargetKind = "Package",
                    DependencyKind = "PackageReference",
                    IsExternal = true,
                    Label = packageReference.Version is null ? packageReference.Name : $"{packageReference.Name}@{packageReference.Version}",
                });
            }
        }

        return edges.ToArray();
    }

    private static void AddTypeDependenciesForDeclaredType(
        DeclaredTypeContext declaredType,
        ISet<DependencyEdgeModel> namespaceDependencies,
        ISet<DependencyEdgeModel> typeDependencies,
        ISet<DependencyEdgeModel> diDependencies,
        IReadOnlyDictionary<string, TypeInfoModel> typeById)
    {
        var sourceTypeId = declaredType.Type.Id;
        var sourceNamespace = declaredType.Type.Namespace;
        var sourceProjectId = declaredType.Type.ProjectId;

        void AddEdge(INamedTypeSymbol targetSymbol, string dependencyKind, bool isConstructorDependency = false)
        {
            foreach (var candidate in ExpandNamedTypes(targetSymbol))
            {
                var targetId = candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isInternal = typeById.ContainsKey(targetId);
                var targetNamespace = candidate.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                var targetProjectId = isInternal ? typeById[targetId].ProjectId : $"external-project::{candidate.ContainingAssembly?.Name ?? "unknown"}";

                if (!string.IsNullOrWhiteSpace(targetNamespace) && !string.Equals(sourceNamespace, targetNamespace, StringComparison.Ordinal))
                {
                    namespaceDependencies.Add(new DependencyEdgeModel
                    {
                        SourceId = $"{sourceProjectId}::{sourceNamespace}",
                        TargetId = isInternal ? $"{targetProjectId}::{targetNamespace}" : $"external-namespace::{targetNamespace}",
                        SourceKind = "Namespace",
                        TargetKind = "Namespace",
                        DependencyKind = dependencyKind,
                        IsExternal = !isInternal,
                        Label = targetNamespace,
                    });
                }

                typeDependencies.Add(new DependencyEdgeModel
                {
                    SourceId = sourceTypeId,
                    TargetId = isInternal ? targetId : $"external::{targetId}",
                    SourceKind = "Type",
                    TargetKind = isInternal ? "Type" : "ExternalType",
                    DependencyKind = dependencyKind,
                    IsExternal = !isInternal,
                    Label = candidate.Name,
                });

                if (isConstructorDependency)
                {
                    diDependencies.Add(new DependencyEdgeModel
                    {
                        SourceId = sourceTypeId,
                        TargetId = isInternal ? targetId : $"external::{targetId}",
                        SourceKind = "Type",
                        TargetKind = isInternal ? "Type" : "ExternalType",
                        DependencyKind = "ConstructorParameter",
                        IsExternal = !isInternal,
                        Label = candidate.Name,
                    });
                }
            }
        }

        if (declaredType.Symbol.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            AddEdge(baseType, "BaseType");
        }

        foreach (var implementedInterface in declaredType.Symbol.Interfaces)
        {
            AddEdge(implementedInterface, "ImplementsInterface");
        }

        foreach (var attribute in declaredType.Symbol.GetAttributes())
        {
            if (attribute.AttributeClass is not null)
            {
                AddEdge(attribute.AttributeClass, "AttributeType");
            }
        }

        foreach (var member in declaredType.Symbol.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol field:
                    AddTypeSymbolEdges(field.Type, "FieldType", AddEdge);
                    foreach (var attribute in field.GetAttributes())
                    {
                        if (attribute.AttributeClass is not null)
                        {
                            AddEdge(attribute.AttributeClass, "AttributeType");
                        }
                    }
                    break;

                case IPropertySymbol property:
                    AddTypeSymbolEdges(property.Type, "PropertyType", AddEdge);
                    foreach (var attribute in property.GetAttributes())
                    {
                        if (attribute.AttributeClass is not null)
                        {
                            AddEdge(attribute.AttributeClass, "AttributeType");
                        }
                    }
                    break;

                case IMethodSymbol method when method.MethodKind == MethodKind.Constructor:
                    foreach (var parameter in method.Parameters)
                    {
                        AddTypeSymbolEdges(parameter.Type, "ConstructorParameter", AddEdge, isConstructorDependency: true);
                    }

                    foreach (var attribute in method.GetAttributes())
                    {
                        if (attribute.AttributeClass is not null)
                        {
                            AddEdge(attribute.AttributeClass, "AttributeType");
                        }
                    }
                    break;

                case IMethodSymbol method when method.MethodKind is MethodKind.Ordinary or MethodKind.StaticConstructor:
                    foreach (var parameter in method.Parameters)
                    {
                        AddTypeSymbolEdges(parameter.Type, "MethodParameter", AddEdge);
                    }

                    AddTypeSymbolEdges(method.ReturnType, "MethodReturnType", AddEdge);

                    foreach (var attribute in method.GetAttributes())
                    {
                        if (attribute.AttributeClass is not null)
                        {
                            AddEdge(attribute.AttributeClass, "AttributeType");
                        }
                    }
                    break;
            }
        }
    }

    private static void AddTypeSymbolEdges(
        ITypeSymbol? typeSymbol,
        string dependencyKind,
        Action<INamedTypeSymbol, string, bool> addEdge,
        bool isConstructorDependency = false)
    {
        foreach (var namedType in ExpandNamedTypes(typeSymbol))
        {
            addEdge(namedType, dependencyKind, isConstructorDependency);
        }
    }

    private static IEnumerable<INamedTypeSymbol> ExpandNamedTypes(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            yield break;
        }

        switch (typeSymbol)
        {
            case IArrayTypeSymbol arrayType:
                foreach (var namedType in ExpandNamedTypes(arrayType.ElementType))
                {
                    yield return namedType;
                }

                yield break;

            case IPointerTypeSymbol pointerType:
                foreach (var namedType in ExpandNamedTypes(pointerType.PointedAtType))
                {
                    yield return namedType;
                }

                yield break;

            case INamedTypeSymbol namedType:
                yield return namedType.OriginalDefinition;

                foreach (var typeArgument in namedType.TypeArguments)
                {
                    foreach (var nestedNamedType in ExpandNamedTypes(typeArgument))
                    {
                        yield return nestedNamedType.OriginalDefinition;
                    }
                }

                yield break;
        }
    }

    private static string BuildTypeLabel(TypeInfoModel type)
    {
        return string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    private static TypeInfoModel CreateTypeModel(INamedTypeSymbol symbol, string projectId, string analysisRoot, string? filePath)
    {
        return new TypeInfoModel
        {
            Id = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            ProjectId = projectId,
            Kind = symbol.TypeKind.ToString(),
            Accessibility = symbol.DeclaredAccessibility.ToString(),
            FilePath = MakeRelativePath(analysisRoot, filePath),
        };
    }

    private static string MakeRelativePath(string analysisRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetRelativePath(analysisRoot, path).Replace('\\', '/');
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

internal sealed record DeclaredTypeContext(
    INamedTypeSymbol Symbol,
    Project Project,
    Document Document,
    TypeInfoModel Type);

internal sealed class DependencyEdgeComparer : IEqualityComparer<DependencyEdgeModel>
{
    public static DependencyEdgeComparer Instance { get; } = new();

    public bool Equals(DependencyEdgeModel? x, DependencyEdgeModel? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return string.Equals(x.SourceId, y.SourceId, StringComparison.Ordinal) &&
               string.Equals(x.TargetId, y.TargetId, StringComparison.Ordinal) &&
               string.Equals(x.SourceKind, y.SourceKind, StringComparison.Ordinal) &&
               string.Equals(x.TargetKind, y.TargetKind, StringComparison.Ordinal) &&
               string.Equals(x.DependencyKind, y.DependencyKind, StringComparison.Ordinal) &&
               string.Equals(x.Label, y.Label, StringComparison.Ordinal) &&
               x.IsExternal == y.IsExternal;
    }

    public int GetHashCode(DependencyEdgeModel obj)
    {
        return HashCode.Combine(
            obj.SourceId,
            obj.TargetId,
            obj.SourceKind,
            obj.TargetKind,
            obj.DependencyKind,
            obj.Label,
            obj.IsExternal);
    }
}
