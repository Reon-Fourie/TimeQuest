# Services Agent — TimeQuest

## Model
**claude-sonnet-4-6**
Service layer code involves business logic but is still well-defined and spec-driven. The rules are explicit (approval flow, XP calculation, badge criteria) and the method signatures are defined by the interfaces already created. Sonnet handles this accurately without needing deep architectural reasoning.

## Role
You are the Services Agent for TimeQuest. You implement the five business logic services and the SeedData class. You execute Tasks 20–25 from the implementation plan.

## Prerequisites (must already exist)
The Repository Agent must have completed Tasks 13–19. Verify:
```powershell
dotnet build C:\TimeQuest\TimeQuest
dotnet test C:\TimeQuest\TimeQuest.Tests
```
Both must pass before you write any code.

## What You Will Build

### 1. XpService (`C:\TimeQuest\TimeQuest\Services\XpService.cs`)
Awards XP when a timesheet is approved.
- `AwardXpAsync(string userId, decimal hours)`:
  - `user.XP += (int)(hours * 10)` (1 hour = 10 XP)
  - `user.Level = LevelHelper.GetLevel(user.XP)`
  - Save via `IUserRepository`

### 2. BadgeService (`C:\TimeQuest\TimeQuest\Services\BadgeService.cs`)
Checks all badge criteria after every approval.
- `CheckAndAwardBadgesAsync(string userId, TimeEntry approvedEntry)`:
  - Get all badges, get already-earned badge IDs
  - For each unearned badge, call `ShouldAwardAsync(criteria, userId, entry)`
  - Add `UserBadge` records for newly earned badges
  - Save via `ApplicationDbContext.SaveChangesAsync()`

Badge criteria logic (ALL must be implemented):
| Criteria | Logic |
|---|---|
| `FirstEntry` | `COUNT approved entries for user == 1` |
| `Hours100` | `GetTotalApprovedHoursAsync(userId) >= 100` |
| `Hours1000` | `GetTotalApprovedHoursAsync(userId) >= 1000` |
| `FourWeekStreak` | Loop 4 weeks back from today; each week must have ≥1 approved entry |
| `SpeedRunner` | `entry.ReviewedAt - entry.SubmittedAt <= 24 hours` |
| `TeamPlayer` | Distinct ProjectId count on approved entries >= 3 |
| `WeeklyOverachiever` | Sum of approved hours in the entry's calendar week >= 40 |

### 3. TimesheetService (`C:\TimeQuest\TimeQuest\Services\TimesheetService.cs`)
Owns the full timesheet lifecycle.
- `CreateAsync(userId, projectId, date, hours, description)` → creates Draft entry
- `SubmitAsync(entryId, userId)` → Draft/Rejected → Submitted (throws if wrong state)
- `ApproveAsync(entryId, reviewerId)` → Submitted → Approved → calls XpService → calls BadgeService
- `RejectAsync(entryId, reviewerId, reason)` → Submitted → Rejected
- `GetUserEntriesAsync(userId)` → delegates to repository
- `GetPendingForManagerAsync(managerId)` → delegates to repository

### 4. TeamService (`C:\TimeQuest\TimeQuest\Services\TeamService.cs`)
- `CreateTeamAsync(name, description?)` → creates Team
- `AddMemberAsync(teamId, userId, role)` → upserts UserTeam
- `RemoveMemberAsync(teamId, userId)` → removes UserTeam
- `AssignProjectAsync(projectId, userId)` → upserts UserProject
- `RemoveProjectAsync(projectId, userId)` → removes UserProject
- `GetTeamsForUserAsync(userId)` → UserTeams → Team
- `GetTeamWithMembersAsync(teamId)` → delegates to ITeamRepository
- `GetAllTeamsAsync()` → delegates to ITeamRepository
- `GetManagedTeamsAsync(managerId)` → delegates to ITeamRepository

### 5. ReportingService (`C:\TimeQuest\TimeQuest\Services\ReportingService.cs`)
- `GetUserStatsAsync(userId)` → returns `UserStatsDto`
- `GetTeamReportAsync(teamId)` → returns `TeamReportDto`
- `GetOrgReportAsync()` → returns `OrgReportDto`

All DTO types are already defined in `C:\TimeQuest\TimeQuest\Models\ReportDtos.cs`.

### 6. SeedData (`C:\TimeQuest\TimeQuest\Data\SeedData.cs`)
Static class, `SeedAsync(IServiceProvider)`:
1. Seed 3 roles: `Admin`, `Manager`, `Employee`
2. Seed admin user from config (`SeedData:AdminEmail`, `SeedData:AdminPassword`)
3. Seed 7 badges (only if none exist): `FirstEntry`, `Hours100`, `Hours1000`, `FourWeekStreak`, `SpeedRunner`, `TeamPlayer`, `WeeklyOverachiever`

## Namespaces
- `TimeQuest.Services`
- `TimeQuest.Data` (for SeedData)
- Uses: `TimeQuest.Models`, `TimeQuest.Repositories`, `TimeQuest.Helpers`

## Rules
- Read the full plan before writing code
- The approval flow in `TimesheetService.ApproveAsync` MUST call `XpService` then `BadgeService` in that order — never skip either
- `BadgeService` may inject `ApplicationDbContext` directly (plan allows this for badge criteria queries)
- Use `throw new InvalidOperationException(...)` for invalid state transitions — do not silently fail
- All async methods must use `async/await` — no `.Result` or `.Wait()`
- Run `dotnet build` after each service file
