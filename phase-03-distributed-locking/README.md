# Phase 3 - Distributed Locking in Multi-Instance APIs

## Topics
- Cross-instance contention and oversell risk
- Redis lock primitives (SET NX PX)
- Safe release with owner tokens (Lua check)
- Lock expiry, stale owners, and fencing tokens

## Project
**InventoryService (Web API)**
- Simulate multiple API instances contending for one SKU
- Add Redis-backed locking around reservations
- Demonstrate lock expiry failure and fencing token fix

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **Cross-instance oversell with in-process locks**
  - Issue: per-process locks do not coordinate across instances, so multiple nodes can reserve the same stock.
  - Production-ready options:
    - Redis distributed lock (SET NX PX) around the critical section.
    - Queue-based reservation worker per SKU.
    - Atomic update with a DB constraint (single-statement guard).
  - Selected fix and why:
    - Redis lock because it coordinates across instances without routing all traffic through one worker.
- **Stale owner writes after lock expiry**
  - Issue: a slow request can finish after its lock expires and overwrite the newer reservation.
  - Production-ready options:
    - Fencing tokens checked on write.
    - Lock renewal/heartbeat for long operations.
    - Single-writer queue per SKU to avoid expiry races.
  - Selected fix and why:
    - Fencing tokens because they prevent stale owners even if a lock expires.

## Demo Modes and Methods
- **naive** (`POST /demo/loadtest`)
  - Calls: `ReservationLoadTest.RunAsync()` -> `InventoryReservationService.ReserveAsync()`
  - What it demonstrates: read-modify-write with no locking.
  - Concurrency note: `Task.Run` uses the thread pool to overlap work.
  - Enterprise scenario: flash sale traffic hitting a single SKU.
  - Core steps:
    1. Seed SKU with quantity 1.
    2. Run 50 overlapping reservations.
    3. Observe multiple successes.
- **instance-lock** (`POST /demo/loadtest`)
  - Calls: `InventoryReservationService.ReserveWithLocalLockAsync()`
  - What it demonstrates: in-process locks protect a single instance only.
  - Concurrency note: single process, shared lock table.
  - Enterprise scenario: a single node under burst load.
  - Core steps:
    1. Acquire local lock.
    2. Reserve stock.
    3. Observe only one success.
- **cross-instance-local** (`POST /demo/cross-instance`)
  - Calls: `CrossInstanceLoadTest.RunAsync()` -> `InventoryReservationService.ReserveWithLocalLockAsync()`
  - What it demonstrates: two instances have separate locks and still oversell.
  - Concurrency note: two simulated instances with independent lock providers.
  - Enterprise scenario: load balancer fans out traffic to two API nodes.
  - Core steps:
    1. Seed SKU with quantity 1.
    2. Split requests across two instances.
    3. Observe oversell despite local locks.
- **redis** (`POST /demo/cross-instance`)
  - Calls: `InventoryReservationService.ReserveWithRedisLockAsync()`
  - What it demonstrates: Redis lock coordinates cross-instance access.
  - Concurrency note: lock acquisition serializes critical sections.
  - Enterprise scenario: multiple nodes using a shared Redis cluster.
  - Core steps:
    1. Acquire Redis lock.
    2. Reserve stock.
    3. Observe one success.
- **redis-unsafe** (`POST /demo/cross-instance`)
  - Calls: `InventoryReservationService.ReserveWithRedisLockUnsafeAsync()`
  - What it demonstrates: lock expiry allows stale owners to overwrite new writes.
  - Concurrency note: one instance is forced to exceed the lock TTL.
  - Enterprise scenario: slow DB call or GC pause exceeds the lease time.
  - Core steps:
    1. Acquire Redis lock with short TTL.
    2. Delay past expiry on one instance.
    3. Observe overwrites.
- **redis-fenced** (`POST /demo/cross-instance`)
  - Calls: `InventoryReservationService.ReserveWithRedisFencingAsync()`
  - What it demonstrates: fencing tokens reject stale writes.
  - Concurrency note: token monotonicity enforces ordering.
  - Enterprise scenario: long-running reservation flow with strict write ordering.
  - Core steps:
    1. Acquire Redis lock and fencing token.
    2. Write with token check.
    3. Observe stale writers fail.

**Observability (all modes)**
- Logs: check the API console output for `Reservation complete` with `correlationId` and `demoMode`.
- Metrics: watch `requests_total`, `errors_total`, `request_duration_ms`, and `inflight_requests`.
- Traces: `InventoryService.DistributedLocking` activities include `service`, `operation`, and `demoMode`.

## Code Walkthrough (per demo)
- **Goal:** show why distributed locks are required and how fencing tokens remove expiry races.
- **Shared state:** `InventoryItems` row with `Quantity` and `LastFencingToken`.
- **Concurrency model:** `Task.Run` overlaps reservations in the thread pool.
- **Critical section:** the read-modify-write sequence.
- **Failure mode:** multiple writers update without a shared lock or write-order check.
- **Fix mechanics:** Redis lock for mutual exclusion, fencing token for ordering.

## Annotated Code Notes
- Phase 3 C# files include line-by-line comments explaining control flow and timing windows.

## Method Notes
- **ReserveNaiveAsync()**
  - Inputs: SKU, quantity, context
  - Outputs: success or conflict
  - Production scenario: flash-sale reservations without coordination.
  - Pitfalls: oversell due to read-modify-write.
  - Fix: use Redis lock modes.
- **ReserveWithLocalLockAsync()**
  - Inputs: SKU, quantity, context
  - Outputs: success or lock timeout
  - Production scenario: single-node protection only.
  - Pitfalls: no cross-instance coordination.
  - Fix: use Redis lock in multi-node deployments.
- **ReserveWithRedisLockUnsafeAsync()**
  - Inputs: SKU, quantity, context
  - Outputs: success or timeout
  - Production scenario: distributed lock with short lease.
  - Pitfalls: lock expiry allows stale writes.
  - Fix: add fencing tokens or lock renewal.
- **ReserveWithRedisFencingAsync()**
  - Inputs: SKU, quantity, context
  - Outputs: success or stale-write rejection
  - Production scenario: distributed lock plus write ordering.
  - Pitfalls: missing token check reintroduces stale writes.
  - Fix: enforce `LastFencingToken < token` on update.

## Cross-Questions
- When is a queue-based worker better than a Redis lock?
- How does fencing differ from a lock lease renewal?
- What happens if Redis is unavailable during a reservation burst?

## Code Examples (to add during implementation)
- Redis lock acquire with safe release (Lua script)
- Fencing token update with a DB guard

## How to Run (Issue Reproduction)
- Start Redis locally (default connection string is `localhost:6379`).
- Start the API: `dotnet run --project phase-03-distributed-locking/InventoryService`
- Reproduce cross-instance oversell:
  1. `POST /demo/cross-instance` with `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "cross-instance-local" }`
  2. Observe multiple successes.
- Verify Redis lock:
  1. `POST /demo/cross-instance` with `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "redis" }`
  2. Observe single success.
- Reproduce lock expiry:
  1. `POST /demo/cross-instance` with `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 20, "mode": "redis-unsafe" }`
  2. Observe inconsistent results.
- Verify fencing token fix:
  1. `POST /demo/cross-instance` with `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 20, "mode": "redis-fenced" }`
  2. Observe stale writes rejected.

## Tooling Notes
- Dot Net 8 Web API
- Swagger for API documentation
- SQLite + EF Core for storage
- Redis for distributed locking

## Expected Output (Samples)
- Broken case: multiple successes, inconsistent final quantity
- Fixed case: one success, stale writes rejected
