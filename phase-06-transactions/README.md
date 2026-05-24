# Phase 6 - Transactions and Consistency

## Topics
- 2-phase commit (2PC): coordinator asks participants to prepare, then commits or aborts
- 3-phase commit (3PC): adds a pre-commit step to reduce blocking
- Distributed transactions across services or databases
- Strong vs eventual consistency: immediate correctness vs delayed convergence
- Sagas and compensating actions: step-by-step workflow with rollback steps

## Project
**OrderFulfillmentSaga (Console App)**
- Simulate Payment, Inventory, Shipping services
- 2PC coordinator: Prepare -> Vote -> Commit, simulate coordinator crash
- 3PC: add Pre-Commit and observe behavior on coordinator failure
- Saga: event-driven steps with compensating transactions
- Log every phase transition to a file for visualization

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **2PC blocking on coordinator failure**
  - Issue: participants remain in the prepared state and hold resources when the coordinator crashes after Prepare.
  - Production-ready options:
    - Use 3PC to add a pre-commit phase so participants can safely proceed after a timeout.
    - Add bounded timeouts with heuristic aborts (accepting possible inconsistency).
    - Avoid distributed transactions and use Sagas with compensations.
  - Selected fix and why:
    - Use Sagas for microservices to avoid blocking and reduce coordination coupling.
- **Missing or incorrect saga compensation**
  - Issue: a failed step leaves partial side effects (charge captured but inventory not reserved).
  - Production-ready options:
    - Implement compensating actions for each step and track state transitions.
    - Persist saga state and use retries with idempotent steps.
    - Use an outbox/inbox pattern for reliable message delivery and dedupe.
  - Selected fix and why:
    - Compensating actions + persisted saga state allow safe recovery without global locks.

## Demo Modes and Methods
- **2pc-block** (`dotnet run -- 2pc-block`)
  - Calls: `TwoPhaseCommitDemo.RunCoordinatorCrashBlocking()`
  - What it demonstrates: participants stay Prepared when the coordinator crashes after Prepare.
  - Concurrency note: participants prepare in parallel; the coordinator crash is simulated mid-flight.
  - Enterprise scenario: cross-service order fulfillment where one service locks inventory awaiting a global commit.
  - Core steps:
    1. Coordinator sends Prepare to all participants.
    2. Participants vote Yes and enter Prepared.
    3. Coordinator crashes before Commit/Abort is issued.
  - What to observe: participants remain Prepared and hold resources.
- **3pc-precommit** (`dotnet run -- 3pc-precommit`)
  - Calls: `ThreePhaseCommitDemo.RunPreCommitFlow()`
  - What it demonstrates: Pre-Commit lets participants safely commit after a timeout.
  - Concurrency note: prepare and pre-commit run in parallel; timeouts drive the final decision.
  - Enterprise scenario: multi-database update with coordination timeouts.
  - Core steps:
    1. Coordinator sends Prepare and collects votes.
    2. Coordinator sends Pre-Commit and crashes before final Commit.
    3. Participants time out and commit safely.
  - What to observe: no indefinite blocking, but still requires reliable timeouts.
- **saga-missing-comp** (`dotnet run -- saga-missing-comp`)
  - Calls: `SagaDemo.RunMissingCompensation()`
  - What it demonstrates: partial side effects when a step fails without compensation.
  - Concurrency note: steps execute sequentially but are modeled as async tasks to simulate remote calls.
  - Enterprise scenario: payment captured, then inventory reservation fails.
  - Core steps:
    1. Charge payment.
    2. Fail inventory reservation.
    3. Skip compensation.
  - What to observe: inconsistent state (charged but not reserved).
- **saga-compensated** (`dotnet run -- saga-compensated`)
  - Calls: `SagaDemo.RunCompensated()`
  - What it demonstrates: compensations restore consistency after a failed step.
  - Concurrency note: compensation steps run after failure with retry-friendly ordering.
  - Enterprise scenario: refund payment when downstream steps fail.
  - Core steps:
    1. Execute steps and track saga state.
    2. Fail a step.
    3. Run compensations in reverse order.
  - What to observe: consistency restored via compensating actions.

## Code Walkthrough (per demo)
- **Goal:** show why blocking 2PC and missing compensations are unsafe in distributed systems.
- **Shared state:** participant state (Prepared/Committed/Aborted) and saga step status.
- **Concurrency model:** Task.WhenAll for parallel participants; async steps for remote call simulation.
- **Critical section:** phase transition updates and resource holds around Prepared/Pre-Commit.
- **Failure mode:** indefinite Prepared state or un-compensated side effects.
- **Fix mechanics:** Pre-Commit timeouts and compensating actions backed by durable saga state.

## Method Notes
- **RunCoordinatorCrashBlocking()**
  - Inputs: none.
  - Outputs: console summary + phase log file.
  - Production scenario: coordinator crashes after Prepare, leaving participants locked.
  - Steps:
    1. Send Prepare to participants and collect votes.
    2. Simulate coordinator crash before Commit/Abort.
  - Pitfalls: indefinite blocking and resource exhaustion.
  - Fix: move to Saga or 3PC with bounded timeouts.
- **RunPreCommitFlow()**
  - Inputs: none.
  - Outputs: console summary + phase log file.
  - Production scenario: coordinator failure between Pre-Commit and Commit.
  - Steps:
    1. Execute Prepare and Pre-Commit.
    2. Use timeouts to decide Commit.
  - Pitfalls: still sensitive to network partitions.
  - Fix: use Sagas when strict atomicity is not required.
- **RunMissingCompensation()**
  - Inputs: none.
  - Outputs: console summary + saga log file.
  - Production scenario: payment succeeds, inventory fails, no refund.
  - Steps:
    1. Execute payment and inventory steps.
    2. Skip compensation on failure.
  - Pitfalls: user charged without fulfillment.
  - Fix: add compensating actions per step.
- **RunCompensated()**
  - Inputs: none.
  - Outputs: console summary + saga log file.
  - Production scenario: refund and release inventory after failure.
  - Steps:
    1. Track step status and failure.
    2. Execute compensations in reverse order.
  - Pitfalls: compensation must be idempotent.
  - Fix: persist saga state and retry safely.

## Cross-Questions
- Why does 2PC block on coordinator failure, and how does 3PC reduce that risk?
- When is eventual consistency acceptable for order workflows?
- What makes a compensation action safe and idempotent?
- How do timeouts and failure detectors affect the safety of 3PC?

## Code Examples (to add during implementation)
- TwoPhaseCommitCoordinator with Prepare/Vote/Commit flow
- ThreePhaseCommitCoordinator with Pre-Commit and timeout handling
- SagaOrchestrator with step execution, compensation, and state logging

## How to Run (Issue Reproduction)
- 2PC blocking demo: `dotnet run --project phase-06-transactions\OrderFulfillmentSaga -- 2pc-block`
- 3PC pre-commit demo: `dotnet run --project phase-06-transactions\OrderFulfillmentSaga -- 3pc-precommit`
- Saga missing compensation: `dotnet run --project phase-06-transactions\OrderFulfillmentSaga -- saga-missing-comp`
- Saga compensated: `dotnet run --project phase-06-transactions\OrderFulfillmentSaga -- saga-compensated`

## Tooling Notes
- Dot Net 8

## Expected Output (Samples)
- Broken 2PC: participants stuck in Prepared with resources held.
- Fixed 3PC: participants commit after timeout without indefinite blocking.
- Broken saga: payment captured but inventory not reserved.
- Fixed saga: compensation runs and state is restored.
