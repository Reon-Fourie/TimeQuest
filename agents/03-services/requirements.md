# Services Agent — Requirements

## Source of Truth
Read before writing code:
- Plan: `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`
- Design: `C:\TimeQuest\design.md`

## Pre-Flight Check
```powershell
dotnet build C:\TimeQuest\TimeQuest       # must succeed
dotnet test C:\TimeQuest\TimeQuest.Tests  # must pass
```
Stop and report if either fails.

## Tasks to Execute

Execute **Tasks 20 through 25** from the implementation plan.

### Task 20 — XpService
Create `C:\TimeQuest\TimeQuest\Services\XpService.cs`

```csharp
namespace TimeQuest.Services;
// Injects: IUserRepository
// AwardXpAsync(string userId, decimal hours):
//   user.XP += (int)(hours * 10)
//   user.Level = LevelHelper.GetLevel(user.XP)
//   _users.Update(user); await _users.SaveChangesAsync()
```

### Task 21 — BadgeService
Create `C:\TimeQuest\TimeQuest\Services\BadgeService.cs`

Injects: `IBadgeRepository`, `ITimeEntryRepository`, `ApplicationDbContext`

The `ShouldAwardAsync` switch must handle all 7 `BadgeCriteria` values. No `_ => false` short-circuit that silently skips unhandled cases — use `_ => false` only as a true default.

### Task 22 — TimesheetService
Create `C:\TimeQuest\TimeQuest\Services\TimesheetService.cs`

Injects: `ITimeEntryRepository`, `XpService`, `BadgeService`

State machine rules to enforce:
- `SubmitAsync`: entry must be `Draft` or `Rejected` — throw `InvalidOperationException` otherwise
- `ApproveAsync`: entry must be `Submitted` — throw otherwise. After saving: call `XpService.AwardXpAsync`, then `BadgeService.CheckAndAwardBadgesAsync`
- `RejectAsync`: entry must be `Submitted` — throw otherwise

### Task 23 — TeamService
Create `C:\TimeQuest\TimeQuest\Services\TeamService.cs`

Injects: `ITeamRepository`, `ApplicationDbContext`

`AddMemberAsync`: if a `UserTeam` record already exists for (teamId, userId), update its Role; otherwise create new. Do not create duplicate records.

### Task 24 — ReportingService
Create `C:\TimeQuest\TimeQuest\Services\ReportingService.cs`

Injects: `IUserRepository`, `ITimeEntryRepository`, `ITeamRepository`, `ApplicationDbContext`

`GetUserStatsAsync` must populate all fields of `UserStatsDto`:
- `LevelTitle` from `LevelHelper.GetTitle(user.XP)`
- `XpToNextLevel` from `LevelHelper.GetXpForNextLevel(user.XP)`
- `HoursByProject` from EF GroupBy query on approved TimeEntries

`GetOrgReportAsync` calls `GetUserStatsAsync` per user in the leaderboard — loop is acceptable here.

### Task 25 — SeedData
Create `C:\TimeQuest\TimeQuest\Data\SeedData.cs`

Create `wwwroot/badges/` directory:
```powershell
New-Item -ItemType Directory -Force "C:\TimeQuest\TimeQuest\wwwroot\badges"
New-Item -ItemType File "C:\TimeQuest\TimeQuest\wwwroot\badges\.gitkeep"
```

Badge image paths use this naming convention:
- `badges/first-quest.png`
- `badges/centurion.png`
- `badges/legendary.png`
- `badges/on-a-roll.png`
- `badges/speed-runner.png`
- `badges/team-player.png`
- `badges/overachiever.png`

Guard: `if (!await context.Badges.AnyAsync())` — only seed badges if table is empty.

## Completion Criteria

- [ ] `dotnet build C:\TimeQuest\TimeQuest` → `Build succeeded. 0 Error(s)`
- [ ] `C:\TimeQuest\TimeQuest\Services\` contains: `XpService.cs`, `BadgeService.cs`, `TimesheetService.cs`, `TeamService.cs`, `ReportingService.cs`
- [ ] `C:\TimeQuest\TimeQuest\Data\SeedData.cs` exists
- [ ] `C:\TimeQuest\TimeQuest\wwwroot\badges\.gitkeep` exists

## Commit When Done
```
git -C C:\TimeQuest commit -m "feat: services layer - XP, badges, timesheet, teams, reporting, seed data"
```
