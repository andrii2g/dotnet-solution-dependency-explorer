# Remediation Guidance Specification

This document describes a proposed deterministic remediation layer for Dependency Explorer.

The purpose of this feature is:

- detect an issue
- explain why it matters
- suggest likely ways to resolve it

This feature is explicitly rule-based.

It must not:

- call external services
- require AI or network access
- rewrite code automatically
- generate speculative free-form advice unrelated to the detected issue

## 1. Goal

When the analyzer finds an issue, it should also suggest a small set of practical next actions.

The first implementation should only suggest possible resolution directions.

It should not:

- apply changes
- create patches
- modify source code

## 2. Design Principles

The remediation system should be:

- deterministic
- local-only
- stable
- easy to review
- easy to extend

The system should prefer:

- short actionable guidance
- issue-category-specific wording
- small parameter substitution

The system should avoid:

- long essays
- generic architecture slogans
- category-independent advice

## 3. Scope

The remediation layer should consume:

- findings
- metrics
- classifications
- graph context when needed

The remediation layer should produce:

- structured remediation suggestions per finding

## 4. Internal Model

Suggested model:

```csharp
internal sealed class RemediationSuggestion
{
    public required string FindingCategory { get; init; }
    public required string Title { get; init; }
    public required string Why { get; init; }
    public required IReadOnlyList<string> SuggestedActions { get; init; }
    public required string FirstStep { get; init; }
    public required IReadOnlyList<string> Tradeoffs { get; init; }
    public string? AvoidWhen { get; init; }
    public required string Confidence { get; init; }
}
```

Optional wrapper if suggestions are attached directly to findings:

```csharp
internal sealed class FindingWithRemediation
{
    public required FindingModel Finding { get; init; }
    public required IReadOnlyList<RemediationSuggestion> Suggestions { get; init; }
}
```

## 5. New Module

Suggested folder:

- `src/DependencyExplorer/Remediation/`

Suggested service:

- `RemediationService`

Responsibilities:

- map finding categories to remediation templates
- fill small deterministic placeholders
- return structured suggestions for export

## 6. Mapping Strategy

The first implementation should use:

- one primary remediation recipe per finding category

Recipes may vary by:

- finding category
- severity
- subject label

The first implementation should not branch deeply on many dimensions.

## 7. Initial Supported Categories

The first implementation should support:

- `project-cycle`
- `namespace-cycle`
- `type-cycle`
- `outgoing-hub`
- `incoming-hub`
- `mixed-project`
- `infrastructure-leakage`

Optional later categories:

- `broad-package-usage`
- `workspace`
- `scale`

## 8. Suggested Recipes

### 8.1 `outgoing-hub`

Title:

- `Split orchestration responsibilities`

Why:

- this type depends on many internal collaborators and may be doing multiple jobs

Suggested actions:

- split command or write behavior from query or read behavior
- extract smaller services around dependency clusters
- move infrastructure-specific interactions behind narrower interfaces
- keep orchestration in a thin coordinator and move business logic elsewhere

First step:

- group methods by dependency usage and extract the largest dependency cluster first

Tradeoffs:

- more classes
- more interfaces
- more indirection

Avoid when:

- the type is intentionally a thin composition root or entrypoint coordinator

### 8.2 `incoming-hub`

Title:

- `Review central dependency surface`

Why:

- many internal types depend on this subject, so changes to it may have wide impact

Suggested actions:

- confirm whether the subject is a stable abstraction or an overloaded implementation
- split unrelated responsibilities into smaller interfaces or services
- preserve a thin stable contract if widespread reuse is intentional

First step:

- list dependents and group them by which members they actually use

Tradeoffs:

- temporary API growth
- possible interface proliferation

### 8.3 `project-cycle`

Title:

- `Break project cycle through an extracted boundary`

Why:

- project cycles make layering, builds, testing, and future extraction harder

Suggested actions:

- extract shared contracts into a lower-level project
- invert one dependency through an interface
- move shared DTOs or models into a neutral dependency-light project

First step:

- identify the weakest edge in the cycle and replace it with a contract boundary

Tradeoffs:

- more projects
- temporary migration overhead

### 8.4 `namespace-cycle`

Title:

- `Separate namespace responsibilities`

Why:

- namespace cycles usually indicate blurred ownership or orchestration mixed with implementation

Suggested actions:

- move shared contracts into a dedicated namespace
- push orchestration into a higher-level namespace
- isolate adapters from core logic

First step:

- identify the smallest cross-namespace dependency that can be inverted or moved

Tradeoffs:

- namespace churn
- temporary reorganization of files

### 8.5 `type-cycle`

Title:

- `Break bidirectional type knowledge`

Why:

- strongly coupled types are harder to test, reuse, and change independently

Suggested actions:

- replace one direct reference with an interface
- move shared state into a value object
- introduce a coordinator instead of direct back-reference
- use callback or event-style interaction where appropriate

First step:

- inspect the cycle and remove direct knowledge in one direction first

Tradeoffs:

- more indirection
- possible short-term complexity increase

### 8.6 `mixed-project`

Title:

- `Split project by concern`

Why:

- mixed projects usually combine transport, orchestration, and infrastructure concerns in one unit

Suggested actions:

- move transport-facing code toward presentation
- move repositories, clients, and gateways toward infrastructure
- keep orchestration and contracts in application-like areas
- keep business rules and state in domain-like areas

First step:

- group files by responsibility and move the smallest cohesive group first

Tradeoffs:

- more project boundaries
- temporary migration overhead

### 8.7 `infrastructure-leakage`

Title:

- `Move framework dependency outward`

Why:

- framework-specific dependencies inside domain-like areas reduce portability and testability

Suggested actions:

- introduce an abstraction at the boundary
- move framework-specific logic into an adapter
- keep domain-facing contracts framework-neutral

First step:

- identify the first framework-bound API and wrap it behind an interface

Tradeoffs:

- extra interfaces
- adapter indirection

## 9. Output Contract

The first implementation should render remediation guidance in:

- `violations.md`
- `report.md`

Optional later:

- `analysis.json`

The recommended rendering style is directly under each finding.

Example:

```text
- [warning] outgoing-hub: Type 'Billing.Application.InvoiceService' is an outgoing dependency hub with 9 unique internal targets.

  Suggested resolution:
  - split command or write behavior from query or read behavior
  - extract smaller services around dependency clusters
  - move infrastructure-specific interactions behind narrower interfaces

  First step:
  - group methods by dependency usage and extract the largest dependency cluster first
```

## 10. Validation Strategy

The remediation layer should remain snapshot-friendly.

Rules:

- wording must be deterministic
- suggestions must be category-driven
- parameter substitution must be minimal and stable

Recommended first validation target:

- `CycleSample`

Reason:

- cycle findings are already deterministic and easy to snapshot

## 11. Non-Goals

The remediation layer should not:

- generate code
- propose file-by-file patches
- rewrite classes automatically
- claim certainty where only heuristics exist

## 12. Implementation Order

Recommended order:

1. add remediation models
2. add `RemediationService`
3. support cycle findings first
4. support hub findings next
5. support classification-driven findings after that
6. render guidance in `violations.md` and `report.md`
7. add fixture snapshot coverage

## 13. Extension Rule

Any new remediation rule should:

- map to an existing finding category
- stay deterministic
- explain why the issue matters
- suggest a small set of realistic next actions
- include a concrete first step
