# Integration Agent — Requirements

## Source of Truth
- Plan: `C:\TimeQuest\docs\superpowers\plans\2026-05-21-timequest-backend.md` (Task 26)
- Design: `C:\TimeQuest\design.md`

## Pre-Flight Check
```powershell
dotnet build C:\TimeQuest\TimeQuest       # must succeed
dotnet test C:\TimeQuest\TimeQuest.Tests  # must pass
```

## Task 26 — Final Program.cs Wiring

Replace `C:\TimeQuest\TimeQuest\Program.cs` with the fully wired version.

Full file content:
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

// Seed roles, admin user, and badges on startup
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

## Verification Steps

### Step 1 — Build
```powershell
dotnet build C:\TimeQuest\TimeQuest
```
Expected: `Build succeeded. 0 Error(s)`

### Step 2 — Tests
```powershell
dotnet test C:\TimeQuest\TimeQuest.Tests
```
Expected: `Passed! - Failed: 0, Passed: 19, Skipped: 0`

### Step 3 — Startup test
```powershell
dotnet run --project C:\TimeQuest\TimeQuest\TimeQuest.csproj
```
Expected:
- No exceptions in the terminal
- `Now listening on: https://localhost:XXXX` message appears
- Ctrl+C to stop

If you see a DB error, check that the migration was applied:
```powershell
dotnet ef database update --project C:\TimeQuest\TimeQuest\TimeQuest.csproj --startup-project C:\TimeQuest\TimeQuest\TimeQuest.csproj
```

## Completion Criteria

- [ ] `dotnet build` → `Build succeeded. 0 Error(s)`
- [ ] `dotnet test` → all 19 tests pass
- [ ] App starts and seeds without errors

## Commit When Done
```
git -C C:\TimeQuest commit -m "feat: wire all repositories, services, and seed data in Program.cs"
```
