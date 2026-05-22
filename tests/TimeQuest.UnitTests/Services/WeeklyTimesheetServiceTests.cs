using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Infrastructure.Services;
using TimeQuest.UnitTests.TestHelpers;
using Xunit;

namespace TimeQuest.UnitTests.Services;

public class WeeklyTimesheetServiceTests
{
    private static WeeklyTimesheetService CreateService(Infrastructure.Data.ApplicationDbContext db)
        => new WeeklyTimesheetService(db, new TestAuditService(), NullLogger<WeeklyTimesheetService>.Instance);

    private static async Task<(ApplicationUser user, WeeklyTimesheet timesheet)> SeedUserAndTimesheetAsync(
        Infrastructure.Data.ApplicationDbContext db,
        DateOnly weekStart)
    {
        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "testuser@test.com",
            NormalizedUserName = "TESTUSER@TEST.COM",
            Email = "testuser@test.com",
            NormalizedEmail = "TESTUSER@TEST.COM",
            DisplayName = "Test User",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);

        var timesheet = new WeeklyTimesheet
        {
            UserId = 1,
            WeekStart = weekStart,
            WeekEnd = weekStart.AddDays(6),
            Status = TimesheetStatus.Draft
        };
        db.WeeklyTimesheets.Add(timesheet);
        await db.SaveChangesAsync();
        return (user, timesheet);
    }

    private static async Task<Project> SeedProjectAsync(Infrastructure.Data.ApplicationDbContext db, int id = 1)
    {
        var project = new Project { Id = id, Name = $"Project {id}", BillingType = BillingType.TM };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    // ── Happy path: full week Mon–Fri entries ────────────────────────────────────

    [Fact]
    public async Task ValidateForSubmissionAsync_FullWeekEntries_ReturnsCanSubmitTrue()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18); // Monday
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);
        var project = await SeedProjectAsync(db);

        // Add Mon–Fri entries
        for (int i = 0; i < 5; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                WeeklyTimesheetId = timesheet.Id,
                UserId = user.Id,
                Date = weekStart.AddDays(i),
                ProjectId = project.Id,
                Hours = 8,
                Notes = $"Day {i + 1} work"
            });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ValidateForSubmissionAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.CanSubmit);
        Assert.Empty(result.MissingDays);
    }

    // ── F1.2 AC2: days covered by TimesheetDayFlag are NOT counted as missing ────

    [Fact]
    public async Task ValidateForSubmissionAsync_FourEntriesPlusLeaveFlag_ReturnsCanSubmitTrue()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18); // Monday
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);
        var project = await SeedProjectAsync(db);

        // Mon–Thu entries (4 days)
        for (int i = 0; i < 4; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                WeeklyTimesheetId = timesheet.Id,
                UserId = user.Id,
                Date = weekStart.AddDays(i),
                ProjectId = project.Id,
                Hours = 8,
                Notes = $"Day {i + 1}"
            });
        }

        // Friday covered by leave flag
        db.TimesheetDayFlags.Add(new TimesheetDayFlag
        {
            WeeklyTimesheetId = timesheet.Id,
            Date = weekStart.AddDays(4), // Friday
            FlagType = DayFlagType.Leave,
            UserId = user.Id
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ValidateForSubmissionAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.CanSubmit);
        Assert.Empty(result.MissingDays);
    }

    // ── Missing days without flag ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateForSubmissionAsync_ThreeEntries_ReturnsMissingTwoDays()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18); // Monday
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);
        var project = await SeedProjectAsync(db);

        // Mon, Tue, Wed entries only (3 of 5)
        for (int i = 0; i < 3; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                WeeklyTimesheetId = timesheet.Id,
                UserId = user.Id,
                Date = weekStart.AddDays(i),
                ProjectId = project.Id,
                Hours = 8,
                Notes = $"Day {i + 1}"
            });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ValidateForSubmissionAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.False(result.CanSubmit);
        Assert.Equal(2, result.MissingDays.Count);
        Assert.Contains(weekStart.AddDays(3), result.MissingDays); // Thursday
        Assert.Contains(weekStart.AddDays(4), result.MissingDays); // Friday
    }

    // ── Full leave week ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateForSubmissionAsync_FiveDayFlags_ReturnsCanSubmitTrue()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18); // Monday
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);

        // All 5 weekdays flagged as leave
        for (int i = 0; i < 5; i++)
        {
            db.TimesheetDayFlags.Add(new TimesheetDayFlag
            {
                WeeklyTimesheetId = timesheet.Id,
                Date = weekStart.AddDays(i),
                FlagType = DayFlagType.Leave,
                UserId = user.Id
            });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ValidateForSubmissionAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.CanSubmit);
        Assert.Empty(result.MissingDays);
    }

    // ── GetOrCreate creates new timesheet when none exists ───────────────────────

    [Fact]
    public async Task GetOrCreateForWeekAsync_NoExistingTimesheet_CreatesNew()
    {
        using var db = DbContextFactory.NewDb();

        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "testuser@test.com",
            NormalizedUserName = "TESTUSER@TEST.COM",
            Email = "testuser@test.com",
            NormalizedEmail = "TESTUSER@TEST.COM",
            DisplayName = "Test User",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var weekStart = new DateOnly(2026, 5, 18);
        var dto = await service.GetOrCreateForWeekAsync(1, weekStart, CancellationToken.None);

        Assert.Equal(weekStart, dto.WeekStart);
        Assert.Equal(TimesheetStatus.Draft, dto.Status);
        Assert.Empty(dto.Entries);
    }

    // ── SubmitAsync transitions Draft → Submitted ────────────────────────────────

    [Fact]
    public async Task SubmitAsync_DraftTimesheet_TransitionsToSubmitted()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18);
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);

        var service = CreateService(db);
        var result = await service.SubmitAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.WeeklyTimesheets.FindAsync(timesheet.Id);
        Assert.Equal(TimesheetStatus.Submitted, updated!.Status);
        Assert.NotNull(updated.SubmittedAt);
    }

    // ── SubmitAsync fails for non-Draft timesheet ────────────────────────────────

    [Fact]
    public async Task SubmitAsync_NonDraftTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var weekStart = new DateOnly(2026, 5, 18);
        var (user, timesheet) = await SeedUserAndTimesheetAsync(db, weekStart);

        timesheet.Status = TimesheetStatus.Submitted;
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.SubmitAsync(user.Id, timesheet.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
