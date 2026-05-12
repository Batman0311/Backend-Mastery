# Backend Learning Roadmap (Dot Net 8)

This roadmap organizes backend fundamentals into phased projects. Each phase lives in its own folder and includes a README describing topics, project goals, intended issues and fixes, cross-questions, and code examples as needed. For each issue, the README lists production-ready options and states why a specific approach was chosen.

## Learning Pattern
For each phase, we implement the issue first (intentionally buggy), reproduce it, then apply the chosen production-ready fix. This keeps the learning path grounded in real failure modes before remediation.

Follow the shared template and rules in [docs/phase-template.md](docs/phase-template.md) for every phase.

## Stack
- Dot Net 8 (C#)
- Web APIs use Swagger
- SQLite + EF Core where a database is required

## Folder Naming
Use phase folders like:
- phase-01-concurrency
- phase-02-locking
- phase-03-transactions
- phase-04-idempotency
- phase-05-db-design
- phase-06-sharding-cache
- phase-07-ddd

## Phases

### Phase 1: Concurrency Fundamentals (Console App)
**Topics**
- Concurrency, parallelism, multi-threading
- Race conditions
- Deadlocks

**Project: ParallelTaskProcessor**
- Accept a batch of CPU-bound jobs
- Run sequential vs parallel using a thread pool
- Introduce a race condition on a shared counter, then fix with locks or atomics
- Introduce a deadlock with two locks, then fix by ordering lock acquisition
- Measure throughput and compare

**Hands-on outcomes**
- Use Thread, Task, and Parallel.ForEach
- Understand Mutex, Semaphore, lock/Monitor
- See why lock ordering matters

### Phase 2: Locking Strategies (Web API)
**Topics**
- Optimistic locking
- Pessimistic locking
- Distributed locking

**Project: InventoryService**
- Reserve stock and release stock endpoints
- Pessimistic lock: row-level lock during reservation
- Optimistic lock: version field with retry on conflict
- Distributed lock: Redis SETNX or Redlock to guard cross-instance reservation
- Load test: simulate 50 concurrent reservations for 1 item

**Hands-on outcomes**
- Compare optimistic vs pessimistic locking strategies
- Understand Redlock and its failure modes
- Distinguish stale reads vs conflict exceptions

### Phase 3: Transactions and Consistency (Console App or Worker)
**Topics**
- 2-phase commit (2PC)
- 3-phase commit (3PC)
- Distributed transactions
- Strong vs eventual consistency

**Project: OrderFulfillmentSaga**
- Simulate Payment, Inventory, Shipping services
- 2PC coordinator: Prepare -> Vote -> Commit, simulate node crash
- 3PC: add Pre-Commit and observe behavior on coordinator failure
- Saga: event-driven steps with compensating transactions
- Log every phase transition to a file for visualization

**Hands-on outcomes**
- See why 2PC blocks on coordinator failure
- See how 3PC reduces blocking but does not eliminate it
- Understand why Sagas are common in microservices

### Phase 4: Idempotency and API Design (Web API)
**Topics**
- Idempotency
- API design process

**Project: PaymentGatewayAPI**
- Idempotency-Key header support
- Store key and result for replay on duplicates
- REST design: resources, verbs, status codes, versioning, pagination, error contracts
- OpenAPI/Swagger documentation
- Retry simulation: same request 5 times, only 1 charge

**Hands-on outcomes**
- Build safe retry patterns
- Implement idempotency key storage with TTL
- Design a contract-first API surface

### Phase 5: Database Design and Indexing (Console App + SQL)
**Topics**
- Database design process
- Indexing strategies

**Project: AnalyticsDashboardDB**
- Requirements -> ERD -> normalization -> denormalization decisions
- Seed large data sets (1M+ rows)
- Use EXPLAIN ANALYZE to find slow queries
- Add indexes: composite, partial, covering
- Measure before/after timings and document tradeoffs

**Hands-on outcomes**
- Read query plans and detect full scans
- Compare index types and use cases
- Understand write overhead and index bloat

### Phase 6: Sharding and Distributed Cache (Web API + Console Benchmark)
**Topics**
- Sharding with consistent hashing
- Distributed cache patterns

**Project: ShardedUserStore**
- Consistent hashing across 3 shards
- Rebalance when a 4th shard is added and observe key migration
- Cache-aside Redis layer with TTL and invalidation
- Benchmark DB-only vs cache-hit vs cache-miss

**Hands-on outcomes**
- Build a hash ring with virtual nodes
- Handle hotspots and cache stampede risks
- Compare read-through, write-through, write-behind

### Phase 7: Domain-Driven Design and Patterns (Web API)
**Topics**
- Domain-driven design
- Repository pattern
- Unit of Work

**Project: ECommerceDomain**
- Layered structure: Domain, Application, Infrastructure, API
- IRepository and IUnitOfWork abstractions
- UnitOfWork wraps a DB transaction
- Domain events for side effects
- Unit tests against domain logic without DB dependency

**Hands-on outcomes**
- Define aggregate boundaries
- Keep domain pure and persistence separate
- Improve testability with layered architecture

## Notes
- We will create folders and scaffolding as we progress phase by phase.
- Each phase README will document intended issues, fixes, and cross-questions.
