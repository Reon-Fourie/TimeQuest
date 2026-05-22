using TimeQuest.Domain;

namespace TimeQuest.Shared.Dtos;

public record WeeklyTimesheetDto(
    int Id,
    int UserId,
    string UserDisplayName,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    TimesheetStatus Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? LockedAt,
    string? LastRejectionComment,
    string? LastRejectedByName,
    DateTimeOffset? LastRejectedAt,
    byte[] RowVersion,
    List<TimeEntryDto> Entries,
    List<TimesheetDayFlagDto> DayFlags
);

public record TimesheetDayFlagDto(
    DateOnly Date,
    DayFlagType FlagType
);

public record SubmissionValidationResult(
    bool CanSubmit,
    List<DateOnly> MissingDays
);
