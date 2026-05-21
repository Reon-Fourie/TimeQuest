# TimeQuest Backend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete backend layer for TimeQuest — models, EF Core + Identity, repositories, services, and seed data — on top of the existing Blazor Web App scaffold.

**Architecture:** Single project under `C:\TimeQuest\TimeQuest\`. Models define entities. `ApplicationDbContext` owns EF config. Repositories wrap EF queries. Services compose repositories and own business logic. `SeedData` runs at startup. UI is deferred to phase 2.

**Tech Stack:** .NET 10, Blazor Web App (existing scaffold), EF Core 9, ASP.NET Core Identity, SQL Server LocalDB, xUnit + xUnit.runner.visualstudio (tests in `TimeQuest.Tests`)

---

## File Map

**Create:**
- `TimeQuest/Models/Enums.cs`
- `TimeQuest/Models/ApplicationUser.cs`
- `TimeQuest/Models/Team.cs`
- `TimeQuest/Models/Project.cs`
- `TimeQuest/Models/UserTeam.cs`
- `TimeQuest/Models/UserProject.cs`
- `TimeQuest/Models/TimeEntry.cs`
- `TimeQuest/Models/Badge.cs`
- `TimeQuest/Models/UserBadge.cs`
- `TimeQuest/Models/ReportDtos.cs`
- `TimeQuest/Data/ApplicationDbContext.cs`
- `TimeQuest/Helpers/LevelHelper.cs`
- `TimeQuest/Repositories/IRepository.cs`
- `TimeQuest/Repositories/Repository.cs`
- `TimeQuest/Repositories/ITimeEntryRepository.cs`
- `TimeQuest/Repositories/TimeEntryRepository.cs`
- `TimeQuest/Repositories/IUserRepository.cs`
- `TimeQuest/Repositories/UserRepository.cs`
- `TimeQuest/Repositories/ITeamRepository.cs`
- `TimeQuest/Repositories/TeamRepository.cs`
- `TimeQuest/Repositories/IProjectRepository.cs`
- `TimeQuest/Repositories/ProjectRepository.cs`
- `TimeQuest/Repositories/IBadgeRepository.cs`
- `TimeQuest/Repositories/BadgeRepository.cs`
- `TimeQuest/Services/XpService.cs`
- `TimeQuest/Services/BadgeService.cs`
- `TimeQuest/Services/TimesheetService.cs`
- `TimeQuest/Services/TeamService.cs`
- `TimeQuest/Services/ReportingService.cs`
- `TimeQuest/Data/SeedData.cs`
- `TimeQuest.Tests/TimeQuest.Tests.csproj`
- `TimeQuest.Tests/LevelHelperTests.cs`

**Modify:**
- `TimeQuest/TimeQuest.csproj` — add NuGet packages
- `TimeQuest/appsettings.json` — add connection string + seed config
- `TimeQuest/Program.cs` — wire EF, Identity, repositories, services, seed

---

## Task 1: Install NuGet Packages

**Files:**
- Modify: `TimeQuest/TimeQuest.csproj`

- [ ] **Step 1: Add EF Core and Identity packages**

Run from `C:\TimeQuest\TimeQuest\`:
```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

- [ ] **Step 2: Verify packages were added**

Run:
```
dotnet restore
```
Expected: `Restore succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/TimeQuest.csproj
git commit -m "chore: add EF Core, Identity, and SQL Server packages"
```

---

## Task 2: Connection String + Seed Config

**Files:**
- Modify: `TimeQuest/appsettings.json`

- [ ] **Step 1: Replace appsettings.json with connection string and seed config**

Full file content for `TimeQuest/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TimeQuestDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "SeedData": {
    "AdminEmail": "admin@timequest.com",
    "AdminPassword": "Admin123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 2: Commit**
```
git add TimeQuest/appsettings.json
git commit -m "chore: add connection string and seed config"
```

---

## Task 3: Enums

**Files:**
- Create: `TimeQuest/Models/Enums.cs`

- [ ] **Step 1: Create Models folder and Enums.cs**

```csharp
namespace TimeQuest.Models;

public enum TeamRole { Member, Manager }

public enum TimeEntryStatus { Draft, Submitted, Approved, Rejected }

public enum BadgeCriteria
{
    FirstEntry,
    Hours100,
    Hours1000,
    FourWeekStreak,
    SpeedRunner,
    TeamPlayer,
    WeeklyOverachiever
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Models/Enums.cs
git commit -m "feat: add domain enums"
```

---

## Task 4: ApplicationUser Model

**Files:**
- Create: `TimeQuest/Models/ApplicationUser.cs`

- [ ] **Step 1: Create ApplicationUser.cs**

```csharp
using Microsoft.AspNetCore.Identity;

namespace TimeQuest.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int XP { get; set; }
    public int Level { get; set; } = 1;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<TimeEntry> ReviewedEntries { get; set; } = new List<TimeEntry>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.` (will have unresolved refs until other models are added — errors are fine at this stage)

- [ ] **Step 3: Commit**
```
git add TimeQuest/Models/ApplicationUser.cs
git commit -m "feat: add ApplicationUser model"
```

---

## Task 5: Team and Project Models

**Files:**
- Create: `TimeQuest/Models/Team.cs`
- Create: `TimeQuest/Models/Project.cs`

- [ ] **Step 1: Create Team.cs**

```csharp
namespace TimeQuest.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
}
```

- [ ] **Step 2: Create Project.cs**

```csharp
namespace TimeQuest.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
```

- [ ] **Step 3: Commit**
```
git add TimeQuest/Models/Team.cs TimeQuest/Models/Project.cs
git commit -m "feat: add Team and Project models"
```

---

## Task 6: Join Table Models (UserTeam, UserProject)

**Files:**
- Create: `TimeQuest/Models/UserTeam.cs`
- Create: `TimeQuest/Models/UserProject.cs`

- [ ] **Step 1: Create UserTeam.cs**

```csharp
namespace TimeQuest.Models;

public class UserTeam
{
    public string UserId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.Member;

    public ApplicationUser User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
```

- [ ] **Step 2: Create UserProject.cs**

```csharp
namespace TimeQuest.Models;

public class UserProject
{
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
```

- [ ] **Step 3: Commit**
```
git add TimeQuest/Models/UserTeam.cs TimeQuest/Models/UserProject.cs
git commit -m "feat: add UserTeam and UserProject join models"
```

---

## Task 7: TimeEntry Model

**Files:**
- Create: `TimeQuest/Models/TimeEntry.cs`

- [ ] **Step 1: Create TimeEntry.cs**

```csharp
namespace TimeQuest.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ApplicationUser? ReviewedBy { get; set; }
}
```

- [ ] **Step 2: Commit**
```
git add TimeQuest/Models/TimeEntry.cs
git commit -m "feat: add TimeEntry model"
```

---

## Task 8: Badge and UserBadge Models

**Files:**
- Create: `TimeQuest/Models/Badge.cs`
- Create: `TimeQuest/Models/UserBadge.cs`

- [ ] **Step 1: Create Badge.cs**

```csharp
namespace TimeQuest.Models;

public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public BadgeCriteria Criteria { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
```

- [ ] **Step 2: Create UserBadge.cs**

```csharp
namespace TimeQuest.Models;

public class UserBadge
{
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
```

- [ ] **Step 3: Verify full model build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**
```
git add TimeQuest/Models/Badge.cs TimeQuest/Models/UserBadge.cs
git commit -m "feat: add Badge and UserBadge models"
```

---

## Task 9: Report DTOs

**Files:**
- Create: `TimeQuest/Models/ReportDtos.cs`

- [ ] **Step 1: Create ReportDtos.cs**

```csharp
namespace TimeQuest.Models;

public record ProjectHoursDto(string ProjectName, decimal Hours);

public record UserStatsDto(
    string UserId,
    string FullName,
    int XP,
    int Level,
    string LevelTitle,
    int XpToNextLevel,
    decimal TotalApprovedHours,
    List<UserBadge> Badges,
    List<ProjectHoursDto> HoursByProject
);

public record MemberStatsDto(
    string UserId,
    string FullName,
    int XP,
    decimal TotalHours,
    int PendingCount
);

public record TeamReportDto(
    int TeamId,
    string TeamName,
    List<MemberStatsDto> Members,
    int PendingApprovals
);

public record TeamSummaryDto(int TeamId, string TeamName, int MemberCount, decimal TotalHours);

public record OrgReportDto(
    int TotalUsers,
    decimal TotalApprovedHours,
    List<TeamSummaryDto> Teams,
    List<UserStatsDto> Leaderboard
);
```

- [ ] **Step 2: Commit**
```
git add TimeQuest/Models/ReportDtos.cs
git commit -m "feat: add reporting DTOs"
```

---

## Task 10: ApplicationDbContext

**Files:**
- Create: `TimeQuest/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Create Data folder and ApplicationDbContext.cs**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Models;

namespace TimeQuest.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserTeam> UserTeams => Set<UserTeam>();
    public DbSet<UserProject> UserProjects => Set<UserProject>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserTeam>().HasKey(ut => new { ut.UserId, ut.TeamId });
        builder.Entity<UserProject>().HasKey(up => new { up.UserId, up.ProjectId });
        builder.Entity<UserBadge>().HasKey(ub => new { ub.UserId, ub.BadgeId });

        builder.Entity<TimeEntry>()
            .Property(t => t.Hours)
            .HasColumnType("decimal(18,2)");

        builder.Entity<TimeEntry>()
            .HasOne(t => t.User)
            .WithMany(u => u.TimeEntries)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TimeEntry>()
            .HasOne(t => t.ReviewedBy)
            .WithMany(u => u.ReviewedEntries)
            .HasForeignKey(t => t.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Data/ApplicationDbContext.cs
git commit -m "feat: add ApplicationDbContext with EF and Identity config"
```

---

## Task 11: Wire EF + Identity in Program.cs (Partial)

Add just enough to `Program.cs` so migrations can run. Full service wiring comes in Task 26.

**Files:**
- Modify: `TimeQuest/Program.cs`

- [ ] **Step 1: Replace Program.cs with EF + Identity wired in**

Full file content for `TimeQuest/Program.cs`:
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Components;
using TimeQuest.Data;
using TimeQuest.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Program.cs
git commit -m "feat: wire EF Core and ASP.NET Core Identity in Program.cs"
```

---

## Task 12: EF Migration + Create Database

**Files:**
- Create: `TimeQuest/Data/Migrations/` (auto-generated)

- [ ] **Step 1: Install EF tools globally (if not already installed)**

Run:
```
dotnet tool install --global dotnet-ef
```
If already installed: `Tool 'dotnet-ef' is already installed.` — that's fine.

- [ ] **Step 2: Create initial migration**

Run from `C:\TimeQuest\`:
```
dotnet ef migrations add InitialCreate --project TimeQuest/TimeQuest.csproj --startup-project TimeQuest/TimeQuest.csproj
```
Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 3: Apply migration to create database**

Run:
```
dotnet ef database update --project TimeQuest/TimeQuest.csproj --startup-project TimeQuest/TimeQuest.csproj
```
Expected: `Done.`

- [ ] **Step 4: Commit**
```
git add TimeQuest/Data/Migrations/
git commit -m "feat: add EF initial migration"
```

---

## Task 13: LevelHelper

**Files:**
- Create: `TimeQuest/Helpers/LevelHelper.cs`

- [ ] **Step 1: Create Helpers folder and LevelHelper.cs**

```csharp
namespace TimeQuest.Helpers;

public static class LevelHelper
{
    private static readonly (int Level, int RequiredXp, string Title)[] Levels =
    {
        (1, 0,    "Novice"),
        (2, 100,  "Apprentice"),
        (3, 300,  "Journeyman"),
        (4, 600,  "Veteran"),
        (5, 1000, "Expert"),
        (6, 1500, "Master"),
        (7, 2100, "Legend"),
        (8, 2800, "Mythic"),
        (9, 3600, "Immortal"),
        (10, 4500, "TimeQuest Champion")
    };

    public static int GetLevel(int xp)
    {
        var level = 1;
        foreach (var (lvl, required, _) in Levels)
            if (xp >= required) level = lvl;
        return level;
    }

    public static string GetTitle(int xp)
    {
        var title = "Novice";
        foreach (var (_, required, t) in Levels)
            if (xp >= required) title = t;
        return title;
    }

    public static int GetXpForNextLevel(int xp)
    {
        foreach (var (_, required, _) in Levels)
            if (xp < required) return required - xp;
        return 0;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Helpers/LevelHelper.cs
git commit -m "feat: add LevelHelper with XP/level/title logic"
```

---

## Task 14: LevelHelper Tests (xUnit test project)

**Files:**
- Create: `TimeQuest.Tests/TimeQuest.Tests.csproj`
- Create: `TimeQuest.Tests/LevelHelperTests.cs`

- [ ] **Step 1: Create test project**

Run from `C:\TimeQuest\`:
```
dotnet new xunit -n TimeQuest.Tests -o TimeQuest.Tests
dotnet sln TimeQuest.sln add TimeQuest.Tests/TimeQuest.Tests.csproj
dotnet add TimeQuest.Tests/TimeQuest.Tests.csproj reference TimeQuest/TimeQuest.csproj
```

- [ ] **Step 2: Write failing tests — create LevelHelperTests.cs**

Replace the default `UnitTest1.cs` content (or delete it and create `LevelHelperTests.cs`):
```csharp
using TimeQuest.Helpers;

namespace TimeQuest.Tests;

public class LevelHelperTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(299, 2)]
    [InlineData(300, 3)]
    [InlineData(599, 3)]
    [InlineData(600, 4)]
    [InlineData(4500, 10)]
    [InlineData(9999, 10)]
    public void GetLevel_ReturnsCorrectLevel(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, LevelHelper.GetLevel(xp));
    }

    [Theory]
    [InlineData(0, "Novice")]
    [InlineData(99, "Novice")]
    [InlineData(100, "Apprentice")]
    [InlineData(300, "Journeyman")]
    [InlineData(4500, "TimeQuest Champion")]
    [InlineData(9999, "TimeQuest Champion")]
    public void GetTitle_ReturnsCorrectTitle(int xp, string expectedTitle)
    {
        Assert.Equal(expectedTitle, LevelHelper.GetTitle(xp));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(50, 50)]
    [InlineData(100, 200)]
    [InlineData(300, 300)]
    [InlineData(4499, 1)]
    [InlineData(4500, 0)]
    [InlineData(9999, 0)]
    public void GetXpForNextLevel_ReturnsCorrectAmount(int xp, int expectedXpNeeded)
    {
        Assert.Equal(expectedXpNeeded, LevelHelper.GetXpForNextLevel(xp));
    }
}
```

- [ ] **Step 3: Run tests — expect them to pass**

Run:
```
dotnet test TimeQuest.Tests/TimeQuest.Tests.csproj
```
Expected:
```
Passed! - Failed: 0, Passed: 19, Skipped: 0
```

- [ ] **Step 4: Commit**
```
git add TimeQuest.Tests/
git commit -m "test: add xUnit project and LevelHelper tests"
```

---

## Task 15: Generic Repository

**Files:**
- Create: `TimeQuest/Repositories/IRepository.cs`
- Create: `TimeQuest/Repositories/Repository.cs`

- [ ] **Step 1: Create IRepository.cs**

```csharp
namespace TimeQuest.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Create Repository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;

namespace TimeQuest.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await DbSet.ToListAsync();

    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Delete(T entity) => DbSet.Remove(entity);

    public async Task SaveChangesAsync() => await Context.SaveChangesAsync();
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**
```
git add TimeQuest/Repositories/IRepository.cs TimeQuest/Repositories/Repository.cs
git commit -m "feat: add generic IRepository and Repository base"
```

---

## Task 16: TimeEntryRepository

**Files:**
- Create: `TimeQuest/Repositories/ITimeEntryRepository.cs`
- Create: `TimeQuest/Repositories/TimeEntryRepository.cs`

- [ ] **Step 1: Create ITimeEntryRepository.cs**

```csharp
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public interface ITimeEntryRepository : IRepository<TimeEntry>
{
    Task<IEnumerable<TimeEntry>> GetByUserAsync(string userId);
    Task<IEnumerable<TimeEntry>> GetPendingForManagerAsync(string managerId);
    Task<IEnumerable<TimeEntry>> GetByTeamAsync(int teamId);
    Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(string userId, DateOnly from, DateOnly to);
    Task<IEnumerable<TimeEntry>> GetApprovedByUserAsync(string userId);
    Task<decimal> GetTotalApprovedHoursAsync(string userId);
    Task<IEnumerable<TimeEntry>> GetApprovedByUserAndWeekAsync(string userId, DateOnly weekStart);
}
```

- [ ] **Step 2: Create TimeEntryRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public class TimeEntryRepository : Repository<TimeEntry>, ITimeEntryRepository
{
    public TimeEntryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TimeEntry>> GetByUserAsync(string userId) =>
        await Context.TimeEntries
            .Include(t => t.Project)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<IEnumerable<TimeEntry>> GetPendingForManagerAsync(string managerId)
    {
        var managedTeamIds = await Context.UserTeams
            .Where(ut => ut.UserId == managerId && ut.Role == TeamRole.Manager)
            .Select(ut => ut.TeamId)
            .ToListAsync();

        var memberIds = await Context.UserTeams
            .Where(ut => managedTeamIds.Contains(ut.TeamId) && ut.Role == TeamRole.Member)
            .Select(ut => ut.UserId)
            .Distinct()
            .ToListAsync();

        return await Context.TimeEntries
            .Include(t => t.User)
            .Include(t => t.Project)
            .Where(t => memberIds.Contains(t.UserId) && t.Status == TimeEntryStatus.Submitted)
            .OrderBy(t => t.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeEntry>> GetByTeamAsync(int teamId)
    {
        var memberIds = await Context.UserTeams
            .Where(ut => ut.TeamId == teamId)
            .Select(ut => ut.UserId)
            .ToListAsync();

        return await Context.TimeEntries
            .Include(t => t.User)
            .Include(t => t.Project)
            .Where(t => memberIds.Contains(t.UserId))
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(string userId, DateOnly from, DateOnly to) =>
        await Context.TimeEntries
            .Include(t => t.Project)
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
            .OrderBy(t => t.Date)
            .ToListAsync();

    public async Task<IEnumerable<TimeEntry>> GetApprovedByUserAsync(string userId) =>
        await Context.TimeEntries
            .Include(t => t.Project)
            .Where(t => t.UserId == userId && t.Status == TimeEntryStatus.Approved)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<decimal> GetTotalApprovedHoursAsync(string userId) =>
        await Context.TimeEntries
            .Where(t => t.UserId == userId && t.Status == TimeEntryStatus.Approved)
            .SumAsync(t => t.Hours);

    public async Task<IEnumerable<TimeEntry>> GetApprovedByUserAndWeekAsync(string userId, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return await Context.TimeEntries
            .Where(t => t.UserId == userId
                && t.Status == TimeEntryStatus.Approved
                && t.Date >= weekStart
                && t.Date <= weekEnd)
            .ToListAsync();
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**
```
git add TimeQuest/Repositories/ITimeEntryRepository.cs TimeQuest/Repositories/TimeEntryRepository.cs
git commit -m "feat: add TimeEntryRepository"
```

---

## Task 17: UserRepository

**Files:**
- Create: `TimeQuest/Repositories/IUserRepository.cs`
- Create: `TimeQuest/Repositories/UserRepository.cs`

- [ ] **Step 1: Create IUserRepository.cs**

```csharp
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<ApplicationUser?> GetWithBadgesAsync(string userId);
    Task<IEnumerable<ApplicationUser>> GetLeaderboardAsync(int top = 10);
    Task<IEnumerable<ApplicationUser>> GetTeamMembersAsync(int teamId);
    void Update(ApplicationUser user);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Create UserRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public async Task<ApplicationUser?> GetByIdAsync(string userId) =>
        await _context.Users.FindAsync(userId);

    public async Task<ApplicationUser?> GetWithBadgesAsync(string userId) =>
        await _context.Users
            .Include(u => u.UserBadges)
            .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<IEnumerable<ApplicationUser>> GetLeaderboardAsync(int top = 10) =>
        await _context.Users
            .OrderByDescending(u => u.XP)
            .Take(top)
            .ToListAsync();

    public async Task<IEnumerable<ApplicationUser>> GetTeamMembersAsync(int teamId) =>
        await _context.UserTeams
            .Where(ut => ut.TeamId == teamId)
            .Include(ut => ut.User)
            .Select(ut => ut.User)
            .ToListAsync();

    public void Update(ApplicationUser user) => _context.Users.Update(user);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
```

- [ ] **Step 3: Commit**
```
git add TimeQuest/Repositories/IUserRepository.cs TimeQuest/Repositories/UserRepository.cs
git commit -m "feat: add UserRepository"
```

---

## Task 18: TeamRepository and ProjectRepository

**Files:**
- Create: `TimeQuest/Repositories/ITeamRepository.cs`
- Create: `TimeQuest/Repositories/TeamRepository.cs`
- Create: `TimeQuest/Repositories/IProjectRepository.cs`
- Create: `TimeQuest/Repositories/ProjectRepository.cs`

- [ ] **Step 1: Create ITeamRepository.cs**

```csharp
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public interface ITeamRepository : IRepository<Team>
{
    Task<Team?> GetWithMembersAsync(int teamId);
    Task<IEnumerable<Team>> GetManagedByAsync(string managerId);
    Task<IEnumerable<Team>> GetAllWithMembersAsync();
}
```

- [ ] **Step 2: Create TeamRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public class TeamRepository : Repository<Team>, ITeamRepository
{
    public TeamRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Team?> GetWithMembersAsync(int teamId) =>
        await Context.Teams
            .Include(t => t.UserTeams)
            .ThenInclude(ut => ut.User)
            .FirstOrDefaultAsync(t => t.Id == teamId);

    public async Task<IEnumerable<Team>> GetManagedByAsync(string managerId) =>
        await Context.UserTeams
            .Where(ut => ut.UserId == managerId && ut.Role == TeamRole.Manager)
            .Include(ut => ut.Team)
            .Select(ut => ut.Team)
            .ToListAsync();

    public async Task<IEnumerable<Team>> GetAllWithMembersAsync() =>
        await Context.Teams
            .Include(t => t.UserTeams)
            .ThenInclude(ut => ut.User)
            .ToListAsync();
}
```

- [ ] **Step 3: Create IProjectRepository.cs**

```csharp
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetActiveAsync();
    Task<IEnumerable<Project>> GetByUserAsync(string userId);
}
```

- [ ] **Step 4: Create ProjectRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Project>> GetActiveAsync() =>
        await Context.Projects.Where(p => p.IsActive).ToListAsync();

    public async Task<IEnumerable<Project>> GetByUserAsync(string userId) =>
        await Context.UserProjects
            .Where(up => up.UserId == userId)
            .Include(up => up.Project)
            .Select(up => up.Project)
            .ToListAsync();
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 6: Commit**
```
git add TimeQuest/Repositories/ITeamRepository.cs TimeQuest/Repositories/TeamRepository.cs TimeQuest/Repositories/IProjectRepository.cs TimeQuest/Repositories/ProjectRepository.cs
git commit -m "feat: add TeamRepository and ProjectRepository"
```

---

## Task 19: BadgeRepository

**Files:**
- Create: `TimeQuest/Repositories/IBadgeRepository.cs`
- Create: `TimeQuest/Repositories/BadgeRepository.cs`

- [ ] **Step 1: Create IBadgeRepository.cs**

```csharp
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public interface IBadgeRepository : IRepository<Badge>
{
    Task<IEnumerable<Badge>> GetEarnedByUserAsync(string userId);
    Task<IEnumerable<Badge>> GetNotEarnedByUserAsync(string userId);
    Task<Badge?> GetByCriteriaAsync(BadgeCriteria criteria);
}
```

- [ ] **Step 2: Create BadgeRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;

namespace TimeQuest.Repositories;

public class BadgeRepository : Repository<Badge>, IBadgeRepository
{
    public BadgeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Badge>> GetEarnedByUserAsync(string userId) =>
        await Context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .Select(ub => ub.Badge)
            .ToListAsync();

    public async Task<IEnumerable<Badge>> GetNotEarnedByUserAsync(string userId)
    {
        var earnedIds = await Context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync();

        return await Context.Badges
            .Where(b => !earnedIds.Contains(b.Id))
            .ToListAsync();
    }

    public async Task<Badge?> GetByCriteriaAsync(BadgeCriteria criteria) =>
        await Context.Badges.FirstOrDefaultAsync(b => b.Criteria == criteria);
}
```

- [ ] **Step 3: Commit**
```
git add TimeQuest/Repositories/IBadgeRepository.cs TimeQuest/Repositories/BadgeRepository.cs
git commit -m "feat: add BadgeRepository"
```

---

## Task 20: XpService

**Files:**
- Create: `TimeQuest/Services/XpService.cs`

- [ ] **Step 1: Create Services folder and XpService.cs**

```csharp
using TimeQuest.Helpers;
using TimeQuest.Repositories;

namespace TimeQuest.Services;

public class XpService
{
    private readonly IUserRepository _users;

    public XpService(IUserRepository users) => _users = users;

    public async Task AwardXpAsync(string userId, decimal hours)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null) return;

        user.XP += (int)(hours * 10);
        user.Level = LevelHelper.GetLevel(user.XP);

        _users.Update(user);
        await _users.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Services/XpService.cs
git commit -m "feat: add XpService"
```

---

## Task 21: BadgeService

**Files:**
- Create: `TimeQuest/Services/BadgeService.cs`

- [ ] **Step 1: Create BadgeService.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;
using TimeQuest.Repositories;

namespace TimeQuest.Services;

public class BadgeService
{
    private readonly IBadgeRepository _badges;
    private readonly ITimeEntryRepository _timeEntries;
    private readonly ApplicationDbContext _context;

    public BadgeService(IBadgeRepository badges, ITimeEntryRepository timeEntries, ApplicationDbContext context)
    {
        _badges = badges;
        _timeEntries = timeEntries;
        _context = context;
    }

    public async Task CheckAndAwardBadgesAsync(string userId, TimeEntry approvedEntry)
    {
        var allBadges = await _badges.GetAllAsync();
        var earnedIds = (await _badges.GetEarnedByUserAsync(userId))
            .Select(b => b.Id)
            .ToHashSet();

        foreach (var badge in allBadges)
        {
            if (earnedIds.Contains(badge.Id)) continue;
            if (await ShouldAwardAsync(badge.Criteria, userId, approvedEntry))
            {
                _context.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task<bool> ShouldAwardAsync(BadgeCriteria criteria, string userId, TimeEntry entry)
    {
        return criteria switch
        {
            BadgeCriteria.FirstEntry =>
                await _context.TimeEntries.CountAsync(t => t.UserId == userId && t.Status == TimeEntryStatus.Approved) == 1,

            BadgeCriteria.Hours100 =>
                await _timeEntries.GetTotalApprovedHoursAsync(userId) >= 100,

            BadgeCriteria.Hours1000 =>
                await _timeEntries.GetTotalApprovedHoursAsync(userId) >= 1000,

            BadgeCriteria.FourWeekStreak =>
                await CheckFourWeekStreakAsync(userId),

            BadgeCriteria.SpeedRunner =>
                entry.SubmittedAt.HasValue && entry.ReviewedAt.HasValue &&
                (entry.ReviewedAt.Value - entry.SubmittedAt.Value).TotalHours <= 24,

            BadgeCriteria.TeamPlayer =>
                await _context.TimeEntries
                    .Where(t => t.UserId == userId && t.Status == TimeEntryStatus.Approved)
                    .Select(t => t.ProjectId)
                    .Distinct()
                    .CountAsync() >= 3,

            BadgeCriteria.WeeklyOverachiever =>
                await CheckWeeklyOverachieverAsync(userId, entry.Date),

            _ => false
        };
    }

    private async Task<bool> CheckFourWeekStreakAsync(string userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 4; i++)
        {
            var weekStart = today.AddDays(-((int)today.DayOfWeek) - (i * 7));
            var weekEnd = weekStart.AddDays(6);
            var hasEntry = await _context.TimeEntries.AnyAsync(t =>
                t.UserId == userId &&
                t.Status == TimeEntryStatus.Approved &&
                t.Date >= weekStart &&
                t.Date <= weekEnd);
            if (!hasEntry) return false;
        }
        return true;
    }

    private async Task<bool> CheckWeeklyOverachieverAsync(string userId, DateOnly entryDate)
    {
        var weekStart = entryDate.AddDays(-(int)entryDate.DayOfWeek);
        var weekEntries = await _timeEntries.GetApprovedByUserAndWeekAsync(userId, weekStart);
        return weekEntries.Sum(t => t.Hours) >= 40;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Services/BadgeService.cs
git commit -m "feat: add BadgeService with all 7 badge criteria checks"
```

---

## Task 22: TimesheetService

**Files:**
- Create: `TimeQuest/Services/TimesheetService.cs`

- [ ] **Step 1: Create TimesheetService.cs**

```csharp
using TimeQuest.Models;
using TimeQuest.Repositories;

namespace TimeQuest.Services;

public class TimesheetService
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly XpService _xpService;
    private readonly BadgeService _badgeService;

    public TimesheetService(ITimeEntryRepository timeEntries, XpService xpService, BadgeService badgeService)
    {
        _timeEntries = timeEntries;
        _xpService = xpService;
        _badgeService = badgeService;
    }

    public async Task<TimeEntry> CreateAsync(string userId, int projectId, DateOnly date, decimal hours, string description)
    {
        var entry = new TimeEntry
        {
            UserId = userId,
            ProjectId = projectId,
            Date = date,
            Hours = hours,
            Description = description,
            Status = TimeEntryStatus.Draft
        };
        await _timeEntries.AddAsync(entry);
        await _timeEntries.SaveChangesAsync();
        return entry;
    }

    public async Task SubmitAsync(int entryId, string userId)
    {
        var entry = await _timeEntries.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException("Entry not found.");

        if (entry.UserId != userId)
            throw new InvalidOperationException("Entry does not belong to this user.");

        if (entry.Status != TimeEntryStatus.Draft && entry.Status != TimeEntryStatus.Rejected)
            throw new InvalidOperationException("Only Draft or Rejected entries can be submitted.");

        entry.Status = TimeEntryStatus.Submitted;
        entry.SubmittedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        entry.RejectionReason = null;

        _timeEntries.Update(entry);
        await _timeEntries.SaveChangesAsync();
    }

    public async Task ApproveAsync(int entryId, string reviewerId)
    {
        var entry = await _timeEntries.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException("Entry not found.");

        if (entry.Status != TimeEntryStatus.Submitted)
            throw new InvalidOperationException("Only Submitted entries can be approved.");

        entry.Status = TimeEntryStatus.Approved;
        entry.ReviewedAt = DateTime.UtcNow;
        entry.ReviewedById = reviewerId;
        entry.UpdatedAt = DateTime.UtcNow;

        _timeEntries.Update(entry);
        await _timeEntries.SaveChangesAsync();

        await _xpService.AwardXpAsync(entry.UserId, entry.Hours);
        await _badgeService.CheckAndAwardBadgesAsync(entry.UserId, entry);
    }

    public async Task RejectAsync(int entryId, string reviewerId, string reason)
    {
        var entry = await _timeEntries.GetByIdAsync(entryId)
            ?? throw new InvalidOperationException("Entry not found.");

        if (entry.Status != TimeEntryStatus.Submitted)
            throw new InvalidOperationException("Only Submitted entries can be rejected.");

        entry.Status = TimeEntryStatus.Rejected;
        entry.ReviewedAt = DateTime.UtcNow;
        entry.ReviewedById = reviewerId;
        entry.RejectionReason = reason;
        entry.UpdatedAt = DateTime.UtcNow;

        _timeEntries.Update(entry);
        await _timeEntries.SaveChangesAsync();
    }

    public async Task<IEnumerable<TimeEntry>> GetUserEntriesAsync(string userId) =>
        await _timeEntries.GetByUserAsync(userId);

    public async Task<IEnumerable<TimeEntry>> GetPendingForManagerAsync(string managerId) =>
        await _timeEntries.GetPendingForManagerAsync(managerId);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Services/TimesheetService.cs
git commit -m "feat: add TimesheetService with create/submit/approve/reject flow"
```

---

## Task 23: TeamService

**Files:**
- Create: `TimeQuest/Services/TeamService.cs`

- [ ] **Step 1: Create TeamService.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Models;
using TimeQuest.Repositories;

namespace TimeQuest.Services;

public class TeamService
{
    private readonly ITeamRepository _teams;
    private readonly ApplicationDbContext _context;

    public TeamService(ITeamRepository teams, ApplicationDbContext context)
    {
        _teams = teams;
        _context = context;
    }

    public async Task<Team> CreateTeamAsync(string name, string? description = null)
    {
        var team = new Team { Name = name, Description = description };
        await _teams.AddAsync(team);
        await _teams.SaveChangesAsync();
        return team;
    }

    public async Task AddMemberAsync(int teamId, string userId, TeamRole role = TeamRole.Member)
    {
        var existing = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);

        if (existing != null)
            existing.Role = role;
        else
            _context.UserTeams.Add(new UserTeam { TeamId = teamId, UserId = userId, Role = role });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(int teamId, string userId)
    {
        var member = await _context.UserTeams
            .FirstOrDefaultAsync(ut => ut.TeamId == teamId && ut.UserId == userId);
        if (member == null) return;

        _context.UserTeams.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task AssignProjectAsync(int projectId, string userId)
    {
        var existing = await _context.UserProjects
            .FirstOrDefaultAsync(up => up.ProjectId == projectId && up.UserId == userId);
        if (existing != null) return;

        _context.UserProjects.Add(new UserProject { ProjectId = projectId, UserId = userId });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveProjectAsync(int projectId, string userId)
    {
        var assignment = await _context.UserProjects
            .FirstOrDefaultAsync(up => up.ProjectId == projectId && up.UserId == userId);
        if (assignment == null) return;

        _context.UserProjects.Remove(assignment);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Team>> GetTeamsForUserAsync(string userId) =>
        await _context.UserTeams
            .Where(ut => ut.UserId == userId)
            .Include(ut => ut.Team)
            .Select(ut => ut.Team)
            .ToListAsync();

    public async Task<Team?> GetTeamWithMembersAsync(int teamId) =>
        await _teams.GetWithMembersAsync(teamId);

    public async Task<IEnumerable<Team>> GetAllTeamsAsync() =>
        await _teams.GetAllAsync();

    public async Task<IEnumerable<Team>> GetManagedTeamsAsync(string managerId) =>
        await _teams.GetManagedByAsync(managerId);
}
```

- [ ] **Step 2: Commit**
```
git add TimeQuest/Services/TeamService.cs
git commit -m "feat: add TeamService"
```

---

## Task 24: ReportingService

**Files:**
- Create: `TimeQuest/Services/ReportingService.cs`

- [ ] **Step 1: Create ReportingService.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using TimeQuest.Data;
using TimeQuest.Helpers;
using TimeQuest.Models;
using TimeQuest.Repositories;

namespace TimeQuest.Services;

public class ReportingService
{
    private readonly IUserRepository _users;
    private readonly ITimeEntryRepository _timeEntries;
    private readonly ITeamRepository _teams;
    private readonly ApplicationDbContext _context;

    public ReportingService(IUserRepository users, ITimeEntryRepository timeEntries, ITeamRepository teams, ApplicationDbContext context)
    {
        _users = users;
        _timeEntries = timeEntries;
        _teams = teams;
        _context = context;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(string userId)
    {
        var user = await _users.GetWithBadgesAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var totalHours = await _timeEntries.GetTotalApprovedHoursAsync(userId);

        var hoursByProject = await _context.TimeEntries
            .Where(t => t.UserId == userId && t.Status == TimeEntryStatus.Approved)
            .Include(t => t.Project)
            .GroupBy(t => t.Project.Name)
            .Select(g => new ProjectHoursDto(g.Key, g.Sum(t => t.Hours)))
            .ToListAsync();

        return new UserStatsDto(
            userId,
            $"{user.FirstName} {user.LastName}",
            user.XP,
            user.Level,
            LevelHelper.GetTitle(user.XP),
            LevelHelper.GetXpForNextLevel(user.XP),
            totalHours,
            user.UserBadges.ToList(),
            hoursByProject
        );
    }

    public async Task<TeamReportDto> GetTeamReportAsync(int teamId)
    {
        var team = await _teams.GetWithMembersAsync(teamId)
            ?? throw new InvalidOperationException("Team not found.");

        var memberIdList = team.UserTeams.Select(ut => ut.UserId).ToList();

        var pendingCount = await _context.TimeEntries
            .CountAsync(t => memberIdList.Contains(t.UserId) && t.Status == TimeEntryStatus.Submitted);

        var members = new List<MemberStatsDto>();
        foreach (var ut in team.UserTeams)
        {
            var totalHours = await _timeEntries.GetTotalApprovedHoursAsync(ut.UserId);
            var pending = await _context.TimeEntries
                .CountAsync(t => t.UserId == ut.UserId && t.Status == TimeEntryStatus.Submitted);

            members.Add(new MemberStatsDto(
                ut.UserId,
                $"{ut.User.FirstName} {ut.User.LastName}",
                ut.User.XP,
                totalHours,
                pending
            ));
        }

        return new TeamReportDto(
            teamId,
            team.Name,
            members.OrderByDescending(m => m.TotalHours).ToList(),
            pendingCount
        );
    }

    public async Task<OrgReportDto> GetOrgReportAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalHours = await _context.TimeEntries
            .Where(t => t.Status == TimeEntryStatus.Approved)
            .SumAsync(t => t.Hours);

        var teams = await _teams.GetAllWithMembersAsync();
        var teamSummaries = new List<TeamSummaryDto>();

        foreach (var team in teams)
        {
            var memberIds = team.UserTeams.Select(ut => ut.UserId).ToList();
            var teamHours = await _context.TimeEntries
                .Where(t => memberIds.Contains(t.UserId) && t.Status == TimeEntryStatus.Approved)
                .SumAsync(t => t.Hours);

            teamSummaries.Add(new TeamSummaryDto(team.Id, team.Name, team.UserTeams.Count, teamHours));
        }

        var topUsers = await _users.GetLeaderboardAsync(10);
        var leaderboard = new List<UserStatsDto>();
        foreach (var user in topUsers)
            leaderboard.Add(await GetUserStatsAsync(user.Id));

        return new OrgReportDto(totalUsers, totalHours, teamSummaries, leaderboard);
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**
```
git add TimeQuest/Services/ReportingService.cs
git commit -m "feat: add ReportingService for user, team, and org reports"
```

---

## Task 25: SeedData

**Files:**
- Create: `TimeQuest/Data/SeedData.cs`

- [ ] **Step 1: Create wwwroot/badges/ placeholder directory**

Create `TimeQuest/wwwroot/badges/.gitkeep` (empty file to track the folder in git — drop badge images here when ready).

- [ ] **Step 2: Create SeedData.cs**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Models;

namespace TimeQuest.Data;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();

        foreach (var role in new[] { "Admin", "Manager", "Employee" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = config["SeedData:AdminEmail"] ?? "admin@timequest.com";
        var adminPassword = config["SeedData:AdminPassword"] ?? "Admin123!";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await context.Badges.AnyAsync())
        {
            var badges = new[]
            {
                new Badge { Name = "First Quest",    Description = "Logged and got your first approved timesheet",          ImagePath = "badges/first-quest.png",    Criteria = BadgeCriteria.FirstEntry },
                new Badge { Name = "Centurion",       Description = "100 total approved hours",                              ImagePath = "badges/centurion.png",       Criteria = BadgeCriteria.Hours100 },
                new Badge { Name = "Legendary",       Description = "1,000 total approved hours",                           ImagePath = "badges/legendary.png",       Criteria = BadgeCriteria.Hours1000 },
                new Badge { Name = "On a Roll",       Description = "4 consecutive weeks with at least 1 approved entry",   ImagePath = "badges/on-a-roll.png",       Criteria = BadgeCriteria.FourWeekStreak },
                new Badge { Name = "Speed Runner",    Description = "Entry approved within 24 hours of submission",         ImagePath = "badges/speed-runner.png",    Criteria = BadgeCriteria.SpeedRunner },
                new Badge { Name = "Team Player",     Description = "Approved hours across 3+ different projects",          ImagePath = "badges/team-player.png",     Criteria = BadgeCriteria.TeamPlayer },
                new Badge { Name = "Overachiever",    Description = "40+ approved hours in a single calendar week",         ImagePath = "badges/overachiever.png",    Criteria = BadgeCriteria.WeeklyOverachiever }
            };
            await context.Badges.AddRangeAsync(badges);
            await context.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 3: Commit**
```
git add TimeQuest/Data/SeedData.cs TimeQuest/wwwroot/badges/
git commit -m "feat: add SeedData (roles, admin user, badges)"
```

---

## Task 26: Final Program.cs Wiring

Register all repositories and services; run seed data at startup.

**Files:**
- Modify: `TimeQuest/Program.cs`

- [ ] **Step 1: Replace Program.cs with full wiring**

Full file content for `TimeQuest/Program.cs`:
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Components;
using TimeQuest.Data;
using TimeQuest.Models;
using TimeQuest.Repositories;
using TimeQuest.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Repositories
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();

// Services
builder.Services.AddScoped<XpService>();
builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<ReportingService>();

var app = builder.Build();

// Seed on startup
using (var scope = app.Services.CreateScope())
{
    await SeedData.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

- [ ] **Step 2: Verify full build**

Run: `dotnet build`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run the app to verify startup + seed**

Run: `dotnet run --project TimeQuest/TimeQuest.csproj`
Expected: App starts, no exceptions. Check the terminal for `Now listening on: https://localhost:...`. Ctrl+C to stop.

- [ ] **Step 4: Commit**
```
git add TimeQuest/Program.cs
git commit -m "feat: wire all repositories, services, and seed data in Program.cs"
```

---

## Self-Review Checklist

After writing this plan, checks against the spec:

- [x] Roles (Admin/Manager/Employee) — seeded in SeedData, wired via Identity
- [x] XP on approval only — `ApproveAsync` triggers `XpService.AwardXpAsync`
- [x] Level from XP — `LevelHelper.GetLevel`, stored on user, updated in `XpService`
- [x] All 7 predefined badges — seeded, criteria logic in `BadgeService`
- [x] Teams + Projects many-to-many — `UserTeam`, `UserProject` join tables
- [x] Manager scoped to their teams — `GetPendingForManagerAsync` filters by `UserTeam.Role = Manager`
- [x] Timesheet states: Draft → Submitted → Approved/Rejected — `TimesheetService` enforces transitions
- [x] Rejection + resubmit — `RejectAsync` sets reason; `SubmitAsync` accepts `Rejected` status
- [x] Reporting: user, team, org — all three in `ReportingService`
- [x] Seed: roles, admin user, badges — `SeedData.SeedAsync`
- [x] `wwwroot/badges/` directory created for badge images
- [x] xUnit tests for `LevelHelper` — all GetLevel, GetTitle, GetXpForNextLevel cases covered
