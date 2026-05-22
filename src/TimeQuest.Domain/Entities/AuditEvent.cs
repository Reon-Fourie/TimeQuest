using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

// Append-only — no Update or Delete permitted on this table
// DELETE and UPDATE permissions revoked on AuditEvents at DB level (SQL DENY)
public class AuditEvent
{
    public long Id { get; set; } // bigint — 7-year retention × 2000 users × high event rate

    public int? ActorId { get; set; } // FK → ApplicationUser (SetNull on delete — preserves audit after user erasure)

    [Required, MaxLength(200)]
    public string ActorDisplayName { get; set; } = string.Empty; // denormalised — survives user deletion / right-to-erasure

    [Required, MaxLength(100)]
    public string ActionType { get; set; } = string.Empty; // e.g. "TimeEntry.Created", "Timesheet.Locked"

    [Required, MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty; // e.g. "TimeEntry", "WeeklyTimesheet"

    [Required, MaxLength(100)]
    public string ResourceId { get; set; } = string.Empty; // string PK repr — accommodates int and Guid entities

    public string? BeforeJson { get; set; } // nvarchar(max)
    public string? AfterJson { get; set; }  // nvarchar(max)

    [Required]
    public DateTimeOffset TimestampUtc { get; set; }

    [MaxLength(50)]
    public string? SourceIp { get; set; }

    [MaxLength(50)]
    public string? CorrelationId { get; set; }

    // Navigation
    public ApplicationUser? Actor { get; set; }
}
