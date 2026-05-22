using Microsoft.EntityFrameworkCore;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;

namespace TimeQuest.Infrastructure.Services;

public class ScopeGuard : IScopeGuard
{
    private readonly ApplicationDbContext _db;

    public ScopeGuard(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns true if the admin user has an AdminProjectScope entry for the given project.
    /// </summary>
    public async Task<bool> CanAccessProjectAsync(int adminUserId, int projectId, CancellationToken ct)
    {
        return await _db.AdminProjectScopes
            .AnyAsync(aps => aps.AdminUserId == adminUserId && aps.ProjectId == projectId, ct);
    }

    /// <summary>
    /// Returns true if the target user is assigned to at least one project that is within the admin's scope.
    /// </summary>
    public async Task<bool> CanAccessUserAsync(int adminUserId, int targetUserId, CancellationToken ct)
    {
        var adminProjectIds = await _db.AdminProjectScopes
            .Where(aps => aps.AdminUserId == adminUserId)
            .Select(aps => aps.ProjectId)
            .ToListAsync(ct);

        if (!adminProjectIds.Any())
            return false;

        return await _db.ProjectUsers
            .AnyAsync(pu => pu.UserId == targetUserId && adminProjectIds.Contains(pu.ProjectId), ct);
    }
}
