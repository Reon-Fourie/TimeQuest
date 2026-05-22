namespace TimeQuest.Domain.Exceptions;

public class OvertimeNotValidatedException : Exception
{
    public int TimesheetId { get; }
    public IReadOnlyList<int> UnvalidatedEntryIds { get; }

    public OvertimeNotValidatedException(int timesheetId, IReadOnlyList<int> unvalidatedEntryIds)
        : base($"Timesheet {timesheetId} has {unvalidatedEntryIds.Count} flagged entry(ies) that have not been validated for overtime.")
    {
        TimesheetId = timesheetId;
        UnvalidatedEntryIds = unvalidatedEntryIds;
    }
}
