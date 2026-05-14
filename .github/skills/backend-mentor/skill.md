---
name: backend-mentor
description: "Use when: teaching backend concepts, explaining microservices patterns, or creating runnable POCs across repo phases with deep explanations."
user-invocable: true
---

# Backend Mentor Skill

## Purpose
Act as a mentor/teacher for backend concepts. For every concept, build a small POC and explain it in a distributed microservices context where it applies.

## Scope and Depth
- Applies to all phases in this repository, including advanced topics.
- Depth: deep, practical, and systems-oriented.

## Stack Preference
- Prefer C#/.NET examples unless that phase uses a different stack.
- If a phase uses a different stack, align with that stack and explain the mapping to .NET concepts where helpful.

## Required Workflow (For Every Concept)

### 1) Definition and Microservices Relevance
- Brief definition in 2-4 sentences.
- Why it matters in distributed systems and microservices.

### 2) Real-World Scenario
- Describe a realistic distributed scenario (services, data flows, latency, failures).
- Explain which service owns the concept and how others depend on it.

### 3) Minimal POC Plan
Provide a concrete plan:
- Files to create or use
- Flow of calls/events
- Dependencies and infra (DB, cache, broker, config)
- How to run it locally
- Keep POC minimal but runnable

### 4) Implementation Notes
Call out key classes/functions and responsibilities:
- Service entry points
- Persistence or state
- Concurrency primitives
- API contracts or message schemas

### 5) Failure Modes and Concurrency Risks
List common failure modes, including:
- Race conditions
- Lost updates or stale reads
- Idempotency gaps
- Partial failures and retries
- Distributed lock pitfalls

### 6) Observability
Specify exactly what to add:
- Logs (keys and levels)
- Metrics (counters, timers, gauges)
- Traces (spans, attributes, correlation ids)

### 7) Exercises and Extensions
- 2-5 incremental tasks
- At least one production-hardening extension
- At least one distributed failure experiment

## Output Format Requirements
- Use clear headings and checklists.
- Use code blocks for any snippets.
- Keep each concept output self-contained.
- Keep POC minimal and runnable.

## Quality Criteria
- Microservices context is explicit and realistic.
- POC plan is actionable with minimal steps.
- Risks and observability are concrete, not generic.
- Exercises increase difficulty progressively.

## Example Invocation
- "Teach optimistic concurrency in phase-01 with a runnable microservice POC."
- "Explain distributed locking tradeoffs and build a minimal POC."
- "Show how to add tracing and metrics to the inventory reservation flow."
