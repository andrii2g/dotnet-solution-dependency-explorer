# Dependency Explorer Specification

This document describes the current implemented behavior of `dotnet-solution-dependency-explorer`.

It is not a future plan. It is the working contract for the tool as it exists now and should be the main reference for:

- contributors
- issue discussions
- behavior changes
- output compatibility decisions

## 1. Purpose

The tool analyzes a `.sln` or `.slnx` and produces:

- project dependency information
- namespace dependency information
- type dependency information
- constructor-DI dependency information
- heuristic classification
- architectural findings
- deterministic remediation suggestions for selected finding categories
- Markdown and Mermaid outputs

The tool is intended for architecture review, refactoring analysis, and large-solution exploration.

## 2. Supported Input

Current supported input mode:

- `analyze --solution <path-to-sln-or-slnx>`

Current supported solution kinds:

- `.sln`
- `.slnx`

The tool loads the full solution scope first. Focus options only narrow reporting and focused graph outputs after full analysis.

## 3. CLI Contract

Primary command:

```bash
DependencyExplorer analyze --solution <path> [options]
```

Current supported options:

- `--solution <path>`
- `--output <directory>`
- `--level <project|namespace|class|all>`
- `--verbose`
- `--skip-classification`
- `--skip-di-graph`
- `--focus-project <name>`
- `--focus-namespace <name>`
- `--focus-class <name>`

Current launcher paths in this repository:

```bash
./bin/depex analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

```powershell
& ./bin/depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

Launcher behavior:

- `bin/depex` and `bin/depex.ps1` run `dotnet restore <solution>` automatically before analysis when called as `analyze --solution ...`

## 4. Analysis Pipeline

The implemented pipeline is:

1. validate CLI arguments
2. create output directory
3. register MSBuild
4. load the solution with `MSBuildWorkspace`
5. collect workspace diagnostics
6. discover projects, packages, documents, and named types
7. extract dependency edges
8. compute metrics
9. apply classification heuristics unless skipped
10. compute architectural findings
11. write JSON, Markdown, and Mermaid outputs
12. write focused runnable-project reports

## 5. Discovery Scope

The tool currently discovers:

- projects
- target frameworks
- explicit `.csproj` project references
- package references
- documents
- named C# types

Named type kinds currently discovered:

- class declarations
- interface declarations
- struct declarations
- record declarations

Identity rules:

- type identity is based on Roslyn fully qualified symbol display
- project references are taken from explicit `.csproj` `ProjectReference` items
- project/file paths written to outputs are normalized to repository-relative paths where possible

## 6. Dependency Kinds

Current project-level dependencies:

- `ProjectReference`
- `PackageReference`

Current namespace/type dependency extraction uses semantic analysis for:

- base types
- implemented interfaces
- field types
- property types
- constructor parameters
- method parameters
- method return types
- attribute types
- generic argument expansion through named type traversal

Constructor-DI graph:

- based only on constructor parameters
- recorded separately from the broader type dependency graph

External dependencies:

- external types and namespaces are preserved in structured data
- Mermaid graphs focus on internal visible graph structure for readability

## 7. Outputs

Unconditional files for a successful run:

- `analysis.json`
- `summary.md`
- `inventory.md`
- `violations.md`
- `report.md`

Conditional Mermaid files depending on selected level and focus:

- `graph-projects.mmd`
- `graph-namespaces.mmd`
- `graph-classes-global.mmd`
- `graph-di-global.mmd`
- `graph-classes-focused.mmd`
- `graph-di-focused.mmd`

Runnable-project focused outputs:

- `reports/<project-name>/report.md`
- `reports/<project-name>/graph-project-neighborhood.mmd`
- `reports/<project-name>/graph-classes-focused.mmd`
- `reports/<project-name>/graph-di-focused.mmd` when DI is enabled

Current reporting rule:

- root `report.md` is the global combined report
- `reports/<project-name>/...` are focused slices for runnable entrypoint projects

## 8. Runnable Project Detection

A project is treated as runnable when one of these is true:

- `OutputType=Exe`
- `OutputType=WinExe`
- project SDK string contains `.Web`
- project SDK string contains `Worker`

This is used only for focused report generation. It does not change the global analysis scope.

## 9. Classification Heuristics

Classification is heuristic, not authoritative.

Current type-level output labels:

- `Domain`
- `Application`
- `Infrastructure`
- `Presentation`
- `Unknown`
- `Mixed`

Current project-level output labels:

- same label set as type-level classification

### 9.1 Type classification signals

Current scoring uses:

- namespace segment signals
- type name signals
- interface/abstraction handling
- outgoing dependency signals
- incoming dependency reuse signals
- constructor dependency count

Current namespace segment signals:

- Domain:
  - `Domain`
  - `Policies`
  - `Rules`
  - `Entities`
  - `Models`
- Application:
  - `Application`
  - `UseCases`
  - `Handlers`
  - `Commands`
  - `Queries`
  - `Abstractions`
  - `Contracts`
- Infrastructure:
  - `Infrastructure`
  - `Data`
  - `Persistence`
  - `Files`
  - `Storage`
  - `Redis`
  - `Mq`
  - `Messaging`
  - `Notifications`
- Presentation:
  - `Api`
  - `Web`
  - `Endpoints`
  - `Controllers`
  - `Hosts`
  - `Workers`

Current name signals:

- Domain-like names:
  - `Order`
  - `Invoice`
  - `Customer`
  - `Money`
  - `Policy`
  - `Rule`
- Application-like names:
  - `Handler`
  - `UseCase`
  - `Service`
  - `Command`
  - `Query`
- Infrastructure-like names:
  - `Repository`
  - `Client`
  - `Provider`
  - `Gateway`
  - `Adapter`
- Presentation-like names:
  - `Controller`
  - `Endpoint`
  - `Host`
  - `Program`
  - `Startup`
  - `Worker`

Important current behavior:

- domain business words do not override obvious presentation/application/infrastructure names
- interface and abstraction contracts are biased toward `Application` unless strong domain signals exist
- namespace matching is segment-based, not loose substring matching

### 9.2 Project classification signals

Project classification is aggregated from:

- project name/path signals
- runnable-entrypoint signal
- classified contained types
- package reference signals

### 9.3 Skip behavior

When `--skip-classification` is used:

- classification is not applied
- `summary.md` records that classification was skipped
- `analysis.json` records that classification was skipped
- `inventory.md` shows `Skipped`
- `violations.md` still exists and contains only non-classification findings plus an explicit skip note

## 10. Architectural Findings

Current findings may include:

- workspace load warnings
- mixed project findings
- domain infrastructure-leakage findings
- cycle findings
- hub findings
- broad package usage findings
- scale findings

For selected finding categories, findings also carry deterministic remediation suggestions.

### 10.1 Cycle detection

Cycle detection is currently implemented using strongly connected components over:

- project graph
- namespace graph
- internal type graph

Current result behavior:

- cycle counts are included in `summary.md`
- largest cycle sizes are included in `summary.md`
- concrete cycle findings are emitted to `violations.md`

Current finding categories:

- `project-cycle`
- `namespace-cycle`
- `type-cycle`

### 10.2 Hub detection

Hub detection currently uses internal type graph fan-in and fan-out.

Current metrics:

- top type fan-out
- top type fan-in
- outgoing hub count
- incoming hub count

Current threshold behavior:

- no hub detection for very small graphs
- otherwise threshold is based on the upper decile of internal fan-in/fan-out values
- threshold floor is `5`

Current finding categories:

- `outgoing-hub`
- `incoming-hub`

Current reporting rule:

- hub findings are emitted only when thresholds are active and nodes exceed them
- summary hub section is omitted when no hub thresholds are active

### 10.3 Remediation suggestions

Remediation suggestions are currently generated locally and deterministically.

They do not use:

- AI
- external services
- network calls

Current remediation coverage:

- `project-cycle`
- `namespace-cycle`
- `type-cycle`
- `outgoing-hub`
- `incoming-hub`
- `mixed-project`
- `infrastructure-leakage`

Current rendering locations:

- `analysis.json`
- `violations.md`
- `report.md`

## 11. Metrics

Current global metrics include:

- project count
- package reference count
- document count
- type count
- project dependency count
- namespace dependency count
- type dependency count
- internal type dependency count
- external type dependency count
- constructor DI dependency count
- project cycle count
- namespace cycle count
- type cycle count
- largest project cycle size
- largest namespace cycle size
- largest type cycle size
- outgoing hub count
- incoming hub count
- outgoing hub threshold
- incoming hub threshold
- top fan-in nodes
- top fan-out nodes

## 12. Progress Reporting

Current progress reporting is intentionally simple and coarse.

Current milestones:

- `5%` loading starts
- `10%` workspace loaded
- `15%` discovery starts
- `15..75%` project scanning
- `80%` report writing starts
- `85..95%` runnable-project report writing
- `100%` done

The progress model is approximate by design and is not currently part of a stronger public contract.

## 13. Validation Contract

Current validation uses committed fixture solutions and golden snapshots.

Current fixtures:

- `LayeredSample`
- `MixedLegacySample`
- `CycleSample`

Current validation scripts:

- `scripts/validate-fixtures.ps1`
- `scripts/validate-fixtures.sh`

Current validated snapshots:

- `docs/examples/LayeredSample/graph-projects.mmd`
- `docs/examples/LayeredSample/graph-namespaces.mmd`
- `docs/examples/LayeredSample/summary.md`
- `docs/examples/MixedLegacySample/violations.md`
- `docs/examples/CycleSample/summary.md`
- `docs/examples/CycleSample/violations.md`

Validation behavior:

- publish analyzer
- run analyzer against fixtures
- normalize generated text
- compare selected outputs against committed snapshots
- fail on drift

## 14. Determinism Rules

Current determinism expectations:

- nodes and edges are sorted before output
- paths are normalized
- stable IDs are used for graph emission
- fixture validation compares normalized text rather than raw process output

Normalization in validation currently includes:

- LF normalization
- trailing whitespace trimming
- blank-line collapsing
- repository-root path normalization

## 15. Known Limits

Current known limits:

- runnable-project focused class graphs are still strict in-project slices and do not yet expand to immediate neighbors
- large Mermaid graphs may exceed viewer render limits even when generation succeeds
- WSL/Linux analysis of Windows-oriented solutions can produce partial-load diagnostics if the target solution references Windows-only restore paths
- classification remains heuristic and should be treated as guidance, not truth
- hub detection is intentionally conservative and will not activate for small graphs

## 16. Improvement Rules

Changes to analyzer behavior should follow these rules:

- update this specification when the implemented contract changes
- update snapshots when output changes intentionally
- preserve deterministic output
- prefer explicit findings over vague heuristics
- prefer `Unknown` over false-confidence classification
- add positive fixtures when introducing new architectural findings

## 17. Open Source Readiness

For open-source work, this document should be treated as the behavioral baseline.

Recommended contributor workflow:

1. read this specification
2. run fixture validation
3. make a focused change
4. refresh snapshots only when behavior changes intentionally
5. update this specification and `CHANGELOG.md` in the same change
