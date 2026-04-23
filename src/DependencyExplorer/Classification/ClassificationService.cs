using A2G.DependencyExplorer.Models;

namespace A2G.DependencyExplorer.Classification;

internal sealed class ClassificationService
{
    private static readonly string[] DomainKeywords = ["Order", "Invoice", "Customer", "Money", "Policy", "Rule"];
    private static readonly string[] ApplicationKeywords = ["Handler", "UseCase", "Service", "Command", "Query"];
    private static readonly string[] InfrastructureKeywords = ["Repository", "Client", "Provider", "Gateway", "Adapter"];
    private static readonly string[] PresentationKeywords = ["Controller", "Endpoint", "Host", "Program", "Startup", "Worker"];
    private static readonly string[] InfrastructureSignals = ["EntityFramework", "DbContext", "HttpClient", "Sql", "Dapper", "File", "Storage", "Queue", "Broker"];
    private static readonly string[] PresentationSignals = ["AspNet", "ControllerBase", "Endpoint", "Routing", "Middleware", "Hosting"];
    private static readonly string[] DomainNamespaceSignals = ["Domain", "Policies", "Rules", "Entities", "Models"];
    private static readonly string[] ApplicationNamespaceSignals = ["Application", "UseCases", "Handlers", "Commands", "Queries", "Abstractions", "Contracts"];
    private static readonly string[] InfrastructureNamespaceSignals = ["Infrastructure", "Data", "Persistence", "Files", "Storage", "Redis", "Mq", "Messaging", "Notifications"];
    private static readonly string[] PresentationNamespaceSignals = ["Api", "Web", "Endpoints", "Controllers", "Hosts", "Workers"];
    private static readonly string[] AbstractionKeywords = ["Abstraction", "Contract", "Interface"];

    public void Apply(
        IReadOnlyList<ProjectInfoModel> projects,
        IReadOnlyList<TypeInfoModel> types,
        IReadOnlyList<DependencyEdgeModel> typeDependencies,
        IReadOnlyList<DependencyEdgeModel> diDependencies)
    {
        var outgoingByType = typeDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);
        var incomingByType = typeDependencies
            .GroupBy(edge => edge.TargetId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);
        var diByType = diDependencies
            .GroupBy(edge => edge.SourceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<DependencyEdgeModel>)group.ToArray(), StringComparer.Ordinal);

        foreach (var type in types)
        {
            type.Classification = ClassifyType(
                type,
                outgoingByType.GetValueOrDefault(type.Id, Array.Empty<DependencyEdgeModel>()),
                incomingByType.GetValueOrDefault(type.Id, Array.Empty<DependencyEdgeModel>()),
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
        IReadOnlyList<DependencyEdgeModel> projectDependencies,
        IReadOnlyList<DependencyEdgeModel> namespaceDependencies,
        IReadOnlyList<DependencyEdgeModel> typeDependencies,
        AnalysisMetrics metrics,
        IReadOnlyList<WorkspaceDiagnosticInfo> diagnostics,
        bool includeClassificationFindings)
    {
        var findings = new List<FindingModel>();
        var projectById = projects.ToDictionary(project => project.Id, StringComparer.Ordinal);
        var typeById = types
            .GroupBy(type => type.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        findings.AddRange(diagnostics.Select(diagnostic => new FindingModel
        {
            Severity = "warning",
            Category = "workspace",
            SubjectId = diagnostic.Kind,
            Message = diagnostic.Message,
        }));

        if (includeClassificationFindings)
        {
            findings.AddRange(projects
                .Where(project => project.Classification?.Layer == "Mixed")
                .Select(project => new FindingModel
                {
                    Severity = "warning",
                    Category = "mixed-project",
                    SubjectId = project.Name,
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
        }

        findings.AddRange(BuildCycleFindings(
            projectDependencies
                .Where(edge => !edge.IsExternal &&
                               string.Equals(edge.SourceKind, "Project", StringComparison.Ordinal) &&
                               string.Equals(edge.TargetKind, "Project", StringComparison.Ordinal))
                .Select(edge => (edge.SourceId, edge.TargetId)),
            "project-cycle",
            "warning",
            sourceId => projectById.TryGetValue(sourceId, out var project) ? project.Name : sourceId,
            5));

        findings.AddRange(BuildCycleFindings(
            namespaceDependencies
                .Where(edge => !edge.IsExternal &&
                               string.Equals(edge.SourceKind, "Namespace", StringComparison.Ordinal) &&
                               string.Equals(edge.TargetKind, "Namespace", StringComparison.Ordinal))
                .Select(edge => (edge.SourceId, edge.TargetId)),
            "namespace-cycle",
            "warning",
            BuildNamespaceLabel,
            5));

        findings.AddRange(BuildCycleFindings(
            typeDependencies
                .Where(edge => !edge.IsExternal &&
                               string.Equals(edge.SourceKind, "Type", StringComparison.Ordinal) &&
                               string.Equals(edge.TargetKind, "Type", StringComparison.Ordinal))
                .Select(edge => (edge.SourceId, edge.TargetId)),
            "type-cycle",
            "warning",
            sourceId => typeById.TryGetValue(sourceId, out var type) ? BuildTypeLabel(type) : sourceId,
            8));

        findings.AddRange(metrics.TopTypeFanOut
            .Where(metric => metrics.OutgoingHubThreshold > 0 && metric.Value >= metrics.OutgoingHubThreshold)
            .Select(metric => new FindingModel
            {
                Severity = "info",
                Category = "outgoing-hub",
                SubjectId = metric.Id,
                Message = $"Type '{metric.Label}' is an outgoing dependency hub with {metric.Value} unique internal targets.",
            }));

        findings.AddRange(metrics.TopTypeFanIn
            .Where(metric => metrics.IncomingHubThreshold > 0 && metric.Value >= metrics.IncomingHubThreshold)
            .Select(metric => new FindingModel
            {
                Severity = "info",
                Category = "incoming-hub",
                SubjectId = metric.Id,
                Message = $"Type '{metric.Label}' is an incoming dependency hub with {metric.Value} unique internal dependents.",
            }));

        findings.AddRange(projects
            .Where(project => project.PackageReferences.Count >= 5)
            .Select(project => new FindingModel
            {
                Severity = "info",
                Category = "broad-package-usage",
                SubjectId = project.Name,
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
            .OrderByDescending(finding => SeverityRank(finding.Severity))
            .ThenBy(finding => finding.Category, StringComparer.Ordinal)
            .ThenBy(finding => finding.SubjectId, StringComparer.Ordinal)
            .ThenBy(finding => finding.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static ClassificationInfo ClassifyType(
        TypeInfoModel type,
        IReadOnlyList<DependencyEdgeModel> outgoingDependencies,
        IReadOnlyList<DependencyEdgeModel> incomingDependencies,
        IReadOnlyList<DependencyEdgeModel> constructorDependencies)
    {
        var scores = CreateScoreMap();
        var reasons = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var namespaceLabel = string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
        var isInterface = string.Equals(type.Kind, "Interface", StringComparison.OrdinalIgnoreCase);
        var isPresentationNamespace = ContainsNamespaceSignal(type.Namespace, PresentationNamespaceSignals);
        var isApplicationNamespace = ContainsNamespaceSignal(type.Namespace, ApplicationNamespaceSignals);
        var isInfrastructureNamespace = ContainsNamespaceSignal(type.Namespace, InfrastructureNamespaceSignals);
        var isDomainNamespace = ContainsNamespaceSignal(type.Namespace, DomainNamespaceSignals);
        var isPresentationTypeName = ContainsAny(type.Name, PresentationKeywords);
        var isApplicationTypeName = ContainsAny(type.Name, ApplicationKeywords);
        var isInfrastructureTypeName = ContainsAny(type.Name, InfrastructureKeywords);
        var hasBusinessName = ContainsAny(type.Name, DomainKeywords);

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

        if (isPresentationNamespace)
        {
            AddScore("Presentation", 4, "presentation-oriented namespace");
        }

        if (isApplicationNamespace)
        {
            AddScore("Application", 4, "application-oriented namespace");
        }

        if (isInfrastructureNamespace)
        {
            AddScore("Infrastructure", 4, "infrastructure-oriented namespace");
        }

        if (isDomainNamespace)
        {
            AddScore("Domain", 4, "domain-oriented namespace");
        }

        if (hasBusinessName && !isPresentationTypeName && !isApplicationTypeName && !isInfrastructureTypeName)
        {
            AddScore("Domain", 2, "business-oriented name");
        }

        if (isApplicationTypeName)
        {
            AddScore("Application", 2, "application-style name");
        }

        if (isInfrastructureTypeName)
        {
            if (isInterface || ContainsAny(namespaceLabel, AbstractionKeywords) || isApplicationNamespace)
            {
                AddScore("Application", 2, "infrastructure-style abstraction name");
            }
            else
            {
                AddScore("Infrastructure", 2, "infrastructure-style name");
            }
        }

        if (isPresentationTypeName)
        {
            AddScore("Presentation", 2, "presentation-style name");
        }

        if (isInterface)
        {
            if (isApplicationNamespace || ContainsAny(namespaceLabel, AbstractionKeywords) || ContainsAny(type.Name, AbstractionKeywords))
            {
                AddScore("Application", 2, "interface or abstraction contract");
            }
            else if (isDomainNamespace)
            {
                AddScore("Domain", 1, "domain interface");
            }
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

        if (incomingDependencies.Count >= 2 && isDomainNamespace)
        {
            AddScore("Domain", 2, "reused by multiple internal dependents");
        }

        if (incomingDependencies.Count >= 2 && isApplicationNamespace)
        {
            AddScore("Application", 1, "referenced by multiple internal dependents");
        }

        if (constructorDependencies.Count >= 2)
        {
            AddScore("Application", 1, "multiple constructor dependencies");
        }

        if (!isInterface && isInfrastructureNamespace && constructorDependencies.Count > 0)
        {
            AddScore("Infrastructure", 1, "infrastructure implementation with constructor wiring");
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
        var projectLabel = $"{project.Name} {project.FilePath}";

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

        if (ContainsProjectSignal(project, DomainNamespaceSignals))
        {
            AddScore("Domain", 3, "domain-oriented project name/path");
        }

        if (ContainsProjectSignal(project, ApplicationNamespaceSignals))
        {
            AddScore("Application", 3, "application-oriented project name/path");
        }

        if (ContainsProjectSignal(project, InfrastructureNamespaceSignals))
        {
            AddScore("Infrastructure", 3, "infrastructure-oriented project name/path");
        }

        if (ContainsProjectSignal(project, PresentationNamespaceSignals))
        {
            AddScore("Presentation", 3, "presentation-oriented project name/path");
        }

        if (project.IsRunnable)
        {
            AddScore("Presentation", 2, "runnable entrypoint project");
        }

        foreach (var type in projectTypes)
        {
            var layer = type.Classification?.Layer;
            if (string.IsNullOrWhiteSpace(layer) || layer is "Unknown" or "Mixed")
            {
                continue;
            }

            var weight = string.Equals(type.Kind, "Interface", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
            AddScore(layer, weight, $"{BuildTypeLabel(type)} classified as {layer}");
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
        if (top.Value <= 1)
        {
            return new ClassificationInfo
            {
                Layer = "Unknown",
                Confidence = "Low",
                Reasons = ["no strong signals"],
            };
        }

        var second = ordered.Length > 1 ? ordered[1] : default;
        if (second.Value >= 3 && top.Value - second.Value <= 1)
        {
            return new ClassificationInfo
            {
                Layer = "Mixed",
                Confidence = "Medium",
                Reasons = new[] { top.Key, second.Key }
                    .SelectMany(signal => reasons.GetValueOrDefault(signal, []))
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

    private static bool ContainsNamespaceSignal(string? namespaceValue, IReadOnlyList<string> signals)
    {
        if (string.IsNullOrWhiteSpace(namespaceValue))
        {
            return false;
        }

        var segments = namespaceValue
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Any(segment => signals.Any(signal => string.Equals(segment, signal, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ContainsProjectSignal(ProjectInfoModel project, IReadOnlyList<string> signals)
    {
        return ContainsNamespaceSignal(project.Name.Replace('-', '.'), signals) ||
               ContainsNamespaceSignal(project.FilePath.Replace('\\', '.').Replace('/', '.'), signals);
    }

    private static IReadOnlyList<FindingModel> BuildCycleFindings(
        IEnumerable<(string SourceId, string TargetId)> edges,
        string category,
        string severity,
        Func<string, string> labelSelector,
        int maxReportedCycles)
    {
        var cycles = FindCycles(edges)
            .Take(maxReportedCycles)
            .ToArray();

        return cycles
            .Select(cycle =>
            {
                var labels = cycle
                    .Select(labelSelector)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                return new FindingModel
                {
                    Severity = severity,
                    Category = category,
                    SubjectId = labels[0],
                    Message = $"Detected {category.Replace('-', ' ')} involving {string.Join(" -> ", labels)}.",
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<IReadOnlyList<string>> FindCycles(IEnumerable<(string SourceId, string TargetId)> edges)
    {
        var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (sourceId, targetId) in edges)
        {
            if (!adjacency.TryGetValue(sourceId, out var sourceTargets))
            {
                sourceTargets = new HashSet<string>(StringComparer.Ordinal);
                adjacency[sourceId] = sourceTargets;
            }

            sourceTargets.Add(targetId);

            if (!adjacency.ContainsKey(targetId))
            {
                adjacency[targetId] = new HashSet<string>(StringComparer.Ordinal);
            }
        }

        var index = 0;
        var stack = new Stack<string>();
        var indexByNode = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowLinkByNode = new Dictionary<string, int>(StringComparer.Ordinal);
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<IReadOnlyList<string>>();

        void Visit(string node)
        {
            indexByNode[node] = index;
            lowLinkByNode[node] = index;
            index++;
            stack.Push(node);
            onStack.Add(node);

            foreach (var target in adjacency[node].OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!indexByNode.ContainsKey(target))
                {
                    Visit(target);
                    lowLinkByNode[node] = Math.Min(lowLinkByNode[node], lowLinkByNode[target]);
                }
                else if (onStack.Contains(target))
                {
                    lowLinkByNode[node] = Math.Min(lowLinkByNode[node], indexByNode[target]);
                }
            }

            if (lowLinkByNode[node] != indexByNode[node])
            {
                return;
            }

            var component = new List<string>();
            string currentNode;
            do
            {
                currentNode = stack.Pop();
                onStack.Remove(currentNode);
                component.Add(currentNode);
            }
            while (!string.Equals(currentNode, node, StringComparison.Ordinal));

            if (component.Count > 1)
            {
                components.Add(component.OrderBy(value => value, StringComparer.Ordinal).ToArray());
            }
        }

        foreach (var node in adjacency.Keys.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!indexByNode.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return components
            .OrderByDescending(component => component.Count)
            .ThenBy(component => string.Join("|", component), StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildNamespaceLabel(string namespaceId)
    {
        var separatorIndex = namespaceId.IndexOf("::", StringComparison.Ordinal);
        return separatorIndex >= 0 ? namespaceId[(separatorIndex + 2)..] : namespaceId;
    }

    private static int SeverityRank(string severity)
    {
        return severity switch
        {
            "error" => 3,
            "warning" => 2,
            "info" => 1,
            _ => 0,
        };
    }

    private static string BuildTypeLabel(TypeInfoModel type)
    {
        return string.IsNullOrWhiteSpace(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }
}
