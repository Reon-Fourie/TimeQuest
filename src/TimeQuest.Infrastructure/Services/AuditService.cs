using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string actorDisplayName,
        int? actorId,
        string actionType,
        string resourceType,
        string resourceId,
        string? beforeJson,
        string? afterJson,
        string? sourceIp,
        string? correlationId,
        CancellationToken ct)
    {
        var auditEvent = new AuditEvent
        {
            ActorId = actorId,
            ActorDisplayName = actorId.HasValue ? actorDisplayName : "System",
            ActionType = actionType,
            ResourceType = resourceType,
            ResourceId = resourceId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            TimestampUtc = DateTimeOffset.UtcNow,
            SourceIp = sourceIp,
            CorrelationId = correlationId
        };

        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);

        // Log at Information level for approval state transitions per §5.6
        // Never log PII content (Notes, TicketRef, DisplayName values)
        _logger.LogInformation("Audit: {ActionType} on {ResourceType}/{ResourceId} by actorId={ActorId}",
            actionType, resourceType, resourceId, actorId);
    }

    /// <summary>
    /// Keyset-paginated audit log search. (F5.2 read path)
    /// Filters by date range, action type, and actor.
    /// </summary>
    public async Task<AuditLogSearchResultDto> SearchAsync(AuditSearchRequest request, CancellationToken ct)
    {
        var query = _db.AuditEvents.AsQueryable();

        if (request.From.HasValue)
            query = query.Where(e => e.TimestampUtc >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.TimestampUtc <= request.To.Value);

        if (request.ActionType is not null)
            query = query.Where(e => e.ActionType == request.ActionType);

        if (request.ActorId.HasValue)
            query = query.Where(e => e.ActorId == request.ActorId.Value);

        // Keyset pagination using (TimestampUtc DESC, Id DESC)
        if (request.CursorToken is not null)
        {
            // CursorToken format: "timestamp|id"
            var parts = request.CursorToken.Split('|');
            if (parts.Length == 2
                && DateTimeOffset.TryParse(parts[0], out var cursorTs)
                && long.TryParse(parts[1], out var cursorId))
            {
                query = query.Where(e =>
                    e.TimestampUtc < cursorTs ||
                    (e.TimestampUtc == cursorTs && e.Id < cursorId));
            }
        }

        var totalCount = await query.CountAsync(ct);

        var entries = await query
            .OrderByDescending(e => e.TimestampUtc)
            .ThenByDescending(e => e.Id)
            .Take(request.PageSize)
            .Select(e => new AuditLogEntryDto(
                e.Id,
                e.ActorDisplayName,
                e.ActionType,
                e.ResourceType,
                e.ResourceId,
                e.BeforeJson,
                e.AfterJson,
                e.TimestampUtc,
                e.SourceIp
            ))
            .ToListAsync(ct);

        string? nextCursor = null;
        if (entries.Count == request.PageSize && entries.Count > 0)
        {
            var last = entries[^1];
            nextCursor = $"{last.TimestampUtc:O}|{last.Id}";
        }

        return new AuditLogSearchResultDto(totalCount, entries, nextCursor);
    }
}
