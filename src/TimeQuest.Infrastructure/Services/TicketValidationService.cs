using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;

namespace TimeQuest.Infrastructure.Services;

public class TicketValidationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<TicketValidationService> _logger;

    public TicketValidationService(
        ApplicationDbContext db,
        IAuditService audit,
        ILogger<TicketValidationService> logger)
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
    /// Called by the background service every 15 minutes.
    /// For each entry with TicketValidationStatus = Pending or Failed, calls integration adapters.
    /// If integration is offline, sets status = Failed and logs; leaves for retry.
    /// </summary>
    public async Task ValidatePendingTicketsAsync(CancellationToken ct)
    {
        var pendingEntries = await _db.TimeEntries
            .Where(te => te.TicketValidationStatus == TicketValidationStatus.Pending
                      || te.TicketValidationStatus == TicketValidationStatus.Failed)
            .ToListAsync(ct);

        _logger.LogInformation("TicketValidation: processing {Count} pending/failed entries", pendingEntries.Count);

        foreach (var entry in pendingEntries)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                // Integration adapters would be injected here (Azure DevOps, JIRA, Linear)
                // For now, this is a stub that can be extended with real adapters
                // The adapter selection logic would use TicketRef prefix or project-level config

                // Example stub:
                var isValid = await ValidateTicketRefAsync(entry.TicketRef, ct);

                entry.TicketValidationStatus = isValid
                    ? TicketValidationStatus.Found
                    : TicketValidationStatus.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TicketValidation: integration offline or error for entry {EntryId}", entry.Id);
                entry.TicketValidationStatus = TicketValidationStatus.Failed;
            }
        }

        if (pendingEntries.Any())
            await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Manually marks a ticket reference as valid. (F6.1 — System Admin fallback)
    /// </summary>
    public async Task<Result> ApproveManuallyAsync(
        int entryId,
        int approvedByUserId,
        CancellationToken ct)
    {
        var entry = await _db.TimeEntries.FindAsync(new object[] { entryId }, ct);
        if (entry == null)
            return Result.Failure("Time entry not found.");

        entry.TicketValidationStatus = TicketValidationStatus.ManuallyApproved;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actorDisplayName: await GetDisplayNameAsync(approvedByUserId, ct),
            actorId: approvedByUserId,
            actionType: "TicketRef.ManuallyApproved",
            resourceType: "TimeEntry",
            resourceId: entryId.ToString(),
            beforeJson: null,
            afterJson: null,
            sourceIp: null,
            correlationId: null,
            ct: ct);

        return Result.Success();
    }

    /// <summary>
    /// Stub for ticket reference validation.
    /// In production, this would route to the appropriate adapter (DevOps/JIRA/Linear)
    /// based on the ticket reference prefix or project-level configuration.
    /// </summary>
    private Task<bool> ValidateTicketRefAsync(string? ticketRef, CancellationToken ct)
    {
        // This is a placeholder — real implementation would call ITicketValidator adapters
        // Always return false in stub (integration not configured)
        return Task.FromResult(false);
    }
}
