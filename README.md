# A2G.DependencyExplorer

Analyze a `.sln` or `.slnx` and generate a compact dependency report with project, namespace, class, and constructor-DI graphs.

## Documentation

- [Quick Start](./docs/quick-start.md)
- [Validation](./docs/validation.md)
- [Example Outputs](./docs/examples)
- [Implementation Plan](./PLAN.md)

## Run

```bash
./depex analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --graph-format mermaid --level all --verbose
```

Windows PowerShell:

```powershell
& ./depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --graph-format mermaid --level all --verbose
```
