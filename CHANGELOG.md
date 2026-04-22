# Changelog

## 2026-04-22

- Added Phase 3 dependency extraction for project, namespace, type, and constructor-parameter edges.
- Added global class Mermaid graph output and scale metrics in `summary.md`.
- Added Phase 2 solution loading through `MSBuildWorkspace` with `Microsoft.Build.Locator`.
- Added full-scope project, package-reference, and named-type discovery across loaded C# projects.
- Added `summary.md` and `analysis.json` emission for workspace diagnostics and discovery inventory review.
- Added the initial `DependencyExplorer` solution and `net8.0` console project scaffold.
- Implemented Phase 1 CLI shell for `analyze --solution <path-to-sln-or-slnx>`.
- Added command parsing, help output, option validation, output-directory creation, and verbose logging shell.
- Reserved later-phase CLI options with explicit Phase 1 error messages.
