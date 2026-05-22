using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class WeeklyTimesheetService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<WeeklyTimesheetService> _logger;

    public WeeklyTimesheetService(
        ApplicationDbContext db,
        IAuditService audit,
        ILogger<WeeklyTimesheetService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    private async Task<string> GetDisplayNameAsync(int userId, CancellationToken ct)
        => await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct) ?? userId.ToString();

    /// <summary>
    /// Returns the WeeklyTimesheet for the given user and week, creating one if none exists.
    /// Resolves architect OQ-3: auto-creates timesheet record on first access.
    /// </summary>
    public async Task<WeeklyTimesheetDto> GetOrCreateForWeekAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken ct)
    {
        var weekEnd = weekStart.AddDays(6);

        var timesheet = await _db.WeeklyTimesheets
            .Include(wt => wt.TimeEntries)
                .ThenInclude(te => te.Project)
            .Include(wt => wt.DayFlags)
            .Include(wt => wt.User)
            .Include(wt => wt.LastRejectedBy)
            .FirstOrDefaultAsync(wt => wt.UserId == userId && wt.WeekStart == weekStart, ct);

        if (timesheet == null)
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, ct)
                       ?? throw new InvalidOperationException($"User {userId} not found.");

            timesheet = new WeeklyTimesheet
            {
                UserId = userId,
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                Status = TimesheetStatus.Draft
            };

            _db.WeeklyTimesheets.Add(timesheet);
            await _db.SaveChangesAsync(ct);

            // Reload with navigation properties
            timesheet = await _db.WeeklyTimesheets
                .Include(wt => wt.TimeEntries)
                    .ThenInclude(te => te.Project)
                .Include(wt => wt.DayFlags)
                .Include(wt => wt.User)
                .Include(wt => wt.LastRejectedBy)
                .FirstAsync(wt => wt.Id == timesheet.Id, ct);
        }

        return MapToDto(timesheet);
    }

    /// <summary>
    /// Validates a timesheet for submission.
    /// Returns missing weekdays (Mon–Fri) that have neither a TimeEntry nor a TimesheetDayFlag.
    /// F1.2 AC2: days covered by TimesheetDayFlag are NOT counted as missing.
    /// </summary>
    public async Task<SubmissionValidationResult> ValidateForSubmissionAsync(
        int userId,
        int timesheetId,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .Include(wt => wt.TimeEntries)
            .Include(wt => wt.DayFlags)
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId && wt.UserId == userId, ct);

        if (timesheet == null)
            return new SubmissionValidationResult(false, new List<DateOnly>());

        var entryDates = timesheet.TimeEntries
            .Select(te => te.Date)
            .ToHashSet();

        var flaggedDates = timesheet.DayFlags
            .Select(df => df.Date)
            .ToHashSet();

        var missingDays = new List<DateOnly>();

        // Iterate Mon–Fri of the week
        var current = timesheet.WeekStart;
        while (current <= timesheet.WeekEnd)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                if (!entryDates.Contains(current) && !flaggedDates.Contains(current))
                    missingDays.Add(current);
            }
            current = current.AddDays(1);
        }

        var canSubmit = !missingDays.Any();
        return new SubmissionValidationResult(canSubmit, missingDays);
    }

    /// <summary>
    /// Submits a timesheet: Draft → Submitted.
    /// </summary>
    public async Task<Result> SubmitAsync(
        int userId,
        int timesheetId,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId && wt.UserId == userId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        if (timesheet.Status != TimesheetStatus.Draft)
            return Result.Failure($"Only Draft timesheets can be submitted. Current status: {timesheet.Status}.");

        timesheet.Status = TimesheetStatus.Submitted;
        timesheet.SubmittedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Timesheet {TimesheetId} submitted by user {UserId}", timesheetId, userId);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(userId, ct),
            actorId: userId,
            actionType: "Timesheet.Submitted",
            resourceType: "WeeklyTimesheet",
            resourceId: timesheetId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    /// <summary>
    /// Adds or updates a day flag for the specified date on the timesheet.
    /// </summary>
    public async Task<Result> SetDayFlagAsync(
        int userId,
        int timesheetId,
        DateOnly date,
        DayFlagType flagType,
        CancellationToken ct)
    {
        var timesheet = await _db.WeeklyTimesheets
            .FirstOrDefaultAsync(wt => wt.Id == timesheetId && wt.UserId == userId, ct);

        if (timesheet == null)
            return Result.Failure("Timesheet not found.");

        var existing = await _db.TimesheetDayFlags
            .FirstOrDefaultAsync(f => f.WeeklyTimesheetId == timesheetId && f.Date == date, ct);

        if (existing != null)
        {
            existing.FlagType = flagType;
        }
        else
        {
            _db.TimesheetDayFlags.Add(new TimesheetDayFlag
            {
                WeeklyTimesheetId = timesheetId,
                Date = date,
                FlagType = flagType,
                UserId = userId
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Removes a day flag for the specified date on the timesheet.
    /// </summary>
    public async Task<Result> RemoveDayFlagAsync(
        int userId,
        int timesheetId,
        DateOnly date,
        CancellationToken ct)
    {
        var flag = await _db.TimesheetDayFlags
            .FirstOrDefaultAsync(f => f.WeeklyTimesheetId == timesheetId
                                   && f.Date == date
                                   && f.UserId == userId, ct);

        if (flag == null)
            return Result.Failure("Day flag not found.");

        _db.TimesheetDayFlags.Remove(flag);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static WeeklyTimesheetDto MapToDto(WeeklyTimesheet wt)
    {
        var entries = wt.TimeEntries
            .Select(te => new TimeEntryDto(
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
                wt.Status == TimesheetStatus.Locked
            ))
            .ToList();

        var flags = wt.DayFlags
            .Select(df => new TimesheetDayFlagDto(df.Date, df.FlagType))
            .ToList();

        return new WeeklyTimesheetDto(
            wt.Id,
            wt.UserId,
            wt.User?.DisplayName ?? string.Empty,
            wt.WeekStart,
            wt.WeekEnd,
            wt.Status,
            wt.SubmittedAt,
            wt.LockedAt,
            wt.LastRejectionComment,
            wt.LastRejectedBy?.DisplayName,
            wt.LastRejectedAt,
            wt.RowVersion ?? Array.Empty<byte>(),
            entries,
            flags
        );
    }
}
