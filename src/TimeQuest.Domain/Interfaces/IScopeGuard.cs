namespace TimeQuest.Domain.Interfaces;

public interface IScopeGuard
{
    Task<bool> CanAccessProjectAsync(int adminUserId, int projectId, CancellationToken ct);
    Task<bool> CanAccessUserAsync(int adminUserId, int targetUserId, CancellationToken ct);
}
