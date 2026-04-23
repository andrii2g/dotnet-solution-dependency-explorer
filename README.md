# Dependency Explorer

Analyze a `.sln` or `.slnx` and generate a compact dependency report with project, namespace, class, and constructor-DI graphs.

For solutions with runnable projects such as web apps and worker services, the tool also emits one focused report folder per runnable project under `reports/<project-name>/`.

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

`depex` restores the target solution automatically before analysis.

Windows PowerShell:

```powershell
& ./depex.ps1 analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```
