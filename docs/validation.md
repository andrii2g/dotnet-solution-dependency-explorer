# Validation

Run the fixture regression harness from the repository root.

Preferred for WSL/Linux:

```bash
bash ./scripts/validate-fixtures.sh
```

Windows PowerShell alternative:

```powershell
& ./scripts/validate-fixtures.ps1
```

The script publishes the CLI, analyzes the committed fixture solutions, writes fresh outputs under `artifacts/validation/runs`, normalizes the generated files, and compares them against the committed snapshots in `docs/examples`.

Reference commands for manual inspection:

```powershell
dotnet publish ./src/DependencyExplorer/DependencyExplorer.csproj -c Debug -o ./artifacts/manual-tool /p:UseAppHost=false
dotnet ./artifacts/manual-tool/DependencyExplorer.dll analyze --solution ./samples/Fixtures/LayeredSample/LayeredSample.slnx --output ./artifacts/manual-layered --level all --verbose
dotnet ./artifacts/manual-tool/DependencyExplorer.dll analyze --solution ./samples/Fixtures/MixedLegacySample/MixedLegacySample.slnx --output ./artifacts/manual-mixed --level all --verbose
```
