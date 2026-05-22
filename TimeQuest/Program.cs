using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Components;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.BackgroundServices;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<TimeEntryService>();
builder.Services.AddScoped<WeeklyTimesheetService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IScopeGuard, ScopeGuard>();
builder.Services.AddScoped<TicketValidationService>();

// ── Background services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<TicketValidationBackgroundService>();

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
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
