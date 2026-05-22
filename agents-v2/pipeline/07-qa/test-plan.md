# Test Plan — Timesheet & Billing Workflow System

## 1. Scope

**In scope:**
- F1.1 Daily Time Entry Logging
- F1.2 Pre-Submission Entry Validation
- F1.3 Weekly Timesheet Submission
- F2.1 Team Lead Review Queue
- F2.2 Team Lead Approve / Reject
- F2.3 Overtime & After-Hours Validation
- F2.4 Financial Admin Extra Approval Step
- F3.1 Final Approval and Immutable Lock
- F3.2 Export Locked Timesheet Data
- F4.1 User and Team Management
- F4.2 Project Creation and Assignment
- F4.3 Scoped Admin Visibility and Edit Restrictions
- F5.1 Reconciliation Reports
- F5.2 Comprehensive Audit Trail
- F6.1 Ticket Reference Validation

**Out of scope (this iteration):**
- Performance / load testing (deferred — see §8)
- Security penetration test (covered by phase 8 security review)
- Cross-browser coverage beyond Chromium + WebKit
- Native mobile (out of scope per spec §6)
- bUnit component tests (no bUnit package yet — deferred to integration phase)
- Full F1.1 entry-create E2E (TimeEntryFormModal stub in frontend iteration 1)
- Full F1.2 acknowledgement-checkbox flow (SubmissionReviewModal stub in frontend iteration 1)
- Full F2.3 overtime justification form (deferred to frontend iteration 2)

---

## 2. Test levels

| Level | Coverage | Tooling | Owner |
|---|---|---|---|
| Unit | Service methods, helpers, validators | xUnit | Backend Dev |
| Integration | EF Core + repos, service + DB | xUnit + SQLite in-memory | Backend Dev |
| Component | Blazor components (three-state, EditForm wiring) | bUnit (pending package setup) | Frontend Dev |
| E2E | Persona journeys, auth guards, RBAC, form validation | Playwright | QA (this phase) |
| Manual | Exploratory, accessibility, keyboard-only, screen reader | Checklist below (§7) | Human |

---

## 3. Acceptance-criteria → test traceability matrix

| Feature | AC description (condensed) | Test type | Test ID | Project |
|---|---|---|---|---|
| F1.1 | Saving without Date/Project/Hours prevented; field-level message | Unit + E2E | `TimeEntryServiceTests.CreateEntry_MissingFields_ReturnsError`; `team-member-journey` (gap — modal stub) | Unit / E2E |
| F1.1 | Saving without Notes prevented with "Notes are required" | Unit | `TimeEntryServiceTests.CreateEntry_MissingNotes_ReturnsError` | Unit |
| F1.1 | Hours > 10 saved but flagged "Review required" | Unit | `TimeEntryServiceTests.CreateEntry_HighHours_FlagsEntry` | Unit |
| F1.1 | Duplicate user+date+project blocked | Unit | `TimeEntryServiceTests.CreateEntry_Duplicate_ReturnsError` | Unit |
| F1.1 | Task Type and Ticket Ref optional, persisted and displayed | Integration | `WeeklyTimesheetServiceTests.GetWeeklyTimesheet_ReturnsOptionalFields` | Integration |
| F1.1 | Saved entry appears in weekly view as Draft | E2E | `team-member-journey` — "entry appears in grid" (gap — modal stub) | E2E |
| F1.2 | Missing-days warning + acknowledgement gate on submit | E2E | `team-member-journey` — submission modal (gap — SubmissionReviewModal stub) | E2E |
| F1.2 | Leave/holiday flag allows submission without warning | Unit | `WeeklyTimesheetServiceTests.ValidateForSubmission_AllDaysFlagged_NoWarning` | Unit |
| F1.2 | Flagged entries show warning; submit still possible after ack | E2E | `team-member-journey` (gap — modal stub) | E2E |
| F1.2 | Zero entries: submit permanently disabled | E2E | `team-member-journey` — "submit disabled when no entries" | E2E |
| F1.3 | Submission transitions Draft → Submitted; SubmittedAt recorded | Unit | `WeeklyTimesheetServiceTests.Submit_TransitionsToSubmitted` | Unit |
| F1.3 | Edit/delete hidden after Submitted; API returns 403 | Unit + E2E | `TimeEntryServiceTests.DeleteEntry_SubmittedTimesheet_Returns403` | Unit |
| F1.3 | Confirmation message shown | E2E | `team-member-journey` — "on-screen confirmation" (gap — modal stub) | E2E |
| F1.3 | Submitted timesheet appears in TeamLead queue | E2E | `team-lead-journey` — "approval queue shows rows" | E2E |
| F2.1 | Queue shows only user's team timesheets | Unit | `ApprovalServiceTests.GetPendingForLead_ScopedToTeam` | Unit |
| F2.1 | Sorted by SubmittedAt ascending | Unit | `ApprovalServiceTests.GetPendingForLead_SortedOldestFirst` | Unit |
| F2.1 | Row displays: name, week, hours, flagged count, status | E2E | `team-lead-journey` — "approvals queue renders" | E2E |
| F2.1 | Row click navigates to detail | E2E | `team-lead-journey` — "approval detail page renders" | E2E |
| F2.2 | Approve transitions Submitted → ApprovedByLead; ApprovalRecord persisted | Unit | `ApprovalServiceTests.LeadApprove_TransitionsToApprovedByLead` | Unit |
| F2.2 | Reject transitions to Draft; edit re-enabled; comment mandatory (min 10 chars) | Unit + E2E | `ApprovalServiceTests.LeadReject_TransitionsToDraft`; `team-lead-journey` — "reject form validates min 10 chars" | Unit + E2E |
| F2.2 | Rejection comment visible to Team Member | E2E | `team-member-journey` — "rejection banner visible" (gap — needs seeded rejected timesheet) | E2E |
| F2.2 | Approved appears in Financial Admin queue | E2E | `financial-admin-journey` — "financial queue shows data" | E2E |
| F2.2 | TeamLead cannot approve outside team scope (403) | Unit | `ApprovalServiceTests.LeadApprove_OutOfScope_Returns403` | Unit |
| F2.3 | Flagged timesheet cannot be approved until all entries validated | Unit | `ApprovalServiceTests.LeadApprove_FlaggedEntriesNotValidated_Blocked` | Unit |
| F2.3 | Validate requires min 10 char justification | Unit | `TimeEntryServiceTests.ValidateOvertime_ShortNote_ReturnsError` | Unit |
| F2.3 | After all validated, approve/reject unblocked | Unit | `ApprovalServiceTests.LeadApprove_AllValidated_Succeeds` | Unit |
| F2.3 | Overtime fields persisted and in audit trail | Unit | `TimeEntryServiceTests.ValidateOvertime_PersistsFields` | Unit |
| F2.4 | ApprovedByLead timesheets in Financial queue | Unit + E2E | `ApprovalServiceTests.FinancialApprove_QueueContainsApprovedByLead`; `financial-admin-journey` | Unit + E2E |
| F2.4 | Financial Approve → FinancialApproved; Financial Reject → Draft | Unit | `ApprovalServiceTests.FinancialApprove_Transitions`; `FinancialReject_Transitions` | Unit |
| F2.4 | Reject comment mandatory; Team Member sees it | Unit + E2E | `financial-admin-journey` — "reject comment required" | Unit + E2E |
| F2.4 | FinancialApproved is not Locked | Unit | `ApprovalServiceTests.FinancialApprove_StatusIsNotLocked` | Unit |
| F3.1 | Lock transitions FinancialApproved → Locked; LockedAt/LockedBy recorded | Unit | `ApprovalServiceTests.FinalLock_TransitionsToLocked` | Unit |
| F3.1 | Any edit/delete/status-change on Locked returns 403 | Unit | `TimeEntryServiceTests.Edit_LockedTimesheet_Returns403` | Unit |
| F3.1 | Locked timesheets in export pool immediately | Unit | `ExportServiceTests.ExportLockedEntries_IncludesLocked` | Unit |
| F3.2 | Export contains only Locked entries | Unit | `ExportServiceTests.ExportLockedEntries_ExcludesNonLocked` | Unit |
| F3.2 | CSV column structure matches billing template | Unit | `ExportServiceTests.BuildCsv_ColumnOrder` | Unit |
| F3.2 | Filter by date range (required) | E2E | `financial-admin-journey` — "export page filter renders" | E2E |
| F3.2 | Download link presented; file retained 24h | E2E | `financial-admin-journey` — download (gap — JS interop stub) | E2E |
| F3.2 | Export action in audit trail | Unit | `ExportServiceTests.ExportLockedEntries_WritesAuditEvent` | Unit |
| F4.1 | Admin creates team with name + TeamLead | E2E | `administrator-journey` — "create team" (gap — create-team page deferred) | E2E |
| F4.1 | Admin adds/removes members | E2E | `administrator-journey` — "team detail renders; remove member" | E2E |
| F4.1 | Team Member can belong to multiple teams | Unit | `AdminServiceTests.AddMember_MultipleTeams_Allowed` | Unit |
| F4.1 | Admin cannot modify teams outside scope (403) | Unit | `AdminServiceTests.UpdateTeam_OutOfScope_Returns403` | Unit |
| F4.1 | Team membership reflected in TeamLead queue on next load | E2E | `team-lead-journey` — implicit via queue scope test | E2E |
| F4.2 | Admin creates project with Name + BillingType | E2E | `administrator-journey` — "create project" (gap — create-project page deferred) | E2E |
| F4.2 | Admin assigns users to project | E2E | `administrator-journey` — "project detail renders" | E2E |
| F4.2 | Unlimited active projects supported | Unit | `AdminServiceTests.GetProjects_ReturnsAll` (design assertion) | Unit |
| F4.2 | Deactivated project absent from time-entry picker | Unit | `AdminServiceTests.DeactivateProject_ExcludedFromPicker` | Unit |
| F4.2 | Admin cannot assign users outside scope (403) | Unit | `AdminServiceTests.AssignUserToProject_OutOfScope_Returns403` | Unit |
| F4.3 | Admin sees only in-scope users | Unit | `AdminServiceTests.GetUsers_ScopedToAdmin` | Unit |
| F4.3 | Admin querying out-of-scope project returns 403 | Unit | `AdminServiceTests.GetProject_OutOfScope_Returns403` | Unit |
| F4.3 | Admin mutating out-of-scope entity blocked (403) | Unit | `AdminServiceTests.UpdateProject_OutOfScope_Returns403` | Unit |
| F4.3 | Financial Admin exempt from scope restrictions | Unit | `AdminServiceTests.GetAllProjects_FinancialAdmin_NoFilter` | Unit |
| F4.3 | Scope enforced at service layer; direct API calls restricted | Unit | `ScopeGuardTests.ThrowsOnViolation` | Unit |
| F5.1 | Reports cover only Locked entries | Unit | `ReportServiceTests.GetReport_ExcludesNonLocked` | Unit |
| F5.1 | Daily report: grouped by user+project, total hours | Unit | `ReportServiceTests.GetDailyReport_GroupsCorrectly` | Unit |
| F5.1 | Weekly report: ISO week, daily breakdown, totals | Unit | `ReportServiceTests.GetWeeklyReport_IncludesDailyBreakdown` | Unit |
| F5.1 | Monthly report: grouped by project, per-user + project totals | Unit | `ReportServiceTests.GetMonthlyReport_GroupsCorrectly` | Unit |
| F5.1 | All three report types exportable to CSV | E2E | `financial-admin-journey` — "reports page renders; generate report" | E2E |
| F5.1 | ≤12-month report renders within 30s | E2E | `financial-admin-journey` — "reports page render time" (manual NFR check) | Manual |
| F5.2 | Every action recorded with full fields | Unit | `AuditServiceTests.Log_RecordsAllFields` | Unit |
| F5.2 | No role can delete or modify AuditEvents | Unit | `AuditServiceTests.AuditEvents_AppendOnly` (DB constraint test) | Unit |
| F5.2 | FinancialAdmin/SystemAdmin can search by actor, date, action | E2E | `financial-admin-journey` / `system-admin-journey` — "audit search" | E2E |
| F5.2 | ≤1-year query returns within 5s | Manual | Manual performance check against dev dataset | Manual |
| F5.2 | Records retained 7 years | Manual | Verify retention policy configuration | Manual |
| F6.1 | Ticket ref validated by prefix-detected adapter | Unit | `TicketValidationServiceTests.Validate_DetectsAdapterByPrefix` | Unit |
| F6.1 | Valid ref shows green indicator within 3s | E2E | `team-member-journey` — validation indicator (gap — modal stub) | E2E |
| F6.1 | Invalid ref shows "Ticket not found" without blocking save | Unit | `TicketValidationServiceTests.Validate_InvalidRef_DoesNotBlockSave` | Unit |
| F6.1 | Offline: entry saved as Pending; retry every 15 min; 3 attempts then Failed | Unit | `TicketValidationBackgroundServiceTests.Retry_FailsAfterThreeAttempts` | Unit |
| F6.1 | SysAdmin can view Pending/Failed; manually approve or retry | E2E | `system-admin-journey` — "integrations page renders" | E2E |

---

## 4. Personas → E2E journey

- **Team Member**: dev-login → `/timesheet/{currentWeek}` → assert page structure, empty state, submit disabled → assert role-scoped nav → auth guard on `/approvals` → a11y check
- **Team Lead**: dev-login → `/approvals` → assert queue page renders → assert detail page renders (conditional on seed data) → assert reject form min-10-char validation → auth guard on `/financial/queue` → a11y check
- **Financial Admin**: dev-login → `/financial/queue` → `/financial/lock` → `/financial/reports` (filter form + date validation) → `/financial/export` → `/audit` (search form) → auth guard on `/admin/teams` → a11y checks
- **Administrator**: dev-login → `/admin/teams` → team detail (edit form + required-name validation) → `/admin/projects` → project detail → auth guards on `/approvals` and `/financial/queue` → a11y check
- **System Admin**: dev-login → `/sysadmin/integrations` → `/audit` → audit date-range validation → auth guards → a11y check

Each journey is one spec file in `tests/e2e/tests/`.

---

## 5. Test data strategy

- **Seed users**: 5 seeded identities via backend `SeedData` (one per role). Details in `tests/e2e/.env.example`.
- **Dev auth bypass**: `/auth/dev-login?role=<roleName>` endpoint (dev-only, registered only when `ASPNETCORE_ENVIRONMENT=Development`). Sets ASP.NET Core auth cookie for the matching seed user. **This endpoint must be built by the backend team before E2E journey tests can run.**
- **Per-test state**: Tests are designed to work against fresh seed state. Tests that require non-trivial pre-existing data (e.g. submitted timesheets) use `test.skip()` conditionally when seed data is absent rather than hard-failing.
- **Cleanup**: No test cleanup required in this iteration. Tests are read-focused; writes that happen (e.g. team edits in admin journey) use seed data that can be restored by re-running seed.
- **No production data**: Tests target dev URL only. `.env.example` defaults to localhost.

---

## 6. Non-functional spot checks

- **§5.1 Performance**: Reports page "Generate report" roundtrip asserted within 30s (E2E `networkidle` wait with timeout). Formal load testing deferred.
- **§5.4 Auth**: Every E2E spec includes at least one assertion that a role cannot access an out-of-scope route (redirected to 403).
- **§5.7 Accessibility**: `axe-playwright` wired in every persona journey spec. Zero serious/critical violations required to pass.

---

## 7. Manual / accessibility checklist

- [ ] Keyboard-only navigation: Team Member complete journey (timesheet → add entry → submit)
- [ ] Keyboard-only navigation: Team Lead approve + reject with comment
- [ ] Keyboard-only navigation: Financial Admin generate report + export CSV
- [ ] Screen reader (NVDA/JAWS + Chrome): page title announces on navigate
- [ ] Screen reader: form validation error messages announced (aria-live assertive)
- [ ] Screen reader: ConfirmDialog focus trap and restore-on-close
- [ ] Screen reader: StatusChip colour-blind text label (not colour-only)
- [ ] Focus visible: `:focus-visible` outline on all interactive elements
- [ ] Contrast ≥ 4.5:1 on body text; ≥ 3:1 on large text (axe checks; manual spot-check red/amber chip)
- [ ] Skip-to-content link: Tab on page load → first focusable is "Skip to content"
- [ ] Modal: `aria-modal=true`, `aria-labelledby`/`aria-describedby` wired; Tab stays within dialog
- [ ] Tables: `scope="col"` on all `<th>` elements
- [ ] Date inputs: InputDate renders date picker accessible in Chrome/Edge (verify via keyboard)
- [ ] Mobile viewport 320px: nav hides correctly; page content scrollable

---

## 8. Out of automated scope

- Performance load testing (Locust / k6) — deferred; NFR §5.1 targets (200 RPS sustained) require a dedicated load environment
- Security penetration test — covered by phase 8 security review (separate agent)
- Firefox and Safari mobile — deferred; spec §5.8 browser matrix covers Chrome/Edge/Firefox/Safari 17+; add WebKit cross-browser after Chromium baseline passes
- bUnit component tests — deferred pending bUnit package setup in solution
- Visual regression testing — not in scope for this project
- Entra SSO full-flow automation — Playwright cannot automate Entra's SSO UI with real tenants; dev bypass is the approved approach

---

## 9. Open questions

1. **Dev auth bypass endpoint** — F1.1–F6.1 E2E journey tests cannot run without `/auth/dev-login`. Backend team must implement before QA pipeline runs.
2. **Seed data with submitted/approved timesheets** — Team Lead and Financial Admin journey tests degrade to empty-state assertions without pre-seeded workflow data. Backend team should add a `/test/seed-workflow` endpoint (dev-only) or extend `SeedData` with a complete end-to-end data fixture.
3. **F5.2 AC4 (1-year query in 5s)** — Requires production-volume data. Manual NFR spot check deferred to staging environment with synthetic data.
4. **F5.2 AC5 (7-year retention)** — Verified at infrastructure level (Azure SQL backup policy + Log Analytics retention); cannot be automated as a unit test.
5. **F3.2 AC4 (file retained 24h)** — Not yet implementable; JS interop download is a stub. Will need iteration 2 frontend + signed Azure Blob URL or stream approach.
6. **F1.1 / F1.2 E2E** — Full AC coverage requires `TimeEntryFormModal` and `SubmissionReviewModal` (frontend iteration 2).
