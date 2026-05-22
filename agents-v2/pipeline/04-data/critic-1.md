# Data Critic — Iteration 1

## Feature coverage walk

- F1.1: Daily Time Entry Logging → entities `TimeEntry`, `WeeklyTimesheet`, `Project` + `UQ_TimeEntry_UserId_Date_ProjectId` (unique, enforces no-duplicate at DB level), `FlaggedForReview` bool, `Notes`/`TaskType`/`TicketRef` columns present — PASS
- F1.2: Pre-Submission Entry Validation → `WeeklyTimesheet.Status` (Draft/Submitted) present; **FAIL** — F1.2 AC2 states "a week where all missing days are covered by a leave or public holiday flag allows submission without a missing-days warning", yet the design contains no per-day leave/holiday flag anywhere. No `LeaveDay` join table, no `IsLeave`/`IsHoliday` boolean on a day-level entity, no `DayFlag` column. Architecture §8 OQ-6 explicitly asked the Data Designer to choose a representation; the design does not answer it. Without a data model for leave/holiday flags the acceptance criterion for AC2 cannot be implemented.
- F1.3: Weekly Timesheet Submission → `WeeklyTimesheet.Status` enum (`Draft=0..Locked=4`), `SubmittedAt` DateTimeOffset nullable, entry-edit lock enforced at service layer — PASS
- F2.1: Team Lead Review Queue → `UserTeam` with `UserTeamRole` (Member/Lead), `IX_UserTeam_UserId_Role` filtered on `Role=Lead`, `IX_WeeklyTimesheet_Status_SubmittedAt` filtered on `Status=Submitted`, `IX_UserTeam_TeamId_UserId` covering — PASS
- F2.2: Team Lead Approve / Reject → `ApprovalRecord` with `ApprovalAction` (LeadApproved/LeadRejected), `WeeklyTimesheet.LastRejectionComment`/`LastRejectedByUserId`/`LastRejectedAt` denorm fields, `IX_ApprovalRecord_TimesheetId_Timestamp` — PASS
- F2.3: Overtime & After-Hours Validation → `TimeEntry.OvertimeValidatedById`, `OvertimeValidationNote`, `OvertimeValidatedAt` present; `FlaggedForReview` bool; FK `OvertimeValidatedById → ApplicationUser (NoAction)` — PASS
- F2.4: Financial Admin Extra Approval Step → `TimesheetStatus.FinancialApproved` (value 3), `ApprovalAction.FinancialApproved`/`FinancialRejected`, `IX_WeeklyTimesheet_Status_LeadApproved` filtered on `Status=2` — PASS
- F3.1: Final Approval and Immutable Lock → `TimesheetStatus.Locked` (value 4), `WeeklyTimesheet.LockedAt`, `WeeklyTimesheet.LockedById`, `RowVersion` for concurrent-lock detection, `IX_WeeklyTimesheet_Status_FinancialApproved` filtered on `Status=3` — PASS
- F3.2: Export Locked Timesheet Data → `IX_TimeEntry_Date_WeeklyTimesheetId` (INCLUDE covers UserId, ProjectId, Hours, Notes, TaskType, TicketRef), `IX_WeeklyTimesheet_Locked` filtered on `Status=4`, `AuditEvent` records export action — PASS
- F4.1: User and Team Management → `Team`, `UserTeam` with `Role` (Member/Lead), `Team.RowVersion` for concurrent membership edits — PASS
- F4.2: Project Creation and Assignment → `Project` with `IsActive` (soft-deactivation), `ProjectUser`, `BillingType` enum, `IX_ProjectUser_ProjectId_UserId` — PASS
- F4.3: Scoped Admin Visibility → `AdminProjectScope` (AdminUserId, ProjectId), `IX_AdminProjectScope_AdminUserId` covering ProjectId — PASS
- F5.1: Reconciliation Reports → locked entries queryable via `IX_WeeklyTimesheet_Locked`; `TimeEntry` has `Date`, `Hours`, `UserId`, `ProjectId`; `WeeklyTimesheet` has `WeekStart`/`WeekEnd` for period grouping — PASS
- F5.2: Comprehensive Audit Trail → `AuditEvent` with all required fields: `ActorId`, `ActorDisplayName`, `ActionType`, `ResourceType`, `ResourceId`, `BeforeJson`, `AfterJson`, `TimestampUtc`, `SourceIp`, `CorrelationId`; `IX_AuditEvent_TimestampUtc_ActionType` (clustered, keyset cursor), `IX_AuditEvent_ActorId_TimestampUtc`; append-only enforced via SQL DENY per §2/§8 — PASS
- F6.1: Ticket Reference Validation → `TimeEntry.TicketValidationStatus` enum (None/Pending/Found/NotFound/Failed/ManuallyApproved), `IX_TimeEntry_TicketValidationStatus` filtered on `Status IN (1,4)` (Pending, Failed) — PASS

---

## Index audit

- `IX_WeeklyTimesheet_UserId_WeekStart` (UNIQUE): covers weekly view load for a user — PASS
- `IX_WeeklyTimesheet_Status_SubmittedAt` (filtered Status=1, INCLUDE Id/UserId/WeekStart/WeekEnd): covers Team Lead queue hot path — PASS
- `IX_UserTeam_UserId_Role` (filtered Role=1, INCLUDE TeamId): covers "find teams where current user is Lead" — PASS
- `IX_UserTeam_TeamId_UserId` (covering): covers team membership lookup for approval-queue scope JOIN — PASS
- `IX_WeeklyTimesheet_Status_LeadApproved` (filtered Status=2, INCLUDE Id/UserId/WeekStart/WeekEnd/SubmittedAt): covers Financial Admin extra-approval queue — PASS
- `IX_WeeklyTimesheet_Status_FinancialApproved` (filtered Status=3, INCLUDE Id/UserId/WeekStart/WeekEnd): covers final-lock queue — PASS
- `IX_TimeEntry_WeeklyTimesheetId`: covers entry list for approval detail — PASS
- `UQ_TimeEntry_UserId_Date_ProjectId` / `IX_TimeEntry_UserId_Date_ProjectId` (UNIQUE): covers duplicate-entry prevention constraint — PASS
- `IX_TimeEntry_Date_WeeklyTimesheetId` (INCLUDE covering): covers locked export by date range — PASS
- `IX_WeeklyTimesheet_Locked` (filtered Status=4, INCLUDE): covers report and export queries over locked data — PASS
- `IX_TimeEntry_TicketValidationStatus` (filtered Pending/Failed, INCLUDE Id/TicketRef/Date/UserId): covers System Admin validation queue — PASS
- `IX_AdminProjectScope_AdminUserId` (INCLUDE ProjectId): covers scope-check on every admin request — PASS
- `IX_ProjectUser_ProjectId_UserId` (covering): covers admin user-list page — PASS
- `IX_ApprovalRecord_TimesheetId_Timestamp` (DESC): covers latest-rejection lookup for LastRejection* denorm sync — PASS
- `IX_AuditEvent_TimestampUtc_ActionType` (clustered): covers audit log date-range + action-type search with keyset pagination — PASS
- `IX_AuditEvent_ActorId_TimestampUtc` (INCLUDE ActionType/ResourceType/ResourceId): covers actor-filtered audit search — PASS
- F3.2 optional project/team filter on export — no dedicated `IX_TimeEntry_ProjectId_Date` index; the covering INCLUDE on `IX_TimeEntry_Date_WeeklyTimesheetId` contains ProjectId so an index seek on Date + post-filter on ProjectId is acceptable at projected data volumes — GAP (non-blocking; note below)
- F5.1 monthly report grouping by project — relies on `IX_TimeEntry_Date_WeeklyTimesheetId` (INCLUDE ProjectId); no `IX_TimeEntry_ProjectId_Date` for project-first grouping — GAP (non-blocking; note below)

---

## Constraints & security audit

- **Cascade cycles:** All multi-FK tables explicitly use NoAction/Restrict for the cycle-breaking path. WeeklyTimesheet has three FKs to ApplicationUser (UserId Restrict, LockedById NoAction, LastRejectedByUserId NoAction) — zero cascades from that table to User. TimeEntry has two FKs to ApplicationUser (UserId NoAction, OvertimeValidatedById NoAction) — zero cascades. No SQL Server cascade-cycle risk — PASS
- **Money columns:** No monetary amount columns exist in the schema (timesheet is hours-only; billing amounts live in the downstream tool). TimeEntry.Hours is `decimal(5,2)` — PASS
- **PII / sensitive fields:** Passwords managed exclusively by ASP.NET Core Identity (hashed, never in domain entities). Integration API keys stored in Azure Key Vault via Managed Identity (not in any entity). `DisplayName` and `Email` on ApplicationUser are marked Confidential but are application-level PII, not credentials — appropriately handled. Notes/TicketRef flagged Confidential with explicit prohibition on Application Insights logging. AuditEvent.ActorDisplayName is denormalised plain text (name, not a secret) — acceptable for audit trail durability. No unhashed secrets or tokens found in any entity — PASS
- **N+1 hazards:** All three documented query patterns (approval queue, export, audit log) use LINQ `.Select()` projection to DTOs. TotalHours and FlaggedCount in Query 1 computed via `SUM`/`COUNT` subqueries within a single query — no row-by-row round-trip. §8 OQ-1 explicitly flags these subqueries as a potential hot spot and proposes denormalisation if load testing shows degradation. No N+1 patterns found — PASS
- **Naming consistency:** All FK columns follow `<Entity>Id` convention (UserId, TeamId, ProjectId, WeeklyTimesheetId, ApproverId, TimesheetId, ActorId, AdminUserId, LockedById, OvertimeValidatedById). All entity class names are PascalCase. Enum names are PascalCase. DbSet names are plural by EF Core convention. Minor: `ApprovalRecord.TimesheetId` could be named `WeeklyTimesheetId` for consistency with the FK target name — non-blocking — PASS overall

---

## Required fixes (BLOCKED)

1. **Add a leave/holiday day flag data model to resolve F1.2 AC2.** The acceptance criterion "a week where all missing days are covered by a leave or public holiday flag allows submission without a missing-days warning" requires a per-day record of leave/holiday status for each Team Member. Architecture §8 OQ-6 asked the Data Designer to choose a representation; the design does not provide one. The minimum required addition is either:
   - A `DayFlag` join table: `(UserId PK/FK, Date PK DateOnly, FlagType enum{Leave, Holiday})` — flexible and forward-compatible with an HR integration (OQ-007); or
   - A boolean pair on a new `TimesheetDayNote` entity linked to `WeeklyTimesheet`.
   
   Without this, `WeeklyTimesheetService.ValidateForSubmission` (T1.2.a) cannot distinguish a genuinely missing day from an approved absence, making F1.2 AC2 unimplementable.

---

## Notes (non-blocking)

- **`IX_TimeEntry_ProjectId_Date` missing for project-filtered export and monthly reports (F3.2, F5.1):** Export (F3.2) supports optional project filter; monthly report (F5.1) groups by project. At the projected 5M row volume, relying on `IX_TimeEntry_Date_WeeklyTimesheetId` with post-filter on ProjectId may cause key-lookup pressure. Consider adding a non-clustered `IX_TimeEntry_ProjectId_Date` with INCLUDE `(WeeklyTimesheetId, Hours, UserId)` in a follow-on migration once load testing identifies the need.

- **`TotalHours`/`FlaggedCount` compute-vs-denorm (§8 OQ-1):** Design already flags this as an open question. At 2 000 concurrent submitters, `SUM(Hours)` and `COUNT(FlaggedForReview)` subqueries per approval-queue row will scale gracefully up to a few hundred items in the queue, but consider adding `WeeklyTimesheet.TotalHours decimal(7,2)` and `WeeklyTimesheet.FlaggedEntryCount int` maintained by `TimeEntryService` if load tests show p95 degradation on `/approvals`.

- **`ApprovalRecord.TimesheetId` naming nit:** The FK targets `WeeklyTimesheet` but the column is named `TimesheetId` rather than `WeeklyTimesheetId`. Both are unambiguous in context (only one timesheet entity exists), but `WeeklyTimesheetId` would be strictly consistent with the FK naming convention used everywhere else.

- **`WeeklyTimesheet.LastRejectionComment` denorm width:** `MaxLength(1000)` matches `ApprovalRecord.Comment`. If a Financial Admin rejection comment is expected to be longer than 1 000 characters, both columns should be widened consistently. No spec constraint found — note for Backend Dev to confirm with stakeholders.

- **`Team.TeamLeadId` vs `UserTeam.Role=Lead` dual representation:** A Team Lead is represented both as `Team.TeamLeadId` (scalar FK) and via `UserTeam` rows with `Role=Lead`. This is not a normalisation violation (they serve different query purposes), but the Backend Dev must keep them in sync on team-lead reassignment to avoid the Team Lead queue silently routing to a stale lead.

- **`AuditEvent` SQL DENY in migration:** §8 OQ-8 notes the migration must include `DENY UPDATE, DELETE ON AuditEvents TO [app-service-account]`. Confirm the exact SQL Server principal name before the migration is written; if the login name is wrong the DENY is silently a no-op.

- **Forward-only migrations:** The decision to not implement `Down()` bodies is recorded. Ensure the CI pipeline does not attempt `dotnet ef database update --target <previous>` in rollback scenarios; the runbook should explicitly state point-in-time restore as the only rollback mechanism.

VERDICT: BLOCKED
