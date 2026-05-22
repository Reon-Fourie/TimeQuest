# UI/UX Design — Timesheet & Billing Workflow System

## 1. Information Architecture

### Sitemap
```
/                           (anonymous → redirects to /login)
/login                      (anonymous — Entra ID OIDC redirect; no app-owned UI)
/timesheet                  (all roles — redirects to /timesheet/{current-week})
/timesheet/{weekStart}      (all roles — weekly entry view)

/approvals                  (TeamLead)
/approvals/{timesheetId}    (TeamLead)

/financial/queue            (FinancialAdmin)
/financial/queue/{id}       (FinancialAdmin)
/financial/lock             (FinancialAdmin)
/financial/reports          (FinancialAdmin)
/financial/export           (FinancialAdmin)

/admin/teams                (Administrator)
/admin/teams/{teamId}       (Administrator — inline panel on list, navigable directly)
/admin/projects             (Administrator)
/admin/projects/{projectId} (Administrator — inline panel on list, navigable directly)

/audit                      (FinancialAdmin, SystemAdmin)
/sysadmin/integrations      (SystemAdmin)

/403                        (anonymous — access-denied page)
/404                        (anonymous — not-found page)
```

**No-UI features (background / API only):**
- F4.3 Scoped admin access enforcement — enforced at service layer; no dedicated screen. The restriction manifests as filtered lists and HTTP 403 responses on the relevant admin pages (§9).
- F1.1 duplicate entry guard — server-side validation response surfaced in the entry form error state.
- F2.3 overtime flag — computed server-side; surfaced in the approval detail screen.
- Ticket validation retry job (F6.1) — IHostedService; surfaced in `/sysadmin/integrations`.

### Primary navigation
- **Mechanism:** Left rail (≥ md breakpoint). Hamburger drawer (< md breakpoint). Rail collapses to icon-only at a constrained width; expands on hover or explicit toggle.
- **Visibility rules per role:**

| Nav item | TeamMember | TeamLead | Administrator | FinancialAdmin | SystemAdmin |
|---|---|---|---|---|---|
| My Timesheet | ✓ | ✓ | ✓ | ✓ | ✓ |
| Approvals | | ✓ | | | |
| Review Queue | | | | ✓ | |
| Lock Timesheets | | | | ✓ | |
| Reports | | | | ✓ | |
| Export | | | | ✓ | |
| Teams | | | ✓ | | |
| Projects | | | ✓ | | |
| Audit Log | | | | ✓ | ✓ |
| Integrations | | | | | ✓ |

- **Active-state indicator:** Left border accent (3 px, `--color-primary`) on the active nav item. Icon + label both receive `aria-current="page"`.
- **Role group labels:** Nav items are grouped under collapsible headings ("My Work", "Approvals", "Finance", "Administration", "System") — group headings are visible only when that group has at least one item for the current role.

---

## 2. Personas to journeys

- **Team Member:** Land → `/login` (Entra SSO) → redirect to `/timesheet` → `/timesheet/{currentWeek}` → click "+ Add entry" → entry modal opens → fill Date, Project, Hours, Notes (+ optional Task Type, Ticket Ref) → save → entry appears in grid → repeat daily → end of week: click "Submit for approval" → `SubmissionReviewModal` opens (missing-day warnings + flagged entries) → tick acknowledgement checkboxes → click "Submit for approval" → status chip updates to "Submitted"; entries lock → on-screen confirmation "Timesheet for week of {date} submitted for approval". **If rejected:** amber rejection banner appears on return visit showing the rejection comment, rejector name, and date; entries are editable again; Team Member corrects and resubmits.

- **Team Lead:** Land → `/login` → redirect to `/approvals` (queue sorted oldest first) → see submitted timesheets from their team(s) → click row → `/approvals/{timesheetId}` → review entries → see flagged section (hours > threshold) → enter justification per flagged entry → click "Validate" per entry → all validated → click "Approve" (or "Reject" with mandatory comment) → redirect back to `/approvals`; item removed from queue.

- **Financial Admin:** Land → `/login` → redirect to `/financial/queue` → see Lead-approved timesheets → click row → `/financial/queue/{id}` → review → "Approve" or "Reject" with comment → back to queue → navigate to `/financial/lock` → click "Lock" per FinancialApproved row → row disappears from lock queue → navigate to `/financial/reports` → select Daily/Weekly/Monthly + date → click "Generate" → view grid → click "Export CSV" → navigate to `/financial/export` → set date range + optional filters → click "Download CSV" → file downloads.

- **Administrator:** Land → `/login` → redirect to `/timesheet` (admins also log time) → navigate to Teams → `/admin/teams` → click "+ Create team" → fill name + select Team Lead → save → team appears in list → click team row → `/admin/teams/{id}` → click "+ Add members" → select users → save → navigate to Projects → `/admin/projects` → click "+ Create project" → fill name + billing type → save → click project row → `/admin/projects/{id}` → click "+ Add users" → select users → save.

- **System Admin:** Land → `/login` → redirect to `/sysadmin/integrations` → see health status per integration (DevOps, JIRA, Linear) → see pending/failed ticket validation list → click "Approve" on a pending validation to mark it manually valid → click "Retry" on a failed validation to re-queue → monitor status updates.

---

## 3. Screens (wireframes)

### /timesheet/{weekStart} (All roles)
Purpose: View and manage the current user's weekly time entries; add or edit entries; submit for approval.  
Required role: All authenticated roles  
Render mode: InteractiveServer (project default)

```
+--------------------------------------------------+
| [◄ Prev week]  Week 19–25 May 2026  [Next week ►]|
|                                                  |
| [+ Add entry]                      Status: Draft |
|--------------------------------------------------|
| Date  | Project   | Hrs  | Notes       | Actions |
|-------|-----------|------|-------------|---------|
| 19/5  | Project A | 8.5  | Setup…      | [✎][✕] |
| 20/5  | Project B | 10.2 | Review… [!] | [✎][✕] |
| 21/5  | (no entry)                               |
| 22/5  | Project A | 9    | Fixes…      | [✎][✕] |
| 23/5  | (no entry)                               |
|-------|-----------|------|-------------|---------|
| Total: 27.7 hrs          Flagged: 1              |
|                                                  |
|                    [Submit for approval]         |
+--------------------------------------------------+
```
`[!]` = "Review required" badge on entries with Hours > threshold.  
Edit/delete actions hidden when status ≥ Submitted.  
"Submit for approval" disabled when status ≥ Submitted or week has zero entries.

```
LOADING STATE
+--------------------------------------------------+
| [◄]  Week 19–25 May 2026  [►]                   |
| [5 skeleton rows ~40 px each]                    |
+--------------------------------------------------+

EMPTY STATE
+--------------------------------------------------+
| Week 19–25 May 2026                              |
|                                                  |
|   No entries yet                                 |
|   Log your first hour to get started             |
|   [+ Add entry]                                  |
+--------------------------------------------------+

REJECTED STATE (Status = Draft, RejectionComment present)
+--------------------------------------------------+
| ⚠ Your timesheet was returned for correction     |
|   "Incorrect project assignment on 20 May.       |
|    Please re-check and resubmit."                |
|   — Returned by Alice Johnson, 19 May            |
|                                                  |
| Week 19–25 May 2026          [◄][►]              |
| [+ Add entry]                        Status: Draft|
| … (entry rows, all editable again) …             |
|                          [Submit for approval]   |
+--------------------------------------------------+

ERROR STATE
+--------------------------------------------------+
| [!] We couldn't load your entries.               |
|     Check your connection and try again.         |
|     [Try again]                                  |
+--------------------------------------------------+
```

Data: `WeeklyTimesheetDto` (WeekStart, WeekEnd, Status, TotalHours, FlaggedCount, RejectionComment?, RejectedByName?, RejectedAt?, Entries: `TimeEntryDto[]`)  
`RejectionComment`, `RejectedByName`, `RejectedAt` are nullable; rendered as an amber warning banner when Status = Draft and RejectionComment is present (spec F2.2 — submitter must see rejection reason).  
`TimeEntryDto`: Id, Date, ProjectName, Hours, Notes, TaskType?, TicketRef?, FlaggedForReview, TicketValidationStatus  
Actions: "+ Add entry" (opens modal), edit ✎ (opens modal pre-filled), delete ✕ (confirm dialog), "Submit for approval" (opens SubmissionReviewModal)  
Responsive (< md): table becomes card stack; each card shows Date + Project on one line, Hours + Notes on next; ✎ / ✕ in card footer dropdown; week nav becomes full-width row above cards.

---

### /timesheet/{weekStart} — Add/Edit Entry Modal (All roles)
Purpose: Create or edit a single time entry within the current week.  
Required role: All authenticated roles (modal on `/timesheet/{weekStart}`)  
Render mode: InteractiveServer (modal rendered within same circuit)

```
+------------------------------------------+
| Add entry                             [✕] |
|------------------------------------------|
| Date *              [22/05/2026 ▼]       |
| Project *           [Select project ▼]   |
| Hours *             [8.5         ]       |
|                     ⚠ 10.5 hrs — will    |
|                       be flagged for     |
|                       Team Lead review   |
| Notes *             [Sprint planning…]   |
| Task Type           [Development ▼]      |
| Ticket reference    [PROJ-123    ]       |
|                     [✓ Ticket found]     |
|                                          |
|          [Cancel]       [Save entry]     |
+------------------------------------------+

VALIDATION ERROR STATE
+------------------------------------------+
| Add entry                             [✕] |
|                                          |
| ⚠ Please fix the errors below            |
|                                          |
| Date *              [22/05/2026 ▼]       |
| Project *           [Select project ▼]   |
|                     ! Project is required|
| Hours *             [         ]          |
|                     ! Hours is required  |
| Notes *             [         ]          |
|                     ! Notes is required  |
| Task Type           [Development ▼]      |
| Ticket reference    [PROJ-999    ]       |
|                     ⚠ Ticket not found   |
|                       in JIRA            |
|          [Cancel]       [Save entry]     |
+------------------------------------------+
```

Accessibility: `<dialog>` element, `aria-modal="true"`, focus trapped within modal on open; focus returns to trigger element on close; Escape key closes.  

Data: `ProjectListDto` (for project dropdown); `TaskTypeDto[]` (for task type dropdown); `TimeEntryDto` (pre-filled on edit)  
Validation:
- **Date**: required; must fall within the week's Mon–Sun range; duplicate Date + Project blocked with "A time entry already exists for this date and project"
- **Project**: required; must be one of the user's assigned projects
- **Hours**: required; numeric, 0.5–24; values > threshold show a soft warning (not error); duplicate-project rule checked after project is selected
- **Notes**: required; 1–500 characters
- **Task Type**: optional; enum dropdown
- **Ticket Ref**: optional; triggers async validation on blur — spinner while checking, green "Ticket found" or amber "Ticket not found in {system}" (save not blocked)  
Responsive (< md): modal becomes full-screen bottom sheet; fields full-width.

---

### /approvals (TeamLead)
Purpose: View the queue of submitted timesheets from the Team Lead's teams, sorted oldest-first.  
Required role: TeamLead

```
+--------------------------------------------------+
| Pending approvals                                |
|                                                  |
| Team member     | Week         | Hrs | Flagged   |
|-----------------|--------------|-----|-----------|
| Alice Johnson   | 19–25 May    | 40  | 0         |
| Bob Smith       | 12–18 May    | 38  | 2  [!]    |
| Carol Lee       | 19–25 May    | 42  | 1  [!]    |
+--------------------------------------------------+
```

```
LOADING STATE
| [5 skeleton rows]

EMPTY STATE
|  All caught up
|  No timesheets waiting for your approval

ERROR STATE
| [!] We couldn't load your queue.  [Try again]
```

Data: `ApprovalQueueItemDto[]` (TimesheetId, UserName, WeekStart, WeekEnd, TotalHours, FlaggedCount, SubmittedAt)  
Actions: Click row → `/approvals/{timesheetId}`  
Responsive (< md): card stack; UserName + Week on top line, Hrs + Flagged count below; click entire card.

---

### /approvals/{timesheetId} (TeamLead)
Purpose: Review a submitted timesheet, validate any overtime entries, then approve or reject.  
Required role: TeamLead

```
+--------------------------------------------------+
| ← Back   Alice Johnson — 19–25 May (40 hrs)      |
|--------------------------------------------------|
| All entries                                      |
| Date  | Project   | Hrs  | Notes       | Flag    |
|-------|-----------|------|-------------|---------|
| 19/5  | Project A | 8    | Setup       | —       |
| 20/5  | Project B | 10.2 | Review      | [!]     |
| 21/5  | Project A | 8    | Dev         | —       |
|       | ...                                      |
|--------------------------------------------------|
| Flagged entries — validation required (1 of 1)  |
|                                                  |
| 20/5 Project B — 10.2 hrs                       |
| Justification *  [Client escalation      ]       |
|                  [Validate ✓]                    |
|                                                  |
| ✓ All flagged entries validated                  |
|--------------------------------------------------|
| Comment (required to reject)                     |
| [                                      ]         |
|                                                  |
|     [Reject]              [Approve]              |
+--------------------------------------------------+
```

"Approve" disabled until all flagged entries validated.  
"Reject" disabled until comment field has ≥ 10 characters.

```
LOADING STATE  | [skeleton rows + skeleton validation section]
EMPTY STATE    | No entries in this timesheet
ERROR STATE    | [!] We couldn't load this timesheet.  [Try again]

VALIDATION ERROR (reject without comment)
| ! A rejection reason is required (minimum 10 characters)
```

Data: `TimesheetDetailDto` (TimesheetId, UserName, WeekStart, TotalHours, Entries: `TimeEntryDetailDto[]`)  
`TimeEntryDetailDto`: Id, Date, ProjectName, Hours, Notes, FlaggedForReview, OvertimeValidated  
Validation:
- **Justification (per flagged entry)**: required to unlock "Validate ✓"; min 10 chars (matches spec F2.3)
- **Rejection comment**: required; min 10 chars; "Reject" button disabled until met  
Responsive (< md): entries table → card stack; flagged validation section becomes full-width accordion; approve/reject buttons stack vertically, full-width.

---

### /financial/queue (FinancialAdmin)
Purpose: View Lead-approved timesheets awaiting the Financial Admin extra approval step.  
Required role: FinancialAdmin

```
+--------------------------------------------------+
| Lead-approved timesheets                         |
|                                                  |
| Team member     | Week       | Hrs | Lead appr'd |
|-----------------|------------|-----|-------------|
| Alice Johnson   | 19–25 May  | 40  | 19 May      |
| Bob Smith       | 12–18 May  | 38  | 18 May      |
+--------------------------------------------------+
```

```
LOADING  | [5 skeleton rows]
EMPTY    | No timesheets to review — check back later
ERROR    | [!] We couldn't load the queue.  [Try again]
```

Data: `FinancialQueueItemDto[]` (TimesheetId, UserName, WeekStart, TotalHours, LeadApprovedAt)  
Actions: Click row → `/financial/queue/{timesheetId}`  
Responsive (< md): card stack; UserName + Week top, Hrs + Lead approved date below.

---

### /financial/queue/{timesheetId} (FinancialAdmin)
Purpose: Review a Lead-approved timesheet and apply the Financial Admin approval or rejection.  
Required role: FinancialAdmin

```
+--------------------------------------------------+
| ← Back   Financial approval: Alice Johnson       |
|          Week 19–25 May — Lead approved 19 May   |
|--------------------------------------------------|
| Date  | Project   | Hrs  | Notes                 |
|-------|-----------|------|-----------------------|
| 19/5  | Project A | 8    | Setup                 |
| 20/5  | Project B | 10   | Review (validated)    |
| 21/5  | Project A | 8    | Dev                   |
|--------------------------------------------------|
| Comment (required to reject)                     |
| [                                      ]         |
|                                                  |
|     [Reject]              [Approve]              |
+--------------------------------------------------+
```

```
LOADING  | [skeleton rows]
EMPTY    | No entries in this timesheet
ERROR    | [!] We couldn't load this timesheet.  [Try again]
```

Data: `TimesheetDetailDto` (same shape as approval detail)  
Validation:
- **Rejection comment**: required; min 10 chars; "Reject" button disabled until met  
Responsive (< md): table → card stack; approve/reject stack full-width at bottom.

---

### /financial/lock (FinancialAdmin)
Purpose: View Financial-Admin-approved timesheets and lock them to make them immutable.  
Required role: FinancialAdmin

```
+--------------------------------------------------+
| Ready to lock                                    |
|                                                  |
| Team member     | Week       | Hrs | Lock        |
|-----------------|------------|-----|-------------|
| Alice Johnson   | 19–25 May  | 40  | [🔒 Lock]   |
| Bob Smith       | 12–18 May  | 38  | [🔒 Lock]   |
| Carol Lee       | 19–25 May  | 42  | [🔒 Lock]   |
+--------------------------------------------------+
```

Clicking "Lock" opens a ConfirmDialog before proceeding.  
After lock: row is removed from the list (transitions to Locked status).

```
LOADING  | [5 skeleton rows]
EMPTY    | All timesheets locked — nothing to do
ERROR    | [!] We couldn't load the queue.  [Try again]

CONFIRM DIALOG (on lock)
+------------------------------------------+
| Lock timesheet?                       [✕] |
|                                          |
| Alice Johnson — 19–25 May                |
| This is permanent. Locked timesheets     |
| cannot be edited by any role.            |
|                                          |
|       [Cancel]     [Lock timesheet]      |
+------------------------------------------+
```

ConfirmDialog: `aria-modal="true"`, focus trapped, Escape cancels.  
Data: `LockQueueItemDto[]` (TimesheetId, UserName, WeekStart, TotalHours, FinancialApprovedAt)  
Responsive (< md): card stack; lock button full-width in card footer.

---

### /financial/reports (FinancialAdmin)
Purpose: Generate daily, weekly, or monthly reconciliation reports over locked timesheet data.  
Required role: FinancialAdmin

```
+--------------------------------------------------+
| Reconciliation reports                           |
|                                                  |
| Report type *    [Weekly ▼]                      |
| Week *           [Week 19 May 2026 ▼]            |
|                  [Generate report]               |
|--------------------------------------------------|
| Results — Week 19–25 May (locked entries only)   |
|                                                  |
| Name      | Project   | Mon | Tue | … | Total    |
|-----------|-----------|-----|-----|---|----------|
| Alice     | Project A | 8   | —   | … | 32       |
| Bob       | Project B | —   | 10  | … | 38       |
|-----------|-----------|-----|-----|---|----------|
| Total                 | 8   | 10  | … | 70       |
|                                                  |
|                          [Export CSV]            |
+--------------------------------------------------+
```

Date picker adapts to report type: single date (Daily), ISO week selector (Weekly), month picker (Monthly).

```
LOADING  | Report type + date selectors remain enabled;
         | results area shows spinner + "Generating report…"

EMPTY    | No locked entries in this period

ERROR    | [!] Report generation took too long.
         |     Try a smaller date range.  [Try again]
```

Data: `ReconciliationReportDto` (ReportType, PeriodLabel, Columns: string[], Rows: `ReportRowDto[]`)  
Validation:
- **Report type**: required; enum (Daily, Weekly, Monthly)
- **Date/period**: required; date picker scoped to type  
Responsive (< md): form stacks; results table scrolls horizontally or collapses to per-user cards with totals; Export CSV button full-width.

---

### /financial/export (FinancialAdmin)
Purpose: Export locked timesheet data to CSV with optional date, project, and team filters.  
Required role: FinancialAdmin

```
+--------------------------------------------------+
| Export locked timesheets                         |
|                                                  |
| Date range *   [01/05/2026] to [31/05/2026]      |
| Project        [All projects ▼]                  |
| Team           [All teams ▼]                     |
|                                                  |
| Preview (first 5 rows)                           |
| Date  | Name     | Project   | Hrs | Billing type|
|-------|----------|-----------|-----|-------------|
| 20/5  | Alice    | Project A | 8   | T&M         |
| 20/5  | Bob      | Project B | 10  | Fixed       |
| …     | …        | …         | …   | …           |
|                                                  |
|                      [Download CSV]              |
+--------------------------------------------------+
```

Preview loads automatically when valid date range is entered. Download triggers file download; link retained server-side for 24 hours.

```
LOADING  | Preview area shows spinner; Download button disabled
EMPTY    | No locked entries match these filters
         | Try adjusting your filters
ERROR    | [!] We couldn't generate the export.  [Try again]
```

Data: `ExportPreviewDto` (TotalRows, PreviewRows: `ExportRowDto[]`); full export is a streamed file download  
Validation:
- **Date range**: required; from ≤ to; both date pickers required
- **Project**: optional
- **Team**: optional  
Responsive (< md): filters stack; preview table scrolls horizontally; Download button full-width.

---

### /admin/teams (Administrator)
Purpose: List all teams within the admin's scope; create new teams.  
Required role: Administrator

```
+--------------------------------------------------+
| Teams                         [+ Create team]    |
|                                                  |
| Name         | Team Lead     | Members | Actions |
|--------------|---------------|---------|---------|
| Engineering  | Alice Johnson | 12      | [Edit]  |
| Sales        | Carol Lee     | 8       | [Edit]  |
| Support      | (none)        | 5       | [Edit]  |
+--------------------------------------------------+
```

"Edit" navigates to `/admin/teams/{teamId}`.

```
LOADING  | [3 skeleton rows]
EMPTY    | No teams yet
         | Create your first team to assign members
         | [+ Create team]
ERROR    | [!] We couldn't load teams.  [Try again]
```

Data: `TeamSummaryDto[]` (Id, Name, TeamLeadName, MemberCount)  
Responsive (< md): card stack; Name + Team Lead + member count per card; Edit button in card footer.

---

### /admin/teams/{teamId} (Administrator)
Purpose: Edit team properties, assign/change the Team Lead, add/remove members.  
Required role: Administrator

```
+--------------------------------------------------+
| ← Back   Edit team: Engineering                  |
|                                                  |
| Team name *     [Engineering            ]        |
| Team Lead       [Alice Johnson ▼]                |
| Description     [Core engineering squad ]        |
|                                                  |
| Members (12)              [+ Add members]        |
|--------------|------------|------------------------
| Name         | Role       | Actions               |
|--------------|------------|------------------------
| Alice Johnson| Team Lead  | (Lead — cannot remove)|
| Bob Smith    | Member     | [Remove]              |
| Carol Lee    | Member     | [Remove]              |
| … (9 more, paginated)                            |
|                                                  |
|  [Delete team]          [Cancel]  [Save changes] |
+--------------------------------------------------+
```

"+ Add members" opens a multi-select search dialog scoped to the admin's in-scope users.  
"Delete team" opens ConfirmDialog. All three bottom buttons anchored in the page footer.

```
LOADING  | [skeleton form + 5 skeleton member rows]
EMPTY (members)  | No members yet — click "+ Add members" to assign people to this team
ERROR    | [!] We couldn't load this team.  [Try again]

CONFIRM DIALOG (delete)
+------------------------------------------+
| Delete team: Engineering?             [✕] |
|                                          |
| Members will not be deleted.             |
| This action cannot be undone.            |
|       [Cancel]     [Delete team]         |
+------------------------------------------+
```

Data: `TeamDetailDto` (Id, Name, Description, TeamLeadId, Members: `TeamMemberDto[]`)  
Validation:
- **Team name**: required; 1–100 chars; unique within scope
- **Team Lead**: optional
- **Description**: optional; 0–500 chars  
Responsive (< md): form fields full-width; member list as cards; Save / Cancel / Delete stack at bottom.

---

### /admin/projects (Administrator)
Purpose: List all projects within the admin's scope; create new projects.  
Required role: Administrator

```
+--------------------------------------------------+
| Projects                    [+ Create project]   |
|                                                  |
| Name        | Billing type | Users   | Actions   |
|-------------|--------------|---------|-----------|
| Project A   | T&M          | 8       | [Edit]    |
| Project B   | Fixed        | 5       | [Edit]    |
| Project C   | T&M          | 12      | [Edit]    |
+--------------------------------------------------+
```

```
LOADING  | [3 skeleton rows]
EMPTY    | No projects yet
         | Create your first project
         | [+ Create project]
ERROR    | [!] We couldn't load projects.  [Try again]
```

Data: `ProjectSummaryDto[]` (Id, Name, BillingType, UserCount, IsActive)  
Deactivated projects shown with muted style and "(inactive)" label; no edit action.  
Responsive (< md): card stack; Name + BillingType + user count per card.

---

### /admin/projects/{projectId} (Administrator)
Purpose: Edit project properties; manage assigned users; deactivate the project.  
Required role: Administrator

```
+--------------------------------------------------+
| ← Back   Edit project: Project A                 |
|                                                  |
| Project name *  [Project A              ]        |
| Billing type *  [T&M ▼]                          |
| Description     [Revenue client project ]        |
|                                                  |
| Assigned users (8)         [+ Add users]         |
|--------------|-------------|---------------------
| Name         | Team        | Actions             |
|--------------|-------------|---------------------
| Alice Johnson| Engineering | [Remove]            |
| Bob Smith    | Engineering | [Remove]            |
| … (6 more, paginated)                            |
|                                                  |
| [Deactivate project]    [Cancel]  [Save changes] |
+--------------------------------------------------+
```

"+ Add users" opens multi-select search scoped to admin's in-scope users.  
"Deactivate project" replaces "Delete" — preserves existing entries; removes project from time-entry picker.

```
LOADING  | [skeleton form + 5 skeleton user rows]
EMPTY (users)  | No users assigned yet — click "+ Add users" to assign people to this project
ERROR    | [!] We couldn't load this project.  [Try again]

CONFIRM DIALOG (deactivate)
+------------------------------------------+
| Deactivate project: Project A?        [✕] |
|                                          |
| Existing time entries will be preserved. |
| The project will no longer appear in the |
| time-entry picker.                       |
|       [Cancel]     [Deactivate]          |
+------------------------------------------+
```

Data: `ProjectDetailDto` (Id, Name, BillingType, Description, IsActive, Users: `ProjectUserDto[]`)  
Validation:
- **Project name**: required; 1–100 chars; unique within scope
- **Billing type**: required; enum (T&M, Fixed)
- **Description**: optional; 0–500 chars  
Responsive (< md): form fields full-width; user list as cards; action buttons stack at bottom.

---

### /audit (FinancialAdmin, SystemAdmin)
Purpose: Search the append-only audit log by actor, date range, and action type.  
Required role: FinancialAdmin, SystemAdmin

```
+--------------------------------------------------+
| Audit log                                        |
|                                                  |
| Actor          [Any actor ▼]                     |
| Action type    [Any action ▼]                    |
| Date range     [01/05/2026] to [31/05/2026]      |
|                              [Search]            |
|--------------------------------------------------|
| 1 247 results                                    |
|                                                  |
| Timestamp      | Actor   | Action  | Resource    |
|----------------|---------|---------|-------------|
| 20/5 09:15 UTC | Alice   | Approve | Timesheet 42|
| 20/5 09:00 UTC | Bob     | Reject  | Timesheet 41|
| 20/5 08:45 UTC | Carol   | Lock    | Timesheet 40|
| … (paginated, 50 per page)                       |
|                                                  |
|        [← Prev]   Page 1 of 25   [Next →]        |
+--------------------------------------------------+
```

Clicking a row expands an inline detail panel showing the before/after JSON snapshot, source IP, and correlation ID.

```
LOADING  | Filters disabled; results area shows spinner
EMPTY    | No audit entries match your filters
ERROR    | [!] We couldn't load the audit log.  [Try again]
```

Data: `AuditLogSearchResultDto` (TotalCount, Page, PageSize, Entries: `AuditLogEntryDto[]`)  
`AuditLogEntryDto`: TimestampUtc, ActorName, ActionType, ResourceType, ResourceId, BeforeJson, AfterJson, SourceIp, CorrelationId  
Pagination: keyset-based (server-side); page size 50; renders `[← Prev] Page N of N [Next →]`  
Validation:
- All filters optional
- Date range: from ≤ to when both provided  
Responsive (< md): filters stack; table → card stack showing Timestamp, Actor, Action, Resource; expanded row becomes full-width detail card below.

---

### /sysadmin/integrations (SystemAdmin)
Purpose: Monitor DevOps, JIRA, and Linear integration health; review and action pending/failed ticket validations.  
Required role: SystemAdmin

```
+--------------------------------------------------+
| Integration health                               |
|                                                  |
| Service   | Status        | Last checked | Detail|
|-----------|---------------|--------------|-------|
| JIRA      | ● Online      | 2 min ago    | [↓]   |
| Linear    | ● Online      | 1 min ago    | [↓]   |
| DevOps    | ⚠ Degraded    | 15 min ago   | [↓]   |
|                                                  |
| Ticket validations — pending / failed (3)        |
|                                                  |
| Status   | Ref       | System | Actions          |
|----------|-----------|--------|------------------|
| Pending  | JIRA-1042 | JIRA   | [Approve] [Retry]|
| Failed   | LIN-99    | Linear | [Retry]          |
| Pending  | DEVOPS#12 | DevOps | [Approve] [Retry]|
+--------------------------------------------------+
```

"[↓]" expands an inline collapsible row showing last error message and credentials status.

```
LOADING  | [3 skeleton service rows + 3 skeleton validation rows]
EMPTY (validations)  | All ticket references validated — nothing to action
ERROR    | [!] We couldn't load integration status.  [Try again]
```

Data: `IntegrationStatusDto` (Services: `ServiceStatusDto[]`); `ValidationQueueDto` (Validations: `TicketValidationDto[]`)  
`ServiceStatusDto`: Name, Status (Online/Degraded/Offline), LastCheckedAt, LastErrorMessage  
`TicketValidationDto`: Id, TicketRef, Source, Status (Pending/Failed), EntryDate, UserName  
Actions: "Approve" → marks validation as manually approved; "Retry" → re-queues for background job  
Responsive (< md): service rows as expandable cards; validation rows as cards with actions in footer.

---

### /403 (anonymous)
Purpose: Inform the user they lack permission for the requested resource.  
Required role: anonymous  
Render mode: Static

```
+------------------------------------------+
| Access denied                            |
|                                          |
| You don't have permission to view        |
| this page.                               |
|                                          |
| [Go to my timesheet]                     |
+------------------------------------------+
```

---

### /404 (anonymous)
Purpose: Inform the user the requested page does not exist.  
Required role: anonymous  
Render mode: Static

```
+------------------------------------------+
| Page not found                           |
|                                          |
| The page you're looking for doesn't      |
| exist or has been moved.                 |
|                                          |
| [Go to my timesheet]                     |
+------------------------------------------+
```

---

## 4. Component inventory

### Generic (all screens)
| Component | Purpose | Props |
|---|---|---|
| `EmptyState` | Generic empty placeholder | `title: string`, `message: string`, `ctaLabel?: string`, `ctaHref?: string` |
| `LoadingSkeleton` | Animated skeleton rows | `rowCount: int`, `columns?: int` |
| `ErrorBanner` | Inline error with optional retry | `message: string`, `onRetry?: EventCallback` |
| `ConfirmDialog` | Modal for destructive actions | `title: string`, `message: string`, `confirmLabel: string`, `isDangerous: bool`, `onConfirm: EventCallback`, `onCancel: EventCallback` |
| `ValidationSummary` | `aria-live` form-error summary | `errors: ValidationErrorDto[]` |
| `StatusChip` | Coloured status badge | `status: TimesheetStatus` |

### Time entry
| Component | Purpose | Props |
|---|---|---|
| `TimeEntryGrid` | Weekly entry table/card stack | `entries: TimeEntryDto[]`, `isEditable: bool`, `onEdit: EventCallback<Guid>`, `onDelete: EventCallback<Guid>` |
| `TimeEntryFormModal` | Add/edit entry modal (`<dialog>`) | `weekStart: DateOnly`, `initialEntry?: TimeEntryDto`, `projects: ProjectListItemDto[]`, `onSave: EventCallback<TimeEntryDto>`, `onClose: EventCallback` |
| `TicketRefValidator` | Async tick/warning/spinner for ticket field | `ticketRef: string`, `status: TicketValidationStatus`, `systemName?: string` |
| `SubmissionReviewModal` | Pre-submit warnings modal (`<dialog>`) | `missingDays: DateOnly[]`, `flaggedEntries: TimeEntryDto[]`, `onConfirm: EventCallback`, `onCancel: EventCallback` |

### Approval
| Component | Purpose | Props |
|---|---|---|
| `ApprovalQueueList` | Reusable queue table (approvals, financial, lock) | `items: IApprovalQueueItem[]`, `columns: ColumnDef[]`, `onRowClick?: EventCallback<Guid>` |
| `FlaggedEntryValidator` | Per-entry overtime validation section | `entries: TimeEntryDetailDto[]`, `onValidate: EventCallback<(Guid, string)>`, `allValidated: bool` |
| `ApprovalPanel` | Approve/reject buttons + comment field | `onApprove: EventCallback`, `onReject: EventCallback<string>`, `rejectMinChars: int`, `isProcessing: bool` |

### Admin
| Component | Purpose | Props |
|---|---|---|
| `TeamForm` | Create/edit team fields | `team?: TeamDetailDto`, `availableLeads: UserListItemDto[]`, `onSave: EventCallback<TeamDto>`, `onCancel: EventCallback` |
| `TeamMemberList` | Member table with remove | `members: TeamMemberDto[]`, `onRemove?: EventCallback<Guid>` |
| `ProjectForm` | Create/edit project fields | `project?: ProjectDetailDto`, `onSave: EventCallback<ProjectDto>`, `onCancel: EventCallback` |
| `ProjectUserList` | Assigned users with remove | `users: ProjectUserDto[]`, `onRemove?: EventCallback<Guid>` |
| `UserSearchPicker` | Multi-select user search dialog | `scopedUsers: UserListItemDto[]`, `selectedIds: List<Guid>`, `onConfirm: EventCallback<List<Guid>>` |

### Reports / export / audit
| Component | Purpose | Props |
|---|---|---|
| `ReportFilterForm` | Type + date selectors for reports | `onGenerate: EventCallback<ReportFiltersDto>`, `isGenerating: bool` |
| `ReportResultsGrid` | Report results table + CSV export | `report: ReconciliationReportDto`, `onExport: EventCallback` |
| `ExportFilterForm` | Date range + optional filters | `onDownload: EventCallback<ExportFiltersDto>`, `isLoading: bool` |
| `AuditLogSearch` | Filter form for audit log | `onSearch: EventCallback<AuditSearchFiltersDto>`, `isLoading: bool` |
| `AuditLogTable` | Paginated results with expandable rows | `result: AuditLogSearchResultDto`, `onPageChange: EventCallback<int>` |

### Integration
| Component | Purpose | Props |
|---|---|---|
| `IntegrationStatusCard` | Expandable service health row | `service: ServiceStatusDto` |
| `TicketValidationRow` | Pending/failed ticket with actions | `validation: TicketValidationDto`, `onApprove?: EventCallback<Guid>`, `onRetry: EventCallback<Guid>`, `isProcessing: bool` |

---

## 5. Design tokens

Reference the design-token baseline from the `uiux-design-system` skill. The following are project-specific overrides only:

```css
:root {
  /* Brand — no custom colour specified in spec; use system default */
  /* Override if a brand colour is confirmed with stakeholders (OQ from spec) */

  /* Status colours (semantic, not brand) */
  --color-status-draft:              var(--color-neutral-500);
  --color-status-submitted:          var(--color-info-600);
  --color-status-approved-lead:      var(--color-warning-600);
  --color-status-financial-approved: var(--color-warning-700);
  --color-status-locked:             var(--color-success-700);
  --color-status-flagged:            var(--color-warning-500);

  /* Integration health */
  --color-integration-online:        var(--color-success-600);
  --color-integration-degraded:      var(--color-warning-600);
  --color-integration-offline:       var(--color-error-600);
}
```

Dark mode: not in scope per spec §5.8 (no mention). Defer until stakeholder request.

---

## 6. Responsive breakpoints

Reference the breakpoint baseline from the `uiux-design-system` skill. No overrides — minimum viewport width 320 px as specified in spec §5.8.

---

## 7. Accessibility commitments

Reference the WCAG 2.1 AA baseline from the `uiux-design-system` skill. Project-specific additions:

- **Modal dialogs** (`TimeEntryFormModal`, `SubmissionReviewModal`, `ConfirmDialog`): must use `<dialog>` element with `aria-modal="true"`; focus trapped within modal on open; focus returns to the triggering element on close; Escape key must close the modal.
- **Approval tables**: all sortable columns must have `aria-sort`; action buttons in table rows must have `aria-label` including the row context (e.g. `aria-label="Approve timesheet for Alice Johnson, week 19–25 May"`).
- **Async ticket validation** (`TicketRefValidator`): status updates must be announced via `aria-live="polite"` region so screen-reader users hear the result without losing focus.
- **Status chips** (`StatusChip`): must not rely on colour alone; include a text label and optionally an icon with `aria-hidden="true"`.
- **Lock confirmation dialog**: `aria-describedby` must point to the warning text ("This is permanent…") so the consequence is read before the confirm button.

---

## 8. Microcopy (key strings)

| Key | Copy |
|---|---|
| `timesheet.nav.label` | "My Timesheet" |
| `timesheet.week.empty.title` | "No entries yet" |
| `timesheet.week.empty.message` | "Log your first hour to get started" |
| `timesheet.week.error` | "We couldn't load your entries." |
| `timesheet.week.submit.button` | "Submit for approval" |
| `timesheet.week.submit.success` | "Timesheet for week of {date} submitted for approval" |
| `timesheet.week.rejected.banner` | "Your timesheet was returned for correction" |
| `timesheet.week.rejected.by` | "Returned by {name}, {date}" |
| `timesheet.status.draft` | "Draft" |
| `timesheet.status.submitted` | "Submitted" |
| `timesheet.status.approved_lead` | "Approved by team lead" |
| `timesheet.status.financial_approved` | "Pending lock" |
| `timesheet.status.locked` | "Locked" |
| `entry.form.add.title` | "Add entry" |
| `entry.form.edit.title` | "Edit entry" |
| `entry.form.save` | "Save entry" |
| `entry.form.hours.flag_warning` | "This entry will be flagged for Team Lead review" |
| `entry.validation.duplicate` | "A time entry already exists for this date and project" |
| `entry.ticket.found` | "Ticket found" |
| `entry.ticket.not_found` | "Ticket not found in {system}" |
| `entry.ticket.pending` | "Checking ticket…" |
| `submit.modal.title` | "Review before submitting" |
| `submit.modal.missing_heading` | "Missing days" |
| `submit.modal.flagged_heading` | "Flagged entries" |
| `submit.modal.acknowledge` | "I acknowledge the above gaps" |
| `submit.modal.confirm` | "Submit for approval" |
| `submit.modal.cancel` | "Go back" |
| `approvals.empty.title` | "All caught up" |
| `approvals.empty.message` | "No timesheets waiting for your approval" |
| `approvals.overtime.validate` | "Validate" |
| `approvals.overtime.all_done` | "✓ All flagged entries validated" |
| `approvals.reject.placeholder` | "Enter a reason for rejection" |
| `approvals.reject.hint` | "Minimum 10 characters" |
| `approvals.reject.button` | "Reject" |
| `approvals.approve.button` | "Approve" |
| `lock.empty.title` | "All timesheets locked" |
| `lock.empty.message` | "Nothing to do" |
| `lock.button` | "Lock" |
| `lock.confirm.title` | "Lock timesheet?" |
| `lock.confirm.message` | "This is permanent. Locked timesheets cannot be edited by any role." |
| `lock.confirm.confirm` | "Lock timesheet" |
| `lock.locked_error` | "This timesheet is locked and cannot be modified." |
| `reports.generate.button` | "Generate report" |
| `reports.empty` | "No locked entries in this period" |
| `reports.loading` | "Generating report…" |
| `reports.error.timeout` | "Report generation took too long. Try a smaller date range." |
| `reports.export.button` | "Export CSV" |
| `export.download.button` | "Download CSV" |
| `export.empty.title` | "No locked entries match these filters" |
| `export.empty.message` | "Try adjusting your filters" |
| `admin.teams.empty.title` | "No teams yet" |
| `admin.teams.empty.message` | "Create your first team to assign members" |
| `admin.teams.create` | "+ Create team" |
| `admin.teams.delete.title` | "Delete team: {name}?" |
| `admin.teams.delete.message` | "Members will not be deleted. This action cannot be undone." |
| `admin.projects.empty.title` | "No projects yet" |
| `admin.projects.empty.message` | "Create your first project" |
| `admin.projects.create` | "+ Create project" |
| `admin.projects.deactivate.title` | "Deactivate project: {name}?" |
| `admin.projects.deactivate.message` | "Existing time entries will be preserved. The project will no longer appear in the time-entry picker." |
| `integrations.validations.empty` | "All ticket references validated — nothing to action" |
| `integrations.status.online` | "Online" |
| `integrations.status.degraded` | "Degraded" |
| `integrations.status.offline` | "Offline" |
| `audit.empty` | "No audit entries match your filters" |
| `common.error.retry` | "Try again" |
| `common.cancel` | "Cancel" |
| `common.save` | "Save changes" |
| `common.back` | "← Back" |
| `common.not_authorised` | "You don't have permission to view this page." |
| `common.not_found` | "The page you're looking for doesn't exist or has been moved." |

---

## 9. Open questions for Data Designer

1. **Weekly timesheet creation trigger** — the `/timesheet/{weekStart}` screen loads a `WeeklyTimesheetDto`. Should a `WeeklyTimesheet` row be auto-created on the first `TimeEntry` insert for that week, or must the user explicitly initiate it? Auto-creation simplifies the load query but creates empty rows for weeks where a user logs nothing.

2. **Approval queue index** — the Team Lead queue filters `WeeklyTimesheet` by `Status = 'Submitted'` AND `TeamId IN (...)`. A filtered non-clustered index on `(Status, TeamId)` with covering columns `(UserId, WeekStart, WeekEnd, TotalHours, FlaggedCount, SubmittedAt)` would eliminate key lookups. Confirm whether `TotalHours` and `FlaggedCount` will be persisted columns or computed on each query.

3. **`FlaggedCount` denormalisation** — the approval queue and weekly view both surface a flagged-entry count. Computing this per request via `COUNT(*)` over `TimeEntry WHERE FlaggedForReview = 1` is fine at low volume, but at 2 000 users × peak submission days it could be hot. Consider a persisted `FlaggedCount` column on `WeeklyTimesheet` incremented/decremented by the service layer.

4. **Lock queue index** — `/financial/lock` filters by `Status = 'FinancialApproved'`. A filtered index on `(Status)` with covering columns `(TimesheetId, UserId, WeekStart, TotalHours, FinancialApprovedAt)` covers the list view without a key lookup.

5. **Audit log pagination** — the audit log screen uses page-by-page navigation over potentially millions of rows. OFFSET-based pagination degrades at large offsets. Recommend keyset pagination using `(TimestampUtc, Id)` as the cursor; confirm the Data Designer designs the `AuditEvent` clustered index to support this (e.g. clustered on `(TimestampUtc DESC, Id DESC)`).

6. **Report query shape** — the weekly reconciliation report pivots entries by day-of-week per user per project. This is either a dynamic pivot in SQL or a client-side grouping. Confirm preferred approach and whether a materialised view or covering index on `(Status, Date, UserId, ProjectId)` is sufficient.

7. **Export preview count** — `/financial/export` shows a "preview (first 5 rows)" before download. This requires one COUNT query + one TOP 5 query over locked entries for the selected filters. Confirm whether the Data Designer wants this as a single stored procedure or two separate indexed queries.

8. **`ProjectUserDto.TeamName`** — the project user list on `/admin/projects/{id}` shows a Team column. This requires joining `ProjectUser → User → UserTeam → Team`. Confirm whether the team affiliation shown is the user's primary team or all teams (a user can belong to multiple teams per spec F4.1).

9. **Leave/holiday flag data model (OQ-012 from spec)** — the SubmissionReviewModal distinguishes "missing days" from "leave/holiday days". The data model needs a per-day leave flag. Confirm the minimal representation (e.g. a `LeaveDay` table with `(UserId, Date, Type)` or a boolean column on a `TimesheetDay` projection) before the weekly view's empty-state logic is implemented.

10. **`WeeklyTimesheetDto` rejection fields** — the rejection banner on `/timesheet/{weekStart}` requires `RejectionComment`, `RejectedByName`, and `RejectedAt` on the DTO. Confirm these are stored on the `ApprovalRecord` entity (most recent rejection record for the timesheet) and can be joined efficiently without a separate endpoint call.

---

## 10. Changelog

- Iteration 1: initial design
- Iteration 2: fixed 6 critic blockers — (1) overtime justification min-chars corrected 1→10; (2) added empty state to `/financial/queue/{id}`; (3) added empty state to `/admin/teams/{id}` member list; (4) added empty state to `/admin/projects/{id}` user list; (5) added rejection banner state to `/timesheet/{weekStart}`, added `RejectionComment`/`RejectedByName`/`RejectedAt` to `WeeklyTimesheetDto`, updated Team Member journey; (6) added `timesheet.week.submit.success`, `timesheet.week.rejected.banner`, `timesheet.week.rejected.by` microcopy keys.
