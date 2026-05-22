using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

public class WeeklyTimesheet
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }         // FK → ApplicationUser (Restrict)

    [Required]
    public DateOnly WeekStart { get; set; }

    [Required]
    public DateOnly WeekEnd { get; set; }

    [Required]
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? LockedAt { get; set; }

    public int? LockedById { get; set; }           // FK → ApplicationUser (NoAction — breaks cycle)
    public int? LastRejectedByUserId { get; set; } // FK → ApplicationUser (NoAction — breaks cycle)

    [MaxLength(1000)]
    public string? LastRejectionComment { get; set; } // denormalised from most recent rejection ApprovalRecord
    public DateTimeOffset? LastRejectedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!; // detects concurrent lock attempts by two Financial Admins

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? LockedBy { get; set; }
    public ApplicationUser? LastRejectedBy { get; set; }
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    public ICollection<TimesheetDayFlag> DayFlags { get; set; } = new List<TimesheetDayFlag>();
}
