using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;

namespace TimeQuest.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<UserTeam> UserTeams => Set<UserTeam>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<AdminProjectScope> AdminProjectScopes => Set<AdminProjectScope>();
    public DbSet<WeeklyTimesheet> WeeklyTimesheets => Set<WeeklyTimesheet>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TimesheetDayFlag> TimesheetDayFlags => Set<TimesheetDayFlag>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Composite PKs ──────────────────────────────────────────────────────────

        modelBuilder.Entity<UserTeam>()
            .HasKey(ut => new { ut.UserId, ut.TeamId });

        modelBuilder.Entity<ProjectUser>()
            .HasKey(pu => new { pu.UserId, pu.ProjectId });

        modelBuilder.Entity<AdminProjectScope>()
            .HasKey(aps => new { aps.AdminUserId, aps.ProjectId });

        modelBuilder.Entity<TimesheetDayFlag>()
            .HasKey(tdf => new { tdf.WeeklyTimesheetId, tdf.Date });

        // ── TimeEntry.Hours precision ──────────────────────────────────────────────

        modelBuilder.Entity<TimeEntry>()
            .Property(te => te.Hours)
            .HasColumnType("decimal(5,2)");

        // ── FK delete behaviours ───────────────────────────────────────────────────

        // Team.TeamLeadId → ApplicationUser (NoAction)
        modelBuilder.Entity<Team>()
            .HasOne(t => t.TeamLead)
            .WithMany()
            .HasForeignKey(t => t.TeamLeadId)
            .OnDelete(DeleteBehavior.NoAction);

        // UserTeam.UserId → ApplicationUser (NoAction)
        modelBuilder.Entity<UserTeam>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.UserTeams)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // UserTeam.TeamId → Team (Cascade)
        modelBuilder.Entity<UserTeam>()
            .HasOne(ut => ut.Team)
            .WithMany(t => t.UserTeams)
            .HasForeignKey(ut => ut.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectUser.UserId → ApplicationUser (NoAction)
        modelBuilder.Entity<ProjectUser>()
            .HasOne(pu => pu.User)
            .WithMany(u => u.ProjectUsers)
            .HasForeignKey(pu => pu.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ProjectUser.ProjectId → Project (Cascade)
        modelBuilder.Entity<ProjectUser>()
            .HasOne(pu => pu.Project)
            .WithMany(p => p.ProjectUsers)
            .HasForeignKey(pu => pu.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // AdminProjectScope.AdminUserId → ApplicationUser (NoAction)
        modelBuilder.Entity<AdminProjectScope>()
            .HasOne(aps => aps.AdminUser)
            .WithMany(u => u.AdminProjectScopes)
            .HasForeignKey(aps => aps.AdminUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // AdminProjectScope.ProjectId → Project (Cascade)
        modelBuilder.Entity<AdminProjectScope>()
            .HasOne(aps => aps.Project)
            .WithMany(p => p.AdminProjectScopes)
            .HasForeignKey(aps => aps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // WeeklyTimesheet.UserId → ApplicationUser (Restrict)
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasOne(wt => wt.User)
            .WithMany(u => u.WeeklyTimesheets)
            .HasForeignKey(wt => wt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // WeeklyTimesheet.LockedById → ApplicationUser (NoAction)
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasOne(wt => wt.LockedBy)
            .WithMany()
            .HasForeignKey(wt => wt.LockedById)
            .OnDelete(DeleteBehavior.NoAction);

        // WeeklyTimesheet.LastRejectedByUserId → ApplicationUser (NoAction)
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasOne(wt => wt.LastRejectedBy)
            .WithMany()
            .HasForeignKey(wt => wt.LastRejectedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TimeEntry.WeeklyTimesheetId → WeeklyTimesheet (Restrict)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.WeeklyTimesheet)
            .WithMany(wt => wt.TimeEntries)
            .HasForeignKey(te => te.WeeklyTimesheetId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimeEntry.UserId → ApplicationUser (NoAction)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.User)
            .WithMany(u => u.TimeEntries)
            .HasForeignKey(te => te.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // TimeEntry.ProjectId → Project (Restrict)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.Project)
            .WithMany(p => p.TimeEntries)
            .HasForeignKey(te => te.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimeEntry.OvertimeValidatedById → ApplicationUser (NoAction)
        modelBuilder.Entity<TimeEntry>()
            .HasOne(te => te.OvertimeValidatedBy)
            .WithMany()
            .HasForeignKey(te => te.OvertimeValidatedById)
            .OnDelete(DeleteBehavior.NoAction);

        // ApprovalRecord.TimesheetId → WeeklyTimesheet (Restrict)
        modelBuilder.Entity<ApprovalRecord>()
            .HasOne(ar => ar.Timesheet)
            .WithMany(wt => wt.ApprovalRecords)
            .HasForeignKey(ar => ar.TimesheetId)
            .OnDelete(DeleteBehavior.Restrict);

        // ApprovalRecord.ApproverId → ApplicationUser (Restrict)
        modelBuilder.Entity<ApprovalRecord>()
            .HasOne(ar => ar.Approver)
            .WithMany(u => u.ApprovalRecords)
            .HasForeignKey(ar => ar.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimesheetDayFlag.WeeklyTimesheetId → WeeklyTimesheet (Cascade)
        modelBuilder.Entity<TimesheetDayFlag>()
            .HasOne(tdf => tdf.WeeklyTimesheet)
            .WithMany(wt => wt.DayFlags)
            .HasForeignKey(tdf => tdf.WeeklyTimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        // TimesheetDayFlag.UserId → ApplicationUser (NoAction)
        modelBuilder.Entity<TimesheetDayFlag>()
            .HasOne(tdf => tdf.User)
            .WithMany()
            .HasForeignKey(tdf => tdf.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // AuditEvent.ActorId → ApplicationUser (SetNull)
        modelBuilder.Entity<AuditEvent>()
            .HasOne(ae => ae.Actor)
            .WithMany(u => u.AuditEvents)
            .HasForeignKey(ae => ae.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Unique constraints ─────────────────────────────────────────────────────

        modelBuilder.Entity<WeeklyTimesheet>()
            .HasIndex(wt => new { wt.UserId, wt.WeekStart })
            .IsUnique()
            .HasDatabaseName("UQ_WeeklyTimesheet_UserId_WeekStart");

        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => new { te.UserId, te.Date, te.ProjectId })
            .IsUnique()
            .HasDatabaseName("UQ_TimeEntry_UserId_Date_ProjectId");

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Team_Name");

        // ── Performance indexes ────────────────────────────────────────────────────

        // Weekly view — load user's week
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasIndex(wt => new { wt.UserId, wt.WeekStart })
            .HasDatabaseName("IX_WeeklyTimesheet_UserId_WeekStart");

        // Team Lead queue — submitted timesheets
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasIndex(wt => new { wt.Status, wt.SubmittedAt })
            .HasDatabaseName("IX_WeeklyTimesheet_Status_SubmittedAt");

        // Team Lead scope — find teams where user is Lead
        modelBuilder.Entity<UserTeam>()
            .HasIndex(ut => new { ut.UserId, ut.Role })
            .HasDatabaseName("IX_UserTeam_UserId_Role");

        // Team membership lookup
        modelBuilder.Entity<UserTeam>()
            .HasIndex(ut => new { ut.TeamId, ut.UserId })
            .HasDatabaseName("IX_UserTeam_TeamId_UserId");

        // Financial extra-approval queue
        modelBuilder.Entity<WeeklyTimesheet>()
            .HasIndex(wt => wt.Status)
            .HasDatabaseName("IX_WeeklyTimesheet_Status_LeadApproved");

        // Entry list for approval detail
        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => te.WeeklyTimesheetId)
            .HasDatabaseName("IX_TimeEntry_WeeklyTimesheetId");

        // Export locked entries by date range
        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => new { te.Date, te.WeeklyTimesheetId })
            .HasDatabaseName("IX_TimeEntry_Date_WeeklyTimesheetId");

        // Ticket validation pending queue
        modelBuilder.Entity<TimeEntry>()
            .HasIndex(te => te.TicketValidationStatus)
            .HasDatabaseName("IX_TimeEntry_TicketValidationStatus");

        // Admin project scope check
        modelBuilder.Entity<AdminProjectScope>()
            .HasIndex(aps => aps.AdminUserId)
            .HasDatabaseName("IX_AdminProjectScope_AdminUserId");

        // Project user list for admin
        modelBuilder.Entity<ProjectUser>()
            .HasIndex(pu => new { pu.ProjectId, pu.UserId })
            .HasDatabaseName("IX_ProjectUser_ProjectId_UserId");

        // Latest rejection ApprovalRecord for timesheet
        modelBuilder.Entity<ApprovalRecord>()
            .HasIndex(ar => new { ar.TimesheetId, ar.Timestamp })
            .HasDatabaseName("IX_ApprovalRecord_TimesheetId_Timestamp");

        // Audit log search by time + action type
        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => new { ae.TimestampUtc, ae.ActionType })
            .HasDatabaseName("IX_AuditEvent_TimestampUtc_ActionType");

        // Audit log search by actor
        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => new { ae.ActorId, ae.TimestampUtc })
            .HasDatabaseName("IX_AuditEvent_ActorId_TimestampUtc");

        // Pre-submission leave/holiday check — covered by composite PK index on TimesheetDayFlag
        // The composite PK (WeeklyTimesheetId, Date) creates the required index automatically

    }

    // Identity roles seeded at runtime by SeedData.cs (not via HasData) so that
    // SQLite in-memory EnsureCreated works without SQL Server-specific RETURNING clauses.
}
