using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

public class Team
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? TeamLeadId { get; set; } // nullable — team may exist before a lead is assigned

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!; // concurrent membership edits

    // Navigation
    public ApplicationUser? TeamLead { get; set; }
    public ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
}
