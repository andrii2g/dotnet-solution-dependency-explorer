# Dependency Explorer

Analyze a `.sln` or `.slnx` and generate a compact dependency report with project, namespace, class, and constructor-DI graphs.

## Documentation

- [Quick Start](./docs/quick-start.md)
- [Validation](./docs/validation.md)
- [Example Outputs](./docs/examples)
- [Sample Report](./docs/sample-report.md): Example combined report for the clean `LayeredSample` solution.
- [Legacy Sample Report](./docs/legacy-sample-report.md): Example combined report for the more tangled `MixedLegacySample` solution.
- [Implementation Plan](./PLAN.md)

## Run

```bash
./depex analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

Windows PowerShell:

```powershell
& ./depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```
