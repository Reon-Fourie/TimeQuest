using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

// Append-only — no Update or Delete permitted on this table
public class ApprovalRecord
{
    public int Id { get; set; }

    [Required]
    public int TimesheetId { get; set; } // FK → WeeklyTimesheet  (Restrict)

    [Required]
    public int ApproverId { get; set; }  // FK → ApplicationUser  (Restrict)

    [Required]
    public ApprovalAction Action { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; } // required for LeadRejected / FinancialRejected — enforced in service layer

    [Required]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public WeeklyTimesheet Timesheet { get; set; } = null!;
    public ApplicationUser Approver { get; set; } = null!;
}
