namespace TimeQuest.Shared.Dtos;

public record AuditLogEntryDto(
    long Id,
    string? ActorDisplayName,
    string ActionType,
    string ResourceType,
    string ResourceId,
    string? BeforeJson,
    string? AfterJson,
    DateTimeOffset TimestampUtc,
    string? SourceIp
);

public class AuditSearchRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? ActionType { get; set; }
    public int? ActorId { get; set; }
    public int PageSize { get; set; } = 50;
    public string? CursorToken { get; set; }
}

public record AuditLogSearchResultDto(
    int TotalCount,
    List<AuditLogEntryDto> Entries,
    string? NextCursorToken
);
