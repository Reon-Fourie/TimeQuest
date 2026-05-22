namespace TimeQuest.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string actorDisplayName,
        int? actorId,
        string actionType,
        string resourceType,
        string resourceId,
        string? beforeJson,
        string? afterJson,
        string? sourceIp,
        string? correlationId,
        CancellationToken ct);
}
