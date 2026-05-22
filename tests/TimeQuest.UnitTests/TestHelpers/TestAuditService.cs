using TimeQuest.Domain.Interfaces;

namespace TimeQuest.UnitTests.TestHelpers;

/// <summary>
/// No-op audit service for tests — avoids a secondary SaveChanges call.
/// </summary>
public class TestAuditService : IAuditService
{
    public Task LogAsync(
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
        return Task.CompletedTask;
    }
}
