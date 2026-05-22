# Backend Critic — Iteration 2

## Gate results
- Build: PASS (0 errors, 0 warnings — `dotnet build TimeQuest.sln`)
- Tests: PASS (22 passed / 0 failed / 0 skipped — `dotnet test TimeQuest.sln`)
- Security gate: PASS

---

## Fix verification

1. **Scope guard on LeadApproveAsync/LeadRejectAsync: VERIFIED**
   - `IsLeadForTimesheetUserAsync` helper added at line 36–45 of `ApprovalService.cs`. Both `LeadApproveAsync` (line 143) and `LeadRejectAsync` (line 200) call it immediately after the null-check and return `Result.Failure("Access denied.")` when the lead is not in the timesheet user's team. Satisfies F2.2 AC5.

2. **Duplicate AuditService registration: REMOVED**
   - `Program.cs` contains exactly one `AuditService` registration: `builder.Services.AddScoped<IAuditService, AuditService>();` (line 28). The concrete `AddScoped<AuditService>()` registration is gone.

3. **actorDisplayName resolution: FIXED**
   - Grep for `actorDisplayName:.*\.ToString()` across `src/` returns 0 matches. Every service (`ApprovalService`, `TimeEntryService`) now carries a `GetDisplayNameAsync` private helper that queries `_db.Users.Select(u => u.DisplayName)` and passes the resolved name to `_audit.LogAsync`.

4. **Notes validation at service layer: VERIFIED**
   - `TimeEntryService.CreateEntryAsync` lines 43–44: `if (string.IsNullOrWhiteSpace(request.Notes)) return Result<TimeEntryDto>.Failure("Notes are required.");` — present at the top of the method before any DB access.

5. **ValidateOvertimeAsync removed from ApprovalService: VERIFIED**
   - `ApprovalService.cs` contains no `ValidateOvertimeAsync` method. The single authoritative implementation remains in `TimeEntryService.ValidateOvertimeAsync` (lines 191–220).

---

## F5.2 audit read path check

- **Finding:** `AuditService.cs` implements only `LogAsync` (the write path). There is no `SearchAsync` or any other read method on `AuditService` or `IAuditService`. The corresponding Blazor `AuditLogPage` (T5.2.c) is also absent. The write path (T5.2.a and T5.2.b) is fully implemented and wired to all 11 mutating service methods. The `AuditSearchRequest` and `AuditLogEntryDto` DTOs exist in `TimeQuest.Shared`, indicating the contracts are prepared for the read path.
- **Explicit deferral in summary.md:** "F5.2 audit search read path (AuditService.SearchAsync) not yet implemented — Frontend iteration will need it."
- **Blocking:** no — the write path (the security-critical half) is complete and tested indirectly via `TestAuditService`. The read path is explicitly deferred with a clear reason tied to a later pipeline phase (Frontend). The DTOs are pre-defined. This is a known gap with explicit deferral, not an unacknowledged omission.

---

## Notes (non-blocking, carried from iteration 1)

- **F4.3 FinancialAdmin `ScopeGuard` exemption** — `ScopeGuard.CanAccessProjectAsync` and `CanAccessUserAsync` apply no role check; a Financial Admin is over-restricted rather than under-restricted. Not a security vulnerability but diverges from F4.3 AC4. Fix before Frontend iteration.
- **Concrete-type DI registrations** — `TimeEntryService`, `WeeklyTimesheetService`, `ApprovalService`, etc. are all registered as concrete types. Extracting `ITimeEntryService`, `IWeeklyTimesheetService`, `IApprovalService` interfaces before the Frontend agent starts will be needed for Blazor component mock injection.
- **F2.4 dedicated unit tests absent** — `FinancialApproveAsync` / `FinancialRejectAsync` have no direct unit tests; the only coverage is incidental via the lock-path test. Add before the QA phase to reach the §5.10 80% service-layer coverage target.
- **Audit write and business transaction in separate `SaveChangesAsync` calls** — an audit record can be persisted even if the calling service's own save rolls back. Wrap in a single unit-of-work or `IDbContextTransaction` in a future iteration.
- **`TicketValidationService` adapters are stubs** — acceptable for this iteration per OQ-009; must be flagged for the integration phase.

VERDICT: APPROVED
