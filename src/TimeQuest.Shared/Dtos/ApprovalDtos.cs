using System.ComponentModel.DataAnnotations;
using TimeQuest.Domain;

namespace TimeQuest.Shared.Dtos;

public record ApprovalQueueItemDto(
    int TimesheetId,
    int UserId,
    string UserDisplayName,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    decimal TotalHours,
    int FlaggedCount,
    DateTimeOffset? SubmittedAt,
    TimesheetStatus Status
);

public record TimesheetDetailDto(
    int Id,
    string UserDisplayName,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    TimesheetStatus Status,
    List<TimeEntryDto> Entries,
    List<ApprovalRecordDto> ApprovalHistory
);

public record ApprovalRecordDto(
    int Id,
    string ApproverDisplayName,
    ApprovalAction Action,
    string? Comment,
    DateTimeOffset Timestamp
);

public class LeadRejectRequest
{
    [Required, MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public class FinancialRejectRequest
{
    [Required, MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public class ValidateOvertimeRequest
{
    [Required, MaxLength(500)]
    public string Note { get; set; } = string.Empty;
}

public class LockTimesheetRequest
{
    [Required]
    public byte[] RowVersion { get; set; } = [];
}
