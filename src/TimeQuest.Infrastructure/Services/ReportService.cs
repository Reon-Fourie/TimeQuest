using Microsoft.EntityFrameworkCore;
using TimeQuest.Domain;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class ReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generates a reconciliation report grouped by Daily, Weekly, or Monthly.
    /// Only covers Locked entries. (F5.1)
    /// </summary>
    public async Task<ReconciliationReportDto> GetReconciliationReportAsync(
        DateOnly from,
        DateOnly to,
        ReportGroupBy groupBy,
        CancellationToken ct)
    {
        var entries = await _db.TimeEntries
            .Where(te => te.Date >= from
                      && te.Date <= to
                      && te.WeeklyTimesheet.Status == TimesheetStatus.Locked)
            .Select(te => new
            {
                te.Date,
                te.ProjectId,
                ProjectName = te.Project.Name,
                te.Hours
            })
            .ToListAsync(ct);

        var groups = groupBy switch
        {
            ReportGroupBy.Daily => entries
                .GroupBy(e => e.Date.ToString("yyyy-MM-dd"))
                .Select(g => new ReconciliationGroupDto(
                    g.Key,
                    g.Sum(e => e.Hours),
                    g.Count(),
                    g.GroupBy(e => new { e.ProjectId, e.ProjectName })
                        .Select(pg => new ProjectHoursSummary(pg.Key.ProjectId, pg.Key.ProjectName, pg.Sum(e => e.Hours)))
                        .ToList()
                ))
                .ToList(),

            ReportGroupBy.Weekly => entries
                .GroupBy(e =>
                {
                    // ISO week — find Monday
                    var monday = e.Date.AddDays(-(((int)e.Date.DayOfWeek + 6) % 7));
                    return monday.ToString("yyyy-MM-dd");
                })
                .Select(g => new ReconciliationGroupDto(
                    $"Week of {g.Key}",
                    g.Sum(e => e.Hours),
                    g.Count(),
                    g.GroupBy(e => new { e.ProjectId, e.ProjectName })
                        .Select(pg => new ProjectHoursSummary(pg.Key.ProjectId, pg.Key.ProjectName, pg.Sum(e => e.Hours)))
                        .ToList()
                ))
                .ToList(),

            ReportGroupBy.Monthly => entries
                .GroupBy(e => e.Date.ToString("yyyy-MM"))
                .Select(g => new ReconciliationGroupDto(
                    g.Key,
                    g.Sum(e => e.Hours),
                    g.Count(),
                    g.GroupBy(e => new { e.ProjectId, e.ProjectName })
                        .Select(pg => new ProjectHoursSummary(pg.Key.ProjectId, pg.Key.ProjectName, pg.Sum(e => e.Hours)))
                        .ToList()
                ))
                .ToList(),

            _ => new List<ReconciliationGroupDto>()
        };

        return new ReconciliationReportDto(
            from,
            to,
            groupBy,
            groups,
            entries.Sum(e => e.Hours)
        );
    }
}
