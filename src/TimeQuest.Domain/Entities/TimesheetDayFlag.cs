using System.ComponentModel.DataAnnotations;

namespace TimeQuest.Domain.Entities;

// Resolves F1.2 AC2: per-day leave/holiday flag; ValidateForSubmission checks all non-entry days against this table.
// Composite PK chosen over surrogate int to enforce one flag per day per timesheet at the DB level.
public class TimesheetDayFlag
{
    [Required]
    public int WeeklyTimesheetId { get; set; } // FK → WeeklyTimesheet (Restrict) — part of composite PK

    [Required]
    public DateOnly Date { get; set; }          // part of composite PK; must fall within WeekStart..WeekEnd

    [Required]
    public DayFlagType FlagType { get; set; }

    [Required]
    public int UserId { get; set; }             // FK → ApplicationUser (NoAction) — denorm for direct (UserId, Date) query

    // Navigation
    public WeeklyTimesheet WeeklyTimesheet { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
