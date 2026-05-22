namespace TimeQuest.Domain;

public enum UserTeamRole
{
    Member = 0,
    Lead = 1
}

public enum BillingType
{
    TM = 0,
    Fixed = 1
}

public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    ApprovedByLead = 2,
    FinancialApproved = 3,
    Locked = 4
}

public enum ApprovalAction
{
    LeadApproved = 0,
    LeadRejected = 1,
    FinancialApproved = 2,
    FinancialRejected = 3,
    Locked = 4
}

public enum TicketValidationStatus
{
    None = 0,
    Pending = 1,
    Found = 2,
    NotFound = 3,
    Failed = 4,
    ManuallyApproved = 5
}

public enum DayFlagType
{
    Leave = 0,
    PublicHoliday = 1
}
