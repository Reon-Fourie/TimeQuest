using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class TimeEntryService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly ILogger<TimeEntryService> _logger;

    public TimeEntryService(
        ApplicationDbContext db,
        IAuditService audit,
        IConfiguration config,
        ILogger<TimeEntryService> logger)
    {
        _db = db;
        _audit = audit;
        _config = config;
        _logger = logger;
    }

    private async Task<string> GetDisplayNameAsync(int userId, CancellationToken ct)
        => await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct) ?? userId.ToString();

    public async Task<Result<TimeEntryDto>> CreateEntryAsync(
        int userId,
        int weeklyTimesheetId,
        CreateTimeEntryRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
            return Result<TimeEntryDto>.Failure("Notes are required.");

        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == weeklyTimesheetId && wt.UserId == userId, ct);

        if (timesheet == null)
            return Result<TimeEntryDto>.Failure("Timesheet not found.");

        if (timesheet.Status != TimesheetStatus.Draft)
            return Result<TimeEntryDto>.Failure("Time entries can only be added to a Draft timesheet.");

        // Check for duplicate (UserId, Date, ProjectId)
        var duplicate = await _db.TimeEntries
            .AnyAsync(te => te.UserId == userId
                         && te.Date == request.Date
                         && te.ProjectId == request.ProjectId, ct);

        if (duplicate)
            return Result<TimeEntryDto>.Failure("A time entry already exists for this date and project.");

        var flagThreshold = _config.GetValue<decimal>("FlagThresholdHours", 10m);
        var flagged = request.Hours > flagThreshold;

        var entry = new TimeEntry
        {
            WeeklyTimesheetId = weeklyTimesheetId,
            UserId = userId, // always set from WeeklyTimesheet.UserId — not from DTO
            Date = request.Date,
            ProjectId = request.ProjectId,
            Hours = request.Hours,
            Notes = request.Notes,
            TaskType = request.TaskType,
            TicketRef = request.TicketRef,
            FlaggedForReview = flagged,
            TicketValidationStatus = request.TicketRef != null
                ? TicketValidationStatus.Pending
                : TicketValidationStatus.None
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("TimeEntry created: Id={EntryId}, TimesheetId={TimesheetId}, FlaggedForReview={Flagged}",
            entry.Id, weeklyTimesheetId, flagged);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(userId, ct),
            actorId: userId,
            actionType: "TimeEntry.Created",
            resourceType: "TimeEntry",
            resourceId: entry.Id.ToString(),
            beforeJson: null,
            afterJson: null, // not logging entry content — may contain PII/confidential data
            sourceIp: null,
            correlationId: null,
            ct: ct);

        var project = await _db.Projects.FindAsync(new object[] { entry.ProjectId }, ct);

        return Result<TimeEntryDto>.Success(MapToDto(entry, project?.Name ?? string.Empty, timesheet.Status == TimesheetStatus.Locked));
    }

    public async Task<Result<TimeEntryDto>> UpdateEntryAsync(
        int userId,
        int entryId,
        UpdateTimeEntryRequest request,
        CancellationToken ct)
    {
        var entry = await _db.TimeEntries
            .Include(te => te.WeeklyTimesheet)
            .Include(te => te.Project)
            .FirstOrDefaultAsync(te => te.Id == entryId && te.UserId == userId, ct);

        if (entry == null)
            return Result<TimeEntryDto>.Failure("Time entry not found.");

        if (entry.WeeklyTimesheet.Status == TimesheetStatus.Locked)
            return Result<TimeEntryDto>.Failure("This timesheet is locked and cannot be modified.");

        if (entry.WeeklyTimesheet.Status != TimesheetStatus.Draft)
            return Result<TimeEntryDto>.Failure("Time entries can only be edited on a Draft timesheet.");

        var flagThreshold = _config.GetValue<decimal>("FlagThresholdHours", 10m);

        entry.Hours = request.Hours;
        entry.Notes = request.Notes;
        entry.TaskType = request.TaskType;
        entry.TicketRef = request.TicketRef;
        entry.FlaggedForReview = request.Hours > flagThreshold;

        if (request.TicketRef != null && entry.TicketValidationStatus == TicketValidationStatus.None)
            entry.TicketValidationStatus = TicketValidationStatus.Pending;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(userId, ct),
            actorId: userId,
            actionType: "TimeEntry.Updated",
            resourceType: "TimeEntry",
            resourceId: entry.Id.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result<TimeEntryDto>.Success(MapToDto(entry, entry.Project?.Name ?? string.Empty,
            entry.WeeklyTimesheet.Status == TimesheetStatus.Locked));
    }

    public async Task<Result> DeleteEntryAsync(
        int userId,
        int entryId,
        CancellationToken ct)
    {
        var entry = await _db.TimeEntries
            .Include(te => te.WeeklyTimesheet)
            .FirstOrDefaultAsync(te => te.Id == entryId && te.UserId == userId, ct);

        if (entry == null)
            return Result.Failure("Time entry not found.");

        if (entry.WeeklyTimesheet.Status == TimesheetStatus.Locked)
            return Result.Failure("This timesheet is locked and cannot be modified.");

        if (entry.WeeklyTimesheet.Status != TimesheetStatus.Draft)
            return Result.Failure("Time entries can only be deleted from a Draft timesheet.");

        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(userId, ct),
            actorId: userId,
            actionType: "TimeEntry.Deleted",
            resourceType: "TimeEntry",
            resourceId: entryId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    public async Task<Result> ValidateOvertimeAsync(
        int leadUserId,
        int entryId,
        string note,
        CancellationToken ct)
    {
        var entry = await _db.TimeEntries.FindAsync(new object[] { entryId }, ct);
        if (entry == null)
            return Result.Failure("Time entry not found.");

        entry.OvertimeValidatedById = leadUserId;
        entry.OvertimeValidationNote = note;
        entry.OvertimeValidatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(leadUserId, ct),
            actorId: leadUserId,
            actionType: "TimeEntry.OvertimeValidated",
            resourceType: "TimeEntry",
            resourceId: entryId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    private static TimeEntryDto MapToDto(TimeEntry entry, string projectName, bool isLocked)
        => new(
            entry.Id,
            entry.WeeklyTimesheetId,
            entry.Date,
            entry.ProjectId,
            projectName,
            entry.Hours,
            entry.Notes,
            entry.TaskType,
            entry.TicketRef,
            entry.FlaggedForReview,
            entry.TicketValidationStatus,
            isLocked
        );
}
