# Orchestrator (PowerShell)

The orchestrator is **not** a Claude agent — it's a PowerShell coordinator script. This document exists so humans (and any Claude session run inside this directory for debugging) understand how the pipeline is wired.

## What it does
For each phase (1–7), in order:
1. Run the producer (`<NN>-<name>/run.ps1 -Auto`).
2. Run the critic (`<NN>-<name>-critic/run.ps1 -Auto`).
3. Read the last line of the critic's output file: `pipeline/<NN>-<phase>/critic-<iteration>.md`.
4. If it matches `VERDICT: APPROVED` → advance to next phase.
5. If it matches `VERDICT: BLOCKED` → re-run producer with the next iteration number. The producer's CLAUDE.md tells it to find and address the latest `critic-<N>.md`. Cap at `-MaxIterations` (default 3) per phase.
6. If still blocked after max iterations → write a summary to the log and halt for human intervention.

Phase 7 (deployment) has no critic — runs once and produces a runbook.

## Phase 1 special case
The BA is **interactive** on first run (no `-Auto`). The orchestrator detects that `pipeline/01-spec/spec.md` does not exist yet and launches BA without `-Auto`; once the file appears, the orchestrator continues with the BA critic. On reruns (spec.md already exists), BA runs in `-Auto` mode like other producers.

## Token economy
- Critics' verdicts are written to disk; orchestrator only reads the last line, not the full critique.
- Each agent re-reads its inputs from disk every iteration. No cross-agent context sharing in-memory.
- Producers read the latest critic file from disk themselves, so the orchestrator doesn't shuttle text between processes.

## Log
`pipeline/_logs/orchestrator.log` — append-only, one line per agent invocation with timestamp, phase, iteration, verdict, duration.

## Manual recovery
If a phase is stuck:
- Inspect `pipeline/<NN>-<phase>/critic-<N>.md` to read the blockers.
- Run that phase's producer directly without `-Auto` to fix interactively.
- Re-run orchestrator with `-StartPhase N` to resume.

## Usage
```powershell
# Run full pipeline from phase 1
agents-v2\orchestrator\run.ps1

# Resume from phase 4 (e.g. after fixing data design manually)
agents-v2\orchestrator\run.ps1 -StartPhase 4

# Custom iteration cap
agents-v2\orchestrator\run.ps1 -MaxIterations 5
```
