using A2G.DependencyExplorer.Models;

namespace A2G.DependencyExplorer.Remediation;

internal sealed class RemediationService
{
    public void Apply(IReadOnlyList<FindingModel> findings)
    {
        foreach (var finding in findings)
        {
            finding.Suggestions = BuildSuggestions(finding);
        }
    }

    private static IReadOnlyList<RemediationSuggestionModel> BuildSuggestions(FindingModel finding)
    {
        return finding.Category switch
        {
            "project-cycle" => [BuildProjectCycleSuggestion(finding)],
            "namespace-cycle" => [BuildNamespaceCycleSuggestion(finding)],
            "type-cycle" => [BuildTypeCycleSuggestion(finding)],
            "outgoing-hub" => [BuildOutgoingHubSuggestion(finding)],
            "incoming-hub" => [BuildIncomingHubSuggestion(finding)],
            "mixed-project" => [BuildMixedProjectSuggestion(finding)],
            "infrastructure-leakage" => [BuildInfrastructureLeakageSuggestion(finding)],
            _ => Array.Empty<RemediationSuggestionModel>(),
        };
    }

    private static RemediationSuggestionModel BuildProjectCycleSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Break project cycle through an extracted boundary",
            Why = "Project cycles make layering, builds, testing, and future extraction harder.",
            SuggestedActions =
            [
                "Extract shared contracts into a lower-level project.",
                "Invert one dependency through an interface.",
                "Move shared DTOs or models into a neutral dependency-light project.",
            ],
            FirstStep = "Identify the weakest edge in the cycle and replace it with a contract boundary.",
            Tradeoffs =
            [
                "More project boundaries.",
                "Temporary migration overhead.",
            ],
            Confidence = "High",
        };
    }

    private static RemediationSuggestionModel BuildNamespaceCycleSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Separate namespace responsibilities",
            Why = "Namespace cycles usually indicate blurred ownership or orchestration mixed with implementation.",
            SuggestedActions =
            [
                "Move shared contracts into a dedicated namespace.",
                "Push orchestration into a higher-level namespace.",
                "Isolate adapters from core logic.",
            ],
            FirstStep = "Identify the smallest cross-namespace dependency that can be inverted or moved.",
            Tradeoffs =
            [
                "Namespace churn.",
                "Temporary reorganization of files.",
            ],
            Confidence = "High",
        };
    }

    private static RemediationSuggestionModel BuildTypeCycleSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Break bidirectional type knowledge",
            Why = "Strongly coupled types are harder to test, reuse, and change independently.",
            SuggestedActions =
            [
                "Replace one direct reference with an interface.",
                "Move shared state into a value object.",
                "Introduce a coordinator instead of direct back-reference.",
                "Use callback or event-style interaction where appropriate.",
            ],
            FirstStep = "Inspect the cycle and remove direct knowledge in one direction first.",
            Tradeoffs =
            [
                "More indirection.",
                "Possible short-term complexity increase.",
            ],
            Confidence = "High",
        };
    }

    private static RemediationSuggestionModel BuildOutgoingHubSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Split orchestration responsibilities",
            Why = "This type depends on many internal collaborators and may be doing multiple jobs.",
            SuggestedActions =
            [
                "Split command or write behavior from query or read behavior.",
                "Extract smaller services around dependency clusters.",
                "Move infrastructure-specific interactions behind narrower interfaces.",
                "Keep orchestration in a thin coordinator and move business logic elsewhere.",
            ],
            FirstStep = "Group methods by dependency usage and extract the largest dependency cluster first.",
            Tradeoffs =
            [
                "More classes.",
                "More interfaces.",
                "More indirection.",
            ],
            AvoidWhen = "Avoid this if the subject is intentionally a thin composition root or entrypoint coordinator.",
            Confidence = "Medium",
        };
    }

    private static RemediationSuggestionModel BuildIncomingHubSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Review central dependency surface",
            Why = "Many internal types depend on this subject, so changes to it may have wide impact.",
            SuggestedActions =
            [
                "Confirm whether the subject is a stable abstraction or an overloaded implementation.",
                "Split unrelated responsibilities into smaller interfaces or services.",
                "Preserve a thin stable contract if widespread reuse is intentional.",
            ],
            FirstStep = "List dependents and group them by which members they actually use.",
            Tradeoffs =
            [
                "Temporary API growth.",
                "Possible interface proliferation.",
            ],
            Confidence = "Medium",
        };
    }

    private static RemediationSuggestionModel BuildMixedProjectSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Split project by concern",
            Why = "Mixed projects usually combine transport, orchestration, and infrastructure concerns in one unit.",
            SuggestedActions =
            [
                "Move transport-facing code toward presentation.",
                "Move repositories, clients, and gateways toward infrastructure.",
                "Keep orchestration and contracts in application-like areas.",
                "Keep business rules and state in domain-like areas.",
            ],
            FirstStep = "Group files by responsibility and move the smallest cohesive group first.",
            Tradeoffs =
            [
                "More project boundaries.",
                "Temporary migration overhead.",
            ],
            Confidence = "Medium",
        };
    }

    private static RemediationSuggestionModel BuildInfrastructureLeakageSuggestion(FindingModel finding)
    {
        return new RemediationSuggestionModel
        {
            FindingCategory = finding.Category,
            Title = "Move framework dependency outward",
            Why = "Framework-specific dependencies inside domain-like areas reduce portability and testability.",
            SuggestedActions =
            [
                "Introduce an abstraction at the boundary.",
                "Move framework-specific logic into an adapter.",
                "Keep domain-facing contracts framework-neutral.",
            ],
            FirstStep = "Identify the first framework-bound API and wrap it behind an interface.",
            Tradeoffs =
            [
                "Extra interfaces.",
                "Adapter indirection.",
            ],
            Confidence = "Medium",
        };
    }
}
