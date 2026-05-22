# User Prompt — Project Idea

> The BA agent will read this file (if present) and refine it through interactive conversation, then overwrite it with the canonical captured idea before writing the formal spec.
>
> Leave this section empty for fully interactive intake, or paste a rough idea below.

## Idea

Timesheet & Billing Workflow System — a Harvest replacement.

**Problem:** Harvest is costly, manually intensive for billing, over-exposes financial data, and has poor UX for daily time capture.

**Goal:** Improve billing accuracy, reduce manual effort, enforce role-based access, and enhance the time capture experience.

### Actors & Roles
- **Team Member** — log daily time, submit weekly timesheets
- **Team Lead** — review and approve team timesheets, handle exceptions
- **Administrator** — manage users, teams, project assignments (scoped to assigned projects/users only)
- **Financial Admin (Deon/Herman)** — final approval, locking, exports, billing reporting
- **System Admin** — manage integrations (DevOps, JIRA, Linear)

### Permissions
- Team Member: log own time, submit timesheet
- Team Lead: log own time, submit timesheet, review team timesheets, approve (Lead level)
- Admin: log own time, manage users/projects (scoped), cannot see all data
- Financial Admin: log own time, review all timesheets, extra approval step, final approve & lock, view all data, export data
- Key constraint: Admin visibility scoped to assigned projects/users only

### Status Lifecycle
Draft → Submitted → Approved by Lead → Final Approved → Locked

### Workflow
1. Team Member logs time daily (Project, Hours, Notes required; ticket reference optional)
2. Team Member reviews entries and submits weekly (warns on missing days)
3. Team Lead reviews, approves or rejects with comments; must explicitly validate overtime
4. Financial Admin (Herman/Deon) applies extra approval step
5. Financial Admin performs final approval and locks the timesheet
6. Locked data exported for billing

### User Stories
**Time Capture**
- ST-001: Log time daily against project and task
- ST-002: Submit timesheet weekly
- ST-003: Review missing days / incomplete entries before submission

**Administration**
- ST-004: Assign people to teams and team leads
- ST-005: Assign squad captains and team leads to projects
- ST-006: Create and manage unlimited projects
- ST-012: Admin sees only assigned people/projects (commercial privacy)
- ST-013: Admin edits/removes only assets within their scope

**Team Lead Approval**
- ST-007: Review team's weekly timesheets
- ST-008: Reject or request corrections with comments
- ST-009: Validate overtime and after-hours work

**Financial Admin**
- ST-010: Extra approval step for Herman and Deon
- ST-011: Final approve and lock timesheets
- ST-014: Export user and timesheet data in template format
- ST-016: Generate daily, weekly, monthly reconciliation reports
- ST-017: Audit trail (who created/edited/approved/locked, with timestamps and before/after values)

**Integrations & Future**
- ST-015: Integrate with DevOps, JIRA, Linear for ticket reference validation (manual fallback if offline)
- ST-018: Resource allocation / capacity planning (future scope)

### Acceptance Criteria (key)
- Time entry must include Project, Hours, Notes; missing fields prevent save
- Hours > threshold flagged for review (does not block submission)
- Duplicate date/project entry prevented or flagged
- Incomplete week triggers warning before submission; partial week (leave/holiday) allowed without warning
- Team Lead rejection reverts to Draft with comments visible to submitter
- Overtime entries require explicit Team Lead validation before approval
- Financial Admin final approval locks immediately; locked entries cannot be edited by any role
- Export includes only locked entries, in billing template format

### Field-Level Data Requirements
**User:** ID, Name, Role, Team
**Project:** ID, Name, Billing Type, Assigned Users
**Timesheet Entry:** Date, Hours, Project, Task Type (optional), Notes, Ticket Reference (optional)
**Weekly Timesheet:** User ID, Week Start, Week End, Status
**Approval Record:** Approver ID, Timestamp, Status, Comments (required on rejection)

### Exceptions & Edge Cases
- Missing days: warning before submission; user must acknowledge
- Duplicate entries (same date/project): prevented or flagged before save
- Incorrect project assignment: Team Lead or Financial Admin may reject with comment
- Integration offline: allow manual entry fallback; flag for later reconciliation
- Partial week (leave/holiday): allow reduced hours without warning
- Hours > expected threshold: flag for review; does not block submission

### Integrations
- DevOps, JIRA, Linear — ticket reference validation with manual fallback

### Reporting
Financial Admin generates: daily, weekly, monthly reconciliation reports covering only locked entries.

### Audit Trail
Every event logged with: actor, action (created/edited/approved/locked), timestamp, before/after values.

### Out of Scope
- Payroll processing
- Invoice generation (export feeds downstream billing system)
- Resource planning / capacity forecasting (future — ST-018)

## Constraints (optional)

- Cloud: Azure
- Backend: ASP.NET (.NET 10)
- Frontend: Blazor Web App (Interactive Server)
- Database: SQL Server (Azure SQL in higher environments)

## Non-negotiables (optional)

- Financial Admin (Deon/Herman) must have a dedicated extra approval step before final lock
- Locked timesheets must be immutable — no edits by any role
- Admin visibility must be strictly scoped to assigned projects/users (commercial data privacy)
