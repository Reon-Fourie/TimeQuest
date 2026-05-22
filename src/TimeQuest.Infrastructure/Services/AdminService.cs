using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeQuest.Domain;
using TimeQuest.Domain.Entities;
using TimeQuest.Domain.Interfaces;
using TimeQuest.Infrastructure.Data;
using TimeQuest.Shared.Dtos;

namespace TimeQuest.Infrastructure.Services;

public class AdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IScopeGuard _scopeGuard;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ApplicationDbContext db,
        IScopeGuard scopeGuard,
        ILogger<AdminService> logger)
    {
        _db = db;
        _scopeGuard = scopeGuard;
        _logger = logger;
    }

    // ── Team queries ─────────────────────────────────────────────────────────────

    public async Task<List<TeamDto>> GetTeamsAsync(CancellationToken ct)
    {
        return await _db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(
                t.Id,
                t.Name,
                t.Description,
                t.TeamLeadId,
                t.TeamLead != null ? t.TeamLead.DisplayName : null
            ))
            .ToListAsync(ct);
    }

    public async Task<Result<TeamDetailDto>> GetTeamDetailAsync(int teamId, CancellationToken ct)
    {
        var team = await _db.Teams
            .Include(t => t.TeamLead)
            .Include(t => t.UserTeams)
                .ThenInclude(ut => ut.User)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team == null)
            return Result<TeamDetailDto>.Failure("Team not found.");

        return Result<TeamDetailDto>.Success(new TeamDetailDto(
            team.Id,
            team.Name,
            team.Description,
            team.TeamLeadId,
            team.TeamLead?.DisplayName,
            team.UserTeams.Select(ut => new TeamMemberDto(
                ut.UserId,
                ut.User.DisplayName,
                ut.Role,
                ut.JoinedAt
            )).ToList()
        ));
    }

    public async Task<Result> UpdateTeamAsync(int teamId, UpdateTeamRequest request, CancellationToken ct)
    {
        var team = await _db.Teams.FindAsync(new object[] { teamId }, ct);
        if (team == null)
            return Result.Failure("Team not found.");

        team.Name = request.Name;
        team.Description = request.Description;

        if (request.TeamLeadId.HasValue)
            team.TeamLeadId = request.TeamLeadId.Value;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteTeamAsync(int teamId, CancellationToken ct)
    {
        var team = await _db.Teams.FindAsync(new object[] { teamId }, ct);
        if (team == null)
            return Result.Failure("Team not found.");

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Project queries ───────────────────────────────────────────────────────────

    public async Task<List<ProjectSummaryDto>> GetProjectsAsync(CancellationToken ct)
    {
        return await _db.Projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.BillingType,
                p.ProjectUsers.Count,
                p.IsActive
            ))
            .ToListAsync(ct);
    }

    public async Task<Result<ProjectDetailDto>> GetProjectDetailAsync(int projectId, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.ProjectUsers)
                .ThenInclude(pu => pu.User)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        if (project == null)
            return Result<ProjectDetailDto>.Failure("Project not found.");

        return Result<ProjectDetailDto>.Success(new ProjectDetailDto(
            project.Id,
            project.Name,
            project.BillingType,
            project.Description,
            project.IsActive,
            project.ProjectUsers.Select(pu => new ProjectUserDto(
                pu.UserId,
                pu.User.DisplayName,
                null  // TeamName — multi-team; deferred per OQ-8
            )).ToList()
        ));
    }

    public async Task<Result> UpdateProjectAsync(int projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync(new object[] { projectId }, ct);
        if (project == null)
            return Result.Failure("Project not found.");

        project.Name = request.Name;
        project.BillingType = request.BillingType;
        project.Description = request.Description;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveUserFromProjectAsync(int projectId, int userId, CancellationToken ct)
    {
        var pu = await _db.ProjectUsers
            .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId, ct);

        if (pu == null)
            return Result.Failure("User is not assigned to this project.");

        _db.ProjectUsers.Remove(pu);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── User queries (for pickers) ────────────────────────────────────────────────

    public async Task<List<UserSummaryDto>> GetAllUsersAsync(CancellationToken ct)
    {
        return await _db.Users
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummaryDto(u.Id, u.DisplayName, u.Email ?? string.Empty))
            .ToListAsync(ct);
    }

    // ── Team management ──────────────────────────────────────────────────────────

    public async Task<Result<TeamDto>> CreateTeamAsync(
        CreateTeamRequest request,
        CancellationToken ct)
    {
        var team = new Team
        {
            Name = request.Name,
            Description = request.Description
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);

        return Result<TeamDto>.Success(new TeamDto(
            team.Id, team.Name, team.Description, team.TeamLeadId, null));
    }

    public async Task<Result> AssignTeamLeadAsync(
        int teamId,
        int userId,
        CancellationToken ct)
    {
        var team = await _db.Teams.FindAsync(new object[] { teamId }, ct);
        if (team == null)
            return Result.Failure("Team not found.");

        team.TeamLeadId = userId;

        // Ensure user is also a member with Lead role
        var existing = await _db.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId, ct);

        if (existing != null)
        {
            existing.Role = UserTeamRole.Lead;
        }
        else
        {
            _db.UserTeams.Add(new UserTeam
            {
                UserId = userId,
                TeamId = teamId,
                Role = UserTeamRole.Lead
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddTeamMemberAsync(
        int teamId,
        int userId,
        CancellationToken ct)
    {
        var existing = await _db.UserTeams
            .AnyAsync(ut => ut.UserId == userId && ut.TeamId == teamId, ct);

        if (existing)
            return Result.Failure("User is already a member of this team.");

        _db.UserTeams.Add(new UserTeam
        {
            UserId = userId,
            TeamId = teamId,
            Role = UserTeamRole.Member
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveTeamMemberAsync(
        int teamId,
        int userId,
        CancellationToken ct)
    {
        var member = await _db.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TeamId == teamId, ct);

        if (member == null)
            return Result.Failure("User is not a member of this team.");

        _db.UserTeams.Remove(member);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Project management ───────────────────────────────────────────────────────

    public async Task<Result<ProjectDto>> CreateProjectAsync(
        CreateProjectRequest request,
        CancellationToken ct)
    {
        var project = new Project
        {
            Name = request.Name,
            BillingType = request.BillingType,
            Description = request.Description,
            IsActive = true
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return Result<ProjectDto>.Success(new ProjectDto(
            project.Id, project.Name, project.BillingType, project.Description, project.IsActive));
    }

    public async Task<Result> AssignUserToProjectAsync(
        int projectId,
        int userId,
        CancellationToken ct)
    {
        var existing = await _db.ProjectUsers
            .AnyAsync(pu => pu.UserId == userId && pu.ProjectId == projectId, ct);

        if (existing)
            return Result.Failure("User is already assigned to this project.");

        _db.ProjectUsers.Add(new ProjectUser
        {
            UserId = userId,
            ProjectId = projectId
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeactivateProjectAsync(
        int adminUserId,
        int projectId,
        CancellationToken ct)
    {
        if (!await _scopeGuard.CanAccessProjectAsync(adminUserId, projectId, ct))
            return Result.Failure("Access denied to this project.");

        var project = await _db.Projects.FindAsync(new object[] { projectId }, ct);
        if (project == null)
            return Result.Failure("Project not found.");

        project.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
