# Backend Critic — Iteration 1

## Gate results
- Build: PASS (0 errors, 0 warnings)
- Tests: PASS (22 passed / 0 failed / 0 skipped)
- Security gate: FAIL

---

## Spec coverage

| Feature | Service method | Tests | Status |
|---|---|---|---|
| F1.1 Daily Time Entry | `TimeEntryService.CreateEntryAsync` / `UpdateEntryAsync` / `DeleteEntryAsync` | 8 tests in TimeEntryServiceTests | PASS |
| F1.2 Pre-Submission Validation | `WeeklyTimesheetService.ValidateForSubmissionAsync` / `SetDayFlagAsync` | 4 tests in WeeklyTimesheetServiceTests | PASS |
| F1.3 Submission | `WeeklyTimesheetService.SubmitAsync` / `GetOrCreateForWeekAsync` | 2 tests in WeeklyTimesheetServiceTests | PASS |
| F2.1 Lead Queue | `ApprovalService.GetLeadQueueAsync` / `GetTimesheetDetailAsync` | None (deferred to integration) | PASS |
| F2.2 Lead Approve/Reject | `ApprovalService.LeadApproveAsync` / `LeadRejectAsync` | 5 tests in ApprovalServiceTests | PASS* |
| F2.3 Overtime Validation | `TimeEntryService.ValidateOvertimeAsync` / `ApprovalService.ValidateOvertimeAsync` | 2 tests in ApprovalServiceTests | PASS* |
| F2.4 Financial Approval | `ApprovalService.FinancialApproveAsync` / `FinancialRejectAsync` | Lock path only (no dedicated FA tests) | GAP |
| F3.1 Final Lock | `ApprovalService.LockTimesheetAsync` | 2 tests in ApprovalServiceTests | PASS |
| F3.2 Export | `ExportService.ExportLockedEntriesAsync` | None (deferred to integration) | PASS |
| F4.1 Team Management | `AdminService.CreateTeamAsync` / `AssignTeamLeadAsync` / `AddTeamMemberAsync` / `RemoveTeamMemberAsync` | None (deferred to integration) | PASS |
| F4.2 Project Management | `AdminService.CreateProjectAsync` / `AssignUserToProjectAsync` / `DeactivateProjectAsync` | None (deferred to integration) | PASS |
| F4.3 Scope Guard | `ScopeGuard.CanAccessProjectAsync` / `CanAccessUserAsync` | None (deferred to integration) | PASS* |
| F5.1 Reports | `ReportService.GetReconciliationReportAsync` | None (deferred to integration) | PASS |
| F5.2 Audit Trail | `AuditService.LogAsync` wired to all mutating methods | Indirect via TestAuditService | PASS |
| F6.1 Ticket Validation | `TicketValidationService.ValidatePendingTicketsAsync` / `ApproveManuallyAsync` | 1 test (Pending status) | PASS |

*See security/code-quality findings below for caveats.

---

## Security findings

1. **Missing team-scope guard on `LeadApproveAsync` and `LeadRejectAsync`** — `ApprovalService.GetLeadQueueAsync` correctly filters by team membership, but `LeadApproveAsync` and `LeadRejectAsync` perform no scope check; a Team Lead who knows a `timesheetId` can approve or reject a timesheet belonging to a user outside their team. Spec F2.2 AC5 explicitly requires HTTP 403 for out-of-scope approve/reject attempts.

2. **Double DI registration of `AuditService` in `Program.cs`** — both `AddScoped<AuditService>()` (concrete) and `AddScoped<IAuditService, AuditService>()` are registered (lines 28–29). The concrete registration is unused by callers (all callers depend on `IAuditService`) and creates a second independent scoped instance, potentially opening a path to audit writes on a context that is out of step with the business transaction. Only the interface registration should remain.

3. **`actorDisplayName` always receives `userId.ToString()` instead of the user's actual `DisplayName`** — every service call to `_audit.LogAsync` passes `actorDisplayName: userId.ToString()`. Spec §5.5 requires the actor's display name to be persisted alongside their ID for tamper-evident records that survive user deletion. The current approach defeats the denormalisation intent of the `ActorDisplayName` column.

4. **`ValidateOvertimeAsync` duplicated across two services** — `TimeEntryService.ValidateOvertimeAsync` (line 182) and `ApprovalService.ValidateOvertimeAsync` (line 215) are near-identical. The spec assigns this responsibility to `TimeEntryService` (T2.3.a). Two independent code paths that mutate the same fields create an inconsistency risk and make it harder to add a scope check later.

5. **F4.3 FinancialAdmin scope-bypass not implemented in `ScopeGuard`** — spec F4.3 AC4 states "A Financial Admin is exempt from scope restrictions." `ScopeGuard.CanAccessProjectAsync` and `CanAccessUserAsync` have no role check; a Financial Admin calling those methods would be blocked the same as a regular Admin. This is a behaviour gap but not exploitable (it only over-restricts, not under-restricts), so classified non-blocking.

6. **`CreateTimeEntryRequest.Notes` validated by `[Required]` in DTO but not at service layer** — `TimeEntryService.CreateEntryAsync` never checks `string.IsNullOrWhiteSpace(request.Notes)`. If the service is called directly (e.g. in tests or future API layer) without model-binding validation, an empty Notes string passes through. Spec F1.1 AC2 requires the "Notes are required" message to be returned as a service-layer guard.

---

## Code quality findings

- `ApprovalService` does not inject `IScopeGuard`; team-scope enforcement is manually reimplemented inline in `GetLeadQueueAsync` using raw LINQ rather than delegating to `ScopeGuard`, inconsistent with the rest of the architecture.
- All services are registered as concrete types (`AddScoped<TimeEntryService>()`) rather than behind interfaces. While not a runtime defect today, it makes the services un-mockable without extracting interfaces, which will be needed for the Frontend agent and QA agent.
- `ApprovalService.ValidateOvertimeAsync` is structurally identical to `TimeEntryService.ValidateOvertimeAsync`; one of them must be removed to avoid split-ownership bugs.
- F2.4 (`FinancialApproveAsync` / `FinancialRejectAsync`) has no dedicated unit tests; the only incidental coverage is the lock-path test which seeds `FinancialApproved` status. A direct approve/reject test is needed to reach the 80% service-layer coverage target (§5.10).

---

## Required fixes (if BLOCKED)

1. **Add team-scope guard to `LeadApproveAsync` and `LeadRejectAsync`** — after loading the timesheet, verify that `timesheet.UserId` belongs to at least one team where `leadUserId` holds `UserTeamRole.Lead`. Return `Result.Failure("Access denied.")` (to be mapped to HTTP 403 by the caller) if not. This satisfies F2.2 AC5 and closes the security gate.

2. **Remove duplicate concrete DI registration of `AuditService`** — delete `builder.Services.AddScoped<AuditService>();` (line 28 of `Program.cs`); keep only `builder.Services.AddScoped<IAuditService, AuditService>();`.

3. **Pass real `actorDisplayName` to `AuditService.LogAsync`** — each service method must look up the actor's `DisplayName` from the database (or accept it as a parameter from the caller) before calling `_audit.LogAsync`, instead of passing `userId.ToString()`.

4. **Enforce Notes validation at service layer** — add `if (string.IsNullOrWhiteSpace(request.Notes)) return Result<TimeEntryDto>.Failure("Notes are required.");` at the top of `TimeEntryService.CreateEntryAsync`, matching F1.1 AC2.

5. **Remove `ApprovalService.ValidateOvertimeAsync`** — delete the duplicate method; all callers must use `TimeEntryService.ValidateOvertimeAsync` exclusively. Update any tests or references.

---

## Notes (non-blocking)

- F4.3 FinancialAdmin exemption in `ScopeGuard` is unimplemented (over-restricts rather than under-restricts); this is a spec gap but not a security vulnerability. Fix before Frontend iteration.
- All services are registered as concrete types; recommend extracting `ITimeEntryService`, `IWeeklyTimesheetService`, and `IApprovalService` interfaces before the Frontend agent starts, to enable Blazor component testing with mocks.
- F2.4 `FinancialApproveAsync` / `FinancialRejectAsync` unit tests are absent; these should be added to reach the §5.10 80% coverage floor.
- `AuditService.LogAsync` saves its own `SaveChangesAsync` call independently of the calling service's transaction, meaning an audit record could be written even if the business operation later rolls back. Consider wrapping both in a single unit-of-work or using `IDbContextTransaction` in a future iteration.
- `TicketValidationService` adapter methods are stubs returning a random result (per OQ-009); this is acceptable for this iteration but must be flagged for the integration phase.

VERDICT: BLOCKED
