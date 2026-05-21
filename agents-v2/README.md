# agents-v2 — Full SDLC Agent Pipeline

A 13-agent + 1 orchestrator pipeline that takes a raw user idea and produces a deployable system. Each phase pairs a **producer** with a **critic**; the orchestrator loops producer→critic until the critic approves, then advances.

## Pipeline Phases

| # | Producer | Critic | Output dir |
|---|---|---|---|
| 1 | BA (interactive) | BA Critic | `pipeline/01-spec/` |
| 2 | System Architect | Architect Critic | `pipeline/02-architecture/` |
| 3 | Data Designer | Data Critic | `pipeline/03-data/` |
| 4 | Backend Developer | Backend Critic | `pipeline/04-backend/` |
| 5 | Frontend Developer | Frontend Critic | `pipeline/05-frontend/` |
| 6 | QA / Tester | QA Critic | `pipeline/06-qa/` |
| 7 | Deployment Agent | (no critic) | `pipeline/07-deployment/` |

## Agent Communication via Filesystem (not direct invocation)

Agents do **not** invoke each other. Each agent:
- Reads its inputs from `pipeline/` (prior stage artifacts).
- Writes its output to a specific file under `pipeline/<NN>-<phase>/`.

The orchestrator (PowerShell) spawns each agent as a separate `claude -p` process. It parses the **last line** of each critic's output file:

```
VERDICT: APPROVED
VERDICT: BLOCKED
```

If `BLOCKED`, the orchestrator re-runs the producer (passing the critic's numbered fix list as `-CriticFeedback`). Iteration cap = 3 per phase by default.

This pattern minimizes orchestrator token usage: it never loads full artifacts into its own context — only the verdict line.

## Model Selection (tiered)

| Agent | Model | Why |
|---|---|---|
| BA (interactive) | Sonnet 4.6 | Needs reasoning + conversation |
| BA critic | Haiku 4.5 | Checks for clarity / structure |
| Architect | Sonnet 4.6 | Trade-off reasoning across Azure services |
| Architect critic | Sonnet 4.6 | Architecture review needs depth |
| Data designer | Sonnet 4.6 | Schema design |
| Data critic | Sonnet 4.6 | Query / index review needs depth |
| Backend dev | Sonnet 4.6 | Code generation |
| Backend critic | Sonnet 4.6 | Security + correctness review |
| Frontend dev | Sonnet 4.6 | Blazor code |
| Frontend critic | Sonnet 4.6 | Accessibility + correctness |
| QA | Sonnet 4.6 | Test plan + Playwright |
| QA critic | Haiku 4.5 | Plan completeness check |
| Deployment | Sonnet 4.6 | Bicep + YAML |
| Orchestrator | n/a (PS) | PowerShell, no LLM cost |

## Run the Pipeline

```powershell
# Interactive end-to-end
agents-v2\orchestrator\run.ps1

# Resume from a specific phase (e.g. re-run backend after fixing data design)
agents-v2\orchestrator\run.ps1 -StartPhase 4

# Limit a single phase
agents-v2\01-ba\run.ps1                 # BA in interactive mode
agents-v2\02-architect\run.ps1 -Auto    # Architect non-interactive
```

## Required: pipeline/00-input/user-prompt.md

The BA agent prompts the user interactively on first run and writes the captured idea here as a checkpoint. On rerun, the BA reads this file as context and confirms or refines.

## Iteration log

Each phase appends to `pipeline/_logs/orchestrator.log` so you can see retry counts and timing.
