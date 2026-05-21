# TimeQuest — Agent System

Six Claude Code agents that build the TimeQuest backend from scratch. Run them in order.

## Agent Map

| # | Agent | Model | Tasks | Phase |
|---|---|---|---|---|
| 0 | Orchestrator | claude-opus-4-7 | Coordinates all agents, validates output | Coordination |
| 1 | Foundation | claude-sonnet-4-6 | NuGet, Models, DbContext, Migration | Data Layer Setup |
| 2 | Repositories | claude-sonnet-4-6 | LevelHelper, Generic + 5 specific repos | Data Access |
| 3 | Services | claude-sonnet-4-6 | XpService, BadgeService, TimesheetService, TeamService, ReportingService, SeedData | Business Logic |
| 4 | Integration | claude-sonnet-4-6 | Final Program.cs wiring, wwwroot/badges/, Test project | Wiring + Tests |
| 5 | QA | claude-opus-4-7 | Build verify, test run, spec compliance, startup check | Quality |

## How to Run

### Run a single agent (interactive)
```powershell
# From any terminal
C:\TimeQuest\agents\01-foundation\run.ps1
```

### Run all agents in sequence (automated)
```powershell
C:\TimeQuest\agents\orchestrator\run.ps1
```

### Run a single agent (non-interactive / fully automated)
```powershell
C:\TimeQuest\agents\01-foundation\run.ps1 -Auto
```

## Prerequisites
- Claude Code CLI installed: `npm install -g @anthropic-ai/claude-code`
- Authenticated: `claude auth`
- SQL Server LocalDB installed (comes with Visual Studio)
- .NET 10 SDK installed

## Project Paths
- **Solution root:** `C:\TimeQuest\`
- **Blazor project:** `C:\TimeQuest\TimeQuest\`
- **Design spec:** `C:\TimeQuest\design.md`
- **Implementation plan:** `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`

## Agent Architecture

Each agent directory contains:
- `CLAUDE.md` — agent identity, role, constraints, and model selection rationale
- `requirements.md` — the exact tasks this agent must complete with expected outputs
- `run.ps1` — PowerShell script to invoke the agent

When `run.ps1` executes, it sets the working directory to the agent's own folder so `CLAUDE.md` is picked up as project instructions. The agent then operates on `C:\TimeQuest\` using absolute paths.
