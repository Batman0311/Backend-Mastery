# Phase 5 - Factory Pattern and Resolver with ConcurrentDictionary

## Topics
- Factory pattern and service resolution
- Thread-safe registration and lookup
- Lazy initialization and GetOrAdd semantics
- Instance caching vs factory caching

## Project
**ResolverFactory (Console App)**
- Register factories by key and resolve services on demand
- Demonstrate double-initialization bugs under parallel resolves
- Fix with Lazy and ConcurrentDictionary

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **Race on singleton creation**
  - Issue: a non-thread-safe resolver can create multiple instances for the same key under load.
  - Production-ready options:
    - Use a lock around resolve and cache writes.
    - Use ConcurrentDictionary with Lazy initialization.
    - Use a DI container with singleton lifetime guarantees.
  - Selected fix and why:
    - ConcurrentDictionary with Lazy to avoid blocking and ensure single initialization.
- **GetOrAdd double invocation**
  - Issue: ConcurrentDictionary.GetOrAdd may invoke the value factory multiple times under contention, causing duplicate side effects.
  - Production-ready options:
    - Wrap creation in Lazy and store Lazy instances.
    - Make the factory idempotent and move side effects outside.
    - Pre-warm and register instances at startup.
  - Selected fix and why:
    - Lazy ensures only one factory execution while keeping concurrent lookups fast.

## Demo Modes and Methods
- **ifelse** (`dotnet run -- ifelse`)
  - Calls: `ResolverDemo.RunIfElseFactory()`
  - What it demonstrates: a simple if/else factory that chooses an implementation by key.
  - Concurrency note: none; single-threaded resolution.
  - Enterprise scenario: routing to a handler based on a request type or tenant.
  - Core steps:
    1. Pass a key to the factory.
    2. Use if/else to select the implementation.
    3. Return the created service.
  - What to observe: correct service instances for each key.
- **naive** (`dotnet run -- naive`)
  - Calls: `ResolverDemo.RunNaiveRace()`
  - What it demonstrates: a plain Dictionary resolver can create multiple instances for one key.
  - Concurrency note: Parallel.For overlaps resolves on the thread pool.
  - Enterprise scenario: resolving pricing or discount calculators during a flash sale.
  - Core steps:
    1. Register a factory for a key.
    2. Resolve the same key in parallel.
    3. Observe multiple created instances.
  - What to observe: Created instances > 1.
- **getoradd-bug** (`dotnet run -- getoradd-bug`)
  - Calls: `ResolverDemo.RunGetOrAddBug()`
  - What it demonstrates: GetOrAdd can run the factory more than once.
  - Concurrency note: multiple threads hit the value factory concurrently.
  - Enterprise scenario: expensive model compilation triggered multiple times.
  - Core steps:
    1. Register a factory with side effects.
    2. Resolve in parallel.
    3. Observe factory called multiple times.
  - What to observe: Created instances > 1 even though a single cache entry exists.
- **lazy-fixed** (`dotnet run -- lazy-fixed`)
  - Calls: `ResolverDemo.RunLazyFixed()`
  - What it demonstrates: Lazy guards initialization so only one instance is created.
  - Concurrency note: Lazy.ExecutionAndPublication ensures one creator wins.
  - Enterprise scenario: singleton service creation under burst traffic.
  - Core steps:
    1. Register a factory.
    2. Resolve in parallel.
    3. Observe only one instance created.
  - What to observe: Created instances == 1.

## Code Walkthrough (per demo)
- **Goal:** show why naive or direct GetOrAdd creation is unsafe for side-effectful factories.
- **Shared state:** the resolver cache keyed by service name.
- **Concurrency model:** Parallel.For with multiple threads hitting the same key.
- **Critical section:** read-check-create-write around the cache.
- **Failure mode:** multiple creations or repeated side effects.
- **Fix mechanics:** store Lazy instances so the factory executes once.

## Method Notes
- **RunIfElseFactory()**
  - Inputs: none.
  - Outputs: console output with service names and instance IDs.
  - Production scenario: routing by request type or feature flag.
  - Steps:
    1. Create the if/else factory.
    2. Resolve two keys.
    3. Print the resolved services.
  - Pitfalls: if/else chains become brittle as keys grow.
  - Fix: move to a registry-based resolver or DI container.
- **RunNaiveRace()**
  - Inputs: none.
  - Outputs: console summary of created instances and distinct resolved IDs.
  - Production scenario: resolving per-request services without a DI container.
  - Steps:
    1. Register a factory.
    2. Resolve in parallel.
    3. Print creation counts.
  - Pitfalls: double initialization and potential dictionary races.
  - Fix: use a concurrency-safe resolver and Lazy.
- **RunGetOrAddBug()**
  - Inputs: none.
  - Outputs: console summary of factory invocations and distinct IDs.
  - Production scenario: compiling rule engines or templates on demand.
  - Steps:
    1. Register a side-effectful factory.
    2. Resolve in parallel.
    3. Observe extra creations.
  - Pitfalls: assuming GetOrAdd executes only once.
  - Fix: store Lazy and evaluate once.
- **RunLazyFixed()**
  - Inputs: none.
  - Outputs: console summary showing single creation.
  - Production scenario: singleton resolver under load.
  - Steps:
    1. Register a factory.
    2. Resolve in parallel.
    3. Confirm single creation.
  - Pitfalls: none in this demo.
  - Fix: not applicable.

## Revision: Factory Pattern and Resolver
Factories map keys to creation logic. A resolver must be safe under concurrent access, or the same service can be created multiple times with duplicated side effects. ConcurrentDictionary removes structural races, but GetOrAdd can still call the value factory more than once. Wrapping the creation in Lazy provides a single execution path while keeping lookups fast.

### Main Code Example: Lazy + ConcurrentDictionary
```csharp
var lazy = cache.GetOrAdd(key, _ =>
  new Lazy<IService>(() => factory(), LazyThreadSafetyMode.ExecutionAndPublication));

var instance = lazy.Value;
```

## Interview Questions and Answers (Senior Level)
- Why can ConcurrentDictionary.GetOrAdd still run the factory multiple times? Answer: valueFactory can be invoked concurrently; only the stored value is de-duplicated.
- When is Lazy better than a simple lock? Answer: when you want one-time initialization without holding a lock during the full creation path.
- How do you avoid double initialization when the factory has side effects? Answer: wrap creation in Lazy or move side effects outside and make the factory idempotent.
- What are the tradeoffs of pre-warming the cache? Answer: lower latency at runtime but higher startup cost and memory usage.

## Pitfalls (Senior Level)
- Assuming GetOrAdd executes the factory exactly once.
- Creating services inside a lock that performs I/O, which blocks other threads and increases tail latency.
- Caching instances that are not thread-safe or are request-scoped.
- Registering factories after resolves start, causing races and inconsistent behavior.

## Cross-Questions
- When should you prefer caching factories over caching instances? Answer: when instances are request-scoped or expensive to keep alive.
- Why is LazyThreadSafetyMode.ExecutionAndPublication preferred here? Answer: it guarantees a single successful initialization and caches the result.
- What happens if Lazy initialization throws? Answer: the exception is cached and rethrown on subsequent access unless you recreate the Lazy.

## Code Examples (to add during implementation)
- If/else factory selection
- Naive resolver with Dictionary (bug)
- Lazy-backed resolver (fixed)

## How to Run (Issue Reproduction)
- If/else factory: `dotnet run --project phase-05-factory-resolver/ResolverFactory -- ifelse`
- Naive resolver: `dotnet run --project phase-05-factory-resolver/ResolverFactory -- naive`
- GetOrAdd bug: `dotnet run --project phase-05-factory-resolver/ResolverFactory -- getoradd-bug`
- Lazy fixed: `dotnet run --project phase-05-factory-resolver/ResolverFactory -- lazy-fixed`

## Tooling Notes
- Dot Net 8 console app

## Expected Output (Samples)
- Broken case: Created instances > 1 for the same key
- Fixed case: Created instances == 1 and distinct resolved == 1
