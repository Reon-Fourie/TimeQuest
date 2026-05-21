# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

TimeQuest — a Blazor Web App timesheet system with RPG mechanics (XP, levels, badges). Stack: .NET 10, Blazor Web App (Interactive Server), ASP.NET Core Identity, EF Core 9, SQL Server.

The repo is in early scaffolding state: only the default Blazor template under `TimeQuest/` exists. The data layer, services, repositories, and identity are not yet implemented — they will be built out by the agent pipeline in `agents/` following `design.md` and `docs/superpowers/plans/2026-05-21-timequest-backend.md`.

## Authoritative specs

Before making non-trivial changes, read these — they define the intended architecture and are the source of truth, not the current scaffold:

- `design.md` — full design spec (data model, roles, XP/leveling, approval workflow, badges, services, repositories, reporting, seed data).
- `docs/superpowers/plans/2026-05-21-timequest-backend.md` — phased implementation plan.
- `docs/superpowers/specs/2026-05-21-timequest-design.md` — spec mirror.

Key architectural decisions from the spec:
- Single Blazor Web App project, no microservices. Data flow: Components → Services → Repositories → EF Core → SQL Server.
- Generic `Repository<T>` base wraps `ApplicationDbContext`; specific repos add domain query methods.
- XP awarded only on Manager/Admin **approval** of a `TimeEntry` at 10 XP/hour. Approval triggers `XpService.AwardXp` then `BadgeService.CheckAndAwardBadges`. `Level` is derived but **persisted** for query performance.
- Manager approval scope: a Manager can approve entries only for users in a `Team` where the Manager has `TeamRole.Manager` in `UserTeam`. Admins approve any.
- Three Identity roles seeded at startup: `Admin`, `Manager`, `Employee`. Seven badges seeded from `wwwroot/badges/`.

## Commands

Run from repo root (`C:\Dev\TimeQuest`):

```powershell
dotnet build TimeQuest.sln
dotnet run --project TimeQuest
dotnet ef migrations add <Name> --project TimeQuest
dotnet ef database update --project TimeQuest
```

There is no test project yet; the Integration agent (phase 4) will add one.

## Agent pipelines

Two pipelines live in this repo:

- **`agents-v2/`** — current generation. Full 13-agent SDLC (BA → Architect → Data → Backend → Frontend → QA → Deployment), each producer paired with a critic, coordinated by `agents-v2/orchestrator/run.ps1`. Producers + critics talk via files under `agents-v2/pipeline/`; the orchestrator only parses `VERDICT: APPROVED|BLOCKED` lines to drive iteration. See `agents-v2/README.md`.
- **`agents/`** — first-generation 6-agent build pipeline tied specifically to the TimeQuest backend plan (foundation → repos → services → integration → QA). Kept as reference. Its paths reference `C:\TimeQuest\` but the repo is at `C:\Dev\TimeQuest\`.

When iterating on the agent system itself, edit `agents-v2/`. Don't bring the old `agents/` along.
