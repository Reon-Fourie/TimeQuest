using System.ComponentModel.DataAnnotations;
using TimeQuest.Domain;

namespace TimeQuest.Shared.Dtos;

public record TimeEntryDto(
    int Id,
    int WeeklyTimesheetId,
    DateOnly Date,
    int ProjectId,
    string ProjectName,
    decimal Hours,
    string Notes,
    string? TaskType,
    string? TicketRef,
    bool FlaggedForReview,
    TicketValidationStatus TicketValidationStatus,
    bool IsLocked
);

public class CreateTimeEntryRequest
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required, Range(0.25, 24.0)]
    public decimal Hours { get; set; }

    [Required, MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TaskType { get; set; }

    [MaxLength(100)]
    public string? TicketRef { get; set; }
}

public class UpdateTimeEntryRequest
{
    [Required, Range(0.25, 24.0)]
    public decimal Hours { get; set; }

    [Required, MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TaskType { get; set; }

    [MaxLength(100)]
    public string? TicketRef { get; set; }
}
