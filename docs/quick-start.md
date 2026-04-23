# Quick Start

Build the tool:

```bash
dotnet build ./src/DependencyExplorer/DependencyExplorer.csproj
```

Run it against a solution:

```bash
./depex analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --graph-format mermaid --level all --verbose
```

Focused example:

```bash
./depex analyze --solution ./samples/Fixtures/LayeredSample/LayeredSample.slnx --output ./artifacts/focus --level class --focus-project LayeredSample.Application --focus-namespace LayeredSample.Application.Invoices --focus-class LayeredSample.Application.Invoices.InvoiceService
```

Optional wrappers from the repo root:

```bash
dotnet run --project ./src/DependencyExplorer -- analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --graph-format mermaid --level all --verbose
```

```powershell
& ./depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --graph-format mermaid --level all --verbose
```

Main outputs:

- `report.md`: Single combined report with embedded Mermaid graphs.
- `analysis.json`: Full structured result.
- `summary.md`: Short overview and main counts.
- `inventory.md`: Per-project inventory table.
- `violations.md`: Findings and warnings.
- `graph-projects.mmd`: Project dependency graph.
- `graph-namespaces.mmd`: Namespace dependency graph.
- `graph-classes-global.mmd`: Full class dependency graph.
- `graph-di-global.mmd`: Full constructor-DI graph.
- `graph-classes-focused.mmd`: Focused class graph when focus options are used.
- `graph-di-focused.mmd`: Focused DI graph when focus options are used.

Supported options:

- `--solution <path>`: Solution file to analyze.
- `--output <directory>`: Where generated files go.
- `--level <project|namespace|class|all>`: Which graph levels to emit.
- `--graph-format <mermaid|none>`: Whether to write Mermaid files.
- `--verbose`: Print more console details.
- `--skip-classification`: Disable heuristic classification.
- `--skip-di-graph`: Disable constructor-DI extraction.
- `--focus-project <name>`: Narrow focused outputs to one project.
- `--focus-namespace <name>`: Narrow focused outputs to one namespace or prefix.
- `--focus-class <name>`: Narrow focused outputs to one type.
