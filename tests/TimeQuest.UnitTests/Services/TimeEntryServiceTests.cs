using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Infrastructure.Services;
using TimeQuest.Shared.Dtos;
using TimeQuest.UnitTests.TestHelpers;
using Xunit;

namespace TimeQuest.UnitTests.Services;

public class TimeEntryServiceTests
{
    private static TimeEntryService CreateService(Infrastructure.Data.ApplicationDbContext db, decimal flagThreshold = 10m)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FlagThresholdHours", flagThreshold.ToString() }
            })
            .Build();

        return new TimeEntryService(db, new TestAuditService(), config, NullLogger<TimeEntryService>.Instance);
    }

    private static async Task<(ApplicationUser user, WeeklyTimesheet timesheet, Project project)>
        SeedDraftTimesheetAsync(Infrastructure.Data.ApplicationDbContext db, int userId = 1)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"user{userId}@test.com",
            NormalizedUserName = $"USER{userId}@TEST.COM",
            Email = $"user{userId}@test.com",
            NormalizedEmail = $"USER{userId}@TEST.COM",
            DisplayName = $"User {userId}",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);

        var project = new Project
        {
            Id = userId * 10,
            Name = $"Project {userId}",
            BillingType = BillingType.TM
        };
        db.Projects.Add(project);

        var timesheet = new WeeklyTimesheet
        {
            UserId = userId,
            WeekStart = new DateOnly(2026, 5, 18),
            WeekEnd = new DateOnly(2026, 5, 24),
            Status = TimesheetStatus.Draft
        };
        db.WeeklyTimesheets.Add(timesheet);
        await db.SaveChangesAsync();

        return (user, timesheet, project);
    }

    // ── CreateEntryAsync: happy path ─────────────────────────────────────────────

    [Fact]
    public async Task CreateEntryAsync_ValidRequest_ReturnsTimeEntryDto()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        var service = CreateService(db);
        var request = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Sprint planning session"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(8m, result.Value!.Hours);
        Assert.Equal(project.Id, result.Value.ProjectId);
        Assert.False(result.Value.FlaggedForReview);
    }

    // ── CreateEntryAsync: flags entry when hours > threshold ────────────────────

    [Fact]
    public async Task CreateEntryAsync_HoursExceedThreshold_SetsFlaggedForReview()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        var service = CreateService(db, flagThreshold: 10m);
        var request = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 10.5m,
            Notes = "Overtime session"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.FlaggedForReview);
    }

    // ── CreateEntryAsync: duplicate entry blocked ────────────────────────────────

    [Fact]
    public async Task CreateEntryAsync_DuplicateDateProjectUser_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        // Create first entry
        db.TimeEntries.Add(new TimeEntry
        {
            WeeklyTimesheetId = timesheet.Id,
            UserId = user.Id,
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "First entry"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var duplicateRequest = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 4m,
            Notes = "Duplicate entry"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, duplicateRequest, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    // ── CreateEntryAsync: locked timesheet blocked ───────────────────────────────

    [Fact]
    public async Task CreateEntryAsync_LockedTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        // Lock the timesheet
        timesheet.Status = TimesheetStatus.Locked;
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Attempt on locked timesheet"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Draft", result.Error);
    }

    // ── CreateEntryAsync: submitted timesheet blocked ────────────────────────────

    [Fact]
    public async Task CreateEntryAsync_SubmittedTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        timesheet.Status = TimesheetStatus.Submitted;
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Attempt on submitted"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── DeleteEntryAsync: locked timesheet blocked ───────────────────────────────

    [Fact]
    public async Task DeleteEntryAsync_LockedTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        var entry = new TimeEntry
        {
            WeeklyTimesheetId = timesheet.Id,
            UserId = user.Id,
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Some work"
        };
        db.TimeEntries.Add(entry);

        timesheet.Status = TimesheetStatus.Locked;
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.DeleteEntryAsync(user.Id, entry.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("locked", result.Error!.ToLower());
    }

    // ── DeleteEntryAsync: happy path ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteEntryAsync_DraftTimesheetEntry_Succeeds()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        var entry = new TimeEntry
        {
            WeeklyTimesheetId = timesheet.Id,
            UserId = user.Id,
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Some work"
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.DeleteEntryAsync(user.Id, entry.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var deleted = await db.TimeEntries.FindAsync(entry.Id);
        Assert.Null(deleted);
    }

    // ── TicketRef triggers Pending validation status ─────────────────────────────

    [Fact]
    public async Task CreateEntryAsync_WithTicketRef_SetsTicketValidationStatusPending()
    {
        using var db = DbContextFactory.NewDb();
        var (user, timesheet, project) = await SeedDraftTimesheetAsync(db);

        var service = CreateService(db);
        var request = new CreateTimeEntryRequest
        {
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 8m,
            Notes = "Sprint work",
            TicketRef = "JIRA-1234"
        };

        var result = await service.CreateEntryAsync(user.Id, timesheet.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketValidationStatus.Pending, result.Value!.TicketValidationStatus);
    }
}
