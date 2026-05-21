# Foundation Agent — Requirements

## Source of Truth
Read this plan in full before writing a single line of code:
`C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md`

Read the design spec for context:
`C:\TimeQuest\design.md`

## Tasks to Execute

Execute **Tasks 1 through 12** from the implementation plan in order. Do not skip any step. Do not move to the next task until the current task's build check passes.

### Task 1 — Install NuGet Packages
Run from `C:\TimeQuest\TimeQuest\`:
```powershell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet restore
```
**Verify:** `dotnet restore` exits with `Restore succeeded.`

### Task 2 — Connection String
Update `C:\TimeQuest\TimeQuest\appsettings.json` — add `ConnectionStrings.DefaultConnection` and `SeedData` section as specified in the plan.

### Task 3 — Enums
Create `C:\TimeQuest\TimeQuest\Models\Enums.cs` with `TeamRole`, `TimeEntryStatus`, `BadgeCriteria`.

### Task 4 — ApplicationUser
Create `C:\TimeQuest\TimeQuest\Models\ApplicationUser.cs`.

### Task 5 — Team and Project
Create `C:\TimeQuest\TimeQuest\Models\Team.cs` and `C:\TimeQuest\TimeQuest\Models\Project.cs`.

### Task 6 — Join Tables (UserTeam, UserProject)
Create `C:\TimeQuest\TimeQuest\Models\UserTeam.cs` and `C:\TimeQuest\TimeQuest\Models\UserProject.cs`.

### Task 7 — TimeEntry
Create `C:\TimeQuest\TimeQuest\Models\TimeEntry.cs`.

### Task 8 — Badge and UserBadge
Create `C:\TimeQuest\TimeQuest\Models\Badge.cs` and `C:\TimeQuest\TimeQuest\Models\UserBadge.cs`.

### Task 9 — Report DTOs
Create `C:\TimeQuest\TimeQuest\Models\ReportDtos.cs`.

### Task 10 — ApplicationDbContext
Create `C:\TimeQuest\TimeQuest\Data\ApplicationDbContext.cs`.
**Critical:** Configure both FK relationships on `TimeEntry` (User and ReviewedBy) with `OnDelete(DeleteBehavior.Restrict)` and `OnDelete(DeleteBehavior.NoAction)` respectively to avoid SQL Server cascade cycles.

### Task 11 — Wire EF + Identity in Program.cs (Partial)
Update `C:\TimeQuest\TimeQuest\Program.cs` with `AddDbContext`, `AddIdentity`, `UseAuthentication`, `UseAuthorization`. Do NOT add repository/service registrations yet — that is Task 26.

**Verify after Task 11:** Run `dotnet build C:\TimeQuest\TimeQuest` — must succeed with 0 errors.

### Task 12 — EF Migration
```powershell
# Install EF tools if needed
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add InitialCreate --project C:\TimeQuest\TimeQuest\TimeQuest.csproj --startup-project C:\TimeQuest\TimeQuest\TimeQuest.csproj

# Apply to database
dotnet ef database update --project C:\TimeQuest\TimeQuest\TimeQuest.csproj --startup-project C:\TimeQuest\TimeQuest\TimeQuest.csproj
```
**Verify:** Migration files appear in `C:\TimeQuest\TimeQuest\Data\Migrations\`.

## Completion Criteria

Before declaring complete, verify all of these:
- [ ] `dotnet build C:\TimeQuest\TimeQuest` → `Build succeeded. 0 Error(s)`
- [ ] `C:\TimeQuest\TimeQuest\Models\` contains: `Enums.cs`, `ApplicationUser.cs`, `Team.cs`, `Project.cs`, `UserTeam.cs`, `UserProject.cs`, `TimeEntry.cs`, `Badge.cs`, `UserBadge.cs`, `ReportDtos.cs`
- [ ] `C:\TimeQuest\TimeQuest\Data\ApplicationDbContext.cs` exists
- [ ] `C:\TimeQuest\TimeQuest\Data\Migrations\` contains at least one migration file
- [ ] `C:\TimeQuest\TimeQuest\appsettings.json` contains `ConnectionStrings.DefaultConnection`

## Commit When Done
```
git -C C:\TimeQuest commit -m "feat: foundation layer - models, DbContext, EF migration"
```
