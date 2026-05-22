namespace TimeQuest.Domain.Exceptions;

public class TimesheetNotFoundException : Exception
{
    public int TimesheetId { get; }

    public TimesheetNotFoundException(int timesheetId)
        : base($"Timesheet {timesheetId} was not found.")
    {
        TimesheetId = timesheetId;
    }
}
