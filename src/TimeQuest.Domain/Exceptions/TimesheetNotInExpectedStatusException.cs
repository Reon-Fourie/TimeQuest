namespace TimeQuest.Domain.Exceptions;

public class TimesheetNotInExpectedStatusException : Exception
{
    public int TimesheetId { get; }
    public string ExpectedStatus { get; }
    public string ActualStatus { get; }

    public TimesheetNotInExpectedStatusException(int timesheetId, string expectedStatus, string actualStatus)
        : base($"Timesheet {timesheetId} is in status '{actualStatus}' but expected '{expectedStatus}'.")
    {
        TimesheetId = timesheetId;
        ExpectedStatus = expectedStatus;
        ActualStatus = actualStatus;
    }
}
