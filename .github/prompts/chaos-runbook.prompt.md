---
description: "Generate a reusable chaos runbook for a specific phase, including hypothesis, faults, safety rails, and observability."
name: "Chaos Runbook"
argument-hint: "Phase name, target service, failure type, and success criteria"
agent: "agent"
---
Generate a reusable chaos runbook for the specified phase.

Inputs:
- Phase name (e.g., phase-15-resilience)
- Target service or demo
- Failure type (latency, error, outage, resource exhaustion, message loss)
- Success criteria

Output requirements:
- Use clear headings and checklists.
- Include a one-sentence hypothesis.
- Provide a step-by-step runbook (prepare, inject, observe, recover).
- Include guardrails and stop conditions.
- Specify logs, metrics, and traces to capture.
- List baseline measurements and expected deltas.
- Keep it runnable and minimal; prefer .NET examples when relevant.

Output format:
# Chaos Runbook: <Phase> - <Scenario>

## Goal and Hypothesis
- Hypothesis: <one sentence>

## Scope
- Services:
- Dependencies:
- Environment:
- Traffic:

## Safety Rails
- [ ]
- [ ]

## Baseline
- Metrics to capture:
- Baseline values:

## Fault Injection
- Type:
- Duration:
- Intensity:
- Injection method:

## Execution Steps
1.
2.
3.

## Observability Plan
- Logs:
- Metrics:
- Traces:

## Expected Outcomes
- Success criteria:
- Failure signals:

## Recovery Steps
1.
2.

## Follow-up Actions
- Fixes:
- Next experiment:
