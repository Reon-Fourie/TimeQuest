# QA Agent — Requirements

## Source of Truth
- Spec: `C:\TimeQuest\design.md`
- Plan: `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`
- All source code under: `C:\TimeQuest\TimeQuest\`

## Pre-Flight
```powershell
dotnet build C:\TimeQuest\TimeQuest
dotnet test C:\TimeQuest\TimeQuest.Tests -v normal
```
If build fails — Critical issue, stop and report before doing anything else.

## QA Checklist

### Models (Task 3-9)
Read each file under `C:\TimeQuest\TimeQuest\Models\` and verify:
- [ ] `Enums.cs` — `TeamRole`, `TimeEntryStatus`, `BadgeCriteria` all present with correct values
- [ ] `ApplicationUser.cs` — extends `IdentityUser`, has `XP` (int), `Level` (int), `FirstName`, `LastName`, `AvatarUrl`, nav collections
- [ ] `Team.cs` — `Id`, `Name`, `Description?`, `CreatedAt`, nav `UserTeams`
- [ ] `Project.cs` — `Id`, `Name`, `Description?`, `IsActive` (bool), nav collections
- [ ] `UserTeam.cs` — composite key fields `UserId` + `TeamId`, `Role` (TeamRole), nav properties
- [ ] `UserProject.cs` — composite key `UserId` + `ProjectId`, nav properties
- [ ] `TimeEntry.cs` — all fields: `Id`, `UserId`, `ProjectId`, `Date` (DateOnly), `Hours` (decimal), `Description`, `Status`, `SubmittedAt?`, `ReviewedAt?`, `ReviewedById?`, `RejectionReason?`, `CreatedAt`, `UpdatedAt`, two nav properties to User
- [ ] `Badge.cs` — `Id`, `Name`, `Description`, `ImagePath`, `Criteria` (BadgeCriteria)
- [ ] `UserBadge.cs` — composite key `UserId` + `BadgeId`, `EarnedAt`, nav properties
- [ ] `ReportDtos.cs` — all 6 record types present

### DbContext (Task 10)
Read `C:\TimeQuest\TimeQuest\Data\ApplicationDbContext.cs` and verify:
- [ ] Extends `IdentityDbContext<ApplicationUser>`
- [ ] All 7 non-Identity DbSets present
- [ ] Composite keys configured: `UserTeam`, `UserProject`, `UserBadge`
- [ ] `TimeEntry.Hours` column type `decimal(18,2)`
- [ ] TWO separate `HasOne/WithMany` configs for `TimeEntry.User` and `TimeEntry.ReviewedBy`
- [ ] `ReviewedBy` relationship uses `OnDelete(DeleteBehavior.NoAction)` (not Cascade — would cause SQL Server error)
- [ ] `User` relationship uses `OnDelete(DeleteBehavior.Restrict)`

### LevelHelper (Task 13)
Read `C:\TimeQuest\TimeQuest\Helpers\LevelHelper.cs` and verify:
- [ ] 10 levels hardcoded: 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500
- [ ] `GetLevel(0)` → 1, `GetLevel(100)` → 2, `GetLevel(4500)` → 10
- [ ] `GetXpForNextLevel(4500)` → 0 (max level returns 0, not negative)

### Repositories (Tasks 15-19)
- [ ] `IRepository.cs` + `Repository.cs` exist and are generic
- [ ] `IUserRepository` does NOT extend `IRepository<T>` (user ID is string, not int)
- [ ] `TimeEntryRepository.GetPendingForManagerAsync` correctly filters by managed teams then members
- [ ] All 10 files present (5 interfaces + 5 implementations)

### Services (Tasks 20-25)
- [ ] `XpService.AwardXpAsync` updates both `XP` and `Level` and saves
- [ ] `BadgeService` handles all 7 criteria (no missing switch cases)
- [ ] `TimesheetService.ApproveAsync` calls XpService THEN BadgeService
- [ ] `TimesheetService.SubmitAsync` checks `entry.UserId == userId`
- [ ] `SeedData` guards badge seeding with `if (!await context.Badges.AnyAsync())`
- [ ] All 7 badge image paths follow naming convention `badges/xxx.png`

### Program.cs (Task 26)
Read `C:\TimeQuest\TimeQuest\Program.cs` and verify:
- [ ] All 5 repository `AddScoped` registrations present
- [ ] All 5 service `AddScoped` registrations present
- [ ] Seed call is inside `using (var scope = app.Services.CreateScope())`
- [ ] `UseAuthentication()` comes BEFORE `UseAuthorization()`
- [ ] Connection string throws `InvalidOperationException` if missing (not null)

### Startup Test
```powershell
dotnet run --project C:\TimeQuest\TimeQuest\TimeQuest.csproj
```
- [ ] App starts without exceptions
- [ ] No seed errors logged
- [ ] `Now listening on: https://localhost:XXXX` appears

## Output
Save your full QA report to `C:\TimeQuest\agents\qa-report.md` using the format specified in CLAUDE.md.

End with either:
- `APPROVED FOR UI PHASE` if all Critical and High issues are resolved
- `BLOCKED — fix list:` followed by the prioritised issues
