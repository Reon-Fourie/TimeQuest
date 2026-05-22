namespace TimeQuest.Domain.Exceptions;

public class TimesheetAlreadyLockedException : Exception
{
    public int TimesheetId { get; }

    public TimesheetAlreadyLockedException(int timesheetId)
        : base($"Timesheet {timesheetId} is already locked or was locked concurrently.")
    {
        TimesheetId = timesheetId;
    }
}
