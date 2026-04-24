# Dependency Explorer Findings

- [warning] namespace-cycle: Detected namespace cycle involving CycleSample.Core.Alpha -> CycleSample.Core.Beta -> CycleSample.Core.Gamma.

  Suggested resolution:
  - Separate namespace responsibilities
  - Why: Namespace cycles usually indicate blurred ownership or orchestration mixed with implementation.
  - Move shared contracts into a dedicated namespace.
  - Push orchestration into a higher-level namespace.
  - Isolate adapters from core logic.

  First step:
  - Identify the smallest cross-namespace dependency that can be inverted or moved.

  Tradeoffs:
  - Namespace churn.
  - Temporary reorganization of files.
- [warning] type-cycle: Detected type cycle involving CycleSample.Core.Alpha.AlphaService -> CycleSample.Core.Beta.BetaPolicy -> CycleSample.Core.Gamma.GammaGateway.

  Suggested resolution:
  - Break bidirectional type knowledge
  - Why: Strongly coupled types are harder to test, reuse, and change independently.
  - Replace one direct reference with an interface.
  - Move shared state into a value object.
  - Introduce a coordinator instead of direct back-reference.
  - Use callback or event-style interaction where appropriate.

  First step:
  - Inspect the cycle and remove direct knowledge in one direction first.

  Tradeoffs:
  - More indirection.
  - Possible short-term complexity increase.