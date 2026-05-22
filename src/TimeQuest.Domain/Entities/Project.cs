using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Confidential: commercial data

    [Required]
    public BillingType BillingType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true; // soft-deactivation — not a global filter

    // Navigation
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<AdminProjectScope> AdminProjectScopes { get; set; } = new List<AdminProjectScope>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
