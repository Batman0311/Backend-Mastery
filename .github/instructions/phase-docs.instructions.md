---
description: "Use when creating or updating phase documentation, including phase READMEs, new phase folders, or learning plan changes per roadmap."
applyTo: "**"
---
# Phase Documentation Rules

- Follow the learning pattern: implement the issue first (intentionally buggy), reproduce it, then apply the production-ready fix and compare results.
- Use the README template and section order from docs/phase-template.md.
- Document all production-ready fixes when multiple exist, and explain why the selected fix was chosen.
- Prefer a simple in-memory demo first; add a DB-backed demo when feasible or required.
- Keep the recommended folder structure with Demos/ and optional Data/ and Models/ folders.

# Repo Roadmap Alignment

- Phase naming must match the roadmap pattern (phase-xx-name).
- Topics, project goals, and hands-on outcomes should align with the roadmap for that phase.
- Use Dot Net 8, Swagger for Web APIs, and SQLite + EF Core when DB is required.

# Code and Demo Notes to Include

- Keep Program.cs limited to service registration and endpoint routing; move demo logic into Demos/.
- Add short comments where behavior is non-obvious, especially for intentional bugs.
- Add a brief enterprise scenario comment near each bug reproduction.
- At the method level, add a short comment describing the production scenario it represents.
- Add a short comment where the timing window happens (read-modify-write, lock order, transaction overlap).
- Provide a fixed variant for each bug in the same demo file or a paired demo file.
- If a phase explicitly requests line-by-line comments, add them only for that phase.
