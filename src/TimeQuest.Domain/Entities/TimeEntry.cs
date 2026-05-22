using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

public class TimeEntry
{
    public int Id { get; set; }

    [Required]
    public int WeeklyTimesheetId { get; set; } // FK → WeeklyTimesheet  (Restrict)

    [Required]
    public int UserId { get; set; }            // FK → ApplicationUser  (NoAction — redundant for query coverage, breaks cycle)

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public int ProjectId { get; set; }         // FK → Project          (Restrict)

    [Required]
    public decimal Hours { get; set; }         // decimal(5,2) — max 24.00 per entry; configured in OnModelCreating

    [Required, MaxLength(500)]
    public string Notes { get; set; } = string.Empty; // Confidential — MUST NOT be emitted to Application Insights logs

    [MaxLength(50)]
    public string? TaskType { get; set; }

    [MaxLength(100)]
    public string? TicketRef { get; set; }     // Confidential — MUST NOT be emitted to Application Insights logs

    public bool FlaggedForReview { get; set; } = false; // set by service when Hours > configured threshold

    public int? OvertimeValidatedById { get; set; } // FK → ApplicationUser (NoAction)
    [MaxLength(500)]
    public string? OvertimeValidationNote { get; set; }
    public DateTimeOffset? OvertimeValidatedAt { get; set; }

    [Required]
    public TicketValidationStatus TicketValidationStatus { get; set; } = TicketValidationStatus.None;

    // Navigation
    public WeeklyTimesheet WeeklyTimesheet { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ApplicationUser? OvertimeValidatedBy { get; set; }
}
