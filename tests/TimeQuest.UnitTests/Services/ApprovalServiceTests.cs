using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Exceptions;
using TimeQuest.Infrastructure.Services;
using TimeQuest.UnitTests.TestHelpers;
using Xunit;

namespace TimeQuest.UnitTests.Services;

public class ApprovalServiceTests
{
    private static ApprovalService CreateService(Infrastructure.Data.ApplicationDbContext db)
        => new ApprovalService(db, new TestAuditService(), NullLogger<ApprovalService>.Instance);

    private static async Task<(ApplicationUser user, ApplicationUser lead, WeeklyTimesheet timesheet)>
        SeedSubmittedTimesheetAsync(Infrastructure.Data.ApplicationDbContext db)
    {
        var user = new ApplicationUser
        {
            Id = 10,
            UserName = "member@test.com",
            NormalizedUserName = "MEMBER@TEST.COM",
            Email = "member@test.com",
            NormalizedEmail = "MEMBER@TEST.COM",
            DisplayName = "Member",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var lead = new ApplicationUser
        {
            Id = 20,
            UserName = "lead@test.com",
            NormalizedUserName = "LEAD@TEST.COM",
            Email = "lead@test.com",
            NormalizedEmail = "LEAD@TEST.COM",
            DisplayName = "Lead",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        db.Users.Add(user);
        db.Users.Add(lead);

        // Wire up team membership so the scope guard passes
        var team = new Team { Id = 100, Name = "Test Team", RowVersion = null! };
        db.Teams.Add(team);
        db.UserTeams.Add(new UserTeam { UserId = user.Id, TeamId = team.Id, Role = UserTeamRole.Member });
        db.UserTeams.Add(new UserTeam { UserId = lead.Id, TeamId = team.Id, Role = UserTeamRole.Lead });

        var timesheet = new WeeklyTimesheet
        {
            UserId = user.Id,
            WeekStart = new DateOnly(2026, 5, 18),
            WeekEnd = new DateOnly(2026, 5, 24),
            Status = TimesheetStatus.Submitted,
            SubmittedAt = DateTimeOffset.UtcNow
        };
        db.WeeklyTimesheets.Add(timesheet);
        await db.SaveChangesAsync();

        return (user, lead, timesheet);
    }

    // ── LeadApproveAsync: happy path ─────────────────────────────────────────────

    [Fact]
    public async Task LeadApproveAsync_SubmittedTimesheetNoFlags_TransitionsToApprovedByLead()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var service = CreateService(db);
        var result = await service.LeadApproveAsync(lead.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.WeeklyTimesheets.FindAsync(timesheet.Id);
        Assert.Equal(TimesheetStatus.ApprovedByLead, updated!.Status);

        var approvalRecord = await db.ApprovalRecords
            .FirstOrDefaultAsync(ar => ar.TimesheetId == timesheet.Id);
        Assert.NotNull(approvalRecord);
        Assert.Equal(ApprovalAction.LeadApproved, approvalRecord!.Action);
    }

    // ── LeadApproveAsync: wrong status ───────────────────────────────────────────

    [Fact]
    public async Task LeadApproveAsync_DraftTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        // Change to Draft
        timesheet.Status = TimesheetStatus.Draft;
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.LeadApproveAsync(lead.Id, timesheet.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Submitted", result.Error);
    }

    // ── LeadApproveAsync: unvalidated overtime throws OvertimeNotValidatedException ──

    [Fact]
    public async Task LeadApproveAsync_UnvalidatedFlaggedEntry_ThrowsOvertimeNotValidatedException()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var project = new Project { Id = 99, Name = "Test Project", BillingType = BillingType.TM };
        db.Projects.Add(project);

        // Add a flagged entry without overtime validation
        var entry = new TimeEntry
        {
            WeeklyTimesheetId = timesheet.Id,
            UserId = user.Id,
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 12,
            Notes = "Overtime work",
            FlaggedForReview = true,
            OvertimeValidatedById = null // NOT validated
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<OvertimeNotValidatedException>(
            () => service.LeadApproveAsync(lead.Id, timesheet.Id, CancellationToken.None));
    }

    // ── LeadApproveAsync: validated overtime is allowed ─────────────────────────

    [Fact]
    public async Task LeadApproveAsync_ValidatedFlaggedEntry_Succeeds()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var project = new Project { Id = 99, Name = "Test Project", BillingType = BillingType.TM };
        db.Projects.Add(project);

        // Flagged entry WITH overtime validation
        var entry = new TimeEntry
        {
            WeeklyTimesheetId = timesheet.Id,
            UserId = user.Id,
            Date = new DateOnly(2026, 5, 18),
            ProjectId = project.Id,
            Hours = 12,
            Notes = "Overtime work",
            FlaggedForReview = true,
            OvertimeValidatedById = lead.Id, // Validated
            OvertimeValidationNote = "Client escalation approved",
            OvertimeValidatedAt = DateTimeOffset.UtcNow
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.LeadApproveAsync(lead.Id, timesheet.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.WeeklyTimesheets.FindAsync(timesheet.Id);
        Assert.Equal(TimesheetStatus.ApprovedByLead, updated!.Status);
    }

    // ── LeadRejectAsync: transitions to Draft with rejection comment ─────────────

    [Fact]
    public async Task LeadRejectAsync_SubmittedTimesheet_TransitionsToDraftWithComment()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var service = CreateService(db);
        var result = await service.LeadRejectAsync(lead.Id, timesheet.Id, "Wrong project assignment.", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.WeeklyTimesheets.FindAsync(timesheet.Id);
        Assert.Equal(TimesheetStatus.Draft, updated!.Status);
        Assert.Equal("Wrong project assignment.", updated.LastRejectionComment);
        Assert.Equal(lead.Id, updated.LastRejectedByUserId);
        Assert.NotNull(updated.LastRejectedAt);
    }

    // ── LockTimesheetAsync: happy path (RowVersion not tested here — SQLite) ─────
    // Note: RowVersion concurrency tests require SQL Server — covered in IntegrationTests

    [Fact]
    public async Task LockTimesheetAsync_FinancialApprovedTimesheet_TransitionsToLocked()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var finAdmin = new ApplicationUser
        {
            Id = 30,
            UserName = "fin@test.com",
            NormalizedUserName = "FIN@TEST.COM",
            Email = "fin@test.com",
            NormalizedEmail = "FIN@TEST.COM",
            DisplayName = "Financial Admin",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(finAdmin);

        timesheet.Status = TimesheetStatus.FinancialApproved;
        await db.SaveChangesAsync();

        // SQLite: RowVersion is not enforced in tests — pass empty array
        var service = CreateService(db);
        var result = await service.LockTimesheetAsync(
            finAdmin.Id,
            timesheet.Id,
            Array.Empty<byte>(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.WeeklyTimesheets.FindAsync(timesheet.Id);
        Assert.Equal(TimesheetStatus.Locked, updated!.Status);
        Assert.Equal(finAdmin.Id, updated.LockedById);
        Assert.NotNull(updated.LockedAt);
    }

    // ── LockTimesheetAsync: wrong status returns failure ─────────────────────────

    [Fact]
    public async Task LockTimesheetAsync_SubmittedTimesheet_ReturnsFailure()
    {
        using var db = DbContextFactory.NewDb();
        var (user, lead, timesheet) = await SeedSubmittedTimesheetAsync(db);

        var finAdmin = new ApplicationUser
        {
            Id = 30,
            UserName = "fin@test.com",
            NormalizedUserName = "FIN@TEST.COM",
            Email = "fin@test.com",
            NormalizedEmail = "FIN@TEST.COM",
            DisplayName = "Financial Admin",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(finAdmin);
        await db.SaveChangesAsync();

        // timesheet is Submitted, not FinancialApproved
        var service = CreateService(db);
        var result = await service.LockTimesheetAsync(
            finAdmin.Id,
            timesheet.Id,
            Array.Empty<byte>(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("FinancialApproved", result.Error);
    }
}
