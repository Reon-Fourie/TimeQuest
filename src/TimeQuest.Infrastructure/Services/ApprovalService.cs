using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Exceptions;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class ApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        ApplicationDbContext db,
        IAuditService audit,
        ILogger<ApprovalService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private async Task<string> GetDisplayNameAsync(int userId, CancellationToken ct)
        => await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct) ?? userId.ToString();

    private async Task<bool> IsLeadForTimesheetUserAsync(int leadUserId, int timesheetUserId, CancellationToken ct)
    {
        var leadTeamIds = await _db.UserTeams
            .Where(ut => ut.UserId == leadUserId && ut.Role == UserTeamRole.Lead)
            .Select(ut => ut.TeamId)
            .ToListAsync(ct);

        return await _db.UserTeams
            .AnyAsync(ut => leadTeamIds.Contains(ut.TeamId) && ut.UserId == timesheetUserId, ct);
    }

    // ── Team Lead queue ──────────────────────────────────────────────────────────

    public async Task<List<ApprovalQueueItemDto>> GetLeadQueueAsync(
        int leadUserId,
        CancellationToken ct)
    {
        // Step 1: resolve team IDs where current user is Lead
        var leadTeamIds = await _db.UserTeams
            .Where(ut => ut.UserId == leadUserId && ut.Role == UserTeamRole.Lead)
            .Select(ut => ut.TeamId)
            .ToListAsync(ct);

        // Step 2: load submitted timesheets for users in those teams
        return await _db.WeeklyTimesheets
            .Where(wt => wt.Status == TimesheetStatus.Submitted
                      && _db.UserTeams.Any(ut => leadTeamIds.Contains(ut.TeamId) && ut.UserId == wt.UserId))
            .OrderBy(wt => wt.SubmittedAt)
            .Select(wt => new ApprovalQueueItemDto(
                wt.Id,
                wt.UserId,
                wt.User.DisplayName,
                wt.WeekStart,
                wt.WeekEnd,
                wt.TimeEntries.Sum(te => te.Hours),
                wt.TimeEntries.Count(te => te.FlaggedForReview),
                wt.SubmittedAt,
                wt.Status
            ))
            .ToListAsync(ct);
    }

    public async Task<Result<TimesheetDetailDto>> GetTimesheetDetailAsync(
        int approverId,
        int timesheetId,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .Include(wt => wt.User)
            .Include(wt => wt.TimeEntries)
                .ThenInclude(te => te.Project)
            .Include(wt => wt.ApprovalRecords)
                .ThenInclude(ar => ar.Approver)
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result<TimesheetDetailDto>.Failure("Timesheet not found.");

        var dto = new TimesheetDetailDto(
            timesheet.Id,
            timesheet.User.DisplayName,
            timesheet.WeekStart,
            timesheet.WeekEnd,
            timesheet.Status,
            timesheet.TimeEntries.Select(te => new TimeEntryDto(
                te.Id,
                te.WeeklyTimesheetId,
                te.Date,
                te.ProjectId,
                te.Project?.Name ?? string.Empty,
                te.Hours,
                te.Notes,
                te.TaskType,
                te.TicketRef,
                te.FlaggedForReview,
                te.TicketValidationStatus,
                timesheet.Status == TimesheetStatus.Locked
            )).ToList(),
            timesheet.ApprovalRecords
                .OrderBy(ar => ar.Timestamp)
                .Select(ar => new ApprovalRecordDto(
                    ar.Id,
                    ar.Approver.DisplayName,
                    ar.Action,
                    ar.Comment,
                    ar.Timestamp
                )).ToList()
        );

        return Result<TimesheetDetailDto>.Success(dto);
    }

    // ── Lead approve / reject ────────────────────────────────────────────────────

    public async Task<Result> LeadApproveAsync(
        int leadUserId,
        int timesheetId,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .Include(wt => wt.TimeEntries)
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        // F2.2 AC5: lead can only approve timesheets for users in their team
        if (!await IsLeadForTimesheetUserAsync(leadUserId, timesheet.UserId, ct))
            return Result.Failure("Access denied.");

        if (timesheet.Status != TimesheetStatus.Submitted)
            return Result.Failure($"Only Submitted timesheets can be lead-approved. Current status: {timesheet.Status}.");

        // F2.3: all flagged entries must have been validated
        var unvalidated = timesheet.TimeEntries
            .Where(te => te.FlaggedForReview && te.OvertimeValidatedById == null)
            .Select(te => te.Id)
            .ToList();

        if (unvalidated.Any())
            throw new OvertimeNotValidatedException(timesheetId, unvalidated);

        timesheet.Status = TimesheetStatus.ApprovedByLead;

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            TimesheetId = timesheetId,
            ApproverId = leadUserId,
            Action = ApprovalAction.LeadApproved,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timesheet {TimesheetId} approved by lead {LeadUserId}", timesheetId, leadUserId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(leadUserId, ct),
            actorId: leadUserId,
            actionType: "Timesheet.LeadApproved",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    public async Task<Result> LeadRejectAsync(
        int leadUserId,
        int timesheetId,
        string comment,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .Include(wt => wt.LastRejectedBy)
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        if (!await IsLeadForTimesheetUserAsync(leadUserId, timesheet.UserId, ct))
            return Result.Failure("Access denied.");

        if (timesheet.Status != TimesheetStatus.Submitted)
            return Result.Failure($"Only Submitted timesheets can be lead-rejected. Current status: {timesheet.Status}.");

        timesheet.Status = TimesheetStatus.Draft;
        timesheet.LastRejectionComment = comment;
        timesheet.LastRejectedByUserId = leadUserId;
        timesheet.LastRejectedAt = DateTimeOffset.UtcNow;

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            TimesheetId = timesheetId,
            ApproverId = leadUserId,
            Action = ApprovalAction.LeadRejected,
            Comment = comment,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timesheet {TimesheetId} rejected by lead {LeadUserId}", timesheetId, leadUserId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(leadUserId, ct),
            actorId: leadUserId,
            actionType: "Timesheet.LeadRejected",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    // ── Financial Admin queue ────────────────────────────────────────────────────

    public async Task<List<ApprovalQueueItemDto>> GetFinancialQueueAsync(CancellationToken ct)
    {
        return await _db.WeeklyTimesheets
            .Where(wt => wt.Status == TimesheetStatus.ApprovedByLead)
            .OrderBy(wt => wt.SubmittedAt)
            .Select(wt => new ApprovalQueueItemDto(
                wt.Id,
                wt.UserId,
                wt.User.DisplayName,
                wt.WeekStart,
                wt.WeekEnd,
                wt.TimeEntries.Sum(te => te.Hours),
                wt.TimeEntries.Count(te => te.FlaggedForReview),
                wt.SubmittedAt,
                wt.Status
            ))
            .ToListAsync(ct);
    }

    public async Task<Result> FinancialApproveAsync(
        int financialAdminId,
        int timesheetId,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        if (timesheet.Status != TimesheetStatus.ApprovedByLead)
            return Result.Failure($"Only ApprovedByLead timesheets can be financially approved. Current status: {timesheet.Status}.");

        timesheet.Status = TimesheetStatus.FinancialApproved;

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            TimesheetId = timesheetId,
            ApproverId = financialAdminId,
            Action = ApprovalAction.FinancialApproved,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timesheet {TimesheetId} financially approved by {FinancialAdminId}", timesheetId, financialAdminId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(financialAdminId, ct),
            actorId: financialAdminId,
            actionType: "Timesheet.FinancialApproved",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    public async Task<Result> FinancialRejectAsync(
        int financialAdminId,
        int timesheetId,
        string comment,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        if (timesheet.Status != TimesheetStatus.ApprovedByLead &&
            timesheet.Status != TimesheetStatus.FinancialApproved)
            return Result.Failure($"Timesheet cannot be rejected from status: {timesheet.Status}.");

        timesheet.Status = TimesheetStatus.Draft;
        timesheet.LastRejectionComment = comment;
        timesheet.LastRejectedByUserId = financialAdminId;
        timesheet.LastRejectedAt = DateTimeOffset.UtcNow;

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            TimesheetId = timesheetId,
            ApproverId = financialAdminId,
            Action = ApprovalAction.FinancialRejected,
            Comment = comment,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timesheet {TimesheetId} financially rejected by {FinancialAdminId}", timesheetId, financialAdminId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(financialAdminId, ct),
            actorId: financialAdminId,
            actionType: "Timesheet.FinancialRejected",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    // ── Lock queue ───────────────────────────────────────────────────────────────

    public async Task<List<ApprovalQueueItemDto>> GetLockQueueAsync(CancellationToken ct)
    {
        return await _db.WeeklyTimesheets
            .Where(wt => wt.Status == TimesheetStatus.FinancialApproved)
            .OrderBy(wt => wt.SubmittedAt)
            .Select(wt => new ApprovalQueueItemDto(
                wt.Id,
                wt.UserId,
                wt.User.DisplayName,
                wt.WeekStart,
                wt.WeekEnd,
                wt.TimeEntries.Sum(te => te.Hours),
                wt.TimeEntries.Count(te => te.FlaggedForReview),
                wt.SubmittedAt,
                wt.Status
            ))
            .ToListAsync(ct);
    }

    public async Task<Result> LockTimesheetAsync(
        int financialAdminId,
        int timesheetId,
        byte[] rowVersion,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        if (timesheet.Status != TimesheetStatus.FinancialApproved)
            return Result.Failure($"Only FinancialApproved timesheets can be locked. Current status: {timesheet.Status}.");

        timesheet.Status = TimesheetStatus.Locked;
        timesheet.LockedAt = DateTimeOffset.UtcNow;
        timesheet.LockedById = financialAdminId;

        // Set RowVersion for optimistic concurrency check — only when rowVersion is non-empty
        // (empty array is passed in SQLite tests where RowVersion is not enforced)
        if (rowVersion.Length > 0)
        {
            _db.Entry(timesheet).Property(wt => wt.RowVersion).OriginalValue = rowVersion;
        }

        _db.ApprovalRecords.Add(new ApprovalRecord
        {
            TimesheetId = timesheetId,
            ApproverId = financialAdminId,
            Action = ApprovalAction.Locked,
            Timestamp = DateTimeOffset.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new TimesheetAlreadyLockedException(timesheetId);
        }

        _logger.LogInformation("Timesheet {TimesheetId} locked by {FinancialAdminId}", timesheetId, financialAdminId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(financialAdminId, ct),
            actorId: financialAdminId,
            actionType: "Timesheet.Locked",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }
}
