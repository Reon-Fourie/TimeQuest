namespace TimeQuest.Domain.Entities;

public class ProjectUser
{
    public int UserId { get; set; }    // FK → ApplicationUser (NoAction)
    public int ProjectId { get; set; } // FK → Project         (Cascade)

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
