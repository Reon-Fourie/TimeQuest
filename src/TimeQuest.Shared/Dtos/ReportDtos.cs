namespace TimeQuest.Shared.Dtos;

public enum ReportGroupBy { Daily, Weekly, Monthly }

public record ReconciliationReportDto(
    DateOnly From,
    DateOnly To,
    ReportGroupBy GroupBy,
    List<ReconciliationGroupDto> Groups,
    decimal TotalHours
);

public record ReconciliationGroupDto(
    string PeriodLabel,
    decimal TotalHours,
    int EntryCount,
    List<ProjectHoursSummary> ByProject
);

public record ProjectHoursSummary(
    int ProjectId,
    string ProjectName,
    decimal Hours
);
