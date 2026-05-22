# Data Critic — Iteration 2

## Fix verification

- **TimesheetDayFlag addition:** Entity is present in §2 (C# class definition), ER diagram (§1), index table (§3), FK delete-behaviour table and cascade-cycle table (§4), and changelog (§9). The composite PK `(WeeklyTimesheetId, Date)` is correctly defined. The `DayFlagType` enum is declared in the enum block at the top of §2 with values `Leave = 0, PublicHoliday = 1`. The denorm `UserId` FK is present on the entity and explained in the comment ("denorm for direct (UserId, Date) query"). The navigation collections on `WeeklyTimesheet` (`DayFlags`) and `ApplicationUser` (`AuditEvents` nav already existed; no new nav needed on User for this FK as it is denorm-only) are consistent. The index `IX_TimesheetDayFlag_WeeklyTimesheetId` is listed with columns `(WeeklyTimesheetId, Date)` in the index table with note "Composite PK index; covers `ValidateForSubmission` lookup of all flagged days for a week". The fix is complete and correctly wired.

- **F1.2 AC2 now implementable: YES.** `WeeklyTimesheetService.ValidateForSubmission` (T1.2.a) can now query `DbContext.TimesheetDayFlags.Where(f => f.WeeklyTimesheetId == id)` to obtain all flagged days for the week. Any weekday in `[WeekStart, WeekEnd]` with zero `TimeEntry` rows is considered a gap; if a `TimesheetDayFlag` row exists for that date (regardless of `FlagType`), the gap is excused and no warning is emitted. The composite PK additionally prevents duplicate flags per day per timesheet at the database level, which prevents double-counting. All parts of AC2 are now served.

---

## Feature coverage walk

All features that passed in iteration 1 are unchanged. Only F1.2 is re-evaluated.

- **F1.1** — PASS (unchanged)
- **F1.2** — PASS. AC2 unblocked by `TimesheetDayFlag`. AC1 (missing-days warning with acknowledgement checkbox), AC3 (flagged-entry warning), and AC4 (no-entry block) are all served by existing entities and service layer design. Full F1.2 now passes.
- **F1.3** — PASS (unchanged)
- **F2.1** — PASS (unchanged)
- **F2.2** — PASS (unchanged)
- **F2.3** — PASS (unchanged)
- **F2.4** — PASS (unchanged)
- **F3.1** — PASS (unchanged)
- **F3.2** — PASS (unchanged)
- **F4.1** — PASS (unchanged)
- **F4.2** — PASS (unchanged)
- **F4.3** — PASS (unchanged)
- **F5.1** — PASS (unchanged)
- **F5.2** — PASS (unchanged)
- **F6.1** — PASS (unchanged)

---

## Cascade cycle re-check (new table)

- **TimesheetDayFlag: `WeeklyTimesheetId → WeeklyTimesheet` (Cascade), `UserId → ApplicationUser` (NoAction)** — safe. The two FKs target different tables (`WeeklyTimesheet` and `ApplicationUser`), so no multiple-cascade-path problem exists. Only one cascade is present and it goes to a single target. SQL Server's cycle-detection rule is not triggered. The design's own cascade-cycle table confirms this (`0 cascades to same target` — ✅).

  Additionally, the cascade direction is appropriate: `TimesheetDayFlag` rows are part of the timesheet and should be removed when a (Draft) timesheet is deleted. Setting `Cascade` here is semantically correct and bounded — a timesheet can only be hard-deleted while in `Draft` status (locked timesheets are permanently blocked from deletion by the service layer), so the cascade will never touch billing-protected data.

---

## Remaining checks

- **Money columns:** PASS (unchanged — no monetary columns; `Hours` is `decimal(5,2)`)
- **PII/sensitive fields:** PASS (unchanged — passwords Identity-managed; secrets in Key Vault; Notes/TicketRef flagged Confidential)
- **N+1 hazards:** PASS (unchanged — all query patterns use DTO projection; new `TimesheetDayFlag` is queried as a simple `.Where(f => f.WeeklyTimesheetId == id).ToListAsync()` — single round-trip)
- **Naming consistency (new entity):**
  - Class name `TimesheetDayFlag` — PascalCase, singular — PASS
  - Composite PK columns `WeeklyTimesheetId` and `Date` — both follow `<Entity>Id` / plain-noun convention — PASS
  - Enum `DayFlagType` (PascalCase, defined in enum block) — PASS
  - Property `FlagType` on the entity (stores a `DayFlagType`) — consistent with the pattern of dropping the repetitive type-noun suffix (e.g. `BillingType`, `Status`) — PASS
  - Index name `IX_TimesheetDayFlag_WeeklyTimesheetId` — the index physically covers `(WeeklyTimesheetId, Date)` as the composite PK, but the name only references `WeeklyTimesheetId`. This is a non-blocking cosmetic issue; the index serves its documented purpose regardless of the name.
  - Navigation property `DayFlags` on `WeeklyTimesheet` (plural collection) — consistent with other collection navs — PASS

---

## Notes (non-blocking)

- All non-blocking notes from critic-1.md remain open and are not repeated here. Specifically: `IX_TimeEntry_ProjectId_Date` gap, `TotalHours`/`FlaggedCount` compute-vs-denorm (OQ-1), `ApprovalRecord.TimesheetId` naming nit, `LastRejectionComment` width, `Team.TeamLeadId` vs `UserTeam.Role=Lead` dual representation, `AuditEvent` SQL DENY principal name, and forward-only migration runbook note.

- **`IX_TimesheetDayFlag_WeeklyTimesheetId` index name vs coverage:** The index name implies only the first column, but the columns listed in the index table are `(WeeklyTimesheetId, Date)`. Since these two columns are also the composite PK, the index is automatically created by EF Core / SQL Server as the clustered or unique constraint index — an explicit `HasIndex` call may be redundant. The Backend Dev should confirm whether this index requires an explicit `modelBuilder` call or whether the `HasKey(f => new { f.WeeklyTimesheetId, f.Date })` fluent configuration already creates it. Non-blocking either way.

- **`ApplicationUser` navigation for `TimesheetDayFlag`:** The `ApplicationUser` class definition in §2 does not include a `ICollection<TimesheetDayFlag>` navigation property. This is intentional (the FK is denorm-only and bi-directional navigation from `ApplicationUser` to `TimesheetDayFlag` is not needed). The Backend Dev should ensure the EF fluent config uses `.HasOne(f => f.User).WithMany()` (no inverse nav) to avoid EF trying to wire up a missing collection. Non-blocking — but worth an explicit note in the model-builder configuration.

VERDICT: APPROVED
