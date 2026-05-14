---
name: distributed-chaos-testing
description: "Use when: planning or executing distributed system testing, reliability validation, chaos experiments, failure injection, or resilience verification in microservices."
argument-hint: "Target phase/service, failure type, scope, and success criteria"
user-invocable: true
---

# Distributed System Testing and Chaos Experiments

## Purpose
Design and run controlled failure experiments for distributed systems, with safety rails, measurable hypotheses, and actionable outcomes.

## When to Use
- Validating resilience, retries, timeouts, or fallbacks across microservices
- Reproducing intermittent production failures in a safe environment
- Stressing dependencies (DB, cache, broker) to reveal failure modes
- Auditing observability coverage for failure cases

## Required Workflow

### 1) Define the Goal
- Identify the primary reliability concern (latency, availability, consistency, correctness)
- Write a single-sentence hypothesis
- Define success and failure criteria

Checklist:
- [ ] Hypothesis is testable and time-bounded
- [ ] Clear stop condition exists
- [ ] Expected blast radius documented

### 2) Map the Distributed Path
- List involved services and dependencies
- Identify critical requests, events, and data stores
- Specify ownership and on-call boundaries

Checklist:
- [ ] Call graph or sequence listed
- [ ] Shared state and concurrency points identified
- [ ] External dependencies called out

### 3) Choose Experiment Scope
Decision points:
- Local POC vs staging vs production
- Single service vs multi-service
- Synthetic load vs real traffic shadowing

Checklist:
- [ ] Environment selected based on risk
- [ ] Traffic source defined
- [ ] Rollback plan created

### 4) Design the Failure Injection
Pick one or two controlled faults:
- Latency injection
- Error injection (5xx, timeouts)
- Partial dependency outage
- Message loss or duplication
- Resource exhaustion (CPU, memory, threads)

Checklist:
- [ ] Fault is scoped and reversible
- [ ] Duration and intensity defined
- [ ] Injected failure maps to hypothesis

### 5) Prepare Observability
Specify what to measure:
- Logs: correlation id, request id, retry count, error code
- Metrics: error rate, p95 latency, retries, saturation
- Traces: parent-child span relationships and timings

Checklist:
- [ ] Baseline metrics captured
- [ ] Alerts or dashboards prepared
- [ ] Trace sampling rate set

### 6) Execute and Record
- Run the experiment
- Capture metrics, logs, traces, and timeline notes
- Stop if guardrails are hit

Checklist:
- [ ] Start and stop times recorded
- [ ] Guardrails monitored
- [ ] Raw artifacts saved

### 7) Analyze and Act
- Compare against baseline
- Identify failure modes and bottlenecks
- Propose fixes and validation steps

Checklist:
- [ ] Root cause or weak point documented
- [ ] Fix plan with owner and ETA
- [ ] Follow-up experiment defined

## Output Format Requirements
- Use clear headings and checklists.
- Provide a concrete experiment plan and runbook.
- Include code blocks for configuration or scripts.
- Keep output runnable and minimal.

## Quality Criteria
- Hypothesis is measurable and falsifiable
- Safety rails are explicit and enforced
- Observability is sufficient to explain outcomes
- Actions are clear and testable

## Example Prompts
- "Design a chaos experiment for the inventory reservation flow in phase-02."
- "Inject latency into Redis cache in phase-09 and measure impact."
- "Test retry storms in phase-15 with a controlled fault plan."
