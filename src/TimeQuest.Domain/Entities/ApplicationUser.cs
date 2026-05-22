using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TimeQuest.Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty; // PII – Confidential

    // Navigation
    public ICollection<WeeklyTimesheet> WeeklyTimesheets { get; set; } = new List<WeeklyTimesheet>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<UserTeam> UserTeams { get; set; } = new List<UserTeam>();
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<AdminProjectScope> AdminProjectScopes { get; set; } = new List<AdminProjectScope>();
    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
