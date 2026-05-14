---
description: "Use when adding observability instrumentation in services, demos, or POCs. Covers logs, metrics, traces, and correlation conventions."
applyTo: "**/*.cs"
---
# Observability Instrumentation Conventions

## Logging
- Use structured logs with consistent keys: `service`, `operation`, `correlationId`, `requestId`, `entityId`, `durationMs`, `result`.
- Log at INFO for normal flow, WARN for retries or partial failures, ERROR for failed requests.
- Include the enterprise scenario and demo mode in the first log line when reproducing bugs.

## Metrics
- Emit counters for outcomes: `requests_total`, `errors_total`, `retries_total`.
- Emit timers for latency: `request_duration_ms` with p95/p99 focus.
- Add gauges where saturation matters: `inflight_requests`, `queue_depth`.

## Tracing
- Create a root span per request or demo run.
- Propagate `correlationId` across service boundaries.
- Include dependency spans for DB, cache, and external calls.

## Naming and Tagging
- Use lowercase, dot or underscore separators for metric names.
- Tag spans and metrics with `service`, `operation`, `phase`, and `demoMode`.

## Output Expectations
- Each demo should state where to observe logs and which metrics to watch.
- If a failure is intentional, log the expected failure mode explicitly.
