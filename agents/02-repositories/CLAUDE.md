# Repository Agent — TimeQuest

## Model
**claude-sonnet-4-6**
Repository implementations are pattern-based code generation. Each repository follows the same structure: inherit from `Repository<T>`, inject `ApplicationDbContext`, write LINQ queries. No deep reasoning needed — Sonnet handles this reliably and quickly.

## Role
You are the Repository Agent for TimeQuest. You implement the `LevelHelper` static class and all five data repositories. You execute Tasks 13–19 from the implementation plan.

## Prerequisites (must already exist before you start)
The Foundation Agent must have completed Tasks 1–12. Verify before starting:
```powershell
dotnet build C:\TimeQuest\TimeQuest
# Must succeed with 0 errors
```

If the build fails, stop and report the error. Do not write any code until the foundation is clean.

## What You Will Build

1. **LevelHelper** (`C:\TimeQuest\TimeQuest\Helpers\LevelHelper.cs`)
   - Static class with hardcoded level table (10 levels)
   - `GetLevel(int xp)` → returns current level (1-10)
   - `GetTitle(int xp)` → returns level title string
   - `GetXpForNextLevel(int xp)` → returns XP needed to reach next level

2. **Generic Repository** (`C:\TimeQuest\TimeQuest\Repositories\`)
   - `IRepository<T>` interface
   - `Repository<T>` base class (wraps `ApplicationDbContext`)

3. **Five Specific Repositories** (each has an interface + implementation):
   - `ITimeEntryRepository` / `TimeEntryRepository`
   - `IUserRepository` / `UserRepository` (note: does NOT extend `IRepository<T>` — user ID is `string`, not `int`)
   - `ITeamRepository` / `TeamRepository`
   - `IProjectRepository` / `ProjectRepository`
   - `IBadgeRepository` / `BadgeRepository`

## Key Implementation Notes

### UserRepository is different from the others
`ApplicationUser` has a `string` ID (inherited from `IdentityUser`). The generic `IRepository<T>.GetByIdAsync(int id)` doesn't fit. `IUserRepository` defines its own `GetByIdAsync(string userId)` and does NOT extend `IRepository<T>`.

### TimeEntryRepository.GetPendingForManagerAsync
This is the most complex query. The logic:
1. Find all team IDs where `UserTeam.UserId == managerId && UserTeam.Role == TeamRole.Manager`
2. Find all user IDs who are `Member` in those teams
3. Return `TimeEntries` where `UserId IN (memberIds) && Status == Submitted`

### EF Include patterns
Most repository methods need `.Include()` for nav properties. Follow the plan — the exact `Include` chains are specified.

## Namespaces
- Models: `TimeQuest.Models`
- Data: `TimeQuest.Data`
- Repositories: `TimeQuest.Repositories`
- Helpers: `TimeQuest.Helpers`

## Rules
- Read the full plan at `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md` before writing code
- Run `dotnet build` after each file to catch issues early
- Method signatures must exactly match what the Services Agent will call (they are defined in the interfaces)
- Do not add methods not listed in the plan — services depend on exactly what is defined
- Commit when all repositories are complete and the build passes
