# Phase 2 - Locking Strategies

## Topics
- Optimistic locking
- Pessimistic locking
- Distributed locking
- Conflict detection vs stale reads

## Project
**InventoryService (Web API)**
- Reserve stock and release stock endpoints
- Demonstrate optimistic, pessimistic, and distributed locking strategies
- Load test with 50 concurrent reservations for a single SKU

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **Oversell from naive reservation**
  - Issue: read-modify-write updates without a guard allow multiple reservations to succeed.
  - Production-ready options:
    - Optimistic concurrency with a version column and retries.
    - Pessimistic locking with row-level locks inside a transaction.
    - Atomic update statement that checks quantity in the WHERE clause.
  - Selected fix and why:
    - Optimistic concurrency for read-heavy workloads; retries keep throughput high.
- **Conflict exceptions without retries**
  - Issue: optimistic updates fail under load unless retries are implemented.
  - Production-ready options:
    - Retry with exponential backoff and a max attempt limit.
    - Queue reservations to serialize updates per SKU.
    - Fallback to pessimistic locking for hot SKUs.
  - Selected fix and why:
    - Retry with backoff because it is simple and avoids serializing all traffic.
- **Cross-instance oversell with in-process locks only**
  - Issue: single-instance locks do not protect multi-instance deployments.
  - Production-ready options:
    - Distributed lock (Redis SETNX / Redlock).
    - Queue-based reservation worker.
    - Database constraint with atomic update.
  - Selected fix and why:
    - Distributed locking because it coordinates cross-instance access without centralizing all traffic.

## Demo Modes and Methods
- **naive** (`POST /demo/loadtest`)
  - Calls: `ReservationLoadTest.RunAsync()` -> `InventoryReservationService.ReserveAsync()`
  - What it demonstrates: oversell caused by read-modify-write without a concurrency guard.
  - Concurrency note: `Task.Run` on the thread pool overlaps reservation work.
  - Enterprise scenario: flash sale where many requests hit the same SKU at once.
  - Core steps:
    1. Seed a SKU with quantity 1.
    2. Run 50 concurrent reservations.
    3. Observe multiple successes and negative inventory.
- **optimistic** (`POST /demo/loadtest`)
  - Calls: `InventoryReservationService.ReserveOptimisticAsync()`
  - What it demonstrates: version check + retry prevents oversell.
  - Concurrency note: conflicting updates throw and are retried.
  - Enterprise scenario: read-heavy catalog with occasional collisions.
  - Core steps:
    1. Seed quantity 1.
    2. Retry on `DbUpdateConcurrencyException`.
    3. Observe only one successful reservation.
- **pessimistic** (`POST /demo/loadtest`)
  - Calls: `InventoryReservationService.ReservePessimisticAsync()`
  - What it demonstrates: write lock serializes concurrent writers.
  - Concurrency note: SQLite uses database-level locks, standing in for row locks.
  - Enterprise scenario: hot SKU where contention is constant.
  - Core steps:
    1. Begin an immediate transaction.
    2. Read and update within the lock.
    3. Observe serialized reservations.
- **distributed** (`POST /demo/loadtest`)
  - Calls: `InventoryReservationService.ReserveDistributedAsync()`
  - What it demonstrates: external lock prevents cross-instance oversell.
  - Concurrency note: lock is acquired per SKU before a naive update.
  - Enterprise scenario: multiple API instances behind a load balancer.
  - Core steps:
    1. Acquire distributed lock.
    2. Perform reservation.
    3. Release lock and return result.

## Code Walkthrough (per demo)
- **Goal:** show how each locking strategy addresses the same oversell bug.
- **Shared state:** `InventoryItems` table with `Quantity` and `Version`.
- **Concurrency model:** concurrent HTTP requests simulated by `Task.Run`.
- **Critical section:** the reservation read-modify-write sequence.
- **Failure mode:** multiple updates succeed when no lock or version check exists.
- **Fix mechanics:** optimistic retries, pessimistic lock, or distributed lock.

## Method Notes
- **ReserveNaiveAsync()**
  - Inputs: SKU, quantity
  - Outputs: success or conflict
  - Production scenario: flash-sale reservations without concurrency protection.
  - Pitfalls: oversell due to interleaving updates.
  - Fix: replace with optimistic or pessimistic strategy.
- **ReserveOptimisticAsync()**
  - Inputs: SKU, quantity
  - Outputs: success or conflict
  - Production scenario: read-heavy inventory service.
  - Pitfalls: conflicts if retries are missing.
  - Fix: bounded retries with backoff.
- **ReservePessimisticAsync()**
  - Inputs: SKU, quantity
  - Outputs: success or conflict
  - Production scenario: hot SKU with steady contention.
  - Pitfalls: lock contention reduces throughput.
  - Fix: limit lock scope and keep transactions short.
- **ReserveDistributedAsync()**
  - Inputs: SKU, quantity
  - Outputs: success or conflict
  - Production scenario: multiple API instances.
  - Pitfalls: lock timeouts and stale locks if not released.
  - Fix: use a lock TTL and fencing tokens when using Redis.

## Cross-Questions
- When is optimistic locking slower than pessimistic locking?
- Why does a conflict exception need retries to be useful?
- How does distributed locking fail if the lock holder crashes?

## Code Examples (to add during implementation)
- Example: naive reservation vs optimistic reservation
- Example: distributed lock guard around a reservation

### In-Memory Optimistic Lock (Version Check)
```csharp
// Pack version (high 32 bits) and quantity (low 32 bits) into one atomic value.
long state = ((long)0 << 32) | (uint)1;

var success = false;
for (var attempt = 1; attempt <= 5; attempt++)
{
  var snapshot = Volatile.Read(ref state);
  var version = (int)(snapshot >> 32);
  var quantity = (int)(snapshot & 0xFFFFFFFF);

  if (quantity <= 0)
  {
    break;
  }

  var updated = ((long)(version + 1) << 32) | (uint)(quantity - 1);

  // Compare-and-swap on the packed state to detect conflicts.
  if (Interlocked.CompareExchange(ref state, updated, snapshot) == snapshot)
  {
    success = true;
    break;
  }
}
```

### In-Memory Pessimistic Lock (Single Lock)
```csharp
var gate = new object();
var quantity = 1;

lock (gate)
{
  if (quantity > 0)
  {
    quantity -= 1;
  }
}
```

### DB Optimistic Lock (EF Core Version)
```csharp
var item = await db.InventoryItems.SingleAsync(i => i.Sku == sku);
if (item.Quantity <= 0)
{
  return;
}

item.Quantity -= 1;
item.Version += 1;

try
{
  await db.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
  // Retry with fresh state.
}
```

### DB Pessimistic Lock (SQLite BEGIN IMMEDIATE)
```csharp
await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();
await ExecuteNonQueryAsync(connection, "BEGIN IMMEDIATE;");

// Read and update the row while the write lock is held.

await ExecuteNonQueryAsync(connection, "COMMIT;");
```

## How to Run (Issue Reproduction)
- Start the API: `dotnet run --project phase-02-locking/InventoryService`
- Reproduce the issue (oversell via naive DB reservation):
  1. `POST /inventory/seed` with body `{ "sku": "SKU-1", "quantity": 1 }`
  2. `POST /demo/loadtest` with body `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "naive" }`
  3. Observe multiple successes and a final quantity below zero or inconsistent totals.
- Verify fixes (DB-backed):
  1. `POST /demo/loadtest` with body `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "optimistic" }`
  2. `POST /demo/loadtest` with body `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "pessimistic" }`
  3. `POST /demo/loadtest` with body `{ "sku": "SKU-1", "initialQuantity": 1, "reservationQuantity": 1, "requestCount": 50, "mode": "distributed" }`
  4. Observe at most one success with the rest failing due to conflicts or lock timeouts.
- Compare in-memory strategies (no DB):
  1. `POST /demo/inmemory-optimistic` with body `{ "initialQuantity": 1, "requestCount": 50 }`
  2. `POST /demo/inmemory-pessimistic` with body `{ "initialQuantity": 1, "requestCount": 50 }`

## Tooling Notes
- Dot Net 8 Web API
- Swagger for API documentation
- SQLite + EF Core for storage

## Expected Output (Samples)
- Broken case: multiple successes with a final quantity below zero or inconsistent totals
- Fixed case: exactly one success for quantity 1 with conflicts reported for the rest
