# Repository Agent — Requirements

## Source of Truth
Read before writing code:
- Plan: `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`
- Design: `C:\TimeQuest\design.md`

## Pre-Flight Check
```powershell
dotnet build C:\TimeQuest\TimeQuest
```
Must show `Build succeeded. 0 Error(s)` before you proceed. If it fails, stop and report.

## Tasks to Execute

Execute **Tasks 13 through 19** from the implementation plan.

### Task 13 — LevelHelper
Create `C:\TimeQuest\TimeQuest\Helpers\LevelHelper.cs`

Level table (hardcoded):
| Level | XP Required | Title |
|---|---|---|
| 1 | 0 | Novice |
| 2 | 100 | Apprentice |
| 3 | 300 | Journeyman |
| 4 | 600 | Veteran |
| 5 | 1000 | Expert |
| 6 | 1500 | Master |
| 7 | 2100 | Legend |
| 8 | 2800 | Mythic |
| 9 | 3600 | Immortal |
| 10 | 4500 | TimeQuest Champion |

Methods:
- `GetLevel(int xp)` — iterates the table, returns the highest level where `xp >= required`
- `GetTitle(int xp)` — same iteration pattern, returns the title string
- `GetXpForNextLevel(int xp)` — returns `required - xp` for the first level where `xp < required`; returns `0` at max level

### Task 14 — xUnit Test Project + LevelHelper Tests
Create xUnit test project:
```powershell
dotnet new xunit -n TimeQuest.Tests -o C:\TimeQuest\TimeQuest.Tests
dotnet sln C:\TimeQuest\TimeQuest.sln add C:\TimeQuest\TimeQuest.Tests\TimeQuest.Tests.csproj
dotnet add C:\TimeQuest\TimeQuest.Tests\TimeQuest.Tests.csproj reference C:\TimeQuest\TimeQuest\TimeQuest.csproj
```

Create `C:\TimeQuest\TimeQuest.Tests\LevelHelperTests.cs` with theory tests covering:
- `GetLevel`: 0→1, 99→1, 100→2, 299→2, 300→3, 599→3, 600→4, 4500→10, 9999→10
- `GetTitle`: 0→"Novice", 100→"Apprentice", 4500→"TimeQuest Champion", 9999→"TimeQuest Champion"
- `GetXpForNextLevel`: 0→100, 50→50, 100→200, 300→300, 4499→1, 4500→0, 9999→0

Run: `dotnet test C:\TimeQuest\TimeQuest.Tests` → all tests must pass.

### Task 15 — Generic Repository
Create:
- `C:\TimeQuest\TimeQuest\Repositories\IRepository.cs` — `GetByIdAsync(int id)`, `GetAllAsync()`, `AddAsync(T)`, `Update(T)`, `Delete(T)`, `SaveChangesAsync()`
- `C:\TimeQuest\TimeQuest\Repositories\Repository.cs` — implements `IRepository<T>`, injects `ApplicationDbContext`

### Task 16 — TimeEntryRepository
Create:
- `C:\TimeQuest\TimeQuest\Repositories\ITimeEntryRepository.cs`
- `C:\TimeQuest\TimeQuest\Repositories\TimeEntryRepository.cs`

Methods required on the interface:
- `GetByUserAsync(string userId)` — includes Project, ordered by Date desc
- `GetPendingForManagerAsync(string managerId)` — two-step query: find managed team IDs → find member IDs → get Submitted entries
- `GetByTeamAsync(int teamId)` — find member IDs for team → get entries
- `GetByDateRangeAsync(string userId, DateOnly from, DateOnly to)`
- `GetApprovedByUserAsync(string userId)`
- `GetTotalApprovedHoursAsync(string userId)` → returns `decimal`
- `GetApprovedByUserAndWeekAsync(string userId, DateOnly weekStart)` → weekEnd = weekStart.AddDays(6)

### Task 17 — UserRepository
Create:
- `C:\TimeQuest\TimeQuest\Repositories\IUserRepository.cs` — does NOT extend `IRepository<T>`
- `C:\TimeQuest\TimeQuest\Repositories\UserRepository.cs`

Methods:
- `GetByIdAsync(string userId)` — simple FindAsync
- `GetWithBadgesAsync(string userId)` — includes UserBadges → Badge
- `GetLeaderboardAsync(int top = 10)` — ordered by XP desc, Take(top)
- `GetTeamMembersAsync(int teamId)` — UserTeams where TeamId → include User
- `Update(ApplicationUser user)`
- `SaveChangesAsync()`

### Task 18 — TeamRepository and ProjectRepository
Create:
- `C:\TimeQuest\TimeQuest\Repositories\ITeamRepository.cs` / `TeamRepository.cs`
- `C:\TimeQuest\TimeQuest\Repositories\IProjectRepository.cs` / `ProjectRepository.cs`

TeamRepository methods:
- `GetWithMembersAsync(int teamId)` — includes UserTeams → User
- `GetManagedByAsync(string managerId)` — UserTeams where Role == Manager → include Team
- `GetAllWithMembersAsync()` — all teams with UserTeams → User

ProjectRepository methods:
- `GetActiveAsync()` — where IsActive == true
- `GetByUserAsync(string userId)` — UserProjects where UserId → include Project

### Task 19 — BadgeRepository
Create:
- `C:\TimeQuest\TimeQuest\Repositories\IBadgeRepository.cs` / `BadgeRepository.cs`

Methods:
- `GetEarnedByUserAsync(string userId)` — UserBadges where UserId → include Badge → select Badge
- `GetNotEarnedByUserAsync(string userId)` — get earned IDs, then Badges where Id NOT IN earned
- `GetByCriteriaAsync(BadgeCriteria criteria)` → returns `Badge?`

## Completion Criteria

- [ ] `dotnet build C:\TimeQuest\TimeQuest` → `Build succeeded. 0 Error(s)`
- [ ] `dotnet test C:\TimeQuest\TimeQuest.Tests` → all 19 LevelHelper tests pass
- [ ] `C:\TimeQuest\TimeQuest\Helpers\LevelHelper.cs` exists
- [ ] `C:\TimeQuest\TimeQuest\Repositories\` contains 12 files (6 interfaces + 6 implementations)

## Commit When Done
```
git -C C:\TimeQuest commit -m "feat: LevelHelper, repositories, and xUnit test project"
```
