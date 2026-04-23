# Dependency Explorer Summary

Input path: `CycleSample.slnx`

## Scope

- Level: All
- Focus project: none
- Focus namespace: none
- Focus class: none

## Counts

- Projects: 2
- Package references: 0
- Documents: 10
- Named types: 4
- Project dependency edges: 1
- Namespace dependency edges: 10
- Type dependency edges: 10
- Internal type dependency edges: 6
- External type dependency edges: 4
- Constructor DI edges: 3
- Project cycles: 0
- Namespace cycles: 1
- Type cycles: 1

## Analysis Options

- Classification: enabled
- Constructor DI graph: enabled

## Workspace Diagnostics

- None

## Projects

- `CycleSample.Core`
  Path: `CycleSample.Core/CycleSample.Core.csproj`
  Frameworks: net10.0
  Documents: 6
  Project references: none
  Package references: none

- `CycleSample.Host`
  Path: `CycleSample.Host/CycleSample.Host.csproj`
  Frameworks: net10.0
  Documents: 4
  Project references: CycleSample.Core
  Package references: none

## Top Type Fan-Out

- `CycleSample.Core.Alpha.AlphaService`: 1
- `CycleSample.Core.Beta.BetaPolicy`: 1
- `CycleSample.Core.Gamma.GammaGateway`: 1

## Top Type Fan-In

- `CycleSample.Core.Alpha.AlphaService`: 1
- `CycleSample.Core.Beta.BetaPolicy`: 1
- `CycleSample.Core.Gamma.GammaGateway`: 1

## Cycle Summary

- Project cycles: 0 (largest: 0)
- Namespace cycles: 1 (largest: 3)
- Type cycles: 1 (largest: 3)

## Key Findings

- [warning] Detected namespace cycle involving CycleSample.Core.Alpha -> CycleSample.Core.Beta -> CycleSample.Core.Gamma.
- [warning] Detected type cycle involving CycleSample.Core.Alpha.AlphaService -> CycleSample.Core.Beta.BetaPolicy -> CycleSample.Core.Gamma.GammaGateway.