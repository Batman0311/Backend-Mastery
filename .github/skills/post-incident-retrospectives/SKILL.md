---
name: post-incident-retrospectives
description: "Use when: performing post-incident analysis, reliability retrospectives, root-cause analysis, or corrective action planning for backend systems."
argument-hint: "Incident summary, impact window, systems involved, and desired outputs"
user-invocable: true
---

# Post-Incident Analysis and Reliability Retrospectives

## Purpose
Produce a blameless, actionable retrospective with clear timelines, root causes, and reliability improvements.

## When to Use
- After production incidents or major regressions
- For repeated outages or intermittent failures
- To create corrective actions, owners, and verification plans

## Required Workflow

### 1) Incident Summary
- What happened, when, and how users were impacted
- Duration, scope, and severity

Checklist:
- [ ] Time window stated
- [ ] User impact quantified
- [ ] Systems and teams listed

### 2) Timeline Reconstruction
- List key events in chronological order
- Include detection, mitigation, and recovery points

Checklist:
- [ ] Timestamps are in a single timezone
- [ ] Automated vs manual actions are distinguished

### 3) Root Cause Analysis
- Identify primary cause and contributing factors
- Distinguish trigger vs latent conditions

Checklist:
- [ ] Evidence cited for each cause
- [ ] Contributing factors are separate from symptoms

### 4) Impact Analysis
- User impact (errors, latency, data integrity)
- Business impact (SLA, revenue, support)

Checklist:
- [ ] Metrics and logs referenced
- [ ] Data correctness assessed

### 5) Detection and Response Evaluation
- How was the incident detected
- Which alerts fired or failed
- Response effectiveness and gaps

Checklist:
- [ ] Alert quality assessed (noise vs signal)
- [ ] On-call handoffs documented

### 6) Corrective Actions
- Engineering fixes with owners and due dates
- Observability improvements
- Process improvements

Checklist:
- [ ] Each action has owner and due date
- [ ] Each action has validation criteria

### 7) Follow-Up Verification
- Tests or experiments to validate fixes
- Runbook updates or training items

Checklist:
- [ ] Verification plan defined
- [ ] Runbook changes listed

## Output Format Requirements
- Use clear headings and checklists.
- Keep the tone blameless and factual.
- Provide a final action list with owners and deadlines.
- Include a short executive summary.

## Quality Criteria
- Causes are supported by evidence, not assumptions.
- Actions are measurable and testable.
- Follow-up plan closes the loop on reliability.

## Example Prompts
- "Create a post-incident report for a Redis outage in phase-09."
- "Run a reliability retrospective for a retry storm in phase-15."
- "Draft a blameless RCA for a deadlock incident in phase-01."
