# Changelog

## 2026-04-23

- Upgraded fixture solutions and GitHub Actions validation to .NET 10.
- Added sample reports for README.md.
- Removed `--graph-format`; Mermaid graphs are now always emitted.
- Added short repo-root launchers: `./depex` and `./depex.ps1`.
- Added a root `README.md` with links to the main documentation.
- Added a combined `report.md` with embedded Mermaid sections.
- Added a Bash fixture validator for WSL/Linux usage.
- Added a short quick-start guide under `docs/quick-start.md`.
- Added focused project/namespace/class Mermaid outputs.
- Implemented `--skip-classification` and `--skip-di-graph` runtime behavior.
- Added fixture validation snapshots, script, and CI workflow.
- Switched the CLI parser and help system to `System.CommandLine`.
- Added heuristic classification + inventory + violations.


## 2026-04-22

- Added dependency extraction for project, namespace, type, and constructor-parameter edges.
- Added global class Mermaid graph output and scale metrics in `summary.md`.
- Added solution loading through `MSBuildWorkspace` with `Microsoft.Build.Locator`.
- Added full-scope project, package-reference, and named-type discovery across loaded C# projects.
- Added `summary.md` and `analysis.json` emission for workspace diagnostics and discovery inventory review.
- Added the initial `DependencyExplorer` solution and `net8.0` console project scaffold.
- Added the initial CLI shell for `analyze --solution <path-to-sln-or-slnx>`.
- Added command parsing, help output, option validation, output-directory creation, and verbose logging shell.
- Kept unsupported CLI options behind explicit `not implemented yet` errors.
