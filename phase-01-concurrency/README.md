# Phase 1 - Concurrency Fundamentals

## Topics
- Concurrency vs parallelism
- Multi-threading
- Race conditions
- Deadlocks

## What Is a Race Condition?
A race condition happens when multiple threads read and write shared state without synchronization, and the final result depends on timing. The bug is intermittent because thread scheduling is nondeterministic. The core issue is a non-atomic read-modify-write sequence on shared data.

## Project
**ParallelTaskProcessor (Console App)**
- Accept a batch of CPU-bound jobs
- Run sequential vs parallel using a thread pool
- Measure throughput and compare

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **Race condition on shared counter**
  - Issue: multiple workers update a shared counter without synchronization, producing incorrect totals.
  - Production-ready options:
    - `Interlocked` for simple atomic increments.
    - `lock`/`Monitor` for multi-step updates or invariants.
    - `ConcurrentDictionary` or `ConcurrentBag` when contention is on collections.
  - Selected fix and why:
    - Use `Interlocked` for the counter because the update is a single atomic increment and it avoids lock contention.
- **Deadlock with two locks**
  - Issue: two threads acquire lock A then lock B in opposite order, causing a deadlock.
  - Production-ready options:
    - Enforce a single lock acquisition order across all code paths.
    - Use a single lock to protect both resources if ordering is hard to guarantee.
    - Use `Monitor.TryEnter` with timeout for detection and fallback paths.
  - Selected fix and why:
    - Enforce lock ordering because it is deterministic, low overhead, and commonly used in enterprise systems.
- **Race condition on shared inventory row (SQLite)**
  - Issue: two requests read the same quantity and both write an update, causing oversell.
  - Production-ready options:
    - Optimistic concurrency with a version column and retries.
    - Pessimistic locking with a transactional row lock (where supported).
    - Atomic update statement that checks quantity in the WHERE clause.
  - Selected fix and why:
    - Optimistic concurrency with a version column because it scales for read-heavy workloads and is common in enterprise systems.
- **Deadlock-style lock contention (SQLite)**
  - Issue: two transactions update rows in opposite order, increasing lock contention. In row-locking databases this can deadlock; in SQLite it shows up as lock timeouts.
  - Production-ready options:
    - Enforce a single lock/row acquisition order across all code paths.
    - Keep transactions short and scoped to only required work.
    - Retry transient lock timeouts with backoff.
  - Selected fix and why:
    - Enforce lock ordering because it prevents cycles and is a standard enterprise practice; add small retries for transient locks.

## Demo Modes and Methods
- **race** (`dotnet run -- race`)
  - Calls: `InMemoryConcurrencyDemo.RunRaceConditionDemo()`
  - Demonstrates a shared counter update with `++` inside `Parallel.For`, which loses updates due to interleaving.
- **race-fixed** (`dotnet run -- race-fixed`)
  - Calls: `InMemoryConcurrencyDemo.RunRaceConditionFixed()`
  - Uses `Interlocked.Increment` to make the increment atomic.
- **race-lock** (`dotnet run -- race-lock`)
  - Calls: `InMemoryConcurrencyDemo.RunRaceConditionFixedWithLock()`
  - Serializes increments with a `lock` for correctness when multi-step invariants exist.
- **race-concurrent** (`dotnet run -- race-concurrent`)
  - Calls: `InMemoryConcurrencyDemo.RunConcurrentCollectionDemo()`
  - Uses `ConcurrentBag` to show safe concurrent collection writes.
- **deadlock** (`dotnet run -- deadlock`)
  - Calls: `InMemoryConcurrencyDemo.RunDeadlockDemo()`
  - Creates circular wait by acquiring `lockA` then `lockB` in one task and the reverse order in another.
- **deadlock-fixed** (`dotnet run -- deadlock-fixed`)
  - Calls: `InMemoryConcurrencyDemo.RunDeadlockFixed()`
  - Enforces consistent lock ordering (A then B) to avoid circular wait.
- **db-race** (`dotnet run -- db-race`)
  - Calls: `DatabaseConcurrencyDemo.RunDatabaseRaceConditionDemo()`
  - Demonstrates oversell when two workers read the same row and update without concurrency checks.
- **db-fixed** (`dotnet run -- db-fixed`)
  - Calls: `DatabaseConcurrencyDemo.RunDatabaseRaceConditionFixed()`
  - Adds optimistic concurrency using a version column with retry on conflicts.
- **db-deadlock** (`dotnet run -- db-deadlock`)
  - Calls: `DatabaseConcurrencyDemo.RunDatabaseDeadlockDemo()`
  - Simulates lock contention by updating two SKUs in opposite order inside a transaction.
- **db-deadlock-fixed** (`dotnet run -- db-deadlock-fixed`)
  - Calls: `DatabaseConcurrencyDemo.RunDatabaseDeadlockFixed()`
  - Uses consistent row order plus retries to reduce contention.
- **throughput** (`dotnet run -- throughput`)
  - Calls: `ThroughputDemo.RunThroughputComparison()`
  - Compares sequential vs parallel processing for CPU-bound work.

## Demo Walkthroughs (Step by Step)
### Race Condition (In-Memory)
1. Initialize a local counter per trial.
2. Run `Parallel.For` to simulate high contention on the counter.
3. Observe a counter smaller than the expected total (lost updates).
4. Apply the fix with `Interlocked` or `lock` and re-run.

### Deadlock (In-Memory)
1. Create two locks and a `Barrier` to align timing.
2. Acquire locks in opposite order across two tasks.
3. Observe timeout because both tasks wait on each other.
4. Fix by acquiring locks in the same order.

### Database Oversell (SQLite)
1. Reset inventory and set quantity to 1 for SKU-1.
2. Fire multiple tasks that read then update without concurrency checks.
3. Observe more than one successful reservation.
4. Fix by using optimistic concurrency and retry on conflicts.

### Database Lock Contention (SQLite)
1. Reset inventory and open two transactions.
2. Update SKUs in opposite order to create lock contention.
3. Observe lock failures or delays.
4. Fix by enforcing a single row order and retrying transient locks.

## Cross-Questions
- Why does a race condition sometimes appear and sometimes not?
- When should you prefer `Interlocked` over a `lock`?
- How can lock ordering policies be enforced in a larger codebase?
- What is the difference between CPU-bound and IO-bound work for parallelism?

## Code Examples (to add during implementation)
- Example: sequential vs parallel execution comparison
- Example: broken shared counter vs fixed shared counter
- Example: deadlock reproduction and corrected lock ordering
- Example: DB oversell race vs optimistic concurrency fix

### Example: Fixing with `lock`
```csharp
var counter = 0;
var gate = new object();

Parallel.For(0, iterations, _ =>
{
  // lock serializes access so increments are not interleaved.
  lock (gate)
  {
    counter++;
  }
});
```

### Example: Fixing with `Interlocked`
```csharp
var counter = 0;

Parallel.For(0, iterations, _ =>
{
  // Atomic increment without a lock.
  Interlocked.Increment(ref counter);
});
```

### Example: Using a Concurrent Collection
```csharp
var bag = new ConcurrentBag<int>();

Parallel.For(0, iterations, i =>
{
  // Thread-safe Add provided by the collection.
  bag.Add(i);
});

var count = bag.Count;
```

## How to Run (Issue Reproduction)
- Race condition demo: `dotnet run -- race`
- Race condition fixed: `dotnet run -- race-fixed`
- Race condition fixed with lock: `dotnet run -- race-lock`
- Concurrent collection demo: `dotnet run -- race-concurrent`
- Deadlock demo (intentional hang with timeout message): `dotnet run -- deadlock`
- Deadlock fixed: `dotnet run -- deadlock-fixed`
- Database race condition demo: `dotnet run -- db-race`
- Database race condition fixed: `dotnet run -- db-fixed`
- Database deadlock demo: `dotnet run -- db-deadlock`
- Database deadlock fixed: `dotnet run -- db-deadlock-fixed`
- Throughput comparison: `dotnet run -- throughput`

## Tooling Notes
- Dot Net 8 console app
- Use `Thread`, `Task`, and `Parallel.ForEach` where appropriate
- SQLite + EF Core for the DB-backed concurrency demo
