# Orchestrator Requirements

## Your Task

Coordinate the complete execution of the TimeQuest backend implementation plan.

## Step 1 — Read the plan

Read this file in full before doing anything:
`C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`

Also read the design spec:
`C:\TimeQuest\design.md`

## Step 2 — Verify starting state

Run these checks before starting any agent:
```powershell
# Confirm the Blazor project exists
Test-Path "C:\TimeQuest\TimeQuest\TimeQuest.csproj"

# Confirm no models folder exists yet (clean start)
Test-Path "C:\TimeQuest\TimeQuest\Models"

# Confirm no migrations yet
Test-Path "C:\TimeQuest\TimeQuest\Data\Migrations"
```

## Step 3 — Execute agents in sequence

Run each agent's run.ps1, validate, then proceed:

1. `C:\TimeQuest\agents\01-foundation\run.ps1`
   → Validate: `dotnet build C:\TimeQuest\TimeQuest` passes

2. `C:\TimeQuest\agents\02-repositories\run.ps1`
   → Validate: `dotnet build` passes, all repo files exist

3. `C:\TimeQuest\agents\03-services\run.ps1`
   → Validate: `dotnet build` passes, all service files exist

4. `C:\TimeQuest\agents\04-integration\run.ps1`
   → Validate: `dotnet build` passes, `dotnet test` passes

5. `C:\TimeQuest\agents\05-qa\run.ps1`
   → Validate: app starts, spec compliance confirmed

## Step 4 — Final report

Produce a completion report:
```
# TimeQuest Build Report
Date: <date>
Status: COMPLETE / FAILED

## Phases
- Foundation: PASS / FAIL
- Repositories: PASS / FAIL
- Services: PASS / FAIL
- Integration: PASS / FAIL
- QA: PASS / FAIL

## Issues Found
<list or "None">

## Next Steps
<UI phase 2 or blockers>
```

Save the report to `C:\TimeQuest\agents\build-report.md`.
