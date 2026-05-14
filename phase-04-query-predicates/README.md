# Phase 4 - Query Composition and Predicates

## Topics
- Custom `Where` implementation and deferred execution
- Delegate vs expression predicates and why it matters
- Predicate composition (AND/OR) with parameter rebinding
- `IEnumerable<T>` vs `IQueryable<T>` behavior

## Project
**PredicatePlayground (Console App)**
- Implement custom `Where` behaviors (buggy and fixed)
- Compose predicates dynamically with expression trees
- Compare eager vs deferred execution with visible side effects

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **Lost deferred execution in custom `Where`**
  - Issue: filtering happens immediately, causing unexpected work before enumeration.
  - Production-ready options:
    - Use iterator blocks with `yield return` to preserve deferred execution.
    - Avoid custom LINQ in hot paths; reuse `Enumerable.Where` directly.
    - Materialize intentionally via `ToList()` only when required.
  - Selected fix and why:
    - Use `yield return` because it matches LINQ semantics and avoids hidden eager work.
- **Broken predicate composition**
  - Issue: combining expressions without parameter rebinding throws at runtime.
  - Production-ready options:
    - Rebind parameters with an `ExpressionVisitor` before combining.
    - Use a proven predicate builder library for complex composition.
    - Compose with `Expression.Invoke` (may reduce provider translation support).
  - Selected fix and why:
    - Parameter rebinding keeps expressions provider-friendly and avoids runtime exceptions.

## Demo Modes and Methods
- **where-bug** (`dotnet run --project phase-04-query-predicates/PredicatePlayground -- where-bug`)
  - Calls: `CustomWhereDemo.RunEagerBug()`
  - What it demonstrates: eager execution in a custom `Where`.
  - Concurrency note: single-threaded; no parallelism involved.
  - Enterprise scenario: background batch filters a large export early and spikes CPU.
  - Core steps:
    1. Build a custom filter with side effects.
    2. Observe that evaluation happens immediately.
    3. Iterate results and note the predicate already ran.
  - What to observe: predicate logs appear before iteration starts.
- **where-fixed** (`dotnet run --project phase-04-query-predicates/PredicatePlayground -- where-fixed`)
  - Calls: `CustomWhereDemo.RunLazyFixed()`
  - What it demonstrates: deferred execution with `yield return`.
  - Concurrency note: single-threaded; no parallelism involved.
  - Enterprise scenario: stream processing filters only when a consumer pulls data.
  - Core steps:
    1. Build a custom filter with side effects.
    2. Confirm no evaluation until enumeration.
    3. Iterate results and watch predicate logs align with enumeration.
  - What to observe: predicate logs appear during iteration.
- **predicate-bug** (`dotnet run --project phase-04-query-predicates/PredicatePlayground -- predicate-bug`)
  - Calls: `PredicateBuilderDemo.RunOrBug()`
  - What it demonstrates: parameter mismatch when composing expression trees.
  - Concurrency note: single-threaded; no parallelism involved.
  - Enterprise scenario: dynamic search filters from UI fail under composition.
  - Core steps:
    1. Build two separate predicates.
    2. Combine without rebinding parameters.
    3. Execute and observe runtime exception.
  - What to observe: exception about an unbound parameter.
- **predicate-fixed** (`dotnet run --project phase-04-query-predicates/PredicatePlayground -- predicate-fixed`)
  - Calls: `PredicateBuilderDemo.RunOrFixed()`
  - What it demonstrates: safe OR composition via parameter rebinding.
  - Concurrency note: single-threaded; no parallelism involved.
  - Enterprise scenario: dynamic filters compose safely and remain provider-friendly.
  - Core steps:
    1. Build two predicates.
    2. Rebind parameters to a shared instance.
    3. Execute and inspect results.
  - What to observe: results filtered as expected.

## Code Walkthrough (per demo)
- **Goal:** show how deferred execution and composition errors surface.
- **Shared state:** enumerable sequences and expression parameters.
- **Concurrency model:** none; sequential evaluation only.
- **Critical section:** predicate evaluation during enumeration.
- **Failure mode:** eager evaluation or parameter mismatch exception.
- **Fix mechanics:** iterator blocks for laziness, parameter rebinding for composition.

## Method Notes
- **RunEagerBug()**
  - Inputs: none.
  - Outputs: console output showing early predicate evaluation.
  - Production scenario: batch filtering stage that should be lazy.
  - Steps:
    1. Build a custom filter with side effects.
    2. Observe eager evaluation.
  - Pitfalls: unexpected work before enumeration.
  - Fix: switch to `yield return` in the fixed variant.
- **RunLazyFixed()**
  - Inputs: none.
  - Outputs: console output showing evaluation during iteration.
  - Production scenario: streaming pipeline only evaluates on demand.
  - Steps:
    1. Build a custom filter.
    2. Enumerate and observe lazy behavior.
  - Pitfalls: none in this demo.
  - Fix: not applicable.
- **RunOrBug()**
  - Inputs: none.
  - Outputs: console output with a runtime exception.
  - Production scenario: ad-hoc filters composed per request.
  - Steps:
    1. Build two predicate expressions.
    2. Combine without rebinding.
    3. Execute and hit an exception.
  - Pitfalls: unbound parameter errors.
  - Fix: rebind parameters before combining.
- **RunOrFixed()**
  - Inputs: none.
  - Outputs: console output with filtered results.
  - Production scenario: search filters that remain provider-friendly.
  - Steps:
    1. Build two predicates.
    2. Rebind and combine.
    3. Execute and inspect results.
  - Pitfalls: none in this demo.
  - Fix: not applicable.

## Cross-Questions
- When should you prefer `IQueryable<T>` over `IEnumerable<T>`?
- Why does deferred execution matter for performance and correctness?
- How does parameter rebinding keep expressions provider-friendly?

## Code Examples (to add during implementation)
- Custom `Where` with `yield return`.
- Parameter rebinding with `ExpressionVisitor`.

## How to Run (Issue Reproduction)
- `dotnet run --project phase-04-query-predicates/PredicatePlayground -- where-bug`
- `dotnet run --project phase-04-query-predicates/PredicatePlayground -- where-fixed`
- `dotnet run --project phase-04-query-predicates/PredicatePlayground -- predicate-bug`
- `dotnet run --project phase-04-query-predicates/PredicatePlayground -- predicate-fixed`

## Tooling Notes
- Dot Net 8

## Expected Output (Samples)
- Broken case: predicate logs appear before enumeration or an unbound parameter exception.
- Fixed case: predicate logs appear during enumeration and combined filters succeed.
