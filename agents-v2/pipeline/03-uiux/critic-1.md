# UI/UX Critic — Iteration 1

## Feature coverage
| Feature | Screen / route | Status |
|---|---|---|
| F1.1 Daily Time Entry Logging | `/timesheet/{weekStart}` + Add/Edit Entry Modal; duplicate-entry guard noted as no-UI in §1 | PASS |
| F1.2 Pre-Submission Entry Validation | `SubmissionReviewModal` described in §2 journey and component inventory (§4) | PASS |
| F1.3 Weekly Timesheet Submission | `/timesheet/{weekStart}` submit button + `SubmissionReviewModal`; status chip update shown | PASS |
| F2.1 Team Lead Review Queue | `/approvals` | PASS |
| F2.2 Team Lead Approve / Reject | `/approvals/{timesheetId}` | PASS |
| F2.3 Overtime & After-Hours Validation | No-UI note in §1 (computed server-side; surfaced in approval detail screen); flagged-entry section present in `/approvals/{timesheetId}` | PASS (see Required fix #1 — validation rule mismatch) |
| F2.4 Financial Admin Extra Approval | `/financial/queue` + `/financial/queue/{timesheetId}` | PASS |
| F3.1 Final Approval and Immutable Lock | `/financial/lock` | PASS |
| F3.2 Export Locked Timesheet Data | `/financial/export` | PASS |
| F4.1 User and Team Management | `/admin/teams` + `/admin/teams/{teamId}` | PASS |
| F4.2 Project Creation and Assignment | `/admin/projects` + `/admin/projects/{projectId}` | PASS |
| F4.3 Scoped Admin Visibility | No-UI note in §1 (service-layer enforcement; manifests as filtered lists and HTTP 403) | PASS |
| F5.1 Reconciliation Reports | `/financial/reports` | PASS |
| F5.2 Comprehensive Audit Trail | `/audit` | PASS |
| F6.1 Ticket Reference Validation | `/sysadmin/integrations` + `TicketRefValidator` field within entry modal | PASS |

All 15 features accounted for. No invented features detected.

---

## Persona journeys
- Team Member: PASS — Full journey in §2: login → `/timesheet` → add entry → submit via `SubmissionReviewModal` → status chip updates to Submitted.
- Team Lead: PASS — Full journey in §2: login → `/approvals` → drill to `/approvals/{timesheetId}` → validate overtime → approve or reject.
- Administrator: PASS — Full journey in §2: login → `/timesheet` → Teams → Projects, covering create/edit/member assignment.
- Financial Admin: PASS — Full journey in §2: login → `/financial/queue` → approve/reject → `/financial/lock` → `/financial/reports` → `/financial/export`.
- System Admin: PASS — Full journey in §2: login → `/sysadmin/integrations` → health status → pending/failed list → approve/retry actions.

---

## State coverage audit
| Screen | Empty | Loading | Error |
|---|---|---|---|
| `/timesheet/{weekStart}` | yes | yes | yes |
| Add/Edit Entry Modal (edit pre-fill) | N/A | no | no |
| `/approvals` | yes | yes | yes |
| `/approvals/{timesheetId}` | yes | yes | yes |
| `/financial/queue` | yes | yes | yes |
| `/financial/queue/{timesheetId}` | **no** | yes | yes |
| `/financial/lock` | yes | yes | yes |
| `/financial/reports` (results area) | yes | yes | yes |
| `/financial/export` (preview area) | yes | yes | yes |
| `/admin/teams` | yes | yes | yes |
| `/admin/teams/{teamId}` (member list) | **no** | yes | yes |
| `/admin/projects` | yes | yes | yes |
| `/admin/projects/{projectId}` (user list) | **no** | yes | yes |
| `/audit` | yes | yes | yes |
| `/sysadmin/integrations` | yes (validations section) | yes | yes |

**Three screens are missing an empty state** — this is a blocking gap per checklist item 3. Details in Required fixes #2, #3, #4.

The Add/Edit Entry Modal edit path (loading pre-filled data for an existing entry) also lacks a defined loading or error state for the case where the entry fetch fails before modal population. This is a minor gap but noted as non-blocking since the modal opens within an already-loaded page circuit.

---

## Accessibility audit
- Labels: PASS — §7 commits to `aria-label` with row context on all approval table action buttons (e.g. `aria-label="Approve timesheet for Alice Johnson, week 19–25 May"`); nav items carry `aria-current="page"`.
- Focus visible: PASS — §7 references WCAG 2.1 AA baseline (which requires `:focus-visible` styling); project-specific additions cover all modal trigger/close cycles.
- Keyboard nav: PASS — §7 references WCAG AA baseline; approval, lock, and form flows are all keyboard-operable by design; Escape key closes modals.
- Colour independence: PASS — §7 explicitly states `StatusChip` "must not rely on colour alone; include a text label and optionally an icon with `aria-hidden='true'"`.
- ARIA live (async): PASS — `TicketRefValidator` status updates explicitly mandated to use `aria-live="polite"` in §7; `ValidationSummary` component carries `aria-live` per component inventory in §4.
- Modal focus trap: PASS — §7 explicitly requires `<dialog>` element with `aria-modal="true"`, focus trapped on open, focus returns to trigger on close, Escape closes, for `TimeEntryFormModal`, `SubmissionReviewModal`, and `ConfirmDialog`.

---

## Token consistency
- Hardcoded colours outside tokens: none detected. All colour references in §5 use `var(--color-*)` aliases that resolve through the design-system baseline tokens. No hex values appear in screen specs or component definitions.

---

## Required fixes (BLOCKED)

### Fix 1 — Overtime justification minimum character count contradicts spec (Blocking — §Check 6 Validation rules)
**Location:** §3, `/approvals/{timesheetId}`, Validation section.  
**Problem:** The design specifies justification is "required to unlock 'Validate ✓'; min 1 char". Spec F2.3 acceptance criterion states: "Validating a flagged entry requires a justification note of **at least 10 characters**; the validate button is disabled until the minimum is met." The design sets min 1, the spec requires min 10.  
**Fix:** Update the Validation entry in `/approvals/{timesheetId}` to read "**Justification (per flagged entry)**: required; min **10** chars; 'Validate ✓' button disabled until met."

### Fix 2 — `/financial/queue/{timesheetId}` missing empty state (Blocking — §Check 3 State coverage)
**Location:** §3, `/financial/queue/{timesheetId}` screen spec.  
**Problem:** No empty state is defined for the case where the timesheet detail loads successfully but contains zero entries (edge case: a timesheet that had all entries deleted before reaching FinancialApproved, or a data anomaly). Every data-loading screen must specify loading / empty / error per checklist.  
**Fix:** Add an EMPTY STATE block: `| No entries in this timesheet` (can mirror the wording from `/approvals/{timesheetId}`).

### Fix 3 — `/admin/teams/{teamId}` member list missing empty state (Blocking — §Check 3 State coverage)
**Location:** §3, `/admin/teams/{teamId}` screen spec.  
**Problem:** The member list section (Members table) has no empty state defined for a newly created team with zero members. The screen spec shows a loading skeleton but no "No members yet — click '+ Add members' to get started" placeholder.  
**Fix:** Add an EMPTY STATE for the member list section within the team detail screen (e.g. `| No members yet | [+ Add members]`).

### Fix 4 — `/admin/projects/{projectId}` user list missing empty state (Blocking — §Check 3 State coverage)
**Location:** §3, `/admin/projects/{projectId}` screen spec.  
**Problem:** The assigned users list has no empty state defined for a newly created project with zero assigned users. Same issue as Fix 3.  
**Fix:** Add an EMPTY STATE for the user list section within the project detail screen (e.g. `| No users assigned | [+ Add users]`).

### Fix 5 — Rejection comment not surfaced in Team Member's weekly view (Blocking — §Check 1 Feature coverage / F2.2)
**Location:** §3, `/timesheet/{weekStart}` screen spec; §2 Team Member journey; F2.2 acceptance criterion.  
**Problem:** Spec F2.2 AC states: "The Team Member **can see the rejection comment** in their weekly view." Neither the `/timesheet/{weekStart}` wireframe nor the `WeeklyTimesheetDto` includes a rejection comment field. The Team Member journey in §2 does not mention this state.  
**Fix:** (a) Add `RejectionComment?: string` to `WeeklyTimesheetDto`. (b) Show a rejection comment banner in the `/timesheet/{weekStart}` wireframe when `Status = Draft` and `RejectionComment` is set (e.g. `⚠ Rejected: "{comment}" — please update your entries and resubmit`). (c) Add a microcopy key `timesheet.rejection.banner` for this message. (d) Add the rejected-timesheet state to the Team Member journey in §2.

### Fix 6 — Submission confirmation message missing from microcopy (Blocking — §Check 10 Microcopy keys)
**Location:** §8 microcopy table; F1.3 acceptance criterion.  
**Problem:** Spec F1.3 AC requires an on-screen confirmation: `"Timesheet for week of {WeekStart:dd MMM yyyy} submitted for approval"`. No microcopy key for this confirmation toast or inline message exists in §8.  
**Fix:** Add microcopy key `timesheet.submit.success` with copy `"Timesheet for week of {WeekStart} submitted for approval"` to §8.

---

## Notes (non-blocking)

1. **Add/Edit Entry Modal — edit-path loading/error state:** When the modal is opened in edit mode (pre-filled from a `TimeEntryDto`), there is no defined loading skeleton or error state for the fetch of the existing entry. In practice the data is already loaded in the parent page, so this is unlikely to fail independently, but a defensive error state (e.g. inline `ErrorBanner` within the modal) would improve resilience. No impact on blocking verdict.

2. **`/financial/reports` — CSV export scope:** The reports screen shows an `[Export CSV]` button in the results area. F5.1 AC states "All three report types can be exported to CSV from the same page" — this is satisfied. However, the relationship between `/financial/reports`'s "Export CSV" and the `/financial/export` page is not explicitly disambiguated in the design. Both allow CSV export of locked data but `/financial/reports` is period-specific while `/financial/export` supports arbitrary date ranges. A brief clarifying note in the screen spec would help developers avoid building duplicate or conflicting export paths.

3. **`/admin/teams/{teamId}` — no "Create team" inline flow:** The `[+ Create team]` button on `/admin/teams` is not wired to a defined modal or sub-screen in §3. The site map shows `/admin/teams/{teamId}` as the edit screen and implies creation uses the same form. A note clarifying that `[+ Create team]` opens the same `TeamForm` component in create mode (empty initial values) would remove implementation ambiguity.

4. **`/admin/projects/{projectId}` — `[+ Create project]` same ambiguity:** Same gap as note 3, for project creation.

5. **Microcopy gap — `/financial/queue/{timesheetId}` financial approval actions:** The approval and rejection action for the financial admin step (distinct from lead approval) lacks dedicated microcopy keys. The current table reuses generic `common.cancel` / `common.save` keys but the approval/reject buttons on the financial detail screen would benefit from `financial.approve.button` and `financial.reject.button` keys to allow copy differentiation in future (e.g. "Financial approve" vs just "Approve").

6. **F2.4 rejection comment visibility to submitter:** Spec F2.4 AC states "the rejection comment is visible to the submitter" for Financial Admin rejections too. Fix 5 above covers the Team Lead rejection path; the same `RejectionComment` field and banner pattern should be confirmed to also cover Financial Admin rejections (i.e. the `WeeklyTimesheetDto` `RejectionComment` field and banner logic should surface whichever was the most recent rejection, from either approval stage). This should be validated in the next iteration.

7. **SubmissionReviewModal — leave/holiday day distinction:** The journey in §2 and open question OQ-012 acknowledge this is unresolved. The `SubmissionReviewModal` references "missing days" vs "leave/holiday days" without defining how the leave indicator is set in the weekly view. OQ-9 in §9 mirrors this. This is expected to remain open until the Data Designer and BA resolve OQ-007/OQ-012; no UI change needed now but worth flagging as a dependency.

8. **Dark mode:** Deferred per §5 — consistent with spec having no mention. Correct.

VERDICT: BLOCKED
