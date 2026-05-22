using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

public class UserTeam
{
    public int UserId { get; set; }  // FK → ApplicationUser  (NoAction)
    public int TeamId { get; set; }  // FK → Team             (Cascade — remove memberships when team deleted)

    [Required]
    public UserTeamRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
