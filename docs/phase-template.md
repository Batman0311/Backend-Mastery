# Phase Template

Use this template for every phase so structure, learning flow, and documentation stay consistent.

## Learning Pattern
1. Implement the issue first (intentionally buggy).
2. Reproduce and observe the failure.
3. Apply the production-ready fix and compare results.

## Demo Coverage Rules
- If multiple production-ready fixes exist, document all of them and explain why one was chosen.
- If a problem can be reproduced and fixed using a database, add a DB-backed demo.
- Prefer a simple in-memory demo first when feasible, then add a DB-backed or complex demo.

## Folder Structure (recommended)
```
phase-xx-name/
  README.md
  <ProjectName>/
    Demos/
    Data/            (only if DB is needed)
    Models/          (only if DB is needed)
```

## README Template
```
# Phase X - <Title>

## Topics
- ...

## Project
**<ProjectName> (<App Type>)**
- ...

## Learning Pattern
- Implement the issue first (intentionally buggy).
- Reproduce and observe the failure.
- Apply the production-ready fix and compare results.

## Intended Issues and Fixes
- **<Issue name>**
  - Issue: <what goes wrong>
  - Production-ready options:
    - <option 1>
    - <option 2>
    - <option 3>
  - Selected fix and why:
    - <chosen approach + reason>

## Cross-Questions
- ...

## Code Examples (to add during implementation)
- ...

## How to Run (Issue Reproduction)
- <command 1>
- <command 2>

## Tooling Notes
- Dot Net 8
- Swagger for Web APIs
- SQLite + EF Core where DB is required
```

## Code Guidelines
- Keep Program.cs limited to the Main entry point and routing only.
- Move all demo logic into files under Demos/.
- Add short comments where behavior is non-obvious, especially where code is intentionally buggy.
- For each bug, add a fixed variant in the same demo file or a paired demo file.

## Example Demo Modes
- `race` / `race-fixed`
- `deadlock` / `deadlock-fixed`
- `db-race` / `db-fixed`
