using Microsoft.EntityFrameworkCore;
using TimeQuest.Domain;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class ExportService
{
    private readonly ApplicationDbContext _db;

    public ExportService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns only entries where the parent WeeklyTimesheet has Status = Locked. (F3.2)
    /// </summary>
    public async Task<List<ExportRowDto>> ExportLockedEntriesAsync(
        DateOnly from,
        DateOnly to,
        int? projectId,
        CancellationToken ct)
    {
        var query = _db.TimeEntries
            .Where(te => te.Date >= from
                      && te.Date <= to
                      && te.WeeklyTimesheet.Status == TimesheetStatus.Locked);

        if (projectId.HasValue)
            query = query.Where(te => te.ProjectId == projectId.Value);

        return await query
            .OrderBy(te => te.Date)
            .ThenBy(te => te.User.DisplayName)
            .Select(te => new ExportRowDto(
                te.Date,
                te.User.DisplayName,
                te.Project.Name,
                te.Project.BillingType,
                te.Hours,
                te.Notes,   // Confidential — included only in export, not in logs
                te.TicketRef,
                te.WeeklyTimesheet.WeekStart
            ))
            .ToListAsync(ct);
    }
}
