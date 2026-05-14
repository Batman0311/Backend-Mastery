# Backend Learning Roadmap (Dot Net 8)

This roadmap organizes backend fundamentals into phased projects. Each phase lives in its own folder and includes a README describing topics, project goals, intended issues and fixes, cross-questions, and code examples as needed. For each issue, the README lists production-ready options and states why a specific approach was chosen.

## Learning Pattern
For each phase, we implement the issue first (intentionally buggy), reproduce it, then apply the chosen production-ready fix. This keeps the learning path grounded in real failure modes before remediation.

Follow the shared template and rules in [docs/phase-template.md](docs/phase-template.md) for every phase.

## Stack
- Dot Net 8 (C#)
- Web APIs use Swagger
- SQLite + EF Core where a database is required

## Current Status
- Phase 2 (Locking Strategies) is complete.
- Phase 4 (Query Composition and Predicates) is in progress.

## Folder Naming
Use phase folders like:
- phase-01-concurrency
- phase-02-locking
- phase-03-distributed-locking
- phase-04-query-predicates
- phase-05-factory-resolver
- phase-06-transactions
- phase-07-idempotency
- phase-08-db-design
- phase-09-sharding-cache
- phase-10-ddd
- phase-11-consensus
- phase-12-replication-consistency
- phase-13-service-discovery
- phase-14-backpressure
- phase-15-resilience
- phase-16-fault-injection
- phase-17-messaging
- phase-18-event-sourcing-cqrs
- phase-19-time-ordering
- phase-20-observability
- phase-21-multi-region
- phase-22-strangler-migration

## Phases

### Microservices Low-Level Patterns (Coverage Map)
- Transactional outbox/inbox: safely publish or consume messages around a DB transaction -> Phase 17
- Change data capture (change data tracking): detect DB changes and turn them into events -> Phase 17
- Idempotency: repeated requests produce the same result without double side effects -> Phase 7
- Circuit breaker + retry (backoff + jitter) + timeouts: protect calls and avoid retry storms -> Phase 15
- Bulkheads: isolate resources so one failure does not cascade -> Phase 15
- Saga + compensating actions: coordinate multi-service work without distributed transactions -> Phase 6
- Rate limiting + load shedding: limit or drop traffic to keep latency stable -> Phase 14
- Request/response correlation: track a request across services with a shared ID -> Phase 20
- Cache-aside: load from cache, fall back to DB, then populate cache -> Phase 9
- Strangler: replace a legacy system piece by piece with a proxy -> Phase 22

### Phase 1: Concurrency Fundamentals (Console App)
**Topics**
- Concurrency: multiple tasks make progress in overlapping time windows
- Parallelism: tasks execute at the same time on multiple cores
- Multi-threading: use multiple threads to run work inside one process
- Race conditions: outcome depends on timing of threads accessing shared state
- Deadlocks: threads wait on each other forever due to lock cycles

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
- Optimistic locking: assume no conflict, detect it at save time with a version
- Pessimistic locking: lock data up front to block conflicting updates
- Distributed locking: coordinate access across multiple app instances

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

### Phase 3: Distributed Locking in Multi-Instance APIs (Web API)
**Topics**
- Why optimistic/pessimistic locking alone fails across instances: separate processes can oversell without a shared lock
- Redis locking primitives: SET NX PX for acquire, Lua scripts for safe release, fencing tokens to avoid stale owners
- Lock expiry, renewal, and failure modes: understand timeouts, lost locks, and crashes

**Project: InventoryService (Distributed Locking Focus)**
- Run multiple API instances and simulate cross-instance contention
- Add Redis-backed locking around reservations
- Contrast behavior with optimistic and pessimistic approaches

**Hands-on outcomes**
- Understand when distributed locks are required
- See how Redis locks prevent cross-instance oversell
- Explain tradeoffs vs DB-only locking

### Phase 4: Query Composition and Predicates (Console App)
**Topics**
- Custom `Where` implementation: build the filtering behavior that LINQ provides
- Delegate parameters for `Where`: functions that decide whether each item is included
- `IEnumerable<T>` vs `IQueryable<T>`: in-memory execution vs provider-translated execution
- Deferred execution: queries run only when enumerated
- Predicate composition: combine filters with AND/OR using expression trees

**Project: PredicatePlayground**
- Implement a custom `Where` extension and mirror LINQ semantics
- Support both item-only and item+index predicate overloads
- Build a predicate builder for complex filters (AND/OR, nested conditions)
- Show how expression trees differ from compiled delegates

**Hands-on outcomes**
- Understand what `Where` accepts and why
- Build complex, reusable predicates safely
- See how query providers translate expressions

### Phase 5: Factory Pattern and Resolver with `ConcurrentDictionary` (Console App)
**Topics**
- Factory pattern and service resolution: create objects based on a key or type
- Thread-safe registration and lookup: avoid races when multiple threads resolve services
- Lazy initialization and `GetOrAdd` semantics: create once on demand and reuse safely

**Project: ResolverFactory**
- Register factories by key and resolve services on demand
- Use `ConcurrentDictionary` to ensure single creation per key
- Stress-test with parallel resolves and measure contention

**Hands-on outcomes**
- Build a concurrency-safe factory resolver
- Understand when to cache instances vs factories
- Spot double-initialization risks under load

### Phase 6: Transactions and Consistency (Console App or Worker)
**Topics**
- 2-phase commit (2PC): coordinator asks participants to prepare, then commits or aborts
- 3-phase commit (3PC): adds a pre-commit step to reduce blocking
- Distributed transactions: atomic work across multiple services or databases
- Strong vs eventual consistency: immediate correctness vs delayed convergence
- Sagas and compensating actions: step-by-step workflow with rollback steps

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

### Phase 7: Idempotency and API Design (Web API)
**Topics**
- Idempotency: retries return the same outcome without duplicate effects
- API design process: define resources, verbs, errors, and contracts before coding

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

### Phase 8: Database Design and Indexing (Console App + SQL)
**Topics**
- Database design process: requirements -> entities -> relationships -> schema
- Indexing strategies: use indexes to speed reads with a write cost

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

### Phase 9: Sharding and Distributed Cache (Web API + Console Benchmark)
**Topics**
- Sharding with consistent hashing: spread data across nodes with minimal rebalancing
- Distributed cache patterns: place data near compute to reduce DB load
- Cache-aside: check cache first, then DB, then populate cache

**Project: ShardedUserStore**
- Consistent hashing across 3 shards
- Rebalance when a 4th shard is added and observe key migration
- Cache-aside Redis layer with TTL and invalidation
- Benchmark DB-only vs cache-hit vs cache-miss

**Hands-on outcomes**
- Build a hash ring with virtual nodes
- Handle hotspots and cache stampede risks
- Compare read-through, write-through, write-behind

### Phase 10: Domain-Driven Design and Patterns (Web API)
**Topics**
- Domain-driven design: model software around business concepts and boundaries
- Repository pattern: collection-like access to aggregates without DB details
- Unit of Work: group changes into one transaction

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

### Phase 11: Consensus and Leader Election (Console App)
**Topics**
- Consensus fundamentals (Raft-style concepts): agree on a single leader and log order
- Leader election, heartbeats, quorum: pick a leader and prove it is alive to a majority
- Split-brain risks and term/epoch handling: prevent two leaders from acting at once

**Project: MiniRaftCluster**
- Simulate 3 nodes with in-memory message passing
- Implement leader election with randomized timeouts
- Heartbeat loop from leader to followers
- Kill the leader and observe re-election
- Log term changes and vote decisions to a file

**Hands-on outcomes**
- See why quorum matters
- Understand leader lease/heartbeat behavior
- Explain basic split-brain protections

### Phase 12: Replication and Consistency (Console App)
**Topics**
- Sync vs async replication: wait for replicas or update them later
- Read-your-writes: clients see their own latest writes
- Monotonic reads: once you see a value, you do not see older values
- Eventual consistency: replicas converge over time
- Stale reads and conflict resolution: handle lag and choose a merge strategy

**Project: ReplicatedKeyValueStore**
- Leader node accepts writes; followers replicate async
- Read from leader vs follower and compare staleness
- Add configurable replication delay to visualize lag
- Demonstrate conflict with concurrent writes and resolve with last-write-wins

**Hands-on outcomes**
- See how lag creates stale reads
- Compare strong vs eventual behavior
- Understand tradeoffs in consistency models

### Phase 13: Service Discovery and Health (Web API)
**Topics**
- Service registry concepts and TTL: services register and renew with expirations
- Liveness vs readiness: alive process vs ready to serve traffic
- Client-side vs server-side discovery: caller chooses an instance vs a load balancer

**Project: ServiceRegistryPOC**
- Registry API with register/renew/lookup endpoints
- Services send heartbeats and register metadata
- Simulate node churn and stale entries
- Client resolves service instances and retries on failure

**Hands-on outcomes**
- Distinguish liveness and readiness checks
- See how TTL prevents stale routing
- Understand basic discovery patterns

### Phase 14: Backpressure and Load Shedding (Web API + Console Load Generator)
**Topics**
- Backpressure signals and queue depth: slow callers when the system is saturated
- Load shedding strategies: drop or degrade work to keep core traffic healthy
- Rate limiting and adaptive throttling: control request rate per client or per service

**Project: ThrottledOrderAPI**
- Bounded queue for incoming requests
- Reject or degrade when queue is full
- Implement token bucket rate limiting
- Load test with burst traffic and measure p95 latency

**Hands-on outcomes**
- See why unlimited queues fail under load
- Compare reject vs degrade strategies
- Interpret latency vs throughput tradeoffs

### Phase 15: Circuit Breakers and Retries (Web API)
**Topics**
- Retry policies (exponential backoff + jitter): spaced retries to avoid thundering herds
- Circuit breaker states: open stops calls, half-open probes, closed is normal
- Timeouts: fail fast to protect latency budgets
- Bulkheads: separate resource pools for isolation

**Project: ResilientPayments**
- Payment API calls a flaky downstream service
- Implement retry + jitter and measure success rate
- Add circuit breaker with half-open probes
- Compare with and without timeouts

**Hands-on outcomes**
- Understand retry storms and jitter benefits
- See how circuit breakers protect latency
- Distinguish timeout vs retry responsibility

### Phase 16: Fault Injection (Console App)
**Topics**
- Failure modes: process crash, slow responses, and partial availability
- Chaos testing basics and safety rails: inject faults safely to learn behavior

**Project: ChaosLiteHarness**
- Inject latency, random errors, and node kill
- Run predefined failure scripts
- Capture success rate and recovery time

**Hands-on outcomes**
- Observe system behavior under faults
- Measure mean time to recover
- Learn safe fault injection practices

### Phase 17: Messaging Patterns (Web API + Worker)
**Topics**
- Work queues vs pub/sub: one consumer vs many consumers per message
- At-least-once delivery and duplicates: assume retries and dedupe
- Dead-letter queues and replay: capture failed messages and reprocess later
- Transactional outbox and inbox: make DB writes and messages consistent
- Change data capture (change data tracking): convert table changes into events

**Project: MessageFlowLab**
- Producer API publishes to a queue and a topic
- Worker consumes with retries and DLQ
- Reprocess DLQ and demonstrate duplicates
- Track message IDs for dedupe
- Add outbox table for reliable publish
- Add inbox table for idempotent consumption

**Hands-on outcomes**
- Understand queue vs pub/sub behavior
- See why DLQs matter for failures
- Learn practical deduping

### Phase 18: Event Sourcing and CQRS (Web API)
**Topics**
- Event sourcing fundamentals: store state as a sequence of events
- CQRS read models and projections: separate write model from optimized reads
- Rebuild state from event log: replay events to reconstruct current state

**Project: EventStoreShop**
- Append-only event log for orders
- Project events into a read model table
- Rebuild read model from scratch
- Demonstrate audit trail and time travel

**Hands-on outcomes**
- See how events become the source of truth
- Understand projection lag and rebuild cost
- Compare command vs query model responsibilities

### Phase 19: Time and Ordering (Console App)
**Topics**
- Logical clocks (Lamport): order events without synchronized clocks
- Causal ordering and message reordering: preserve cause-effect relationships
- Time skew and its impact: clock drift can break ordering assumptions

**Project: OrderedEventBus**
- Simulate out-of-order delivery
- Attach logical timestamps and reorder
- Show causal vs non-causal sequences

**Hands-on outcomes**
- Understand why wall-clock time is unreliable
- See how logical clocks help ordering
- Recognize causal vs concurrent events

### Phase 20: Observability (Web API)
**Topics**
- Structured logging: logs with consistent fields for search and analytics
- Metrics: numeric measurements like latency and error rate
- Tracing: follow a request across services
- Correlation IDs and request propagation: carry a single ID across calls
- SLOs and error budgets basics: reliability targets and allowed failure rates

**Project: ObservabilityStarter**
- Add correlation ID middleware
- Emit structured logs and request duration metrics
- Add distributed tracing across 2 services
- Build a dashboard with p95 latency and error rate

**Hands-on outcomes**
- Trace a request across services
- Interpret key service health signals
- Use metrics to catch regressions

### Phase 21: Multi-Region Basics (Console App + Web API)
**Topics**
- Latency tradeoffs and quorum reads: consistency improves with more replicas but adds latency
- Active-active vs active-passive: write in many regions vs one primary
- Read replicas and failover: redirect reads and promote a replica on failure

**Project: MultiRegionReadModel**
- Simulate two regions with different latency
- Write to primary and replicate to secondary
- Compare local vs cross-region reads
- Demonstrate failover to secondary

**Hands-on outcomes**
- See latency vs consistency tradeoffs
- Understand failover behaviors

### Phase 22: Legacy Migration and Strangler Pattern (Web API + Proxy)
**Topics**
- Strangler pattern and incremental cutover: replace endpoints gradually via a proxy
- Routing rules and safety rails: traffic splits, feature flags, and rollback switches
- Shadow traffic and diffing: send a copy to the new service and compare results

**Project: StranglerGateway**
- Legacy service and new service with overlapping endpoints
- Proxy routes % of traffic to the new service
- Shadow requests compare responses and log diffs
- Gradual cutover plan with rollback switch

**Hands-on outcomes**
- Migrate endpoints without a big-bang rewrite
- Build confidence with shadow traffic and rollback
- Recognize multi-region design constraints

## Notes
- We will create folders and scaffolding as we progress phase by phase.
- Each phase README will document intended issues, fixes, and cross-questions.
