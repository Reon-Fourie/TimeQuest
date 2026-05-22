namespace TimeQuest.Domain.Entities;

public class AdminProjectScope
{
    public int AdminUserId { get; set; } // FK → ApplicationUser (NoAction)
    public int ProjectId { get; set; }   // FK → Project         (Cascade)

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser AdminUser { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
