# Contributing

Thanks for contributing to Dependency Explorer.

## Before you change code

1. Read the current behavior spec in [docs/specification.md](./docs/specification.md).
2. Review the quick start and validation docs:
   - [docs/quick-start.md](./docs/quick-start.md)
   - [docs/validation.md](./docs/validation.md)
3. Run the fixture validation locally before and after your change.

## Development workflow

Build:

```bash
dotnet build ./src/DependencyExplorer/DependencyExplorer.csproj
```

Run:

```bash
./bin/depex analyze --solution ./DependencyExplorer.slnx --output ./artifacts/review --level all --verbose
```

Validate:

```bash
bash ./scripts/validate-fixtures.sh
```

Windows PowerShell alternative:

```powershell
& ./scripts/validate-fixtures.ps1
```

## Contribution rules

- Keep behavior deterministic.
- Prefer small, reviewable changes.
- Update documentation when behavior changes.
- Update fixture snapshots only when behavior changes intentionally.
- Add or extend fixtures for new analysis rules and findings.
- Do not add CLI switches for features that are not implemented.

## Pull requests

Please include:

- what changed
- why it changed
- whether snapshots changed
- how you validated it

For behavior changes, update:

- [docs/specification.md](./docs/specification.md)
- [CHANGELOG.md](./CHANGELOG.md)

## Issues

Good issues usually include:

- example solution shape
- expected behavior
- actual behavior
- generated report snippets or screenshots when relevant
- environment details if the problem is MSBuild or restore related
