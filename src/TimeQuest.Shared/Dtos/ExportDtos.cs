using TimeQuest.Domain;

namespace TimeQuest.Shared.Dtos;

public record ExportRowDto(
    DateOnly Date,
    string UserName,
    string ProjectName,
    BillingType BillingType,
    decimal Hours,
    string Notes,
    string? TicketRef,
    DateOnly WeekStart
);
