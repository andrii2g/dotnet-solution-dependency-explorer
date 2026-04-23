# Quick Start

Build the tool:

```bash
dotnet build ./src/DependencyExplorer/DependencyExplorer.csproj
```

Run it against a solution:

```bash
./bin/depex analyze --solution ./samples/Fixtures/LayeredSample/LayeredSample.slnx --output ./artifacts/review --level all --verbose
```

`depex` restores the target solution automatically before analysis.

Focused example:

```bash
./bin/depex analyze --solution ./samples/Fixtures/LayeredSample/LayeredSample.slnx --output ./artifacts/focus --level class --focus-project LayeredSample.Application --focus-namespace LayeredSample.Application.Invoices --focus-class LayeredSample.Application.Invoices.InvoiceService
```

Optional wrappers from the repo root:

```bash
dotnet run --project ./src/DependencyExplorer -- analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

```powershell
& ./bin/depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

Main outputs:

- `report.md`: Single combined report with embedded Mermaid graphs.
- `reports/<project-name>/report.md`: Focused report for each runnable project such as a web app or worker service.
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
- `--verbose`: Print more console details.
- `--skip-classification`: Disable heuristic classification.
- `--skip-di-graph`: Disable constructor-DI extraction.
- `--focus-project <name>`: Narrow focused outputs to one project.
- `--focus-namespace <name>`: Narrow focused outputs to one namespace or prefix.
- `--focus-class <name>`: Narrow focused outputs to one type.
