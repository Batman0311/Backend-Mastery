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

## Demo Modes and Methods
- **<mode name>** (`dotnet run -- <mode>`)
  - Calls: `<ClassName>.<MethodName>()`
  - What it demonstrates: <one sentence>
  - Concurrency note: <how work overlaps; Task.Run/Parallel.ForEach/Thread behavior>
  - Enterprise scenario: <real-world situation that produces the same timing window>
  - Core steps:
    1. <step>
    2. <step>
    3. <step>
  - What to observe: <expected output or behavior>

## Code Walkthrough (per demo)
- **Goal:** <what bug or fix the demo teaches>
- **Shared state:** <what is shared, why it is risky>
- **Concurrency model:** <Task/Thread/Parallel.ForEach>
- **Critical section:** <where synchronization is required>
- **Failure mode:** <how and where the bug shows>
- **Fix mechanics:** <why the fix works>

## Method Notes
- **<MethodName>()**
  - Inputs: <args and meaning>
  - Outputs: <return values or side effects>
  - Production scenario: <real-world trigger for this method>
  - Steps:
    1. <step>
    2. <step>
  - Pitfalls: <what can go wrong>
  - Fix: <how the fixed variant changes the steps>

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

## Expected Output (Samples)
- <short sample output for broken case>
- <short sample output for fixed case>
```

## Code Guidelines
- Keep Program.cs limited to service registration and endpoint routing only.
- For Web APIs, move request handling into Controllers/ classes.
- Move all demo logic into files under Demos/.
- Add short comments where behavior is non-obvious, especially where code is intentionally buggy.
- When using `Task.Run`, add a brief comment that explains thread-pool concurrency and why overlapping work reproduces the bug in real systems.
- Add an "enterprise scenario" comment near each bug reproduction to connect it to production traffic patterns.
- At the method level, add a short comment that describes the production scenario the method represents.
- At the code level, add a short comment where the timing window happens (read-modify-write, lock order, transaction overlap).
- For each bug, add a fixed variant in the same demo file or a paired demo file.

## Example Demo Modes
- `race` / `race-fixed`
- `deadlock` / `deadlock-fixed`
- `db-race` / `db-fixed`
