# Spec: Timesheet & Billing Workflow System

## 1. Vision
A Harvest replacement for internal timesheet management and billing reconciliation. Team Members log daily time against projects and submit weekly for a two-stage approval chain (Team Lead → Financial Admin); Financial Admins lock approved records to make them immutable and export them to a downstream billing tool. The system enforces strict role-scoped visibility to protect commercial data while providing a full, seven-year audit trail to satisfy South African financial compliance requirements.

## 2. User Personas
- **Team Member** — daily contributor; primary goal: log time accurately and submit weekly timesheets with minimal friction
- **Team Lead** — first-level approver; primary goal: review, validate (including overtime), and approve or reject team timesheets before financial review
- **Administrator** — scoped operations manager; primary goal: manage user, team, and project assignments within their assigned scope without access to commercially sensitive data outside it
- **Financial Admin (Deon / Herman)** — billing owner; primary goal: apply the extra approval step, final-lock approved timesheets, generate reconciliation reports, and export billing data
- **System Admin** — integration owner; primary goal: configure and maintain DevOps/JIRA/Linear integrations for ticket reference validation

## 3. Epics
- E1: Time Capture & Submission
- E2: Multi-Level Approval Workflow
- E3: Financial Lock & Export
- E4: Administration & RBAC
- E5: Reporting & Audit Trail
- E6: Integrations

## 4. Features

### F1.1: Daily Time Entry Logging (Epic E1)
**As a** Team Member **I want** to log time against a project on a specific date with notes **so that** my work hours are captured accurately for billing.

**Acceptance criteria:**
- [ ] Saving a time entry without Date, Project, or Hours is prevented; a field-level validation message identifies the missing field(s)
- [ ] Saving a time entry without Notes is prevented with the message "Notes are required"
- [ ] A time entry where Hours > 10 for a single day is saved but flagged with a "Review required" indicator visible to the Team Lead
- [ ] Creating a second entry for the same User + Date + Project is blocked with the message "A time entry already exists for this date and project"
- [ ] Task Type and Ticket Reference fields are optional; when provided they are persisted and displayed in the weekly view
- [ ] A saved entry appears immediately in the user's weekly view with status "Draft"

**Tasks:**
- T1.1.a: Create `TimeEntry`, `WeeklyTimesheet` entities and EF Core migration with all required columns
- T1.1.b: Implement `TimeEntryRepository` with duplicate-check query (unique on user / date / project)
- T1.1.c: Implement `TimeEntryService.CreateEntry` with mandatory-field validation, hours-threshold flag, and duplicate guard
- T1.1.d: Build Blazor `TimeEntryForm` component (fields: Date, Project, Hours, Notes; optional: TaskType, TicketRef) with server-side validation wiring

---

### F1.2: Pre-Submission Entry Validation (Epic E1)
**As a** Team Member **I want** to see a summary of missing or incomplete days before submitting my weekly timesheet **so that** I can fix gaps before they reach my Team Lead.

**Acceptance criteria:**
- [ ] Clicking "Submit" on a week with at least one weekday with zero logged hours shows a warning listing the missing dates; the user must tick "I acknowledge the above gaps" before the confirm button activates
- [ ] A week where all missing days are covered by a leave or public holiday flag allows submission without a missing-days warning
- [ ] A week with at least one entry flagged "Review required" shows a warning listing those entries; the user may still submit after acknowledgement
- [ ] A week with no entries at all cannot be submitted (confirm button is permanently disabled)

**Tasks:**
- T1.2.a: Implement `WeeklyTimesheetService.ValidateForSubmission` — returns a structured `SubmissionWarnings` result (missing days, flagged entries, blocking conditions)
- T1.2.b: Build Blazor `SubmissionReviewModal` component that displays warnings grouped by type and enables confirm only when all acknowledgement checkboxes are ticked

---

### F1.3: Weekly Timesheet Submission (Epic E1)
**As a** Team Member **I want** to submit my reviewed weekly timesheet **so that** it enters the approval workflow and is locked from further edits by me.

**Acceptance criteria:**
- [ ] A successfully submitted timesheet transitions from `Draft` to `Submitted` and `SubmittedAt` timestamp is recorded
- [ ] A Team Member cannot edit or delete entries on a timesheet with status `Submitted` or later; edit controls are hidden and direct API calls return HTTP 403
- [ ] The submitter sees an on-screen confirmation: "Timesheet for week of {WeekStart:dd MMM yyyy} submitted for approval"
- [ ] The responsible Team Lead sees the submitted timesheet in their pending-approval queue on next page load

**Tasks:**
- T1.3.a: Implement `WeeklyTimesheetService.Submit` — transitions status, records `SubmittedAt`, and enforces entry-level edit lock
- T1.3.b: Build Blazor `WeeklyTimesheetView` page with submission button wired to `SubmissionReviewModal` (F1.2) and the submit service

---

### F2.1: Team Lead Review Queue (Epic E2)
**As a** Team Lead **I want** to see a queue of submitted timesheets from my team members **so that** I can review and action them efficiently.

**Acceptance criteria:**
- [ ] The queue shows only timesheets for Team Members in teams where the current user holds the Team Lead role
- [ ] The queue is sorted by `SubmittedAt` ascending (oldest first)
- [ ] Each row displays: Team Member name, week start/end date, total hours, count of "Review required" flagged entries, and current status
- [ ] Clicking a row navigates to a detail view showing all individual time entries for that timesheet

**Tasks:**
- T2.1.a: Implement `TimesheetRepository.GetPendingForTeamLead` scoped to the caller's teams
- T2.1.b: Build Blazor `TeamLeadQueuePage` with sorted list and drill-down navigation to `TimesheetDetailView`

---

### F2.2: Team Lead Approve / Reject (Epic E2)
**As a** Team Lead **I want** to approve or reject a submitted timesheet with comments **so that** correct timesheets proceed to financial review and incorrect ones are returned for correction.

**Acceptance criteria:**
- [ ] Approving transitions the timesheet from `Submitted` to `ApprovedByLead`; approver ID and timestamp are recorded in an `ApprovalRecord`
- [ ] Rejecting transitions the timesheet to `Draft`, re-enables entry editing for the Team Member, and records a mandatory rejection comment (reject button is disabled until at least 10 characters are entered)
- [ ] The Team Member can see the rejection comment in their weekly view
- [ ] Approved timesheets appear in the Financial Admin queue immediately
- [ ] A Team Lead cannot approve or reject timesheets outside their team scope; the attempt returns HTTP 403

**Tasks:**
- T2.2.a: Implement `ApprovalService.LeadApprove` and `ApprovalService.LeadReject` with team-scope guard, status transitions, and `ApprovalRecord` persistence
- T2.2.b: Build Blazor `TimesheetApprovalPanel` component with approve button, reject button + mandatory comment field, wired to scope-checked service calls

---

### F2.3: Overtime & After-Hours Validation (Epic E2)
**As a** Team Lead **I want** the system to require me to explicitly validate overtime entries before I can approve a timesheet **so that** non-standard hours are justified on record.

**Acceptance criteria:**
- [ ] A timesheet containing at least one "Review required" entry cannot be approved until each flagged entry has been individually validated by the Team Lead
- [ ] Validating a flagged entry requires a justification note of at least 10 characters; the validate button is disabled until the minimum is met
- [ ] After all flagged entries are validated the standard approve/reject flow (F2.2) proceeds unblocked
- [ ] `OvertimeValidatedBy`, `OvertimeValidationNote`, and `OvertimeValidatedAt` are persisted on the entry and visible in the audit trail

**Tasks:**
- T2.3.a: Implement `TimeEntryService.ValidateOvertime` persisting the three overtime fields and writing an audit event
- T2.3.b: Extend `TimesheetApprovalPanel` to surface flagged entries in a dedicated section with inline validation note fields; disable the approve button until all are resolved

---

### F2.4: Financial Admin Extra Approval Step (Epic E2)
**As a** Financial Admin **I want** to apply an additional approval step to Lead-approved timesheets **so that** sensitive billing records receive extra sign-off before final lock.

**Acceptance criteria:**
- [ ] All `ApprovedByLead` timesheets appear in the Financial Admin extra-approval queue
- [ ] Financial Admin can approve (transition to `FinancialApproved`) or reject (transition to `Draft`) with a mandatory comment on rejection
- [ ] On rejection the Team Member's entries become editable again and the rejection comment is visible to the submitter
- [ ] `FinancialApproved` status is distinct from `Locked`; a `FinancialApproved` timesheet is not yet immutable

**Tasks:**
- T2.4.a: Implement `ApprovalService.FinancialApprove` and `ApprovalService.FinancialReject` with status transitions and `ApprovalRecord` persistence
- T2.4.b: Build Blazor `FinancialApprovalQueuePage` listing all `ApprovedByLead` timesheets with approve/reject controls and mandatory comment field

---

### F3.1: Final Approval and Immutable Lock (Epic E3)
**As a** Financial Admin **I want** to lock a `FinancialApproved` timesheet **so that** billing-ready data cannot be changed by any role.

**Acceptance criteria:**
- [ ] Locking transitions the timesheet from `FinancialApproved` to `Locked`; `LockedAt` and `LockedBy` are recorded
- [ ] Any attempt to edit, delete, or change the status of a `Locked` entry or timesheet — by any role including Financial Admin — returns HTTP 403 with the message "This timesheet is locked and cannot be modified"
- [ ] Locked timesheets appear in the export-eligible pool immediately

**Tasks:**
- T3.1.a: Implement `ApprovalService.FinalLock` — transitions to `Locked`, persists `LockedAt`/`LockedBy`, and writes an audit event
- T3.1.b: Add lock guard to `TimeEntryService` and `WeeklyTimesheetService` — all mutating operations verify `IsLocked` before proceeding and throw `TimesheetLockedException` (mapped to HTTP 403) if true
- T3.1.c: Build Blazor `FinalLockQueuePage` listing `FinancialApproved` timesheets with a lock button

---

### F3.2: Export Locked Timesheet Data (Epic E3)
**As a** Financial Admin **I want** to export locked timesheet data in the billing template format **so that** the downstream billing system can process it without manual reformatting.

**Acceptance criteria:**
- [ ] The export contains only `Locked` entries; entries in any other status are excluded
- [ ] The CSV column structure matches the agreed billing template (see OQ-005)
- [ ] The Financial Admin can filter by date range (required) and optionally by project or team
- [ ] A download link is presented immediately after generation and the file is retained server-side for 24 hours
- [ ] Each export action is recorded in the audit trail with exporter ID, timestamp, and the filter parameters used

**Tasks:**
- T3.2.a: Implement `ExportService.GenerateLockedExport` — parameterised query over locked entries, streamed to CSV with billing template columns
- T3.2.b: Build Blazor `ExportPage` with date-range picker, optional project/team filters, and download button
- T3.2.c: Wire `AuditService.Record` into `ExportService` for every export generation

---

### F4.1: User and Team Management (Epic E4)
**As an** Administrator **I want** to create teams, assign Team Members, and designate Team Leads **so that** the approval workflow routes to the correct approver.

**Acceptance criteria:**
- [ ] An Admin can create a team with a name and an initial Team Lead drawn from in-scope users
- [ ] An Admin can add and remove Team Members from teams within their assigned scope
- [ ] A Team Member can belong to multiple teams; a Team Lead can lead multiple teams
- [ ] An Admin cannot assign users or modify teams outside their own project/team scope; the attempt returns HTTP 403
- [ ] Changes to team membership are reflected in the Team Lead review queue on next page load

**Tasks:**
- T4.1.a: Implement `TeamService.CreateTeam`, `AddMember`, `RemoveMember` with scope guards delegating to `ScopeGuard` (F4.3)
- T4.1.b: Build Blazor `TeamManagementPage` with team list, member assignment UI, and Team Lead selector

---

### F4.2: Project Creation and Assignment (Epic E4)
**As an** Administrator **I want** to create projects and assign users to them **so that** time entries reference valid, managed projects.

**Acceptance criteria:**
- [ ] An Admin can create a project with at minimum a Name and a Billing Type
- [ ] An Admin can assign users (Team Members and Team Leads) to a project within their scope
- [ ] The system supports an unlimited number of active projects
- [ ] A deactivated project no longer appears in the time entry project picker; existing entries referencing it are preserved
- [ ] An Admin cannot assign users to a project outside their own scope; the attempt returns HTTP 403

**Tasks:**
- T4.2.a: Implement `ProjectService.CreateProject`, `AssignUser`, `RemoveUser`, and `Deactivate` with scope guards
- T4.2.b: Build Blazor `ProjectManagementPage` with project CRUD and user-assignment panel

---

### F4.3: Scoped Admin Visibility and Edit Restrictions (Epic E4)
**As an** Administrator **I want** the system to restrict my visibility and edit access to only my assigned projects and users **so that** I cannot access commercially sensitive data outside my scope.

**Acceptance criteria:**
- [ ] An Admin querying users sees only users assigned to projects within their scope
- [ ] An Admin querying projects sees only their assigned projects; querying an out-of-scope project by ID returns HTTP 403
- [ ] An Admin attempting to mutate a user, team, or project outside their scope is blocked with HTTP 403
- [ ] A Financial Admin is exempt from scope restrictions and sees all users, projects, and timesheets
- [ ] Scope restrictions are enforced at the service layer; direct API calls are equally restricted

**Tasks:**
- T4.3.a: Implement `ScopeGuard` service — given current user identity and target entity, returns `Authorized` or throws `ScopeViolationException` (mapped to HTTP 403)
- T4.3.b: Integrate `ScopeGuard` into `ProjectService`, `TeamService`, and `UserService` on all read and mutate operations

---

### F5.1: Reconciliation Reports (Epic E5)
**As a** Financial Admin **I want** to generate daily, weekly, and monthly reconciliation reports over locked data **so that** I can support invoicing and financial audit.

**Acceptance criteria:**
- [ ] Reports cover only `Locked` entries
- [ ] Daily report: all locked entries for a selected date, grouped by user and project, with total hours per group
- [ ] Weekly report: all locked entries for a selected ISO week, grouped by user and project, with a daily breakdown and weekly totals
- [ ] Monthly report: all locked entries for a selected calendar month, grouped by project with per-user and project-level totals
- [ ] All three report types can be exported to CSV from the same page
- [ ] Any report covering up to 12 months of data renders within 30 seconds

**Tasks:**
- T5.1.a: Implement `ReportService.GetDailyReport`, `GetWeeklyReport`, `GetMonthlyReport` — parameterised queries over locked entries returning structured DTOs
- T5.1.b: Build Blazor `ReconciliationReportPage` with period-type selector, date picker, on-screen data grid, and CSV export button

---

### F5.2: Comprehensive Audit Trail (Epic E5)
**As a** Financial Admin **I want** to search a tamper-evident log of every action taken on timesheets and entries **so that** I can support compliance audits and investigate disputes.

**Acceptance criteria:**
- [ ] Every create, edit, delete, submit, approve (all levels), reject (all levels), lock, and export action is recorded with: actor user ID + display name, action type, target entity type + ID, before/after JSON snapshot, UTC timestamp, source IP, and correlation ID
- [ ] No role or service account can delete or modify an existing audit record; the table is append-only enforced at the database constraint level
- [ ] Financial Admin and System Admin can search by actor, date range, and action type
- [ ] Search results for queries spanning up to 1 year are returned within 5 seconds
- [ ] Audit records are retained for 7 years

**Tasks:**
- T5.2.a: Implement `AuditService.Record` writing immutable `AuditEvent` rows; add `NO DELETE` constraint and deny `UPDATE` permission to all application roles on the `AuditEvents` table
- T5.2.b: Wire `AuditService.Record` into all service methods that create, mutate, transition, lock, or export data
- T5.2.c: Build Blazor `AuditLogPage` with search filters (actor, date range, action type) and paginated results

---

### F6.1: Ticket Reference Validation (Epic E6)
**As a** System Admin **I want** time entries with a ticket reference to be validated against DevOps, JIRA, or Linear **so that** only valid ticket IDs enter the billing record and time is traceable to work items.

**Acceptance criteria:**
- [ ] On entering a ticket reference, the system detects the target system by prefix or per-project default and calls the relevant adapter
- [ ] A valid reference shows a green confirmation indicator next to the field within 3 seconds
- [ ] An invalid reference shows "Ticket not found in {system}" but does not prevent saving or submission
- [ ] If the integration is offline, the entry is saved with `TicketValidationStatus = Pending`; a background job retries every 15 minutes and updates the status on success or marks it `Failed` after 3 attempts
- [ ] System Admin can view a page listing all entries with `Pending` or `Failed` ticket validation status, with the ability to manually mark a reference valid

**Tasks:**
- T6.1.a: Implement `IntegrationService` with adapters for Azure DevOps, JIRA, and Linear; each adapter implements `ITicketValidator.ValidateAsync(ticketRef)`
- T6.1.b: Implement offline fallback: save entry with `TicketValidationStatus = Pending` and a background retry job using a hosted service or Azure Functions trigger
- T6.1.c: Build Blazor `IntegrationStatusPage` for System Admin showing pending/failed validations, integration health indicators, and manual-approve controls

---

## 5. Non-Functional Requirements

### 5.1 Performance
- **Server response time** (API): p50 < 100ms, p95 < 300ms, p99 < 800ms
- **Page load (TTI)**: < 3s on broadband (includes Blazor Server SignalR circuit establishment)
- **Throughput**: sustained 200 RPS; peak 600 RPS
- **Batch / background jobs**: reconciliation report generation < 30s; CSV export generation < 60s; ticket validation retry job runs every 15 minutes with per-job timeout of 5 minutes

### 5.2 Scalability
- **Concurrent users**: 500 at launch; 2,000 at 12 months
- **Data volume**: ~5M timesheet entry rows at 3-year mark (500 users × 5 entries/day × 260 working days × 7-year retention); ~25GB total database at 3 years including audit log
- **Geographic distribution**: single-region (Azure South Africa North, Johannesburg)

### 5.3 Availability & Reliability
- **Uptime SLA**: 99.5% (~3.6 hours downtime/month) [DEFAULT — see OQ-003]
- **Planned maintenance windows**: weekends 00:00–06:00 SAST
- **Recovery objectives**: RTO = 4 hours, RPO = 24 hours [DEFAULT — see OQ-004]
- **Disaster recovery scope**: automated daily Azure SQL backups retained 35 days; geo-redundant storage; monthly restore test documented in runbook

### 5.4 Security & Compliance
- **Authentication mechanism**: Azure Entra ID (SSO); ASP.NET Core Identity federated to Entra
- **Multi-factor**: Required — enforced via Entra Conditional Access policy
- **Data classification**: Confidential — employee names, daily work hours, project names, billing rate / amounts, ticket references, approval comments
- **Regulatory regimes**: POPIA (South Africa), GDPR (EU) [legal confirmation required — see OQ-002]
- **Data residency**: Azure South Africa North; no data transferred outside South Africa without explicit consent and legal review
- **Encryption**: TLS 1.2 minimum in transit; Azure SQL Transparent Data Encryption at rest; Azure Key Vault for all secrets
- **Session policy**: idle timeout 30 minutes; absolute session timeout 8 hours [DEFAULT — see OQ-010]
- **Password policy**: delegated to Entra ID; local passwords not supported
- **Secret handling**: Azure Key Vault (connection strings, integration API keys, signing certificates); no secrets in app config files or source control

### 5.5 Auditing
- **Audited actions**: time entry created / edited / deleted; timesheet submitted / approved (lead) / rejected (lead) / financial-approved / financial-rejected / locked; user assigned / removed from team or project; project created / modified / deactivated; data exported; login / logout; failed login attempts
- **Audit record fields**: actor (user ID + display name), action type, target entity (type + ID), before-state JSON, after-state JSON, UTC timestamp, source IP, correlation ID
- **Retention**: 7 years (South Africa Companies Act financial record requirement; satisfies POPIA data-trail obligation)
- **Immutability**: append-only; `DELETE` and `UPDATE` permissions on the `AuditEvents` table are revoked from all application roles and service accounts
- **Audit log readers**: Financial Admin, System Admin

### 5.6 Observability
- **Production log level**: Warning; Information for authentication events and approval state-transition events
- **Must be logged**: successful/failed logins, all timesheet status transitions, lock events, export downloads, integration validation failures, background job start/completion/failure
- **MUST NEVER be logged**: passwords, tokens, session IDs, the text content of time entry Notes (may be commercially sensitive), ticket reference content in plain-text form
- **Log retention**: 30 days hot (Application Insights); 12 months cold (Azure Storage Archive tier)
- **Required metrics**: daily active users; timesheets submitted this week; approval queue depth per stage (Submitted, ApprovedByLead, FinancialApproved); export generation count per day; integration validation failure rate
- **Alerts**: application error rate > 1% over any 5-minute window; approval queue depth > 100 items for > 48 hours; export service failure; any integration adapter offline > 30 minutes; Azure SQL DTU/CPU > 80% for > 10 minutes sustained
- **Tracing**: required — Application Insights distributed tracing across all service calls and background jobs

### 5.7 Accessibility
- **Target standard**: WCAG 2.1 AA
- **Keyboard-only navigation**: required for all core flows (time entry, submission, approval, lock, export)
- **Screen-reader support**: required; all Blazor components must carry ARIA labels and roles
- **Notes**: no specific accessibility accommodation requirements known at this stage; AA baseline provides broad coverage

### 5.8 Browser & Device Support
- **Browsers**: Chrome 120+, Edge 120+, Firefox 120+, Safari 17+ [DEFAULT — see OQ-011]
- **Mobile**: responsive layout required (daily time entry expected from mobile devices); no native iOS/Android app in scope
- **Offline capability**: not required — Blazor Server requires a live SignalR circuit
- **Minimum viewport width**: 320px

### 5.9 Localisation & Internationalisation
- **Launch languages**: English (South African)
- **Future languages**: none planned
- **Date/number/currency formats**: South African — dd/MM/yyyy dates; 24-hour clock; ZAR (R) currency where monetary values are displayed; space as thousands separator per SANS standard
- **Right-to-left support**: not required

### 5.10 Maintainability & Delivery
- **Code coverage minimum**: 80% on service-layer classes; 60% overall project
- **Build time budget**: < 5 minutes on CI
- **Deployment frequency target**: multiple times per day to dev; weekly to production
- **Documentation**: README covering local setup and database migration; runbook covering deployment, Azure backup restore procedure, and integration credential rotation

### 5.11 Cost Constraints
- **Monthly Azure budget**: no cap set — stakeholders explicitly confirmed no fixed budget limit; architect must present a SKU-level cost estimate for dev and prod and Financial Admin must approve it before infrastructure is provisioned [see OQ-001]
- **Per-user cost target**: not applicable at this stage; to be derived once the architect's cost estimate is approved

---

## 6. Out of Scope
- Payroll processing — no integration with payroll systems
- Invoice generation — this system feeds a downstream billing tool; invoice creation is that tool's responsibility
- Resource allocation and capacity planning — future scope (ST-018); stakeholder scheduling required before it is included
- Native mobile applications (iOS / Android)
- Offline time entry — Blazor Server requires an active circuit
- Multi-currency billing — ZAR only at launch
- Integration with HR leave management systems — leave/holiday partial-week flags are set manually by the Team Member at submission time (see OQ-007 for whether v2 should integrate)
- Self-service user registration — all user accounts are provisioned via Azure Entra ID by IT

---

## 7. Open Questions
- OQ-001: **Monthly Azure infrastructure budget** — stakeholders confirmed no fixed cap. Architect must present SKU-level cost estimates for dev and prod environments; Financial Admin must approve before infrastructure is provisioned.
- OQ-002: **Compliance legal sign-off** — POPIA (SA) and GDPR (EU) identified as applicable based on South African operation and potential EU client exposure. Legal/compliance team must formally confirm scope, identify any additional regimes (e.g. ISAE 3402, SOC 2 Type II), and confirm data residency obligations before the security architecture is finalised. [NFR default applied: §5.4 — regimes assumed from context]
- OQ-003: **Uptime SLA** — defaulted to 99.5% (~3.6 hours/month). Stakeholders must confirm or upgrade before infrastructure architecture is finalised. [NFR default applied: §5.3 — 99.5%]
- OQ-004: **RTO / RPO targets** — defaulted to RTO 4 hours / RPO 24 hours for internal-tooling tier. Financial Admin should confirm whether billing data criticality requires a tighter RPO (e.g. 1 hour). [NFR default applied: §5.3 — RTO 4h, RPO 24h]
- OQ-005: **Export billing template format** — the spec requires export to match "the billing template format" but the exact CSV column names, order, and data types have not been provided. Financial Admin (Deon/Herman) must supply or approve the template before F3.2 can be implemented.
- OQ-006: **Overtime / hours threshold value** — the threshold above which a single time entry is flagged "Review required" (e.g. > 10 hours/day, or > 45 hours/week) must be confirmed by Financial Admin and Team Leads. Confirm whether this threshold is a global constant or configurable per project.
- OQ-007: **Leave and public holiday source** — the system must distinguish genuine missing days from approved leave / public holidays at submission time. Currently this is a manual acknowledgement by the Team Member. Confirm whether v1 requires an admin-managed leave calendar or integration with an HR system, or whether the manual flag approach is acceptable.
- OQ-008: **Admin scope assignment mechanism** — how are Administrators initially assigned to projects and users? Who performs this assignment (Financial Admin only, or also System Admin)? What happens to an Admin's in-scope data if they are removed from a project?
- OQ-009: **Integration credentials (DevOps / JIRA / Linear)** — credential type (PAT, OAuth2 service account, or API key) and rotation policy for each integration must be confirmed with the System Admin before F6.1 can be designed.
- OQ-010: **Concurrent session policy** — defaulted to single concurrent session per user. If users are expected to access the system from both desktop and mobile simultaneously this default must be revised. [NFR default applied: §5.4 — single concurrent session]
- OQ-011: **Browser support matrix** — defaulted to latest stable Chrome, Edge, Firefox, Safari. If any users run locked-down enterprise browser versions (e.g. IE compatibility mode, older Safari on managed iPads) this must be confirmed before UI implementation. [NFR default applied: §5.8]
- OQ-012: **Partial-week leave flag UI** — how does a Team Member indicate that a missing weekday is due to leave or a public holiday? Options: (a) a per-day toggle in the weekly view; (b) a free-text acknowledgement note at submission time; (c) a leave-type selector per day. Confirm preferred UX with Financial Admin and Team Leads before F1.2 is built.
