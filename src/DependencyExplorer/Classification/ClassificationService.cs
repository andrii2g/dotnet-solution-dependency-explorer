using DependencyExplorer.Models;

namespace DependencyExplorer.Classification;

internal sealed class ClassificationService
{
    private static readonly string[] DomainKeywords = ["Order", "Invoice", "Customer", "Money", "Policy", "Rule"];
    private static readonly string[] ApplicationKeywords = ["Handler", "UseCase", "Service", "Command", "Query"];
    private static readonly string[] InfrastructureKeywords = ["Repository", "Client", "Provider", "Gateway", "Adapter"];
    private static readonly string[] PresentationKeywords = ["Controller", "Endpoint", "Host", "Program", "Startup", "Worker"];
    private static readonly string[] InfrastructureSignals = ["EntityFramework", "DbContext", "HttpClient", "Sql", "Dapper", "File", "Storage", "Queue", "Broker"];
    private static readonly string[] PresentationSignals = ["AspNet", "ControllerBase", "Endpoint", "Routing", "Middleware", "Hosting"];

    public void Apply(
        IReadOnlyList<ProjectInfoModel> projects,
        IReadOnlyList<TypeInfoModel> types,
        IReadOnlyList<DependencyEdgeModel> typeDependencies,
        IReadOnlyList<DependencyEdgeModel> diDependencies)
    {
        var outgoingByType = typeDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);
        var diByType = diDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);

        foreach (var type in types)
        {
            type.Classification = ClassifyType(
                type,
                outgoingByType.GetValueOrDefault(type.Id, Array.Empty<DependencyEdgeModel>()),
                diByType.GetValueOrDefault(type.Id, Array.Empty<DependencyEdgeModel>()));
        }

        var projectGroups = types
            .GroupBy(type => type.ProjectId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TypeInfoModel>)group.ToArray(), StringComparer.Ordinal);

        foreach (var project in projects)
        {
            project.Classification = ClassifyProject(project, projectGroups.GetValueOrDefault(project.Id, Array.Empty<TypeInfoModel>()));
        }
    }

    public IReadOnlyList<FindingModel> BuildFindings(
        IReadOnlyList<ProjectInfoModel> projects,
        IReadOnlyList<TypeInfoModel> types,
        IReadOnlyList<DependencyEdgeModel> typeDependencies,
        AnalysisMetrics metrics,
        IReadOnlyList<WorkspaceDiagnosticInfo> diagnostics)
    {
        var findings = new List<FindingModel>();

        findings.AddRange(diagnostics.Select(diagnostic => new FindingModel
        {
            Severity = "warning",
            Category = "workspace",
            SubjectId = diagnostic.Kind,
            Message = diagnostic.Message,
        }));

        findings.AddRange(projects
            .Where(project => project.Classification?.Layer == "Mixed")
            .Select(project => new FindingModel
            {
                Severity = "warning",
                Category = "mixed-project",
                SubjectId = project.Id,
                Message = $"Project '{project.Name}' looks mixed: {string.Join("; ", project.Classification!.Reasons)}",
            }));

        var typeDepsBySource = typeDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);

        findings.AddRange(
            types
                .Where(type => type.Classification?.Layer == "Domain")
                .Where(type => typeDepsBySource.GetValueOrDefault(type.Id, Array.Empty<DependencyEdgeModel>())
                    .Any(edge => edge.IsExternal && ContainsAny(edge.TargetId, InfrastructureSignals)))
                .Select(type => new FindingModel
                {
                    Severity = "warning",
                    Category = "infrastructure-leakage",
                    SubjectId = type.Id,
                    Message = $"Type '{BuildTypeLabel(type)}' looks domain-oriented but depends on infrastructure-style APIs.",
                }));

        findings.AddRange(metrics.TopTypeFanOut
            .Where(metric => metric.Value >= 5)
            .Select(metric => new FindingModel
            {
                Severity = "info",
                Category = "high-coupling",
                SubjectId = metric.Id,
                Message = $"Type '{metric.Label}' has high outgoing coupling ({metric.Value} unique internal targets).",
            }));

        findings.AddRange(projects
            .Where(project => project.PackageReferences.Count >= 5)
            .Select(project => new FindingModel
            {
                Severity = "info",
                Category = "broad-package-usage",
                SubjectId = project.Id,
                Message = $"Project '{project.Name}' references many packages ({project.PackageReferences.Count}).",
            }));

        if (metrics.TypeCount >= 100 || metrics.TypeDependencyCount >= 1000)
        {
            findings.Add(new FindingModel
            {
                Severity = "info",
                Category = "scale",
                SubjectId = "global",
                Message = $"Global analysis scope is large ({metrics.TypeCount} types, {metrics.TypeDependencyCount} type edges).",
            });
        }

        return findings
            .OrderBy(finding => finding.Severity, StringComparer.Ordinal)
            .ThenBy(finding => finding.Category, StringComparer.Ordinal)
            .ThenBy(finding => finding.SubjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static ClassificationInfo ClassifyType(
        TypeInfoModel type,
        IReadOnlyList<DependencyEdgeModel> outgoingDependencies,
        IReadOnlyList<DependencyEdgeModel> constructorDependencies)
    {
        var scores = CreateScoreMap();
        var reasons = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void AddScore(string layer, int value, string reason)
        {
            scores[layer] += value;
            if (!reasons.TryGetValue(layer, out var items))
            {
                items = [];
                reasons[layer] = items;
            }

            items.Add(reason);
        }

        if (ContainsAny(type.Name, DomainKeywords))
        {
            AddScore("Domain", 2, "business-oriented name");
        }

        if (ContainsAny(type.Name, ApplicationKeywords))
        {
            AddScore("Application", 2, "application-style name");
        }

        if (ContainsAny(type.Name, InfrastructureKeywords))
        {
            AddScore("Infrastructure", 2, "infrastructure-style name");
        }

        if (ContainsAny(type.Name, PresentationKeywords))
        {
            AddScore("Presentation", 2, "presentation-style name");
        }

        foreach (var dependency in outgoingDependencies)
        {
            if (ContainsAny(dependency.TargetId, InfrastructureSignals) || ContainsAny(dependency.Label ?? string.Empty, InfrastructureSignals))
            {
                AddScore("Infrastructure", 2, $"dependency on {dependency.Label ?? dependency.TargetId}");
            }

            if (ContainsAny(dependency.TargetId, PresentationSignals) || ContainsAny(dependency.Label ?? string.Empty, PresentationSignals))
            {
                AddScore("Presentation", 2, $"dependency on {dependency.Label ?? dependency.TargetId}");
            }
        }

        if (constructorDependencies.Count >= 2)
        {
            AddScore("Application", 1, "multiple constructor dependencies");
        }

        if (scores["Domain"] > 0 && scores["Infrastructure"] == 0 && scores["Presentation"] == 0)
        {
            AddScore("Domain", 1, "no strong infrastructure or presentation signals");
        }

        return BuildClassification(scores, reasons);
    }

    private static ClassificationInfo ClassifyProject(ProjectInfoModel project, IReadOnlyList<TypeInfoModel> projectTypes)
    {
        var scores = CreateScoreMap();
        var reasons = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void AddScore(string layer, int value, string reason)
        {
            scores[layer] += value;
            if (!reasons.TryGetValue(layer, out var items))
            {
                items = [];
                reasons[layer] = items;
            }

            items.Add(reason);
        }

        foreach (var type in projectTypes)
        {
            var layer = type.Classification?.Layer;
            if (string.IsNullOrWhiteSpace(layer) || layer is "Unknown" or "Mixed")
            {
                continue;
            }

            AddScore(layer, 1, $"{BuildTypeLabel(type)} classified as {layer}");
        }

        foreach (var package in project.PackageReferences)
        {
            if (ContainsAny(package.Name, InfrastructureSignals))
            {
                AddScore("Infrastructure", 2, $"package {package.Name}");
            }

            if (ContainsAny(package.Name, PresentationSignals))
            {
                AddScore("Presentation", 2, $"package {package.Name}");
            }
        }

        return BuildClassification(scores, reasons);
    }

    private static ClassificationInfo BuildClassification(
        IReadOnlyDictionary<string, int> scores,
        IReadOnlyDictionary<string, List<string>> reasons)
    {
        var ordered = scores
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        var top = ordered[0];
        if (top.Value <= 0)
        {
            return new ClassificationInfo
            {
                Layer = "Unknown",
                Confidence = "Low",
                Reasons = ["no strong signals"],
            };
        }

        var strongSignals = ordered.Where(item => item.Value >= 2).ToArray();
        if (strongSignals.Length >= 2)
        {
            return new ClassificationInfo
            {
                Layer = "Mixed",
                Confidence = "Medium",
                Reasons = strongSignals
                    .SelectMany(signal => reasons.GetValueOrDefault(signal.Key, []))
                    .Distinct(StringComparer.Ordinal)
                    .Take(5)
                    .ToArray(),
            };
        }

        return new ClassificationInfo
        {
            Layer = top.Key,
            Confidence = top.Value >= 3 ? "High" : "Medium",
            Reasons = reasons.GetValueOrDefault(top.Key, ["strongest available signal"])
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToArray(),
        };
    }

    private static Dictionary<string, int> CreateScoreMap()
    {
        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Domain"] = 0,
            ["Application"] = 0,
            ["Infrastructure"] = 0,
            ["Presentation"] = 0,
        };
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildTypeLabel(TypeInfoModel type)
    {
        return string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }
}
