using System.ComponentModel.DataAnnotations;
using TimeQuest.Domain;

namespace TimeQuest.Shared.Dtos;

public record TeamDto(
    int Id,
    string Name,
    string? Description,
    int? TeamLeadId,
    string? TeamLeadName
);

public record TeamMemberDto(
    int UserId,
    string DisplayName,
    UserTeamRole Role,
    DateTimeOffset JoinedAt
);

public class CreateTeamRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public record ProjectDto(
    int Id,
    string Name,
    BillingType BillingType,
    string? Description,
    bool IsActive
);

public class CreateProjectRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public BillingType BillingType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public record UserSummaryDto(
    int Id,
    string DisplayName,
    string Email
);

public record TeamDetailDto(
    int Id,
    string Name,
    string? Description,
    int? TeamLeadId,
    string? TeamLeadName,
    List<TeamMemberDto> Members
);

public record ProjectDetailDto(
    int Id,
    string Name,
    BillingType BillingType,
    string? Description,
    bool IsActive,
    List<ProjectUserDto> Users
);

public record ProjectUserDto(
    int UserId,
    string DisplayName,
    string? TeamName
);

public record ProjectSummaryDto(
    int Id,
    string Name,
    BillingType BillingType,
    int UserCount,
    bool IsActive
);

public class UpdateTeamRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? TeamLeadId { get; set; }
}

public class UpdateProjectRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public BillingType BillingType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
